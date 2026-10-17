using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LostCity.Meta.Core.Tests
{
    public sealed class ProtagonistVisualMotionTests
    {
        private GameObject root;
        private Component motion;
        private Type type;
        private readonly List<Object> extra = new List<Object>();
        private Transform Bone(string name) => root.GetComponentsInChildren<Transform>(true).Single(item => item.name == "Rig_" + name);
        private object Call(string name, params object[] args) => type.GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(motion, args);
        private T Get<T>(string name) => (T)type.GetProperty(name).GetValue(motion);

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab");
            Assert.That(prefab, Is.Not.Null, "Build the protagonist prefab before native motion acceptance.");
            root = Object.Instantiate(prefab);
            type = Type.GetType("LostCity.Characters.ProtagonistVisualMotion, Assembly-CSharp", true);
            motion = root.GetComponent(type);
            Call("InitializeRig");
            Assert.That(Get<bool>("IsReady"), Is.True);
        }
        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            foreach (Object item in extra) if (item != null) Object.DestroyImmediate(item);
            extra.Clear();
        }
        private void Pose(float phase = 0f, float amount = 0f, float clock = 0f,
            float yaw = 0f, float turn = 0f, float turnPhase = 0f, float pickup = -1f, Vector3 target = default)
            => Call("SampleEditorPose", phase, amount, clock, yaw, turn, turnPhase, pickup, target);

        [Test]
        public void ImportedSkeletonHasIndependentJointsAndDeformingSkin()
        {
            Assert.That(Bone("LeftForearm").parent, Is.EqualTo(Bone("LeftUpperArm")));
            Assert.That(Bone("RightShin").parent, Is.EqualTo(Bone("RightThigh")));
            Assert.That(Bone("LeftUpperArm").position.x, Is.LessThan(0f));
            Assert.That(Bone("RightUpperArm").position.x, Is.GreaterThan(0f));
            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(skins.Length, Is.GreaterThan(10));
            Assert.That(skins.Sum(skin => skin.sharedMesh.boneWeights.Count(weight => weight.weight1 > .01f)), Is.GreaterThan(100));
            foreach (var skin in skins)
                foreach (BoneWeight weight in skin.sharedMesh.boneWeights)
                    Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, Is.EqualTo(1f).Within(.001f));
        }

        [TestCase(0f)]
        [TestCase(.5f)]
        public void WalkHasContralateralArmsAndLegs(float phase)
        {
            Pose(phase, 1f);
            float legs = Bone("LeftFoot").position.z - Bone("RightFoot").position.z;
            float arms = Bone("LeftHand").position.z - Bone("RightHand").position.z;
            Assert.That(Mathf.Abs(legs), Is.GreaterThan(.25f));
            Assert.That(legs * arms, Is.LessThan(-.01f));
        }

        [Test]
        public void FullWalkCycleReturnsToSameBonePose()
        {
            Pose(0f, 1f);
            var start = BonePose();
            Pose(1f, 1f);
            AssertPose(start, .0001f, .02f);
        }

        [Test]
        public void StanceVelocityCancelsRootTravelAndCycleVelocityIsContinuous()
        {
            float Travel(float phase)
            {
                object[] args = { phase, 1.25f, 0f, 0f, 0f };
                type.GetMethod("FootTrajectory").Invoke(null, args);
                return (float)args[2];
            }
            const float h = .0001f;
            float stanceVelocity = (Travel(.2f + h) - Travel(.2f)) / h;
            Assert.That(stanceVelocity, Is.EqualTo(-1.25f).Within(.003f));
            float incoming = (Travel(1f) - Travel(1f - h)) / h;
            float outgoing = (Travel(h) - Travel(0f)) / h;
            Assert.That(incoming, Is.EqualTo(outgoing).Within(.015f));
        }

        [Test]
        public void IdleBreathKeepsFeetPlantedAndLoops()
        {
            Pose();
            Vector3 left = Bone("LeftFoot").position, right = Bone("RightFoot").position;
            var start = BonePose();
            for (int i = 1; i <= 32; i++)
            {
                Pose(clock: i * .1f);
                Assert.That(Vector3.Distance(left, Bone("LeftFoot").position), Is.LessThan(.003f));
                Assert.That(Vector3.Distance(right, Bone("RightFoot").position), Is.LessThan(.003f));
            }
            AssertPose(start, .0002f, .02f);
        }

        [Test]
        public void WalkKeepsOneSupportFootAndDoesNotPenetrateGround()
        {
            for (int frame = 0; frame < 32; frame++)
            {
                Pose(frame / 32f, 1f);
                float left = Lowest("LeftBoot"), right = Lowest("RightBoot");
                Assert.That(left, Is.GreaterThan(-.015f), "Left sole at phase " + frame / 32f);
                Assert.That(right, Is.GreaterThan(-.015f), "Right sole at phase " + frame / 32f);
                Assert.That(Mathf.Min(left, right), Is.LessThan(.045f), "Both feet airborne at phase " + frame / 32f);
            }
        }

        [Test]
        public void WalkingHeadBobsLessThanPelvisAndPassingArmsRemainRelaxed()
        {
            float headMin = float.PositiveInfinity, headMax = float.NegativeInfinity;
            float hipMin = float.PositiveInfinity, hipMax = float.NegativeInfinity;
            for (int i = 0; i < 32; i++)
            {
                Pose(i / 32f, 1f);
                headMin = Mathf.Min(headMin, Bone("Head").position.y);
                headMax = Mathf.Max(headMax, Bone("Head").position.y);
                hipMin = Mathf.Min(hipMin, Bone("Pelvis").position.y);
                hipMax = Mathf.Max(hipMax, Bone("Pelvis").position.y);
            }
            Assert.That(headMax - headMin, Is.LessThan((hipMax - hipMin) * .5f));
            Pose(.25f, 1f);
            foreach (string side in new[] { "Left", "Right" })
                Assert.That(Vector3.Angle(Bone(side + "Forearm").position - Bone(side + "UpperArm").position,
                    Bone(side + "Hand").position - Bone(side + "Forearm").position), Is.LessThan(45f));
        }

        [TestCase(100f, 0f, 25f)]
        [TestCase(-100f, 0f, 25f)]
        [TestCase(0f, 100f, 12f)]
        [TestCase(0f, -100f, 12f)]
        public void AttentionRespectsHeadLimitsWithoutRotatingChest(float x, float y, float limit)
        {
            Pose();
            Quaternion head = Bone("Head").rotation, chest = Bone("Chest").rotation;
            var target = new GameObject("AttentionTarget"); extra.Add(target);
            target.transform.position = Bone("Head").position + new Vector3(x, y, .01f);
            Call("SetAttentionTarget", target.transform);
            Pose();
            Assert.That(Quaternion.Angle(head, Bone("Head").rotation), Is.InRange(limit - .1f, limit + .1f));
            Assert.That(Quaternion.Angle(chest, Bone("Chest").rotation), Is.LessThan(.01f));
        }

        [Test]
        public void WalkPickupAndTurnNeverChangeLimbLengths()
        {
            var pairs = new[] { ("UpperArm", "Forearm"), ("Forearm", "Hand"), ("Thigh", "Shin"), ("Shin", "Foot") };
            var lengths = new Dictionary<string, float>();
            foreach (string side in new[] { "Left", "Right" })
                foreach (var pair in pairs)
                    lengths[side + pair.Item1] = Vector3.Distance(Bone(side + pair.Item1).position, Bone(side + pair.Item2).position);
            for (int i = 0; i < 32; i++)
            {
                float phase = i / 32f;
                for (int action = 0; action < 3; action++)
                {
                    if (action == 0) Pose(phase, 1f);
                    if (action == 1) Pose(pickup: phase, target: new Vector3(-.3f, .98f, .36f));
                    if (action == 2) Pose(yaw: 90f * phase, turn: 1f, turnPhase: phase);
                    foreach (string side in new[] { "Left", "Right" })
                        foreach (var pair in pairs)
                            Assert.That(Vector3.Distance(Bone(side + pair.Item1).position, Bone(side + pair.Item2).position),
                                Is.EqualTo(lengths[side + pair.Item1]).Within(.0001f));
                }
            }
        }

        [TestCase(.25f, "Left", "Right")]
        [TestCase(.75f, "Right", "Left")]
        public void LargeStationaryTurnAlternatesSupportFeet(float phase, string moving, string support)
        {
            Pose();
            float movingY = Bone(moving + "Foot").position.y, supportY = Bone(support + "Foot").position.y;
            Pose(yaw: -90f, turn: 1f, turnPhase: phase);
            Assert.That(Bone(moving + "Foot").position.y - movingY, Is.GreaterThan(.04f));
            Assert.That(Bone(support + "Foot").position.y, Is.EqualTo(supportY).Within(.003f));
        }

        [Test]
        public void WalkDoesNotModifyGameplayRootPhysicsOrTime()
        {
            var player = new GameObject("ExternalPhysics"); extra.Add(player);
            var body = player.AddComponent<Rigidbody2D>(); body.gravityScale = 0f;
            root.transform.SetParent(player.transform);
            root.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
            float time = Time.timeScale;
            for (int i = 0; i < 30; i++) Call("Advance", Vector3.right * 3.2f, .02f);
            Assert.That(player.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(player.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(Time.timeScale, Is.EqualTo(time));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void PauseOrInvalidDeltaPreservesCurrentPose(float delta)
        {
            Pose(.25f, 1f);
            var start = BonePose();
            Call("Advance", Vector3.forward, delta);
            AssertPose(start, .0001f, .02f);
        }

        [Test]
        public void StationaryPlaybackSettlesWithoutMarching()
        {
            for (int i = 0; i < 30; i++) Call("Advance", Vector3.forward * 1.25f, .02f);
            for (int i = 0; i < 15; i++) Call("Advance", Vector3.zero, .02f);
            Assert.That(Get<float>("LocomotionBlend"), Is.Zero);
            Vector3 foot = Bone("LeftFoot").position;
            for (int i = 0; i < 20; i++) Call("Advance", Vector3.zero, .02f);
            Assert.That(Vector3.Distance(foot, Bone("LeftFoot").position), Is.LessThan(.003f));
        }

        [Test]
        public void PickupUsesLeftHandReachesContactAndReturns()
        {
            Vector3 target = new Vector3(-.30f, .98f, .36f);
            Assert.That(Call("TryPresentPickup", target, null), Is.EqualTo(true));
            Pose(pickup: .5f, target: target);
            Assert.That(Vector3.Distance(Get<Vector3>("VisualGripPoint"), target), Is.LessThan(.02f));
            Assert.That(Bone("RightHand").position.x, Is.GreaterThan(.25f));
            Assert.That(Lowest("LeftBoot"), Is.InRange(-.015f, .025f));
            Assert.That(Lowest("RightBoot"), Is.InRange(-.015f, .025f));
            for (int i = 0; i < 50; i++) Call("Advance", Vector3.zero, .02f);
            Assert.That(Get<bool>("IsPickingUp"), Is.False);
            Assert.That(Bone("LeftHand").position.y, Is.LessThan(1f));
        }

        [TestCase(0f, .1f, .3f)]
        [TestCase(-.3f, .98f, 2f)]
        [TestCase(-.3f, .98f, -.3f)]
        public void UnreachableGroundOrBehindTargetDoesNotStretchArm(float x, float y, float z)
        {
            var start = BonePose();
            Assert.That(Call("TryPresentPickup", new Vector3(x, y, z), null), Is.EqualTo(false));
            AssertPose(start, .0001f, .02f);
        }

        [Test]
        public void PickupPauseResumeAndDestroyedTargetAreSafe()
        {
            var target = new GameObject("Clue lifetime"); extra.Add(target);
            Assert.That(Call("TryPresentPickup", new Vector3(-.3f, .98f, .36f), target), Is.EqualTo(true));
            for (int i = 0; i < 12; i++) Call("Advance", Vector3.zero, .02f);
            float progress = Get<float>("PickupProgress");
            var before = BonePose();
            target.SetActive(false);
            Call("Advance", Vector3.zero, 0f);
            AssertPose(before, .0001f, .02f);
            Call("Advance", Vector3.zero, .02f);
            Assert.That(Get<float>("PickupProgress"), Is.GreaterThan(progress), "Successful collection hides, not destroys, original.");
            Object.DestroyImmediate(target);
            Call("Advance", Vector3.zero, .02f);
            Assert.That(Get<bool>("IsPickingUp"), Is.False);
        }

        [Test]
        public void MovementCancelsReachWithoutTakingOwnershipOfMovement()
        {
            Assert.That(Call("TryPresentPickup", new Vector3(-.3f, .98f, .36f), null), Is.EqualTo(true));
            for (int i = 0; i < 15; i++) Call("Advance", Vector3.zero, .02f);
            Call("Advance", Vector3.forward, .02f);
            Assert.That(Get<bool>("IsPickingUp"), Is.False);
            for (int i = 0; i < 15; i++) Call("Advance", Vector3.forward, .02f);
            Assert.That(Get<float>("LocomotionBlend"), Is.EqualTo(1f));
        }

        private Dictionary<Transform, (Vector3, Quaternion)> BonePose() => root.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name.StartsWith("Rig_", StringComparison.Ordinal)).ToDictionary(item => item, item => (item.localPosition, item.localRotation));
        private void AssertPose(Dictionary<Transform, (Vector3, Quaternion)> expected, float distance, float angle)
        {
            foreach (var item in expected)
            {
                Assert.That(Vector3.Distance(item.Key.localPosition, item.Value.Item1), Is.LessThan(distance), item.Key.name);
                Assert.That(Quaternion.Angle(item.Key.localRotation, item.Value.Item2), Is.LessThan(angle), item.Key.name);
            }
        }
        private float Lowest(string part)
        {
            Transform node = root.GetComponentsInChildren<Transform>(true).Single(item => item.name == part);
            var mesh = new Mesh();
            try
            {
                float min = float.PositiveInfinity;
                foreach (SkinnedMeshRenderer skin in node.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(mesh);
                    foreach (Vector3 vertex in mesh.vertices) min = Mathf.Min(min, skin.transform.TransformPoint(vertex).y);
                }
                return min;
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
