using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LostCity.Meta.Core.Tests
{
    // An asmdef cannot reference predefined Assembly-CSharp. Resolve only the Unity-facing services by reflection.
    public sealed class MetaRuntimeTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private GameObject root;
        private SaveGameManager save;
        private Component registry;
        private string directory;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "LostCity-registry-" + Guid.NewGuid().ToString("N"));
            root = Track(new GameObject("Registry test"));
            save = root.AddComponent<SaveGameManager>();
            save.Initialize(Path.Combine(directory, "save.json"));
            registry = root.AddComponent(RuntimeType("LostCity.Meta.CaseRegistry"));
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void ChapterOrdering_DrivesUnlocking_AndUnassignedCasesAreDiscovered()
        {
            var a = Playable("case_z");
            var b = Playable("case_a");
            var loose = Playable("unassigned");
            var first = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "chapter_1");
            Set(first, "cases", new[] { a, b });
            var second = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "chapter_2");
            var c = Playable("case_c");
            Set(second, "cases", new[] { c });
            Set(second, "requiredCompletedCaseIds", new[] { "case_a" });
            Initialize(Catalog(new[] { b, c, loose, a }, new[] { first, second }));

            Assert.That(Items(Property(registry, "Cases")), Is.EqualTo(new[] { a, b, c, loose }));
            Assert.That(State(a), Is.EqualTo(CaseState.Available));
            Assert.That(State(b), Is.EqualTo(CaseState.Locked));
            Assert.That(State(c), Is.EqualTo(CaseState.Locked));
            Assert.That(State(loose), Is.EqualTo(CaseState.Available));
            Assert.That(Call(registry, "GetChapterForCase", loose), Is.Null);
            Assert.That(Call(registry, "GetCurrentChapter"), Is.SameAs(first));
            save.CompleteCase("case_z");
            Assert.That(State(a), Is.EqualTo(CaseState.Completed));
            Assert.That(State(b), Is.EqualTo(CaseState.Available));
            save.CompleteCase("case_a");
            Assert.That(State(c), Is.EqualTo(CaseState.Available));
            Assert.That(Call(registry, "GetCurrentChapter"), Is.SameAs(second));
            Assert.That(CanStart(a), Is.True, "Completed cases must remain replayable.");
            var entries = Items(Call(registry, "GetEntries"));
            Assert.That(Property(entries[0], "Definition"), Is.SameAs(a));
            Assert.That(Property(entries[0], "State"), Is.EqualTo(CaseState.Completed));
        }

        [TestCase("LostCity.CombatSandbox.CaseDefinition", "caseId", "cases", "GetCase")]
        [TestCase("LostCity.Meta.ChapterDefinition", "chapterId", "chapters", null)]
        [TestCase("LostCity.CombatSandbox.ClueDefinition", "id", "clues", "GetClue")]
        [TestCase("LostCity.CombatSandbox.EnemyDefinition", "enemyId", "enemies", "GetEnemy")]
        [TestCase("LostCity.CombatSandbox.BossDefinition", "bossId", "bosses", "GetBoss")]
        public void DuplicateIds_ExcludeEveryAmbiguousDefinition(string type, string field, string collection, string getter)
        {
            var a = Definition(type, field, "duplicate");
            var b = Definition(type, field, "duplicate");
            var catalog = Definition("LostCity.Meta.GameContentCatalog");
            Set(catalog, collection, new[] { a, b });
            LogAssert.Expect(LogType.Error, new Regex("Duplicate .* ID 'duplicate'.*"));
            LogAssert.Expect(LogType.Error, new Regex("Duplicate .* ID 'duplicate'.*"));
            if (collection == "chapters")
            {
                LogAssert.Expect(LogType.Error, new Regex("Chapter .*invalid or ambiguous.*"));
                LogAssert.Expect(LogType.Error, new Regex("Chapter .*invalid or ambiguous.*"));
            }
            Initialize(catalog);
            Assert.That(Items(Property(registry, char.ToUpperInvariant(collection[0]) + collection.Substring(1))), Is.Empty);
            if (getter != null) Assert.That(Call(registry, getter, "duplicate"), Is.Null);
        }

        [Test]
        public void InvalidIds_AndForeignDefinitionsCannotStart()
        {
            var a = Playable("good");
            var invalid = Playable("invalid_case");
            Set(invalid, "caseId", " ");
            LogAssert.Expect(LogType.Error, new Regex("Invalid CaseDefinition ID.*"));
            Initialize(Catalog(new[] { a, invalid }));
            Assert.That(CanStart(invalid), Is.False);
            var impostor = Playable("good");
            Assert.That(CanStart(impostor), Is.False);
            Assert.That(CanStart(null), Is.False);
        }

        [Test]
        public void Placeholder_UnlocksWithoutBecomingPlayable()
        {
            var a = Playable("case_a");
            var placeholder = Definition("LostCity.CombatSandbox.CaseDefinition", "caseId", "pending");
            Set(placeholder, "isPlayable", false);
            var chapter = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "chapter");
            Set(chapter, "cases", new[] { a, placeholder });
            Initialize(Catalog(new[] { a, placeholder }, new[] { chapter }));
            Assert.That(State(placeholder), Is.EqualTo(CaseState.Locked));
            save.CompleteCase("case_a");
            Assert.That(State(placeholder), Is.EqualTo(CaseState.Available));
            Assert.That(CanStart(placeholder), Is.False);
        }

        [TestCase("nullClue")]
        [TestCase("emptyClues")]
        [TestCase("duplicateClue")]
        [TestCase("unregisteredClue")]
        [TestCase("blankClueId")]
        [TestCase("missingBoss")]
        [TestCase("missingPrefab")]
        [TestCase("missingDamageable")]
        [TestCase("missingQuestion")]
        [TestCase("missingAnswer")]
        [TestCase("missingReward")]
        public void CanStart_RejectsIncompletePlayableContent(string defect)
        {
            var definition = Playable("case");
            var catalog = Catalog(new[] { definition });
            Initialize(catalog);
            Assert.That(CanStart(definition), Is.True);
            var clue = (ScriptableObject)Items(Property(definition, "Clues"))[0];
            var boss = (ScriptableObject)Property(definition, "BossDefinition");
            switch (defect)
            {
                case "nullClue": Set(definition, "clues", new ScriptableObject[] { null }); break;
                case "emptyClues": Set(definition, "clues", new ScriptableObject[0]); break;
                case "duplicateClue": Set(definition, "clues", new[] { clue, clue }); break;
                case "unregisteredClue":
                    Set(definition, "clues", new[] { Definition("LostCity.CombatSandbox.ClueDefinition", "id", "other") }); break;
                case "blankClueId": Set(clue, "id", ""); break;
                case "missingBoss": Set(definition, "bossDefinition", null); break;
                case "missingPrefab": Set(boss, "bossPrefab", null); break;
                case "missingDamageable": Set(boss, "bossPrefab", Track(new GameObject("No damageable"))); break;
                case "missingQuestion": Set(definition, "deductionQuestion", ""); break;
                case "missingAnswer": Set(definition, "correctAnswer", ""); break;
                case "missingReward": Set(definition, "rewardPool", new ScriptableObject[0]); break;
            }
            Assert.That(CanStart(definition), Is.False, defect);
        }

        [Test]
        public void CompletionRewardResolution_FiltersBeforeDisplay_AndFallsBackOnlyWhenNoValidCaseReward()
        {
            var definition = Playable("case");
            var reward = (ScriptableObject)Items(Property(definition, "RewardPool"))[0];
            var invalid = Definition("LostCity.CombatSandbox.RewardDefinition");
            Set(definition, "rewardPool", new[] { null, invalid, null, reward });
            Assert.That(Items(Call(definition, "GetCompletionRewards")), Is.EqualTo(new[] { reward }));
            var boss = (ScriptableObject)Property(definition, "BossDefinition");
            Set(boss, "rewardPool", new[] { null, reward });
            Set(definition, "rewardPool", new[] { invalid, null });
            Assert.That(Items(Call(definition, "GetCompletionRewards")), Is.EqualTo(new[] { reward }));
            Initialize(Catalog(new[] { definition }));
            Assert.That(CanStart(definition), Is.True);
        }

        [Test]
        public void DuplicateChapterMembership_IsNotTreatedAsAnUnlockedUnassignedCase()
        {
            var a = Playable("case");
            var first = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "one");
            var second = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "two");
            Set(first, "cases", new[] { a });
            Set(second, "cases", new[] { a });
            LogAssert.Expect(LogType.Error, new Regex("Case 'case' has multiple chapter references.*"));
            Initialize(Catalog(new[] { a }, new[] { first, second }));
            Assert.That(CanStart(a), Is.False);
            Assert.That(State(a), Is.EqualTo(CaseState.Locked));
        }

        [Test]
        public void DuplicateCaseReferencesInChapter_BlockAllAffectedCasesRatherThanSkipThePredecessor()
        {
            var a = Playable("case_a");
            var b = Playable("case_b");
            var chapter = Definition("LostCity.Meta.ChapterDefinition", "chapterId", "chapter");
            Set(chapter, "cases", new[] { a, a, b });
            LogAssert.Expect(LogType.Error, new Regex("Chapter .*invalid or ambiguous.*"));
            LogAssert.Expect(LogType.Error, new Regex("Case 'case_a' has multiple chapter references.*"));
            Initialize(Catalog(new[] { a, b }, new[] { chapter }));
            save.CompleteCase("case_a");
            Assert.That(CanStart(a), Is.False);
            Assert.That(CanStart(b), Is.False);
            Assert.That(Call(registry, "GetCurrentChapter"), Is.Null);
        }

        [Test]
        public void SceneLoadFailure_ClearsSessionAndRestoresTimeScale()
        {
            var a = Playable("case");
            Initialize(Catalog(new[] { a }));
            var session = root.AddComponent(RuntimeType("LostCity.Meta.CaseSessionManager"));
            Call(session, "Initialize", registry);
            Assert.That(Call(session, "BeginCase", a), Is.True);
            Assert.That(Call(session, "BeginCase", a), Is.False);
            Call(session, "EndCase");
            var flow = root.AddComponent(RuntimeType("LostCity.Meta.SceneFlowManager"));
            Call(flow, "Initialize", session);
            Set(flow, "investigationSceneName", "__LostCity_nonexistent_scene__");
            Time.timeScale = 0f;
            LogAssert.Expect(LogType.Error, new Regex("Scene load failed for '__LostCity_nonexistent_scene__'.*"));
            Assert.That(Call(flow, "StartCase", a), Is.False);
            Assert.That(Property(session, "HasActiveCase"), Is.False);
            Assert.That(Property(session, "ActiveCaseId"), Is.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Property(flow, "IsLoading"), Is.False);
            Assert.That(Call(session, "BeginCase", a), Is.True);
            Set(flow, "hubSceneName", "__LostCity_missing_hub__");
            Time.timeScale = 0f;
            LogAssert.Expect(LogType.Error, new Regex("Scene load failed for '__LostCity_missing_hub__'.*"));
            Call(flow, "LoadHub");
            Assert.That(Property(session, "HasActiveCase"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DefinitionArrayProperties_TolerateNullSerializedFields_AndReturnCopies()
        {
            var catalog = Definition("LostCity.Meta.GameContentCatalog");
            foreach (string field in new[] { "cases", "chapters", "clues", "enemies", "bosses" })
            {
                catalog.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(catalog, null);
                Assert.That(Items(Property(catalog, char.ToUpperInvariant(field[0]) + field.Substring(1))), Is.Empty);
            }
            var chapter = Definition("LostCity.Meta.ChapterDefinition");
            chapter.GetType().GetField("cases", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chapter, null);
            chapter.GetType().GetField("requiredCompletedCaseIds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chapter, null);
            Assert.That(Items(Property(chapter, "Cases")), Is.Empty);
            Assert.That(Items(Property(chapter, "RequiredCompletedCaseIds")), Is.Empty);
            var a = Playable("case");
            Set(chapter, "cases", new[] { a });
            ((Array)Property(chapter, "Cases")).SetValue(null, 0);
            Assert.That(Items(Property(chapter, "Cases")), Is.EqualTo(new[] { a }));
        }

        [Test]
        public void CatalogDiscovery_FindsEveryAssetType_InStablePathOrder()
        {
            var builder = Type.GetType("LostCity.CombatSandbox.EditorTools.GameContentCatalogBuilder, Assembly-CSharp-Editor", true);
            foreach (string name in new[] { "LostCity.CombatSandbox.CaseDefinition", "LostCity.Meta.ChapterDefinition",
                "LostCity.CombatSandbox.ClueDefinition", "LostCity.CombatSandbox.EnemyDefinition", "LostCity.CombatSandbox.BossDefinition" })
            {
                var type = RuntimeType(name);
                var found = Items(builder.GetMethod("Find", BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(type).Invoke(null, null)).Cast<UnityEngine.Object>().ToArray();
                var paths = found.Select(AssetDatabase.GetAssetPath).ToArray();
                Assert.That(paths, Is.EqualTo(paths.OrderBy(path => path, StringComparer.Ordinal).ToArray()));
                var expected = AssetDatabase.FindAssets("t:" + type.Name).Select(AssetDatabase.GUIDToAssetPath).Distinct()
                    .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path)).Where(type.IsInstanceOfType).ToArray();
                Assert.That(found, Is.EquivalentTo(expected));
            }
        }

        private ScriptableObject Playable(string id)
        {
            var definition = Definition("LostCity.CombatSandbox.CaseDefinition", "caseId", id);
            var clue = Definition("LostCity.CombatSandbox.ClueDefinition", "id", "clue_" + id);
            var boss = Definition("LostCity.CombatSandbox.BossDefinition", "bossId", "boss_" + id);
            var prefab = Track(new GameObject("Boss"));
            prefab.AddComponent(RuntimeType("LostCity.CombatSandbox.Damageable"));
            Set(boss, "bossPrefab", prefab);
            var reward = Definition("LostCity.CombatSandbox.RewardDefinition");
            Set(reward, "statType", 1);
            Set(definition, "isPlayable", true);
            Set(definition, "clues", new[] { clue });
            Set(definition, "bossDefinition", boss);
            Set(definition, "rewardPool", new[] { reward });
            Set(definition, "deductionQuestion", "Question");
            Set(definition, "correctAnswer", "Answer");
            return definition;
        }

        private ScriptableObject Catalog(ScriptableObject[] definitions, ScriptableObject[] chapters = null)
        {
            var catalog = Definition("LostCity.Meta.GameContentCatalog");
            Set(catalog, "cases", definitions);
            Set(catalog, "chapters", chapters ?? new ScriptableObject[0]);
            Set(catalog, "clues", definitions.SelectMany(value => Items(Property(value, "Clues")))
                .OfType<ScriptableObject>().Distinct().ToArray());
            Set(catalog, "bosses", definitions.Select(value => Property(value, "BossDefinition"))
                .OfType<ScriptableObject>().Distinct().ToArray());
            return catalog;
        }

        private ScriptableObject Definition(string type, string field = null, string id = null)
        {
            var definition = Track(ScriptableObject.CreateInstance(RuntimeType(type)));
            if (field != null) Set(definition, field, id);
            return definition;
        }

        private void Initialize(ScriptableObject catalog) => Call(registry, "Initialize", catalog, save);
        private CaseState State(ScriptableObject definition) => (CaseState)Call(registry, "GetCaseState", definition);
        private bool CanStart(ScriptableObject definition)
        {
            object[] args = { definition, null };
            return (bool)registry.GetType().GetMethod("CanStart").Invoke(registry, args);
        }
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object[] Items(object value) => value == null ? new object[0] : ((IEnumerable)value).Cast<object>().ToArray();

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (value is string text) property.stringValue = text;
            else if (value is bool flag) property.boolValue = flag;
            else if (value is int number) property.intValue = number;
            else if (value is Array array)
            {
                property.arraySize = array.Length;
                for (int i = 0; i < array.Length; i++)
                {
                    var item = property.GetArrayElementAtIndex(i);
                    if (array.GetValue(i) is string textItem) item.stringValue = textItem;
                    else item.objectReferenceValue = array.GetValue(i) as UnityEngine.Object;
                }
            }
            else property.objectReferenceValue = value as UnityEngine.Object;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
