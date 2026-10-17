using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace LostCity.Meta.Core.Tests
{
    // Keep the existing asmdef: predefined runtime and UI assemblies are accessed by reflection.
    public sealed class MetaHubUITests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private string directory;
        private SaveGameManager save;
        private Component registry;

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            directory = null;
            save = null;
            registry = null;
        }

        [Test]
        public void Row_RepeatedBind_ReplacesSelectionWithoutDuplicatingCallbacks()
        {
            Component row = Row(out Component button);
            ScriptableObject first = Case("first_case", true);
            ScriptableObject latest = Case("latest_case", true);
            var oldSelection = new SelectionSink();
            var currentSelection = new SelectionSink();
            Bind(row, Entry(first, CaseState.Available, true), oldSelection);
            for (int i = 0; i < 3; i++) Bind(row, Entry(latest, CaseState.Completed, true), currentSelection);

            Click(button);

            Assert.That(oldSelection.Values, Is.Empty, "Rebinding must release the previous selection callback.");
            Assert.That(currentSelection.Values, Is.EqualTo(new[] { latest }), "One click must select the latest case exactly once.");
            Assert.That(TextValue(Field(row, "startButtonText")), Is.EqualTo("重新调查"));
        }

        [Test]
        public void Row_DisableEnableCallbacks_UnsubscribeAndRestoreExactlyOneSelection()
        {
            Component row = Row(out Component button);
            ScriptableObject definition = Case("lifecycle_case", true);
            var selection = new SelectionSink();
            Bind(row, Entry(definition, CaseState.Available, true), selection);
            Click(button);
            Assert.That(selection.Values.Count, Is.EqualTo(1));

            for (int i = 0; i < 3; i++)
            {
                row.gameObject.SetActive(false);
                // EditMode has no player loop; drive the handlers explicitly, using the real UnityEvent.
                Call(row, "OnDisable");
                Click(button);
                Assert.That(selection.Values.Count, Is.EqualTo(i + 1), "Disabled rows must release their listener.");
                row.gameObject.SetActive(true);
                Call(row, "OnEnable");
                Click(button);
                Assert.That(selection.Values.Count, Is.EqualTo(i + 2), "Re-enabling must not accumulate listeners.");
            }
        }

        [TestCase(CaseState.Locked, true)]
        [TestCase(CaseState.Available, false)]
        [TestCase(CaseState.Completed, false)]
        public void Row_LockedOrMetadataOnlyCases_CannotBeSelected(CaseState state, bool playable)
        {
            Component row = Row(out Component button);
            var selection = new SelectionSink();
            Bind(row, Entry(Case("blocked_case", playable), state, false), selection);

            Assert.That(Property(button, "interactable"), Is.False);
            Click(button); // UnityEvent.Invoke bypasses Button's interactable check; the row must still reject it.

            Assert.That(selection.Values, Is.Empty);
            if (!playable) Assert.That(TextValue(Field(row, "startButtonText")), Is.EqualTo("开发中"));
            if (state == CaseState.Locked) Assert.That(TextValue(Field(row, "stateText")), Does.Contain("未解锁"));
        }

        [Test]
        public void Row_SaveBlocked_DoesNotSelectUntilSavingIsAllowed()
        {
            Component row = Row(out Component button);
            ScriptableObject definition = Case("save_guard_case", true);
            var selection = new SelectionSink();
            Bind(row, Entry(definition, CaseState.Available, true), selection);
            Call(row, "SetSaveAllowed", false);
            Assert.That(Property(button, "interactable"), Is.False);
            Click(button);
            Assert.That(selection.Values, Is.Empty);

            Call(row, "SetSaveAllowed", true);
            Assert.That(Property(button, "interactable"), Is.True);
            Click(button);
            Assert.That(selection.Values, Is.EqualTo(new[] { definition }));
        }

        [Test]
        public void Hub_RetryWriteFailure_IsExplicitAndPreservesPendingProgress()
        {
            InitializeRegistry(Definition("LostCity.Meta.GameContentCatalog"));
            Component hub = Hub(out Component retry);
            Assert.That(retry.gameObject.activeSelf, Is.False, "Healthy storage must not show a retry button.");
            Directory.CreateDirectory(save.SavePath + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex("Save write failed.*"));
            save.DiscoverClue("pending_clue_marker");
            Call(hub, "Refresh");
            Assert.That(save.IsReadOnly, Is.False);
            Assert.That(save.HasUnsavedChanges, Is.True);
            Assert.That(retry.gameObject.activeSelf, Is.True);
            Assert.That(Property(retry, "interactable"), Is.True, "Recovery must remain clickable while case starts are blocked.");
            Assert.That(Property(Field(hub, "view"), "CanSave"), Is.False);

            Directory.Delete(save.SavePath + ".tmp");
            Call(hub, "Refresh");
            Call(hub, "Refresh");
            Assert.That(save.HasUnsavedChanges, Is.True, "Refreshing the UI must not retry IO automatically.");
            Assert.That(File.ReadAllText(save.SavePath), Does.Not.Contain("pending_clue_marker"));
            int reloadNotifications = 0;
            save.DataChanged += () => reloadNotifications++;

            Click(retry);

            Assert.That(save.HasUnsavedChanges, Is.False);
            Assert.That(save.LastError, Is.Empty);
            Assert.That(File.ReadAllText(save.SavePath), Does.Contain("pending_clue_marker"));
            Assert.That(reloadNotifications, Is.Zero, "Writable retry must call SaveNow, not Reload.");
            Assert.That(retry.gameObject.activeSelf, Is.False);
            Assert.That(Property(Field(hub, "view"), "CanSave"), Is.True);
            Assert.That(TextValue(Field(hub, "statusText")), Does.Not.Contain("异常"));
        }

        [Test]
        public void Hub_RetryReadFailure_ReloadsAfterStorageIsRepaired()
        {
            InitializeRegistry(Definition("LostCity.Meta.GameContentCatalog"));
            Component hub = Hub(out Component retry);
            File.Move(save.SavePath, save.SavePath + ".original");
            Directory.CreateDirectory(save.SavePath);
            LogAssert.Expect(LogType.Error, new Regex("Save load failed; storage is read-only.*"));
            save.Reload();
            Call(hub, "Refresh");
            Assert.That(save.IsReadOnly, Is.True);
            Assert.That(retry.gameObject.activeSelf, Is.True);
            Assert.That(Property(retry, "interactable"), Is.True);

            Directory.Delete(save.SavePath);
            var repaired = SaveGameData.CreateDefault();
            repaired.CompletedCaseIds.Add("reloaded_case_marker");
            File.WriteAllText(save.SavePath, JsonUtility.ToJson(repaired));
            Call(hub, "Refresh");
            Assert.That(save.IsReadOnly, Is.True, "UI refresh must not reload without an explicit click.");

            Click(retry);

            Assert.That(save.IsReadOnly, Is.False);
            Assert.That(save.LastError, Is.Empty);
            Assert.That(save.IsCaseCompleted("reloaded_case_marker"), Is.True);
            Assert.That(retry.gameObject.activeSelf, Is.False);
            Assert.That(Property(Field(hub, "view"), "CanSave"), Is.True);
            Assert.That(TextValue(Field(hub, "statusText")), Does.Not.Contain("只读"));
        }

        [Test]
        public void Hub_RetryFutureVersion_KeepsFileProtectedAndRetryAvailable()
        {
            InitializeRegistry(Definition("LostCity.Meta.GameContentCatalog"));
            Component hub = Hub(out Component retry);
            var future = SaveGameData.CreateDefault();
            future.Version = SaveGameData.CurrentVersion + 1;
            string original = JsonUtility.ToJson(future);
            File.WriteAllText(save.SavePath, original);
            LogAssert.Expect(LogType.Error, new Regex("Unsupported future save version.*"));
            save.Reload();
            Call(hub, "Refresh");
            Assert.That(retry.gameObject.activeSelf, Is.True);

            LogAssert.Expect(LogType.Error, new Regex("Unsupported future save version.*"));
            Click(retry);

            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo(original));
            Assert.That(save.IsReadOnly, Is.True);
            Assert.That(retry.gameObject.activeSelf, Is.True);
            Assert.That(Property(retry, "interactable"), Is.True);
            Assert.That(Property(Field(hub, "view"), "CanSave"), Is.False);
            Assert.That(TextValue(Field(hub, "statusText")), Does.Contain("只读"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Archive_ShowsDiscoveredCluesForIncompleteAndCompletedCases_WithoutLeakingUndiscoveredContent(bool completed)
        {
            ScriptableObject known = Clue("known_clue", "known");
            ScriptableObject hidden = Clue("hidden_clue", "hidden");
            ScriptableObject definition = Case("archive_case", true);
            Set(definition, "caseName", "archive_case_name");
            Set(definition, "caseSummary", "completion_summary_marker");
            Set(definition, "clues", new[] { known, hidden });
            ScriptableObject catalog = Definition("LostCity.Meta.GameContentCatalog");
            Set(catalog, "cases", new[] { definition });
            Set(catalog, "clues", new[] { known, hidden });
            InitializeRegistry(catalog);
            Component archive = DocumentView("LostCity.Meta.InvestigationArchiveUI", out Component document);

            Call(archive, "Refresh");
            Assert.That(TextValue(document), Does.Not.Contain("known_name_marker"));
            save.DiscoverClue("known_clue");
            if (completed) save.CompleteCase("archive_case");
            Call(archive, "Refresh");
            string text = TextValue(document);

            Assert.That(text, Does.Contain("archive_case_name"));
            foreach (string field in new[] { "name", "category", "journal" })
            {
                Assert.That(text, Does.Contain("known_" + field + "_marker"));
                Assert.That(text, Does.Not.Contain("hidden_" + field + "_marker"));
            }
            if (completed) Assert.That(text, Does.Contain("completion_summary_marker"));
            else
            {
                Assert.That(text, Does.Contain("调查中的已发现线索"));
                Assert.That(text, Does.Not.Contain("completion_summary_marker"), "Incomplete cases must not expose completion summaries.");
                Assert.That(save.IsCaseCompleted("archive_case"), Is.False);
            }
        }

        [Test]
        public void Bestiary_UnencounteredEntriesHideNamesAndDescriptions_AndEncounterCategoriesStaySeparate()
        {
            ScriptableObject enemy = Definition("LostCity.CombatSandbox.EnemyDefinition");
            Set(enemy, "enemyId", "shared_encounter_id");
            Set(enemy, "displayName", "enemy_name_marker");
            Set(enemy, "bestiaryDescription", "enemy_description_marker");
            ScriptableObject boss = Definition("LostCity.CombatSandbox.BossDefinition");
            Set(boss, "bossId", "shared_encounter_id");
            Set(boss, "bossName", "boss_name_marker");
            Set(boss, "bestiaryDescription", "boss_description_marker");
            ScriptableObject catalog = Definition("LostCity.Meta.GameContentCatalog");
            Set(catalog, "enemies", new[] { enemy });
            Set(catalog, "bosses", new[] { boss });
            InitializeRegistry(catalog);
            Component bestiary = DocumentView("LostCity.Meta.BestiaryUI", out Component document);

            Call(bestiary, "Refresh");
            string text = TextValue(document);
            Assert.That(text.Split(new[] { "未知记录" }, StringSplitOptions.None).Length - 1, Is.EqualTo(2));
            foreach (string marker in new[] { "enemy_name_marker", "enemy_description_marker", "boss_name_marker", "boss_description_marker" })
                Assert.That(text, Does.Not.Contain(marker));

            save.EncounterEnemy("shared_encounter_id");
            Call(bestiary, "Refresh");
            text = TextValue(document);
            Assert.That(text, Does.Contain("enemy_name_marker").And.Contain("enemy_description_marker"));
            Assert.That(text, Does.Not.Contain("boss_name_marker").And.Not.Contain("boss_description_marker"));
            Assert.That(text, Does.Contain("未知记录"));

            save.EncounterBoss("shared_encounter_id");
            Call(bestiary, "Refresh");
            text = TextValue(document);
            Assert.That(text, Does.Contain("boss_name_marker").And.Contain("boss_description_marker"));
            Assert.That(text, Does.Not.Contain("未知记录"));
        }

        private Component Row(out Component button)
        {
            GameObject root = UiObject("Row");
            root.SetActive(false);
            Component row = root.AddComponent(RuntimeType("LostCity.Meta.HubCaseRow"));
            foreach (string field in new[] { "caseNameText", "descriptionText", "stateText", "startButtonText" })
                Set(row, field, UiObject(field, root.transform).AddComponent(UiType("Text")));
            button = UiObject("Start", root.transform).AddComponent(UiType("Button"));
            Set(row, "startButton", button);
            root.SetActive(true);
            return row;
        }

        private void InitializeRegistry(ScriptableObject catalog)
        {
            directory = Path.Combine(Path.GetTempPath(), "LostCity-hub-ui-" + Guid.NewGuid().ToString("N"));
            GameObject root = Track(new GameObject("UI test services"));
            save = root.AddComponent<SaveGameManager>();
            save.Initialize(Path.Combine(directory, "save.json"));
            registry = root.AddComponent(RuntimeType("LostCity.Meta.CaseRegistry"));
            Call(registry, "Initialize", catalog, save);
        }

        private Component DocumentView(string type, out Component document)
        {
            GameObject root = UiObject("Document panel");
            root.SetActive(false);
            Component view = root.AddComponent(RuntimeType(type));
            document = UiObject("Document", root.transform).AddComponent(UiType("Text"));
            Set(view, "documentText", document);
            InjectSupport(view);
            return view;
        }

        private Component Hub(out Component retry)
        {
            GameObject root = UiObject("Hub panel");
            root.SetActive(false);
            Component hub = root.AddComponent(RuntimeType("LostCity.Meta.HubController"));
            retry = UiObject("Retry", root.transform).AddComponent(UiType("Button"));
            Set(hub, "retrySaveButton", retry);
            Set(hub, "statusText", UiObject("Status", root.transform).AddComponent(UiType("Text")));
            Set(hub, "caseListRoot", UiObject("Rows", root.transform).transform);
            Component template = Row(out _);
            template.gameObject.SetActive(false);
            Set(hub, "rowTemplate", template);
            Call(hub, "OnEnable");
            InjectSupport(hub);
            Call(hub, "Refresh");
            return hub;
        }

        private void InjectSupport(Component view)
        {
            // Inject only view dependencies, without bootstrap, persistent singletons, or a scene/player loop.
            (Field(view, "view") as IDisposable)?.Dispose();
            object support = Activator.CreateInstance(RuntimeType("LostCity.Meta.HubViewSupport"), new object[] { (Action)(() => { }) });
            support.GetType().GetProperty("Save").GetSetMethod(true).Invoke(support, new object[] { save });
            support.GetType().GetProperty("Registry").GetSetMethod(true).Invoke(support, new object[] { registry });
            Set(view, "view", support);
        }

        private ScriptableObject Case(string id, bool playable)
        {
            ScriptableObject definition = Definition("LostCity.CombatSandbox.CaseDefinition");
            Set(definition, "caseId", id);
            Set(definition, "isPlayable", playable);
            return definition;
        }

        private ScriptableObject Clue(string id, string marker)
        {
            ScriptableObject definition = Definition("LostCity.CombatSandbox.ClueDefinition");
            Set(definition, "id", id);
            Set(definition, "clueName", marker + "_name_marker");
            Set(definition, "category", marker + "_category_marker");
            Set(definition, "journalText", marker + "_journal_marker");
            return definition;
        }

        private static object Entry(ScriptableObject definition, CaseState state, bool canStart) =>
            Activator.CreateInstance(RuntimeType("LostCity.Meta.CaseRegistryEntry"),
                new object[] { definition, null, state, canStart, canStart ? string.Empty : "案件不可进入" });

        private static void Bind(Component row, object entry, SelectionSink sink)
        {
            MethodInfo bind = row.GetType().GetMethod("Bind");
            Type callbackType = bind.GetParameters()[1].ParameterType;
            Delegate callback = Delegate.CreateDelegate(callbackType, sink, typeof(SelectionSink).GetMethod("Select"));
            bind.Invoke(row, new object[] { entry, callback });
        }

        private sealed class SelectionSink
        {
            public readonly List<UnityEngine.Object> Values = new List<UnityEngine.Object>();
            public void Select(UnityEngine.Object definition) { Values.Add(definition); }
        }

        private GameObject UiObject(string name, Transform parent = null)
        {
            GameObject root = Track(new GameObject(name, typeof(RectTransform)));
            if (parent != null) root.transform.SetParent(parent, false);
            return root;
        }
        private ScriptableObject Definition(string name) => Track(ScriptableObject.CreateInstance(RuntimeType(name)));
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static Type UiType(string name) => Type.GetType("UnityEngine.UI." + name + ", UnityEngine.UI", true);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static string TextValue(object text) => (string)Property(text, "text");
        private static void Click(Component button) => ((UnityEvent)Property(button, "onClick")).Invoke();
        private static object Call(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, target.GetType().Name + "." + name);
            if (value is Array source && field.FieldType.IsArray)
            {
                Array typed = Array.CreateInstance(field.FieldType.GetElementType(), source.Length);
                for (int i = 0; i < source.Length; i++) typed.SetValue(source.GetValue(i), i);
                value = typed;
            }
            field.SetValue(target, value);
        }
    }
}
