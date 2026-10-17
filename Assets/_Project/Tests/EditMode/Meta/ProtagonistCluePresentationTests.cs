using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostCity.Meta.Core.Tests
{
    // The runtime is in a predefined assembly, so this asmdef uses reflection.
    public sealed class ProtagonistCluePresentationTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab";
        private static readonly Vector3 ReachPoint = new Vector3(-.3f, .98f, .36f);
        private readonly List<Object> assets = new List<Object>();
        private readonly List<GameObject> ownedRoots = new List<GameObject>();
        private readonly HashSet<GameObject> originalRoots = new HashSet<GameObject>();
        private Scene scene;
        private object originalRuntime;
        private float originalTimeScale;
        private string directory;
        private SaveGameManager save;
        private Component progress, motion, bridge, pickup, anchor, prompt;
        private ScriptableObject clue, secondClue;
        private GameObject visual;
        private Transform sourceVisual, contact;
        private Rigidbody2D body;
        private int mutations;

        [SetUp]
        public void SetUp()
        {
            originalRuntime = RuntimeInstance.GetValue(null);
            scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects()) originalRoots.Add(root);
            originalTimeScale = Time.timeScale;
            RuntimeInstance.SetValue(null, null);
            directory = Path.Combine(Path.GetTempPath(), "LostCity-clue-visual-" + Guid.NewGuid().ToString("N"));
            save = Root("Temporary save").AddComponent<SaveGameManager>();
            save.Initialize(Path.Combine(directory, "save.json"));
            save.AddPermanentUnlock("existing_test_unlock");

            // Never activate this stub or call GameRuntime.Awake: that initializes the user's save.
            GameObject runtimeRoot = Root("Inactive runtime stub");
            runtimeRoot.SetActive(false);
            Component runtime = runtimeRoot.AddComponent(RuntimeType("LostCity.Meta.GameRuntime"));
            Component session = runtimeRoot.AddComponent(RuntimeType("LostCity.Meta.CaseSessionManager"));
            Set(runtime, "<SaveGame>k__BackingField", save);
            Set(runtime, "<CaseSession>k__BackingField", session);
            RuntimeInstance.SetValue(null, runtime);
            GameObject promptRoot = Root("Inactive prompt stub");
            promptRoot.SetActive(false);
            prompt = promptRoot.AddComponent(RuntimeType("LostCity.CombatSandbox.GamePromptManager"));

            clue = Definition("ClueDefinition");
            secondClue = Definition("ClueDefinition");
            Set(clue, "id", "visual_test_clue");
            Set(secondClue, "id", "visual_test_second_clue");
            ScriptableObject definition = Definition("CaseDefinition");
            Set(definition, "caseId", "visual_test_case");
            Set(definition, "clues", TypedArray(clue.GetType(), clue, secondClue));
            GameObject progressRoot = Root("Temporary investigation");
            progressRoot.SetActive(false);
            progress = progressRoot.AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(progress, "caseDefinition", definition);
            Call(progress, "Awake");

            GameObject player = Root("Test player");
            body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "Build the real protagonist prefab before native integration tests.");
            visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            ownedRoots.Add(visual);
            visual.transform.SetParent(player.transform, false);
            motion = visual.GetComponent(RuntimeType("LostCity.Characters.ProtagonistVisualMotion"));
            Assert.That(motion, Is.Not.Null);
            Set(motion, "movementBody", body);
            Call(motion, "InitializeRig");
            Assert.That(Get<bool>(motion, "IsReady"), Is.True);

            pickup = CreatePickup(clue, ReachPoint);
            anchor = pickup.GetComponent(RuntimeType("LostCity.Characters.ProtagonistPickupAnchor"));
            sourceVisual = Get<Transform>(anchor, "VisualRoot");
            contact = Get<Transform>(anchor, "ContactPoint");
            GameObject bridgeRoot = Root("Presentation bridge");
            bridgeRoot.SetActive(false);
            bridge = bridgeRoot.AddComponent(RuntimeType("LostCity.Characters.ProtagonistCluePresentation"));
            Set(bridge, "investigationProgress", progress);
            Set(bridge, "motion", motion);
            SetPickups(pickup);
            bridgeRoot.SetActive(true);
            // Explicit lifecycle messages match the other EditMode integration fixtures.
            // Remove any automatic subscription before installing exactly one test subscription.
            Call(bridge, "OnDisable");
            Call(bridge, "OnEnable");
            save.DataChanged += () => mutations++;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (bridge != null) Call(bridge, "OnDisable");
                for (int i = ownedRoots.Count - 1; i >= 0; i--)
                    if (ownedRoots[i] != null) Object.DestroyImmediate(ownedRoots[i]);
                ownedRoots.Clear();
                originalRoots.Clear();
                foreach (Object asset in assets) if (asset != null) Object.DestroyImmediate(asset);
                assets.Clear();
            }
            finally
            {
                RuntimeInstance.SetValue(null, originalRuntime);
                Time.timeScale = originalTimeScale;
                if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
                directory = null;
                mutations = 0;
            }
        }

        [Test]
        public void SuccessfulCollection_CopiesOnlyRenderData_AndCompletesWithoutAnotherSaveMutation()
        {
            Call(pickup, "Collect");
            GameObject proxy = AssertProxy();
            AssertRecorded("visual_test_clue");
            string collectedSave = File.ReadAllText(save.SavePath);
            Assert.That(pickup != null && !pickup.gameObject.activeSelf, Is.True,
                "Collection hides the original rather than destroying its lifetime token.");
            foreach (Component component in proxy.GetComponentsInChildren<Component>(true))
                Assert.That(component is Transform || component is MeshFilter || component is MeshRenderer, Is.True,
                    "The proxy must not clone interaction, physics or persistence components.");
            MeshFilter copied = proxy.GetComponentsInChildren<MeshFilter>().Single();
            MeshFilter original = sourceVisual.GetComponentInChildren<MeshFilter>(true);
            Assert.That(copied.sharedMesh, Is.SameAs(original.sharedMesh));
            Assert.That(copied.GetComponent<MeshRenderer>().sharedMaterials,
                Is.EqualTo(original.GetComponent<MeshRenderer>().sharedMaterials));

            Vector3 start = proxy.transform.position;
            Tick(24); // .48 seconds: no hand-held visual before contact.
            Assert.That(Vector3.Distance(proxy.transform.position, start), Is.LessThan(.0001f));
            Tick(3); // .54 seconds: contact, original is still hidden but not destroyed.
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.True);
            Assert.That(Get<bool>(motion, "PickupContact"), Is.True);
            Assert.That(Vector3.Distance(Get<Vector3>(motion, "VisualGripPoint"), ReachPoint), Is.LessThan(.025f));
            Tick(10); // .74 seconds: return phase follows the grip, not the wrist origin.
            Vector3 expected = start + Get<Vector3>(motion, "VisualGripPoint") - ReachPoint;
            Assert.That(Vector3.Distance(proxy.transform.position, expected), Is.LessThan(.0001f));
            Tick(100);
            AssertNoProxy();
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.False);
            AssertRecorded("visual_test_clue");
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo(collectedSave));
        }

        [Test]
        public void DuplicateObjectAndStableIdAlias_DoNotRestartOrCreateSecondProxy()
        {
            Call(pickup, "Collect");
            GameObject proxy = AssertProxy();
            Tick(12);
            float before = Get<float>(motion, "PickupProgress");
            string collectedSave = File.ReadAllText(save.SavePath);
            Call(pickup, "Collect");
            Assert.That(Call(progress, "TryCollectClue", clue), Is.False);
            ScriptableObject alias = Definition("ClueDefinition");
            Set(alias, "id", "visual_test_clue");
            Assert.That(Call(progress, "TryCollectClue", alias), Is.False);
            Assert.That(AssertProxy(), Is.SameAs(proxy));
            Assert.That(Get<float>(motion, "PickupProgress"), Is.EqualTo(before));
            AssertRecorded("visual_test_clue");
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo(collectedSave));
        }

        [TestCase("null")]
        [TestCase("blank")]
        [TestCase("unrelated")]
        public void FailedCollection_DoesNotStartPresentationOrWriteData(string failure)
        {
            ScriptableObject invalid = failure == "null" ? null : Definition("ClueDefinition");
            if (invalid != null) Set(invalid, "id", failure == "blank" ? " " : "not_in_case");
            Component failedPickup = CreatePickup(invalid, ReachPoint);
            SetPickups(failedPickup);
            string before = File.ReadAllText(save.SavePath);
            Call(failedPickup, "Collect");
            Assert.That(failedPickup.gameObject.activeSelf, Is.True);
            AssertNoProxy();
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.False);
            AssertRecorded();
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo(before));
        }

        [TestCase("moving")]
        [TestCase("unreachable")]
        [TestCase("armed")]
        public void SuccessfulDataCollection_WhenPresentationIsUnsafe_SkipsOnlyVisual(string condition)
        {
            if (condition == "moving") body.velocity = Vector2.right;
            if (condition == "unreachable") contact.position = new Vector3(-.3f, .98f, 2f);
            if (condition == "armed") Set(motion, "armedPose", true);
            Call(pickup, "Collect");
            AssertNoProxy();
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.False);
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            AssertRecorded("visual_test_clue");
        }

        [TestCase("missing")]
        [TestCase("contact")]
        [TestCase("visual")]
        public void MissingPickupAnchorData_SkipsVisualWithoutChangingCollection(string missing)
        {
            if (missing == "missing") Object.DestroyImmediate(anchor);
            else Set(anchor, missing == "contact" ? "contactPoint" : "visualRoot", null);
            Call(pickup, "Collect");
            AssertNoProxy();
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.False);
            Assert.That(pickup.gameObject.activeSelf, Is.False);
            AssertRecorded("visual_test_clue");
        }

        [Test]
        public void AnotherSuccessfulClueDuringReach_RecordsOnceWithoutReplacingActiveProxy()
        {
            Component another = CreatePickup(secondClue, ReachPoint);
            SetPickups(pickup, another);
            Call(pickup, "Collect");
            GameObject first = AssertProxy();
            Tick(10);
            float before = Get<float>(motion, "PickupProgress");
            Call(another, "Collect");
            Assert.That(AssertProxy(), Is.SameAs(first));
            Assert.That(Get<float>(motion, "PickupProgress"), Is.EqualTo(before));
            Assert.That(another.gameObject.activeSelf, Is.False);
            AssertRecorded("visual_test_clue", "visual_test_second_clue");
            Tick(100);
            AssertNoProxy();
            AssertRecorded("visual_test_clue", "visual_test_second_clue");
        }

        [TestCase("movement")]
        [TestCase("destroyed_target")]
        [TestCase("disabled_motion")]
        [TestCase("disabled_bridge")]
        public void InterruptedPresentation_CleansProxyWithoutUndoingOrRepeatingCollection(string interruption)
        {
            Call(pickup, "Collect");
            AssertProxy();
            Tick(15);
            string before = File.ReadAllText(save.SavePath);
            if (interruption == "movement") Call(motion, "Advance", Vector3.forward, .02f);
            if (interruption == "destroyed_target")
            {
                Object.DestroyImmediate(pickup.gameObject);
                Call(motion, "Advance", Vector3.zero, .02f);
            }
            if (interruption == "disabled_motion")
            {
                ((Behaviour)motion).enabled = false;
                Call(motion, "OnDisable");
            }
            if (interruption == "disabled_bridge")
            {
                ((Behaviour)bridge).enabled = false;
                Call(bridge, "OnDisable");
            }
            Call(bridge, "LateUpdate");
            AssertNoProxy();
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.False,
                "Bone recovery may continue for .18s, but pickup ownership ends immediately.");
            AssertRecorded("visual_test_clue");
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo(before));
        }

        [Test]
        public void DisabledBridge_UnsubscribesAndDoesNotReplayCollectionsOnEnable()
        {
            Call(pickup, "Collect");
            AssertProxy();
            Component another = CreatePickup(secondClue, ReachPoint);
            SetPickups(pickup, another);
            Call(bridge, "OnDisable");
            Call(another, "Collect");
            AssertNoProxy();
            AssertRecorded("visual_test_clue", "visual_test_second_clue");
            Call(bridge, "OnEnable");
            Tick(100);
            AssertNoProxy();
            AssertRecorded("visual_test_clue", "visual_test_second_clue");
        }

        [Test]
        public void ProxyPreservesSourceWorldGeometry_WithScaledVisualRoot()
        {
            sourceVisual.localScale = new Vector3(.8f, 1.2f, .7f);
            // Keep contact in the same reachable world location while changing only visual scale.
            contact.position = ReachPoint;
            MeshFilter original = sourceVisual.GetComponentInChildren<MeshFilter>();
            Vector3[] expected = original.sharedMesh.vertices.Select(original.transform.TransformPoint).ToArray();
            Call(pickup, "Collect");
            MeshFilter copied = AssertProxy().GetComponentsInChildren<MeshFilter>().Single();
            Vector3[] actual = copied.sharedMesh.vertices.Select(copied.transform.TransformPoint).ToArray();
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
                Assert.That(Vector3.Distance(actual[i], expected[i]), Is.LessThan(.0001f),
                    "Proxy creation must not multiply the visual root scale into its children twice.");
            AssertRecorded("visual_test_clue");
        }

        private Component CreatePickup(ScriptableObject definition, Vector3 point)
        {
            GameObject root = Root("Temporary clue pickup");
            root.SetActive(false);
            root.transform.position = point;
            root.AddComponent<BoxCollider2D>();
            Component result = root.AddComponent(RuntimeType("LostCity.CombatSandbox.CluePickup"));
            Call(result, "Initialize", definition, progress);
            Set(result, "promptManager", prompt);
            Call(result, "Awake");
            Transform model = new GameObject("ClueVisual").transform;
            model.SetParent(root.transform, false);
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "Paper mesh with source collider";
            part.transform.SetParent(model, false);
            part.transform.localScale = new Vector3(.06f, .006f, .08f);
            Transform pointTransform = new GameObject("Contact").transform;
            pointTransform.SetParent(model, false);
            Component resultAnchor = root.AddComponent(RuntimeType("LostCity.Characters.ProtagonistPickupAnchor"));
            Set(resultAnchor, "contactPoint", pointTransform);
            Set(resultAnchor, "visualRoot", model);
            root.SetActive(true);
            return result;
        }

        private void Tick(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                Call(motion, "Advance", Vector3.zero, .02f);
                Call(bridge, "LateUpdate");
            }
        }

        private GameObject AssertProxy()
        {
            GameObject value = (GameObject)Field(bridge, "proxy");
            Assert.That(value != null, Is.True, "A reachable stationary collection must create a visual proxy.");
            Assert.That(TestProxyRoots().Length, Is.EqualTo(1),
                "No second or orphaned proxy may coexist.");
            Assert.That(Get<bool>(motion, "IsPickingUp"), Is.True);
            return value;
        }

        private void AssertNoProxy()
        {
            Assert.That((GameObject)Field(bridge, "proxy") == null, Is.True);
            Assert.That(TestProxyRoots().Length, Is.Zero);
        }

        private GameObject[] TestProxyRoots() => scene.IsValid() && scene.isLoaded
            ? scene.GetRootGameObjects().Where(root => root.name == "CollectedClueVisual"
                && !originalRoots.Contains(root)).ToArray()
            : Array.Empty<GameObject>();

        private void AssertRecorded(params string[] ids)
        {
            Assert.That(mutations, Is.EqualTo(ids.Length), "Only original collection may mutate persistent data.");
            Assert.That(((IEnumerable)Get<object>(progress, "CollectedClues")).Cast<ScriptableObject>()
                .Select(item => Get<string>(item, "Id")), Is.EquivalentTo(ids));
            Assert.That(save.DiscoveredClueIds, Is.EquivalentTo(ids));
            SaveGameData disk = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(save.SavePath));
            Assert.That(disk.DiscoveredClueIds, Is.EquivalentTo(ids));
            Assert.That(disk.PermanentUnlockIds, Is.EqualTo(new[] { "existing_test_unlock" }));
            Assert.That(disk.CompletedCaseIds, Is.Empty);
            Assert.That(save.HasUnsavedChanges, Is.False);
        }

        private void SetPickups(params Component[] values) => Set(bridge, "pickups",
            TypedArray(RuntimeType("LostCity.CombatSandbox.CluePickup"), values.Cast<object>().ToArray()));
        private GameObject Root(string name)
        {
            var root = new GameObject(name);
            ownedRoots.Add(root);
            return root;
        }
        private ScriptableObject Definition(string name)
        {
            var value = ScriptableObject.CreateInstance(RuntimeType("LostCity.CombatSandbox." + name));
            assets.Add(value);
            return value;
        }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static FieldInfo RuntimeInstance => RuntimeType("LostCity.Meta.GameRuntime")
            .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        private static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static T Get<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
        private void TrackProxy()
        {
            if (bridge == null) return;
            GameObject proxy = (GameObject)Field(bridge, "proxy");
            if (proxy != null && !ownedRoots.Contains(proxy)) ownedRoots.Add(proxy);
        }
        private object Call(object target, string name, params object[] args)
        {
            // Track the exact bridge-owned object, not arbitrary same-named scene roots.
            TrackProxy();
            try
            {
                return target.GetType().GetMethod(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
            }
            finally { TrackProxy(); }
        }
        private static Array TypedArray(Type type, params object[] values)
        {
            var result = Array.CreateInstance(type, values.Length);
            for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
            return result;
        }
    }
}
