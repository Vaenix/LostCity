using System;
using System.IO;
using System.Text.RegularExpressions;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LostCity.Meta.Core.Tests
{
    public sealed class SaveGameManagerTests
    {
        private string directory;
        private GameObject root;
        private SaveGameManager save;
        private string PathName => Path.Combine(directory, "save.json");

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "LostCity-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            root = new GameObject("Save test");
            save = root.AddComponent<SaveGameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            Directory.Delete(directory, true);
        }

        [Test]
        public void Mutations_ReloadEveryApprovedField_WithoutAliasingOrDuplicateWrites()
        {
            save.Initialize(PathName);
            int changes = 0;
            save.DataChanged += () => changes++;
            Assert.That(save.CompleteCase(" case_a "), Is.True);
            save.DiscoverClue("clue_a");
            save.EncounterEnemy("enemy_a");
            save.EncounterBoss("boss_a");
            save.AddPermanentUnlock("unlock_a");
            var progress = new PlayerProgressData
            {
                Level = 4, CurrentExperience = 3, ExperienceToNextLevel = 11, PendingUpgradeChoices = 2,
                AttackMultiplier = 1.2f, MaxHpMultiplier = 1.3f, FireRateMultiplier = 1.4f,
                CritChanceBonus = .1f, DodgeChanceBonus = .2f, DroneProjectileBonus = 2
            };
            string expectedProgressJson = JsonUtility.ToJson(progress);
            save.SetPlayerProgress(progress);
            string originalJson = File.ReadAllText(PathName);
            save.SetPlayerProgress(progress);
            Assert.That(save.CompleteCase("case_a"), Is.False);
            Assert.That(save.DiscoverClue("clue_a"), Is.False);
            Assert.That(save.EncounterEnemy("enemy_a"), Is.False);
            Assert.That(save.EncounterBoss("boss_a"), Is.False);
            Assert.That(save.AddPermanentUnlock("unlock_a"), Is.False);
            Assert.That(changes, Is.EqualTo(6));
            Assert.That(File.ReadAllText(PathName), Is.EqualTo(originalJson));
            progress.Level = 99;
            save.GetPlayerProgress().Level = 88;
            save.GetSnapshot().CompletedCaseIds.Clear();
            save.GetSnapshot().PlayerProgress.Level = 77;
            Assert.That(save.CompletedCaseIds, Is.EqualTo(new[] { "case_a" }));
            Assert.That(save.GetPlayerProgress().Level, Is.EqualTo(4));
            var fresh = root.AddComponent<SaveGameManager>();
            fresh.Initialize(PathName);
            Assert.That(fresh.CompletedCaseIds, Is.EqualTo(new[] { "case_a" }));
            Assert.That(fresh.DiscoveredClueIds, Is.EqualTo(new[] { "clue_a" }));
            Assert.That(fresh.EncounteredEnemyIds, Is.EqualTo(new[] { "enemy_a" }));
            Assert.That(fresh.EncounteredBossIds, Is.EqualTo(new[] { "boss_a" }));
            Assert.That(fresh.PermanentUnlockIds, Is.EqualTo(new[] { "unlock_a" }));
            Assert.That(JsonUtility.ToJson(fresh.GetPlayerProgress()),
                Is.EqualTo(expectedProgressJson));
            Assert.That(File.Exists(PathName + ".tmp"), Is.False);
        }

        [TestCase("{\"Version\":1,\"PlayerProgress\":{\"Level\":3}}")]
        [TestCase("{\"Version\":1,\"PlayerProgress\":{\"Level\":3,\"ExperienceToNextLevel\":0}}")]
        public void SparseJson_RepairsMissingXpThresholdThroughActualDeserializer(string json)
        {
            File.WriteAllText(PathName, json);
            save.Initialize(PathName);
            Assert.That(save.GetPlayerProgress().Level, Is.EqualTo(3));
            Assert.That(save.GetPlayerProgress().ExperienceToNextLevel, Is.EqualTo(5));
            Assert.That(save.GetPlayerProgress().PendingUpgradeChoices, Is.Zero);
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(1f));
            Assert.That(save.CompletedCaseIds, Is.Empty);
            save.SaveNow();
            save.Reload();
            Assert.That(save.GetPlayerProgress().ExperienceToNextLevel, Is.EqualTo(5));
        }

        [Test]
        public void CollectionReaders_ReturnCopies()
        {
            save.Initialize(PathName);
            save.CompleteCase("case_a");
            var view = save.CompletedCaseIds;
            save.CompleteCase("case_b");
            Assert.That(view.Count, Is.EqualTo(1), "Previously returned collections must not track later mutations.");
            if (view is string[] array) array[0] = "caller_mutated";
            else if (view is System.Collections.Generic.IList<string> list && !list.IsReadOnly) list[0] = "caller_mutated";
            Assert.That(save.IsCaseCompleted("case_a"), Is.True);
        }

        [Test]
        public void CorruptFile_IsPreservedBeforeCreatingDefault()
        {
            File.WriteAllText(PathName, "not JSON");
            LogAssert.Expect(LogType.Error, new Regex(".*Corrupt save.*"));
            save.Initialize(PathName);
            var backups = Directory.GetFiles(directory, "save.json.corrupt-*.json");
            Assert.That(backups.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(backups[0]), Is.EqualTo("not JSON"));
            Assert.That(save.CompletedCaseIds, Is.Empty);
            Assert.That(save.IsReadOnly, Is.False);
        }

        [Test]
        public void FutureVersion_IsReadOnlyAndNeverOverwritten_EvenAfterReload()
        {
            string json = "{\"Version\":999,\"CompletedCaseIds\":[\"future_case\"]}";
            File.WriteAllText(PathName, json);
            LogAssert.Expect(LogType.Error, new Regex(".*future save version.*"));
            save.Initialize(PathName);
            Assert.That(save.IsReadOnly, Is.True);
            Assert.That(save.CompleteCase("new_case"), Is.False);
            save.SetPlayerProgress(new PlayerProgressData { Level = 9 });
            save.SaveNow();
            LogAssert.Expect(LogType.Error, new Regex(".*future save version.*"));
            save.Reload();
            Assert.That(File.ReadAllText(PathName), Is.EqualTo(json));
            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_IsIdempotent_AndDoesNotSwitchPathsOrDropProgress()
        {
            save.Initialize(PathName);
            save.CompleteCase("case_a");
            save.Initialize(Path.Combine(directory, "other.json"));
            Assert.That(save.SavePath, Is.EqualTo(PathName));
            Assert.That(save.IsCaseCompleted("case_a"), Is.True);
            Assert.That(File.Exists(Path.Combine(directory, "other.json")), Is.False);
        }

        [Test]
        public void UnreadableSavePath_BecomesReadOnlyWithoutDestroyingExistingData()
        {
            Directory.CreateDirectory(PathName);
            LogAssert.Expect(LogType.Error, new Regex(".*Save load failed.*"));
            save.Initialize(PathName);
            Assert.That(save.IsReadOnly, Is.True);
            Assert.That(save.LastError, Is.Not.Empty);
            Assert.That(save.CompleteCase("case_a"), Is.False);
            Assert.That(Directory.Exists(PathName), Is.True);
        }

        [Test]
        public void FutureFileAppearingAfterInitialize_BlocksPendingWrite()
        {
            save.Initialize(PathName);
            string json = "{\"Version\":999}";
            File.WriteAllText(PathName, json);
            File.WriteAllText(PathName + ".tmp", "future temporary data");
            LogAssert.Expect(LogType.Error, new Regex(".*future save version.*"));
            save.CompleteCase("case_a");
            Assert.That(save.IsReadOnly, Is.True);
            Assert.That(File.ReadAllText(PathName), Is.EqualTo(json));
            Assert.That(File.ReadAllText(PathName + ".tmp"), Is.EqualTo("future temporary data"));
        }

        [Test]
        public void FailedWrite_KeepsOldFileAndDirtyMemory_ForExplicitRetry()
        {
            save.Initialize(PathName);
            string oldJson = File.ReadAllText(PathName);
            Directory.CreateDirectory(PathName + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex(".*Save write failed.*"));
            save.DiscoverClue("pending");
            Assert.That(File.ReadAllText(PathName), Is.EqualTo(oldJson));
            Assert.That(save.HasUnsavedChanges, Is.True);
            Assert.That(save.LastError, Is.Not.Empty);
            Assert.That(save.DiscoveredClueIds, Does.Contain("pending"));
            Directory.Delete(PathName + ".tmp");
            save.SaveNow();
            Assert.That(save.HasUnsavedChanges, Is.False);
            save.Reload();
            Assert.That(save.DiscoveredClueIds, Does.Contain("pending"));
        }
    }
}
