using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostCity.Characters
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent]
    public sealed class ProtagonistVisualMotion : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D movementBody;
        [SerializeField, Min(.5f)] private float cycleDistance = 1.25f;
        [SerializeField, Min(.05f)] private float blendSeconds = .18f;
        [SerializeField, Min(.1f)] private float idleSeconds = 3.2f;
        [SerializeField, Min(.1f)] private float pickupSeconds = 1.2f;
        [SerializeField, Range(0f, 45f)] private float headYawLimit = 25f;
        [SerializeField, Range(0f, 25f)] private float headPitchLimit = 12f;
        [SerializeField] private bool armedPose;
        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private readonly Dictionary<Transform, Vector3> restPositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Quaternion> restRotations = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<string, Quaternion> canonicalRotations = new Dictionary<string, Quaternion>();
        private readonly Dictionary<string, Vector3> canonicalPositions = new Dictionary<string, Vector3>();
        private Transform attentionTarget;
        private UnityEngine.Object pickupLifetime;
        private Vector3 pickupPoint, lastFacing, moveDirection = Vector3.forward;
        private float cycle, idleTime, blend, yawOffset, turnCycle, pickupTime = -1f, lookYaw, lookPitch;
        private bool initialized, tracksPickupLifetime;
        private Rigidbody2D observedBody;
        private Vector2 previousBodyPosition, observedVelocity;
        private float recoveryTime;
        private readonly Dictionary<Transform, Vector3> recoveryPositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Quaternion> recoveryRotations = new Dictionary<Transform, Quaternion>();
        private Quaternion poseBasis;
        public bool IsReady { get; private set; }
        public bool IsPickingUp => pickupTime >= 0f;
        public float PickupProgress => IsPickingUp ? Mathf.Clamp01(pickupTime / pickupSeconds) : 0f;
        public bool PickupContact => IsPickingUp && PickupProgress >= .5f / 1.2f;
        public Transform LeftHand => Bone("LeftHand");
        public float LocomotionBlend => blend;
        public Vector3 VisualGripPoint => LeftHand != null ? LeftHand.position - poseBasis * PickupWristOffset : transform.position;
        public static Vector3 PickupWristOffset => new Vector3(0f, .02f, -.105f);

        private void Awake() => InitializeRig();
        private void OnEnable()
        {
            InitializeRig();
            if (movementBody == null) movementBody = GetComponentInParent<Rigidbody2D>();
            ResetMovementObservation();
            lastFacing = transform.forward;
        }
        private void OnTransformParentChanged()
        {
            movementBody = GetComponentInParent<Rigidbody2D>();
            ResetMovementObservation();
        }
        private void FixedUpdate() => SampleMovement(Time.fixedDeltaTime);
        private void LateUpdate() => UpdateFromMovement(Time.deltaTime);

        private void ResetMovementObservation()
        {
            observedBody = movementBody;
            previousBodyPosition = movementBody != null ? movementBody.position : Vector2.zero;
            observedVelocity = Vector2.zero;
        }
        private void SampleMovement(float fixedDeltaTime)
        {
            if (!Finite(fixedDeltaTime) || fixedDeltaTime <= 0f) return;
            if (movementBody == null) movementBody = GetComponentInParent<Rigidbody2D>();
            if (movementBody == null || observedBody != movementBody)
            {
                ResetMovementObservation();
                return;
            }
            // MovePosition can finish a step with zero velocity. Observe the previous
            // completed physics step, not input intent or an interpolated visual bone.
            Vector2 position = movementBody.position;
            Vector2 velocity = (position - previousBodyPosition) / fixedDeltaTime;
            previousBodyPosition = position;
            observedVelocity = Finite(velocity.x) && Finite(velocity.y) ? velocity : Vector2.zero;
        }
        private void UpdateFromMovement(float deltaTime)
        {
            Vector2 velocity = movementBody != null ? observedVelocity : Vector2.zero;
            Advance(new Vector3(velocity.x, velocity.y, 0f), deltaTime);
        }
        public void SetAttentionTarget(Transform target) => attentionTarget = target;

        public bool TryPresentPickup(Vector3 worldPoint, UnityEngine.Object targetLifetime = null)
        {
            InitializeRig();
            if (!IsReady || IsPickingUp || armedPose || !Finite(worldPoint) || blend > .12f
                || observedVelocity.sqrMagnitude > .01f
                || (movementBody != null && movementBody.velocity.sqrMagnitude > .01f)) return false;
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            // Test a bent-torso reach before starting; bone lengths never change.
            Vector3 shoulder = canonicalPositions["LeftUpperArm"] + Vector3.down * .06f;
            Vector3 spine = canonicalPositions["Spine"] + Vector3.down * .06f;
            shoulder = spine + Quaternion.Euler(20f, 0f, 0f) * (shoulder - spine);
            float length = Vector3.Distance(canonicalPositions["LeftUpperArm"], canonicalPositions["LeftForearm"])
                + Vector3.Distance(canonicalPositions["LeftForearm"], canonicalPositions["LeftHand"]);
            Vector3 wristTarget = local + PickupWristOffset;
            if (local.y < .72f || local.y > 1.14f || local.z < .05f || local.z > .65f
                || Vector3.Distance(shoulder, wristTarget) > length - .012f) return false;
            pickupPoint = worldPoint;
            pickupLifetime = targetLifetime;
            tracksPickupLifetime = targetLifetime != null;
            recoveryTime = 0f;
            pickupTime = 0f;
            return true;
        }
        public void CancelPickup()
        {
            if (IsPickingUp)
            {
                recoveryPositions.Clear();
                recoveryRotations.Clear();
                foreach (Transform bone in bones.Values)
                    if (bone != null) { recoveryPositions[bone] = bone.localPosition; recoveryRotations[bone] = bone.localRotation; }
                recoveryTime = .18f;
            }
            pickupTime = -1f;
            pickupLifetime = null;
            tracksPickupLifetime = false;
        }
        public void Advance(Vector3 worldVelocity, float deltaTime)
        {
            InitializeRig();
            // Pause freezes the current pose, including a partially completed reach.
            if (!IsReady || !Finite(deltaTime) || deltaTime <= 0f) return;
            float dt = Mathf.Min(deltaTime, .1f);
            if (!Finite(worldVelocity)) worldVelocity = Vector3.zero;
            Vector3 planar = Vector3.ProjectOnPlane(worldVelocity, transform.up);
            float speed = Mathf.Min(planar.magnitude, 10f);
            float aimDelta = Vector3.SignedAngle(lastFacing, transform.forward, transform.up);
            lastFacing = transform.forward;
            yawOffset = Mathf.Clamp(Mathf.DeltaAngle(0f, yawOffset - aimDelta), -160f, 160f);
            yawOffset = Mathf.MoveTowards(yawOffset, 0f, dt * 240f);
            float turning = Mathf.Clamp01(Mathf.Abs(yawOffset) / 35f);
            turnCycle = Mathf.Repeat(turnCycle + dt * 2.5f * turning, 1f);
            if (IsPickingUp && (speed > .12f || Mathf.Abs(aimDelta) > 15f
                || (tracksPickupLifetime && pickupLifetime == null))) CancelPickup();
            if (IsPickingUp)
            {
                pickupTime += dt;
                if (pickupTime >= pickupSeconds) CancelPickup();
            }
            blend = Mathf.MoveTowards(blend, speed > .025f ? 1f : 0f, dt / Mathf.Max(.05f, blendSeconds));
            if (speed > .025f)
            {
                cycle = Mathf.Repeat(cycle + speed * dt / Mathf.Max(.5f, cycleDistance), 1f);
                Vector3 local = Quaternion.Inverse(transform.rotation * Quaternion.Euler(0f, yawOffset, 0f)) * planar;
                moveDirection = Vector3.Slerp(moveDirection, local.normalized, 1f - Mathf.Exp(-dt * 16f));
                moveDirection.y = 0f;
                moveDirection.Normalize();
            }
            idleTime += dt;
            Evaluate(cycle, blend, idleTime, yawOffset, turning * (1f - blend), turnCycle,
                IsPickingUp ? pickupTime / pickupSeconds : -1f, pickupPoint, dt);
            if (recoveryTime > 0f)
            {
                recoveryTime = Mathf.Max(0f, recoveryTime - dt);
                float t = Smooth(1f - recoveryTime / .18f);
                foreach (Transform bone in recoveryPositions.Keys)
                    if (bone != null)
                    {
                        bone.localPosition = Vector3.Lerp(recoveryPositions[bone], bone.localPosition, t);
                        bone.localRotation = Quaternion.Slerp(recoveryRotations[bone], bone.localRotation, t);
                    }
            }
        }
        public void InitializeRig()
        {
            if (initialized) return;
            initialized = true;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.StartsWith("Rig_", StringComparison.Ordinal)) continue;
                string key = child.name.Substring(4);
                if (bones.ContainsKey(key)) { IsReady = false; return; }
                bones.Add(key, child);
                restPositions.Add(child, child.localPosition);
                restRotations.Add(child, child.localRotation);
                canonicalPositions.Add(key, transform.InverseTransformPoint(child.position));
                canonicalRotations.Add(key, Quaternion.Inverse(transform.rotation) * child.rotation);
            }
            foreach (string name in RequiredBones)
                if (!bones.ContainsKey(name)) { IsReady = false; return; }
            IsReady = true;
            lastFacing = transform.forward;
        }
        public static readonly string[] RequiredBones = {
            "Root", "Pelvis", "Spine", "Chest", "Neck", "Head", "Bag",
            "LeftUpperArm", "LeftForearm", "LeftHand", "LeftFingers", "LeftThumb",
            "RightUpperArm", "RightForearm", "RightHand", "RightFingers", "RightThumb",
            "LeftThigh", "LeftShin", "LeftFoot", "LeftToe", "RightThigh", "RightShin", "RightFoot", "RightToe"
        };

        private void Evaluate(float phase, float amount, float clock, float bodyYaw, float turn,
            float turnPhase, float reach, Vector3 target, float dt)
        {
            RestoreBones();
            poseBasis = transform.rotation * Quaternion.Euler(0f, bodyYaw, 0f);
            Bone("Root").rotation = poseBasis * canonicalRotations["Root"];
            float gait = Mathf.Cos(phase * Mathf.PI * 2f);
            float breath = Mathf.Sin(clock / Mathf.Max(.1f, idleSeconds) * Mathf.PI * 2f);
            float pickupBlend = reach < 0f ? 0f : Mathf.Min(Smooth(reach / .35f), Smooth((1f - reach) / .22f));
            float bob = -.09f * amount + Mathf.Sin(phase * Mathf.PI * 4f) * .008f * amount;
            Bone("Pelvis").position += poseBasis * Vector3.up * (bob - .06f * pickupBlend);
            Rotate("Spine", Vector3.right, 3f * amount + 20f * pickupBlend);
            Rotate("Chest", Vector3.right, .65f * breath * (1f - pickupBlend));
            Bone("Chest").position += poseBasis * Vector3.up * (.0015f * breath);
            Bone("Neck").position -= poseBasis * Vector3.up * (Mathf.Sin(phase * Mathf.PI * 4f) * .006f * amount);
            for (int side = -1; side <= 1; side += 2)
            {
                string label = side < 0 ? "Left" : "Right";
                FootTrajectory(Mathf.Repeat(phase + (side < 0 ? 0f : .5f), 1f), cycleDistance,
                    out float travel, out float lift, out float roll);
                float step = Mathf.Max(0f, Mathf.Sin((turnPhase + (side < 0 ? 0f : .5f)) * Mathf.PI * 2f));
                Vector3 foot = canonicalPositions[label + "Foot"] + moveDirection * (travel * amount);
                foot.y += lift * amount + step * .055f * turn;
                float pitch = roll * amount;
                foot.y += Mathf.Abs(Mathf.Sin(pitch * Mathf.Deg2Rad)) * (pitch < 0f ? .09f : .205f);
                SolveLimb(label + "Thigh", label + "Shin", label + "Foot", World(foot),
                    poseBasis * (Vector3.forward + moveDirection * .35f));
                Bone(label + "Foot").rotation = poseBasis * Quaternion.Euler(pitch, 0f, 0f) * canonicalRotations[label + "Foot"];
                Rotate(label + "Toe", Vector3.right, -Mathf.Max(0f, pitch) * .7f);
                Vector3 hand = canonicalPositions[label + "Hand"];
                hand.x = side * .40f;
                // Hands descend with the walking pelvis instead of pinning the elbows high.
                hand.y += .015f + bob;
                hand.z += .045f + gait * side * .125f * amount;
                if (armedPose) hand = new Vector3(side * .17f, 1.16f, .28f);
                Vector3 handWorld = World(hand);
                if (side < 0 && pickupBlend > 0f)
                {
                    float seconds = reach * 1.2f;
                    Vector3 contact = target + poseBasis * PickupWristOffset;
                    Vector3 recovered = World(new Vector3(-.19f, 1.10f, .24f));
                    if (seconds < .50f) handWorld = Vector3.Lerp(handWorld, contact, Smooth((seconds - .15f) / .35f));
                    else if (seconds < .65f) handWorld = contact;
                    else if (seconds < .95f) handWorld = Vector3.Lerp(contact, recovered, Smooth((seconds - .65f) / .30f));
                    else handWorld = Vector3.Lerp(recovered, handWorld, Smooth((seconds - .95f) / .25f));
                }
                SolveLimb(label + "UpperArm", label + "Forearm", label + "Hand", handWorld,
                    poseBasis * new Vector3(side * .35f, 0f, -.5f));
                Bone(label + "Hand").rotation = poseBasis * Quaternion.Euler(side < 0 ? -75f * pickupBlend : 0f, 0f, 0f) * canonicalRotations[label + "Hand"];
                Rotate(label + "Fingers", Vector3.right, side < 0 ? -12f * pickupBlend : 0f);
                Rotate(label + "Thumb", Vector3.forward, side < 0 ? 10f * pickupBlend : 0f);
            }
            Vector3 gaze;
            if (pickupBlend > .01f) gaze = (PickupContact ? Bone("LeftHand").position : target) - Bone("Head").position;
            else if (attentionTarget != null && attentionTarget.gameObject.activeInHierarchy) gaze = attentionTarget.position - Bone("Head").position;
            else gaze = transform.forward;
            Vector3 localGaze = Quaternion.Inverse(poseBasis) * gaze;
            float yaw = Mathf.Clamp(Mathf.Atan2(localGaze.x, localGaze.z) * Mathf.Rad2Deg, -headYawLimit, headYawLimit);
            float pitchLook = Mathf.Clamp(-Mathf.Atan2(localGaze.y, new Vector2(localGaze.x, localGaze.z).magnitude) * Mathf.Rad2Deg,
                -headPitchLimit, headPitchLimit);
            float lookBlend = dt < 0f ? 1f : 1f - Mathf.Exp(-dt * 12f);
            lookYaw = Mathf.LerpAngle(lookYaw, yaw, lookBlend);
            lookPitch = Mathf.LerpAngle(lookPitch, pitchLook, lookBlend);
            Rotate("Head", Vector3.up, lookYaw);
            Rotate("Head", Vector3.right, lookPitch);
            Rotate("Bag", Vector3.right, Mathf.Sin((phase - .1f) * Mathf.PI * 2f) * 4f * amount);
        }

        public static void FootTrajectory(float phase, float distance, out float travel, out float lift, out float roll)
        {
            phase = Mathf.Repeat(phase, 1f);
            const float stance = .55f;
            float halfStride = Mathf.Max(.5f, distance) * stance * .5f;
            if (phase <= stance)
            {
                float p = phase / stance;
                travel = Mathf.Lerp(halfStride, -halfStride, p);
                lift = 0f;
                roll = p < .16f ? Mathf.Lerp(-8f, 0f, Smooth(p / .16f))
                    : Mathf.Lerp(0f, 12f, Smooth((p - .78f) / .22f));
            }
            else
            {
                float p = (phase - stance) / (1f - stance);
                // Match stance velocity at both ends instead of stopping the foot at the seam.
                float tangent = -Mathf.Max(.5f, distance) * (1f - stance);
                travel = (2f * p * p * p - 3f * p * p + 1f) * -halfStride
                    + (p * p * p - 2f * p * p + p) * tangent
                    + (-2f * p * p * p + 3f * p * p) * halfStride
                    + (p * p * p - p * p) * tangent;
                lift = Mathf.Pow(Mathf.Sin(p * Mathf.PI), 2f) * .12f;
                roll = Mathf.Lerp(12f, -8f, Smooth(p));
            }
        }
        private void SolveLimb(string upperName, string middleName, string endName, Vector3 target, Vector3 pole)
        {
            Transform upper = Bone(upperName), middle = Bone(middleName), end = Bone(endName);
            float a = Vector3.Distance(upper.position, middle.position), b = Vector3.Distance(middle.position, end.position);
            Vector3 ray = target - upper.position;
            float distance = Mathf.Clamp(ray.magnitude, Mathf.Abs(a - b) + .002f, a + b - .003f);
            if (ray.sqrMagnitude < .000001f || a < .001f || b < .001f) return;
            Vector3 direction = ray.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(pole, direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.ProjectOnPlane(poseBasis * Vector3.right, direction).normalized;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 elbow = upper.position + direction * along + bend * height;
            Vector3 endpoint = upper.position + direction * distance;
            upper.rotation = Quaternion.FromToRotation(middle.position - upper.position, elbow - upper.position) * upper.rotation;
            middle.rotation = Quaternion.FromToRotation(end.position - middle.position, endpoint - middle.position) * middle.rotation;
        }
        private void Rotate(string name, Vector3 axis, float degrees)
        {
            Transform bone = Bone(name);
            bone.rotation = Quaternion.AngleAxis(degrees, poseBasis * axis) * bone.rotation;
        }
        private Transform Bone(string name) => bones.TryGetValue(name, out Transform bone) ? bone : null;
        private Vector3 World(Vector3 point) => transform.position + poseBasis * point;
        private static float Smooth(float value) { value = Mathf.Clamp01(value); return value * value * (3f - 2f * value); }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private void RestoreBones()
        {
            foreach (var item in restPositions) if (item.Key != null) item.Key.localPosition = item.Value;
            foreach (var item in restRotations) if (item.Key != null) item.Key.localRotation = item.Value;
        }
        private void OnDisable()
        {
            CancelPickup();
            RestoreBones();
            ResetMovementObservation();
            cycle = idleTime = blend = yawOffset = turnCycle = lookYaw = lookPitch = recoveryTime = 0f;
        }
#if UNITY_EDITOR
        // Samples the same pose evaluator without driving input or entering Play Mode.
        public void SampleEditorPose(float phase, float amount, float clock, float bodyYaw = 0f,
            float turn = 0f, float turnPhase = 0f, float pickupProgress = -1f, Vector3 pickupTarget = default)
        {
            InitializeRig();
            if (!IsReady) throw new InvalidOperationException("Complete skinned protagonist rig is required.");
            moveDirection = Vector3.forward;
            pickupTime = pickupProgress < 0f ? -1f : pickupProgress * pickupSeconds;
            Evaluate(Mathf.Repeat(phase, 1f), Mathf.Clamp01(amount), clock, bodyYaw, Mathf.Clamp01(turn), turnPhase,
                pickupProgress, pickupTarget, -1f);
        }
        public void RestoreEditorPose() { CancelPickup(); RestoreBones(); }
#endif
    }
}
