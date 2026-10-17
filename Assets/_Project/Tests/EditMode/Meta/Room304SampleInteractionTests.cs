using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LostCity.Meta.Core.Tests
{
    // Predefined runtime/UI assemblies cannot be referenced by this test asmdef.
    public sealed class Room304SampleInteractionTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private object originalRuntime;
        private string directory;
        private float originalTimeScale;
        private SaveGameManager save;
        private Component session;
        private Component sceneFlow;
        private Component status;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalRuntime = RuntimeInstance.GetValue(null);
            RuntimeInstance.SetValue(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
            RuntimeInstance.SetValue(null, originalRuntime);
            Time.timeScale = originalTimeScale;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            directory = null;
        }

        [TestCase(false, 0f)]
        [TestCase(false, -2.5f)]
        [TestCase(false, 3f)]
        [TestCase(true, 0f)]
        [TestCase(true, -2.5f)]
        [TestCase(true, 3f)]
        public void Aim_ProjectedWorldPoints_ReturnToRequestedXYPlane(bool tilted, float planeZ)
        {
            Camera camera = CreateCamera(tilted);
            foreach (Vector2 xy in new[] { Vector2.zero, new Vector2(4f, 3f), new Vector2(-5f, -2f) })
            {
                var expected = new Vector3(xy.x, xy.y, planeZ);
                Vector3 screen = camera.WorldToScreenPoint(expected);
                Assert.That(screen.z, Is.GreaterThan(camera.nearClipPlane));
                Assert.That(TryAim(camera, screen, planeZ, out Vector3 actual), Is.True);
                Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f));
            }
        }

        [Test]
        public void Aim_TopDown_RetainsLegacyScreenToWorldResult()
        {
            Camera camera = CreateCamera(false);
            var screen = new Vector2(373f, 511f);
            const float planeZ = 2f;
            Vector3 legacy = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                Mathf.Abs(camera.transform.position.z - planeZ)));
            Assert.That(TryAim(camera, screen, planeZ, out Vector3 actual), Is.True);
            Assert.That(Vector3.Distance(actual, legacy), Is.LessThan(0.0001f));
        }

        [Test]
        public void Aim_MissingOrDestroyedCamera_IsRejected()
        {
            Assert.That(TryAim(null, Vector2.zero, 0f, out Vector3 point), Is.False);
            Assert.That(point, Is.EqualTo(Vector3.zero));
            Camera camera = CreateCamera(false);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            Assert.That(TryAim(camera, Vector2.zero, 0f, out point), Is.False);
            Assert.That(point, Is.EqualTo(Vector3.zero));
        }

        [TestCase(0f)]
        [TestCase(0.00001f)]
        public void Aim_ParallelAndNearParallelRays_AreRejected(float zDirection)
        {
            Camera camera = CreateCamera(false);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(1f, 0f, zDirection), Vector3.up);
            Assert.That(TryAim(camera, camera.pixelRect.center, 0f, out Vector3 point), Is.False);
            Assert.That(point, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Aim_PlaneBehindRay_IsRejected()
        {
            Camera camera = CreateCamera(false);
            camera.transform.position = new Vector3(0f, 0f, 10f);
            Assert.That(TryAim(camera, camera.pixelRect.center, 0f, out Vector3 point), Is.False);
            Assert.That(point, Is.EqualTo(Vector3.zero));
        }

        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        public void ClueTrigger_RecognizesPlayerTeamOnParent_WithoutHealth(int team, bool expected)
        {
            Component progress = CreateProgress();
            Component pickup = CreatePickup(RequiredClues(progress)[0], progress);
            var player = Root("Minimal player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            if (team >= 0)
            {
                Component member = player.AddComponent(RuntimeType("LostCity.CombatSandbox.TeamMember"));
                Set(member, "team", Enum.ToObject(RuntimeType("LostCity.CombatSandbox.CombatTeam"), team));
            }
            var child = Root("Player collider");
            child.transform.SetParent(player.transform, false);
            var collider = child.AddComponent<CircleCollider2D>();
            Assert.That(player.GetComponent(RuntimeType("LostCity.CombatSandbox.Damageable")), Is.Null);

            Call(pickup, "OnTriggerEnter2D", collider);
            Assert.That(Field(pickup, "playerInRange"), Is.EqualTo(expected));
            Call(pickup, "OnTriggerExit2D", collider);
            Assert.That(Field(pickup, "playerInRange"), Is.False);
        }

        [Test]
        public void Room304Fallback_PickupPipeline_ArchivesOriginalIdsOnceAcrossInvestigations()
        {
            InstallRuntime();
            Call(session, "EndCase");
            Assert.That(Property(session, "HasActiveCase"), Is.False);
            Component progress = CreateProgress();
            var clues = RequiredClues(progress);
            string[] expectedIds = { "room304_patient_bracelet", "room304_visitor_log", "room304_apology_note" };
            Assert.That(clues.Length, Is.EqualTo(expectedIds.Length));
            Assert.That(Array.ConvertAll(clues, clue => (string)Property(clue, "Id")), Is.EquivalentTo(expectedIds));
            for (int i = 0; i < clues.Length; i++)
            {
                Component pickup = CreatePickup(clues[i], progress);
                Call(pickup, "Collect");
                Assert.That(pickup.gameObject.activeSelf, Is.False);
                Call(pickup, "Collect");
                Assert.That(Call(progress, "TryCollectClue", clues[i]), Is.False);
            }
            var alias = Track(UnityEngine.Object.Instantiate(clues[0]));
            Assert.That(Call(progress, "TryCollectClue", alias), Is.False, "Duplicate stable IDs are not new clues.");
            var unrelated = Track(ScriptableObject.CreateInstance(RuntimeType("LostCity.CombatSandbox.ClueDefinition")));
            Set(unrelated, "id", "unrelated_clue");
            Assert.That(Call(progress, "TryCollectClue", unrelated), Is.False);
            Component replay = CreateProgress();
            foreach (var clue in clues) Assert.That(Call(replay, "TryCollectClue", clue), Is.True);
            Assert.That(save.DiscoveredClueIds, Is.EquivalentTo(expectedIds));
            Assert.That(save.CompletedCaseIds, Is.Empty, "The sample must not complete or reward the case.");
            save.Reload();
            Assert.That(save.DiscoveredClueIds, Is.EquivalentTo(expectedIds));
            Assert.That(save.HasUnsavedChanges, Is.False);
        }

        [TestCase("runtime")]
        [TestCase("save")]
        [TestCase("scene")]
        public void ReturnToHub_MissingServices_ShowsChineseError(string missing)
        {
            if (missing != "runtime")
            {
                Component runtime = InstallRuntime();
                SetBacking(runtime, missing == "save" ? "SaveGame" : "SceneFlow", null);
            }
            Call(CreateController(), "ReturnToHub");
            Assert.That(Property(status, "text"), Is.EqualTo("未找到存档或场景服务，请检查场景配置。"));
        }

        [Test]
        public void ReturnToHub_ReadOnlySave_DoesNotEndCaseOrReplaceFutureSave()
        {
            InstallRuntime();
            File.WriteAllText(save.SavePath, "{\"Version\":999}");
            LogAssert.Expect(LogType.Error, new Regex(".*future save version.*"));
            save.Reload();
            Call(CreateController(), "ReturnToHub");
            Assert.That(Property(status, "text"), Does.Contain("存档只读"));
            Assert.That(Property(session, "HasActiveCase"), Is.True);
            Assert.That(Property(sceneFlow, "LastError"), Is.Empty);
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo("{\"Version\":999}"));
        }

        [Test]
        public void ReturnToHub_FailedDiscoveryWrite_BlocksLeavingThenRetriesBeforeSceneLoad()
        {
            InstallRuntime();
            Component controller = CreateController();
            string blocker = save.SavePath + ".tmp";
            Directory.CreateDirectory(blocker);
            LogAssert.Expect(LogType.Error, new Regex(".*Save write failed.*"));
            save.DiscoverClue("room304_patient_bracelet");
            LogAssert.Expect(LogType.Error, new Regex(".*Save write failed.*"));
            Call(controller, "ReturnToHub");
            Assert.That(save.HasUnsavedChanges, Is.True);
            Assert.That(Property(session, "HasActiveCase"), Is.True);
            Assert.That(Property(sceneFlow, "LastError"), Is.Empty);
            Assert.That(Property(status, "text"), Does.Contain("线索进度保存失败"));

            Directory.Delete(blocker);
            LogAssert.Expect(LogType.Error, new Regex("Scene load failed for '__Room304_test_missing_hub__'.*"));
            Call(controller, "ReturnToHub");
            Assert.That(save.HasUnsavedChanges, Is.False);
            Assert.That(JsonUtility.FromJson<SaveGameData>(File.ReadAllText(save.SavePath)).DiscoveredClueIds,
                Is.EqualTo(new[] { "room304_patient_bracelet" }));
            Assert.That(Property(session, "HasActiveCase"), Is.False);
            Assert.That(Property(status, "text"), Is.EqualTo("返回大厅失败，请检查场景配置后重试。"));
        }

        private Camera CreateCamera(bool tilted)
        {
            Camera camera = Root("Aim camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.pixelRect = new Rect(0f, 0f, 1200f, 800f);
            camera.transform.position = tilted ? new Vector3(3f, -16f, -20f) : new Vector3(3f, -2f, -20f);
            camera.transform.rotation = tilted
                ? Quaternion.LookRotation(new Vector3(0f, 16f, 20f), Vector3.up) : Quaternion.identity;
            return camera;
        }

        private static bool TryAim(Camera camera, Vector2 screen, float planeZ, out Vector3 point)
        {
            object[] arguments = { camera, screen, planeZ, null };
            bool result = (bool)RuntimeType("LostCity.CombatSandbox.PlayerAim")
                .GetMethod("TryGetAimPoint", BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
            point = (Vector3)arguments[3];
            return result;
        }

        private Component InstallRuntime()
        {
            directory = Path.Combine(Path.GetTempPath(), "LostCity-room304-" + Guid.NewGuid().ToString("N"));
            save = Root("Temporary save").AddComponent<SaveGameManager>();
            save.Initialize(Path.Combine(directory, "save.json"));
            var root = Root("Inactive runtime stub");
            root.SetActive(false);
            Component runtime = root.AddComponent(RuntimeType("LostCity.Meta.GameRuntime"));
            session = root.AddComponent(RuntimeType("LostCity.Meta.CaseSessionManager"));
            sceneFlow = root.AddComponent(RuntimeType("LostCity.Meta.SceneFlowManager"));
            SetBacking(session, "ActiveCase", LoadCase());
            Call(sceneFlow, "Initialize", session);
            Set(sceneFlow, "hubSceneName", "__Room304_test_missing_hub__");
            SetBacking(runtime, "SaveGame", save);
            SetBacking(runtime, "CaseSession", session);
            SetBacking(runtime, "SceneFlow", sceneFlow);
            RuntimeInstance.SetValue(null, runtime);
            return runtime;
        }

        private Component CreateProgress()
        {
            var root = Root("Investigation");
            root.SetActive(false);
            Component progress = root.AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(progress, "caseDefinition", LoadCase());
            Call(progress, "Awake");
            Assert.That(Property(progress, "CurrentCase"), Is.SameAs(LoadCase()));
            return progress;
        }

        private Component CreatePickup(ScriptableObject clue, Component progress)
        {
            var root = Root("Clue pickup");
            root.AddComponent<BoxCollider2D>();
            Component pickup = root.AddComponent(RuntimeType("LostCity.CombatSandbox.CluePickup"));
            Call(pickup, "Initialize", clue, progress);
            Call(pickup, "Awake");
            Assert.That(root.GetComponent<Collider2D>().isTrigger, Is.True);
            return pickup;
        }

        private Component CreateController()
        {
            Component controller = Root("Sample controller").AddComponent(RuntimeType("LostCity.Room304Sample.Room304SampleController"));
            var textRoot = Track(new GameObject("Status", typeof(RectTransform)));
            status = textRoot.AddComponent(Type.GetType("UnityEngine.UI.Text, UnityEngine.UI", true));
            Set(controller, "statusText", status);
            return controller;
        }

        private static ScriptableObject LoadCase()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/_Project/ScriptableObjects/CombatSandbox/Room304_CaseDefinition.asset");
            Assert.That(definition, Is.Not.Null);
            return definition;
        }

        private static ScriptableObject[] RequiredClues(Component progress)
        {
            var clues = new List<ScriptableObject>();
            foreach (var clue in (IEnumerable)Property(progress, "RequiredClues")) clues.Add((ScriptableObject)clue);
            return clues.ToArray();
        }

        private GameObject Root(string name) => Track(new GameObject(name));
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static FieldInfo RuntimeInstance => RuntimeType("LostCity.Meta.GameRuntime")
            .GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void SetBacking(object target, string name, object value) => Set(target, "<" + name + ">k__BackingField", value);
        private static object Call(object target, string name, params object[] arguments) => target.GetType()
            .GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, arguments);
    }
}
