using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostCity.Meta.Core.Tests
{
    public sealed class ProtagonistMovementObservationTests
    {
        private const float FixedDt = .02f;
        private const float Speed = 3.2f;
        private Scene scene;
        private PhysicsScene2D physics;
        private Rigidbody2D body;
        private Transform aimRoot;
        private GameObject visual;
        private Component motion;
        private Type type;

        private object Call(string name, params object[] args)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing production entry: " + name);
            return method.Invoke(motion, args);
        }
        private T Property<T>(string name) => (T)type.GetProperty(name).GetValue(motion);
        private T Field<T>(string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(motion);
        private void Frame(float dt = FixedDt) => Call("UpdateFromMovement", dt);
        private Transform Bone(string name) => visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Rig_" + name);

        [SetUp]
        public void SetUp()
        {
            // Preview scenes are supported in EditMode without replacing the user's open scenes.
            scene = EditorSceneManager.NewPreviewScene();
            Assert.That(EditorSceneManager.IsPreviewScene(scene), Is.True);
            physics = scene.GetPhysicsScene2D();
            Assert.That(physics.IsValid(), Is.True);
            Assert.That(physics, Is.Not.EqualTo(Physics2D.defaultPhysicsScene));
            body = CreateBody("MovementBody", Vector2.zero);
            aimRoot = new GameObject("AimRoot").transform;
            aimRoot.SetParent(body.transform, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab");
            Assert.That(prefab, Is.Not.Null);
            visual = Object.Instantiate(prefab, aimRoot);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
            type = Type.GetType("LostCity.Characters.ProtagonistVisualMotion, Assembly-CSharp", true);
            motion = visual.GetComponent(type);
            Assert.That(motion, Is.Not.Null);
            // EditMode invokes production lifecycle messages explicitly; no player loop or input is started.
            Call("OnEnable");
            Assert.That(Property<bool>("IsReady"), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            EditorSceneManager.ClosePreviewScene(scene);
        }

        private Rigidbody2D CreateBody(string name, Vector2 position)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            var result = root.AddComponent<Rigidbody2D>();
            result.gravityScale = 0f;
            result.constraints = RigidbodyConstraints2D.FreezeRotation;
            result.position = position;
            root.AddComponent<CircleCollider2D>().radius = .25f;
            return result;
        }

        private void Step(Vector2 displacement)
        {
            body.MovePosition(body.position + displacement);
            // PlayerMotor queues its move before order-150 visual sampling; simulation completes afterwards.
            Call("SampleMovement", FixedDt);
            Assert.That(physics.Simulate(FixedDt), Is.True, "Only the isolated local physics scene may advance.");
        }

        private void Run(int steps, Vector2 displacement)
        {
            for (int i = 0; i < steps; i++) { Step(displacement); Frame(); }
        }

        [TestCase(RigidbodyInterpolation2D.None)]
        [TestCase(RigidbodyInterpolation2D.Interpolate)]
        public void CompletedMovePositionDrivesWalkingWithZeroReportedVelocity(RigidbodyInterpolation2D interpolation)
        {
            body.interpolation = interpolation;
            Vector3 visualPosition = visual.transform.localPosition;
            Quaternion visualRotation = visual.transform.localRotation;
            float timeScale = Time.timeScale;
            Run(20, Vector2.right * (Speed * FixedDt));
            Assert.That(body.position.x, Is.EqualTo(1.28f).Within(.001f));
            Assert.That(body.velocity.sqrMagnitude, Is.LessThan(.000001f));
            Assert.That(Field<Vector2>("observedVelocity").x, Is.EqualTo(Speed).Within(.001f));
            Assert.That(Property<float>("LocomotionBlend"), Is.EqualTo(1f));
            Vector3 foot = visual.transform.InverseTransformPoint(Bone("LeftFoot").position);
            Run(4, Vector2.right * (Speed * FixedDt));
            Assert.That(Vector3.Distance(foot, visual.transform.InverseTransformPoint(Bone("LeftFoot").position)), Is.GreaterThan(.01f));
            Assert.That(body.rotation, Is.Zero);
            Assert.That(visual.transform.localPosition, Is.EqualTo(visualPosition));
            Assert.That(Quaternion.Angle(visualRotation, visual.transform.localRotation), Is.LessThan(.001f));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }

        [Test]
        public void StopSettlesAndKeepsFeetPlanted()
        {
            Run(20, Vector2.right * (Speed * FixedDt));
            Assert.That(Property<float>("LocomotionBlend"), Is.EqualTo(1f));
            Vector2 stopped = body.position;
            Run(20, Vector2.zero);
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero);
            Assert.That(Field<Vector2>("observedVelocity"), Is.EqualTo(Vector2.zero));
            Vector3 foot = Bone("LeftFoot").position;
            float cycle = Field<float>("cycle");
            Run(20, Vector2.zero);
            Assert.That(body.position, Is.EqualTo(stopped));
            Assert.That(Field<float>("cycle"), Is.EqualTo(cycle));
            Assert.That(Vector3.Distance(foot, Bone("LeftFoot").position), Is.LessThan(.003f));
        }

        [Test]
        public void StationaryAimRotationDoesNotCreateLocomotion()
        {
            for (int i = 0; i < 20; i++)
            {
                aimRoot.localRotation = Quaternion.Euler(0f, 0f, i * 9f);
                Step(Vector2.zero);
                Frame();
            }
            Assert.That(body.position, Is.EqualTo(Vector2.zero));
            Assert.That(Field<Vector2>("observedVelocity"), Is.EqualTo(Vector2.zero));
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero);
            Assert.That(Field<float>("cycle"), Is.Zero);
        }

        [Test]
        public void ContinuingMoveRequestsAgainstWallSettleWhenActualMovementStops()
        {
            var wall = new GameObject("ObservationWall");
            SceneManager.MoveGameObjectToScene(wall, scene);
            var wallBody = wall.AddComponent<Rigidbody2D>();
            wallBody.bodyType = RigidbodyType2D.Static;
            wallBody.position = new Vector2(1.2f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 4f);
            Run(8, Vector2.right * (Speed * FixedDt));
            Assert.That(Property<float>("LocomotionBlend"), Is.GreaterThan(.5f));
            Run(32, Vector2.right * (Speed * FixedDt));
            Vector2 blocked = body.position;
            Assert.That(blocked.x, Is.InRange(.7f, .9f), "The native collider must actually block the requested travel.");
            Run(20, Vector2.right * (Speed * FixedDt));
            Assert.That(Vector2.Distance(blocked, body.position), Is.LessThan(.001f));
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero);
        }

        [Test]
        public void PausedFramesFreezePoseAndResumeWalking()
        {
            Run(20, Vector2.right * (Speed * FixedDt));
            var pose = visual.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Rig_", StringComparison.Ordinal))
                .ToDictionary(t => t, t => (t.localPosition, t.localRotation));
            Vector2 position = body.position;
            float cycle = Field<float>("cycle");
            for (int i = 0; i < 10; i++) Frame(0f);
            foreach (var item in pose)
            {
                Assert.That(item.Key.localPosition, Is.EqualTo(item.Value.Item1));
                Assert.That(Quaternion.Angle(item.Key.localRotation, item.Value.Item2), Is.LessThan(.001f));
            }
            Assert.That(body.position, Is.EqualTo(position));
            Assert.That(Field<float>("cycle"), Is.EqualTo(cycle));
            Step(Vector2.right * (Speed * FixedDt));
            Frame();
            Assert.That(Field<float>("cycle"), Is.Not.EqualTo(cycle));
            Assert.That(Property<float>("LocomotionBlend"), Is.EqualTo(1f));
        }

        [TestCase(0, .005f)]
        [TestCase(3, .06f)]
        public void RenderCadenceRetainsFixedStepSpeed(int stepsPerFrame, float frameDt)
        {
            Run(20, Vector2.right * (Speed * FixedDt));
            for (int frame = 0; frame < 4; frame++)
            {
                for (int i = 0; i < stepsPerFrame; i++) Step(Vector2.right * (Speed * FixedDt));
                float before = Field<float>("cycle");
                Frame(frameDt);
                float advanced = Mathf.Repeat(Field<float>("cycle") - before, 1f);
                Assert.That(advanced, Is.EqualTo(Speed * frameDt / 1.25f).Within(.0001f));
                Assert.That(Property<float>("LocomotionBlend"), Is.EqualTo(1f));
            }
        }

        [Test]
        public void DisableLateParentAndReparentResetPositionBaseline()
        {
            Run(20, Vector2.right * (Speed * FixedDt));
            Call("OnDisable");
            ((Behaviour)motion).enabled = false;
            body.position = new Vector2(30f, 0f);
            ((Behaviour)motion).enabled = true;
            Call("OnEnable");
            Run(2, Vector2.zero);
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero, "Disabled travel must not become a stride.");

            visual.transform.SetParent(null, true);
            Call("OnTransformParentChanged");
            for (int i = 0; i < 4; i++) { Call("SampleMovement", FixedDt); Frame(); }
            Assert.That(Field<Rigidbody2D>("movementBody"), Is.Null);
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero);

            Rigidbody2D oldBody = body;
            body = CreateBody("NewMovementBody", new Vector2(-30f, 0f));
            visual.transform.SetParent(body.transform, false);
            Call("OnTransformParentChanged");
            Run(2, Vector2.zero);
            Assert.That(Field<Rigidbody2D>("movementBody"), Is.SameAs(body));
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero, "New parent coordinates are a baseline, not travel.");
            Run(20, Vector2.right * (Speed * FixedDt));
            Assert.That(Property<float>("LocomotionBlend"), Is.EqualTo(1f));
            Assert.That(oldBody.position.x, Is.EqualTo(30f));
        }

        [Test]
        public void ObservedMovementRejectsPickupBeforeBlendAndCancelsActiveReach()
        {
            Vector3 Target() => visual.transform.TransformPoint(new Vector3(-.3f, .98f, .36f));
            Assert.That(Call("TryPresentPickup", Target(), null), Is.EqualTo(true));
            Step(Vector2.right * (Speed * FixedDt));
            Step(Vector2.right * (Speed * FixedDt));
            Assert.That(body.velocity.sqrMagnitude, Is.LessThan(.000001f));
            Assert.That(Property<float>("LocomotionBlend"), Is.Zero);
            Call("CancelPickup");
            Assert.That(Call("TryPresentPickup", Target(), null), Is.EqualTo(false), "Observed displacement must guard pickup before the first visual frame.");
            Run(20, Vector2.zero);
            Assert.That(Call("TryPresentPickup", Target(), null), Is.EqualTo(true));
            Run(2, Vector2.right * (Speed * FixedDt));
            Assert.That(Property<bool>("IsPickingUp"), Is.False);
        }
    }
}
