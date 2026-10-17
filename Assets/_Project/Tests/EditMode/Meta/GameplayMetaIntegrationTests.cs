using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace LostCity.Meta.Core.Tests
{
    public sealed class GameplayMetaIntegrationTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
        private SaveGameManager save;
        private string directory;
        private float timeScale;
        private object originalRuntime;
        private Component stats;
        private Component experience;
        private Component upgrades;
        private Component persistence;
        private Component upgradeUI;
        private Component rewardUI;
        private Component completionUI;
        private Component investigation;
        private Component flow;
        private ScriptableObject definition;
        private ScriptableObject reward;
        private GameObject upgradePanel;
        private GameObject rewardPanel;

        [SetUp]
        public void SetUp()
        {
            timeScale = Time.timeScale;
            originalRuntime = StaticField(RuntimeType("LostCity.Meta.GameRuntime"), "<Instance>k__BackingField").GetValue(null);
            directory = Path.Combine(Path.GetTempPath(), "LostCity-gameplay-" + Guid.NewGuid().ToString("N"));
            save = Track(new GameObject("Test save")).AddComponent<SaveGameManager>();
            save.Initialize(Path.Combine(directory, "save.json"));
        }

        [TearDown]
        public void TearDown()
        {
            StaticField(RuntimeType("LostCity.Meta.GameRuntime"), "<Instance>k__BackingField").SetValue(null, originalRuntime);
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
            Time.timeScale = timeScale;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void SavedProgress_RestoresAfterHealthAwake_WithoutCompoundingOrHealthAutosaves()
        {
            save.SetPlayerProgress(new PlayerProgressData
            {
                Level = 4, CurrentExperience = 3, ExperienceToNextLevel = 17,
                AttackMultiplier = 1.5f, MaxHpMultiplier = 1.4f, FireRateMultiplier = 1.3f,
                CritChanceBonus = .2f, DodgeChanceBonus = .1f, DroneProjectileBonus = 3
            });
            CreatePlayer();
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(Property<float>(stats, "MaxHp"), Is.EqualTo(140f).Within(.0001f));
            Assert.That(Property<int>(experience, "CurrentLevel"), Is.EqualTo(4));
            Assert.That(Property<int>(upgrades, "ExtraDroneProjectiles"), Is.EqualTo(3));
            int writes = 0;
            save.DataChanged += () => writes++;
            Call(persistence, "Initialize", save);
            Call(persistence, "OnDisable");
            Call(persistence, "OnEnable");
            Call(persistence, "OnEnable");
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.5f).Within(.0001f));
            var damageable = stats.GetComponent(RuntimeType("LostCity.CombatSandbox.Damageable"));
            Call(damageable, "SetMaxHealth", 140f, true);
            Assert.That(writes, Is.EqualTo(0), "HP-only events must not persist permanent stats.");
            Call(stats, "MultiplyAttack", 2f);
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(3f).Within(.0001f));
            Call(stats, "AddCritChance", .1f);
            Call(stats, "AddDodgeChance", .1f);
            Call(upgrades, "AddDroneProjectiles", 2);
            Assert.That(save.GetPlayerProgress().CritChanceBonus, Is.EqualTo(.3f).Within(.0001f));
            Assert.That(save.GetPlayerProgress().DodgeChanceBonus, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(save.GetPlayerProgress().DroneProjectileBonus, Is.EqualTo(5));
        }

        [Test]
        public void PendingChoiceDebt_RestoresAcrossNewPlayers_AndCommitsWithItsStatEffect()
        {
            CreatePlayer();
            Call(experience, "AddExperience", 20);
            Assert.That(save.GetPlayerProgress().PendingUpgradeChoices, Is.EqualTo(2));
            ReplacePlayer();
            Assert.That(Property<int>(experience, "CurrentLevel"), Is.EqualTo(3));
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(2));
            Assert.That(upgradePanel.activeSelf, Is.True);
            var captures = new List<SaveGameData>();
            save.DataChanged += () => captures.Add(save.GetSnapshot());
            ChooseUpgrade(1);
            Assert.That(captures.Count, Is.EqualTo(1), "Stats and consumed choice must use one root mutation/write.");
            Assert.That(captures[0].PlayerProgress.PendingUpgradeChoices, Is.EqualTo(1));
            Assert.That(captures[0].PlayerProgress.AttackMultiplier, Is.EqualTo(1.2f).Within(.0001f));
            save.Reload();
            ReplacePlayer();
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(1));
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.2f).Within(.0001f));
            captures.Clear();
            ChooseUpgrade(0);
            Assert.That(captures.Count, Is.EqualTo(1));
            Assert.That(save.GetPlayerProgress().PendingUpgradeChoices, Is.Zero);
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(save.GetPlayerProgress().FireRateMultiplier, Is.EqualTo(1.2f).Within(.0001f));
        }

        [Test]
        public void LevelUnlockAndConsumedChoice_AreOneAtomicSnapshot()
        {
            CreatePlayer();
            Call(experience, "AddExperience", 5);
            var unlock = Definition("RewardDefinition");
            Set(unlock, "rewardType", Enum.Parse(RuntimeType("LostCity.CombatSandbox.RewardType"), "Unlock"));
            Set(unlock, "unlockId", "queued_unlock");
            Set(upgradeUI, "upgradeChoices", TypedArray(unlock.GetType(), unlock));
            var captures = new List<SaveGameData>();
            save.DataChanged += () => captures.Add(save.GetSnapshot());
            ChooseUpgrade(0);
            Assert.That(captures.Count, Is.EqualTo(1));
            Assert.That(captures[0].PlayerProgress.PendingUpgradeChoices, Is.Zero);
            Assert.That(captures[0].PermanentUnlockIds, Is.EqualTo(new[] { "queued_unlock" }));
        }

        [Test]
        public void MultipleLevelChoices_SynchronousNextShowRetainsItsCallbackAndStats()
        {
            CreatePlayer();
            Call(experience, "AddExperience", 20);
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(2));
            ChooseUpgrade(0);
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(1));
            Assert.That(upgradePanel.activeSelf, Is.True);
            ChooseUpgrade(1);
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(0));
            Assert.That(upgradePanel.activeSelf, Is.False);
            Assert.That(save.GetPlayerProgress().Level, Is.EqualTo(3));
            Assert.That(save.GetPlayerProgress().CurrentExperience, Is.EqualTo(8));
            Assert.That(save.GetPlayerProgress().ExperienceToNextLevel, Is.EqualTo(10));
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(save.GetPlayerProgress().FireRateMultiplier, Is.EqualTo(1.2f).Within(.0001f));
        }

        [Test]
        public void BossDeath_PaysXpThenDrainsEveryLevelChoice_BeforeOfferingCaseReward()
        {
            CreatePlayer();
            CreateFlow();
            var hostile = Track(new GameObject("Enemy"));
            var team = hostile.AddComponent(RuntimeType("LostCity.CombatSandbox.TeamMember"));
            Set(team, "team", Enum.Parse(RuntimeType("LostCity.CombatSandbox.CombatTeam"), "Enemy"));
            var projectile = Track(new GameObject("Hostile projectile"));
            projectile.AddComponent(RuntimeType("LostCity.CombatSandbox.Projectile"));
            var boss = Track(new GameObject("Boss"));
            var health = boss.AddComponent(RuntimeType("LostCity.CombatSandbox.Damageable"));
            InitializeHealth(health);
            Call(flow, "TrackBoss", boss);
            Call(flow, "HandleBossDied", health, Activator.CreateInstance(RuntimeType("LostCity.CombatSandbox.DamageInfo")));
            Assert.That(hostile == null, Is.True);
            Assert.That(projectile == null, Is.True);
            Assert.That(Property<int>(experience, "CurrentLevel"), Is.EqualTo(3));
            Assert.That(Property<int>(experience, "PendingUpgradeChoices"), Is.EqualTo(2));
            Assert.That(rewardPanel.activeSelf, Is.False);
            Assert.That(Property<bool>(experience, "AcceptsExperience"), Is.False);
            Call(experience, "AddExperience", 100);
            Assert.That(Property<int>(experience, "CurrentLevel"), Is.EqualTo(3));
            ChooseUpgrade(0);
            Assert.That(rewardPanel.activeSelf, Is.False);
            ChooseUpgrade(1);
            Assert.That(rewardPanel.activeSelf, Is.True);
            Assert.That(save.IsCaseCompleted("test_case"), Is.False);
            Call(rewardUI, "SelectReward", 0);
            Assert.That(save.IsCaseCompleted("test_case"), Is.True);
            Assert.That(Property<bool>(flow, "CompletionPersisted"), Is.True);
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(1.8f).Within(.0001f));
        }

        [Test]
        public void FailedCompletionWrite_RetryPersistsWithoutApplyingRewardTwice()
        {
            CreatePlayer();
            CreateFlow();
            SetBacking(flow, "CurrentState", Enum.Parse(RuntimeType("LostCity.CombatSandbox.GameFlowState"), "Reward"));
            Directory.CreateDirectory(save.SavePath + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex(".*Save write failed.*"));
            Call(flow, "HandleRewardSelected", reward);
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(Property<bool>(flow, "CompletionPersisted"), Is.False);
            Assert.That(save.IsCaseCompleted("test_case"), Is.False);
            Directory.Delete(save.SavePath + ".tmp");
            Call(flow, "RetryCompletion");
            Assert.That(Property<bool>(flow, "CompletionPersisted"), Is.True);
            Assert.That(save.IsCaseCompleted("test_case"), Is.True);
            Assert.That(save.GetPlayerProgress().AttackMultiplier, Is.EqualTo(1.5f).Within(.0001f));
            Call(flow, "HandleRewardSelected", reward);
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.5f).Within(.0001f));
        }

        [Test]
        public void ReadOnlySave_PreventsRewardAndCompletion_UntilExplicitCompatibleReload()
        {
            File.WriteAllText(save.SavePath, "{\"Version\":999}");
            LogAssert.Expect(LogType.Error, new Regex(".*future save version.*"));
            save.Reload();
            CreatePlayer();
            CreateFlow();
            SetBacking(flow, "CurrentState", Enum.Parse(RuntimeType("LostCity.CombatSandbox.GameFlowState"), "Reward"));
            Call(flow, "HandleRewardSelected", reward);
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1f));
            Assert.That(save.IsCaseCompleted("test_case"), Is.False);
            Assert.That(File.ReadAllText(save.SavePath), Is.EqualTo("{\"Version\":999}"));
            File.WriteAllText(save.SavePath, "{\"Version\":1}");
            Call(flow, "RetryCompletion");
            Assert.That(Property<float>(stats, "Attack"), Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(save.IsCaseCompleted("test_case"), Is.True);
        }

        [Test]
        public void UnlockReward_UsesPersistenceBoundaryAndRemainsUnique()
        {
            CreatePlayer();
            var unlock = Definition("RewardDefinition");
            Set(unlock, "rewardType", Enum.Parse(RuntimeType("LostCity.CombatSandbox.RewardType"), "Unlock"));
            Set(unlock, "unlockId", "unlock_test");
            Call(upgrades, "ApplyReward", unlock);
            Call(upgrades, "ApplyReward", unlock);
            Assert.That(save.PermanentUnlockIds, Is.EqualTo(new[] { "unlock_test" }));
        }

        [Test]
        public void CompletionCountdown_IsUnscaled_AndReturnEventIsOneShot()
        {
            var ui = Track(new GameObject("Completion")).AddComponent(RuntimeType("LostCity.CombatSandbox.CaseCompletionUI"));
            Set(ui, "panelRoot", Track(new GameObject("Completion panel")));
            int returns = 0;
            ui.GetType().GetEvent("ReturnRequested").AddEventHandler(ui, new Action(() => returns++));
            Time.timeScale = 0f;
            Call(ui, "ShowChapterComplete", "测试案件", "测试完成");
            Call(ui, "Tick", 2.9f);
            Assert.That(returns, Is.Zero);
            Call(ui, "RequestReturn");
            Call(ui, "Tick", 1f);
            Call(ui, "RequestReturn");
            Assert.That(returns, Is.EqualTo(1));
            Call(ui, "ShowChapterComplete", "测试案件", "测试完成");
            Call(ui, "Tick", 3f);
            Assert.That(returns, Is.EqualTo(2));
        }

        [Test]
        public void EnemyAndBossEncounters_AreRecordedOnceThroughTheirRuntimeHooks()
        {
            InstallRuntimeStub();
            var enemyDefinition = Definition("EnemyDefinition");
            Set(enemyDefinition, "enemyId", "enemy_test");
            var enemy = Track(new GameObject("Enemy")).AddComponent(RuntimeType("LostCity.CombatSandbox.MemoryFragmentEnemy"));
            Set(enemy, "definition", enemyDefinition);
            Call(enemy, "OnEnable");
            Call(enemy, "OnEnable");
            Assert.That(save.EncounteredEnemyIds, Is.EqualTo(new[] { "enemy_test" }));
            var prefab = Track(new GameObject("Boss template"));
            prefab.SetActive(false);
            InitializeHealth(prefab.AddComponent(RuntimeType("LostCity.CombatSandbox.Damageable")));
            var bossDefinition = Definition("BossDefinition");
            Set(bossDefinition, "bossId", "boss_test");
            Set(bossDefinition, "bossPrefab", prefab);
            var spawner = Track(new GameObject("Boss spawner")).AddComponent(RuntimeType("LostCity.CombatSandbox.BossSpawnController"));
            Call(spawner, "SetBossDefinition", bossDefinition);
            var spawned = Track((GameObject)Call(spawner, "SpawnBoss"));
            Assert.That(Call(spawner, "SpawnBoss"), Is.SameAs(spawned));
            Assert.That(save.EncounteredBossIds, Is.EqualTo(new[] { "boss_test" }));
        }

        [Test]
        public void DeathRestart_WithUnsavedProgressKeepsSceneAndOffersRetry()
        {
            CreatePlayer();
            CreateFlow();
            Directory.CreateDirectory(save.SavePath + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex(".*Save write failed.*"));
            Call(stats, "MultiplyAttack", 1.5f);
            var manager = Track(new GameObject("Combat manager")).AddComponent(RuntimeType("LostCity.CombatSandbox.CombatGameManager"));
            Call(manager, "Awake");
            Time.timeScale = 0f;
            LogAssert.Expect(LogType.Error, "重进前保存失败，暂不销毁未写入的玩家进度。");
            Call(manager, "RestartCurrentScene", 1.25f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(stats != null, Is.True);
            Assert.That(save.HasUnsavedChanges, Is.True);
            Assert.That(completionUI.GetType().GetField("waitingForRetry", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(completionUI), Is.True);
        }

        [Test]
        public void SelectedCaseOverridesSerializedFallback_AndClueRecordingIsIdempotent()
        {
            var session = InstallRuntimeStub();
            var selected = Definition("CaseDefinition");
            var fallback = Definition("CaseDefinition");
            var clue = Definition("ClueDefinition");
            Set(clue, "id", "selected_clue");
            Set(selected, "clues", TypedArray(clue.GetType(), clue));
            SetBacking(session, "ActiveCase", selected);
            var progress = Track(new GameObject("Investigation")).AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(progress, "caseDefinition", fallback);
            Call(progress, "Awake");
            Assert.That(Property<object>(progress, "CurrentCase"), Is.SameAs(selected));
            Assert.That(Call(progress, "TryCollectClue", clue), Is.True);
            Assert.That(Call(progress, "TryCollectClue", clue), Is.False);
            Assert.That(save.DiscoveredClueIds, Is.EqualTo(new[] { "selected_clue" }));
        }

        private void CreatePlayer()
        {
            upgradeUI = Track(new GameObject("Upgrade UI")).AddComponent(RuntimeType("LostCity.CombatSandbox.UpgradeSelectionController"));
            upgradePanel = Track(new GameObject("Upgrade panel"));
            upgradePanel.SetActive(false);
            Set(upgradeUI, "panelRoot", upgradePanel);
            var player = Track(new GameObject("Player"));
            var health = player.AddComponent(RuntimeType("LostCity.CombatSandbox.Damageable"));
            Set(health, "onHealthChanged", new UnityEvent<float, float>());
            Set(health, "onDied", new UnityEvent());
            stats = player.AddComponent(RuntimeType("LostCity.CombatSandbox.PlayerStats"));
            Call(stats, "Awake");
            Call(health, "Awake");
            Call(stats, "OnEnable");
            upgrades = player.AddComponent(RuntimeType("LostCity.CombatSandbox.CombatUpgradeStats"));
            Call(upgrades, "Awake");
            experience = player.AddComponent(RuntimeType("LostCity.CombatSandbox.PlayerExperience"));
            Set(experience, "upgradeStats", upgrades);
            Set(experience, "upgradeSelectionController", upgradeUI);
            Call(experience, "Awake");
            persistence = player.AddComponent(RuntimeType("LostCity.Meta.PlayerProgressPersistence"));
            Call(persistence, "Initialize", save);
        }

        private Component InstallRuntimeStub()
        {
            var runtimeObject = Track(new GameObject("Runtime stub"));
            runtimeObject.SetActive(false);
            var runtime = runtimeObject.AddComponent(RuntimeType("LostCity.Meta.GameRuntime"));
            var session = runtimeObject.AddComponent(RuntimeType("LostCity.Meta.CaseSessionManager"));
            SetBacking(runtime, "SaveGame", save);
            SetBacking(runtime, "CaseSession", session);
            StaticField(runtime.GetType(), "<Instance>k__BackingField").SetValue(null, runtime);
            return session;
        }

        private void CreateFlow()
        {
            reward = Definition("RewardDefinition");
            Set(reward, "statType", Enum.Parse(RuntimeType("LostCity.CombatSandbox.RewardStatType"), "AttackMultiplier"));
            Set(reward, "multiplier", 1.5f);
            var bossDefinition = Definition("BossDefinition");
            Set(bossDefinition, "xpReward", 12);
            definition = Definition("CaseDefinition");
            Set(definition, "caseId", "test_case");
            Set(definition, "rewardPool", TypedArray(reward.GetType(), reward));
            Set(definition, "bossDefinition", bossDefinition);
            investigation = Track(new GameObject("Investigation")).AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(investigation, "caseDefinition", definition);
            rewardUI = Track(new GameObject("Reward UI")).AddComponent(RuntimeType("LostCity.CombatSandbox.RewardSelectionUI"));
            rewardPanel = Track(new GameObject("Reward panel"));
            rewardPanel.SetActive(false);
            Set(rewardUI, "panelRoot", rewardPanel);
            completionUI = Track(new GameObject("Completion UI")).AddComponent(RuntimeType("LostCity.CombatSandbox.CaseCompletionUI"));
            Set(completionUI, "panelRoot", Track(new GameObject("Completion panel")));
            flow = Track(new GameObject("Flow")).AddComponent(RuntimeType("LostCity.CombatSandbox.GameFlowManager"));
            Set(flow, "investigationProgress", investigation);
            Set(flow, "rewardSelectionUI", rewardUI);
            Set(flow, "completionUI", completionUI);
            Set(flow, "playerStats", stats);
            Set(flow, "combatUpgradeStats", upgrades);
            Set(flow, "playerExperience", experience);
            Set(flow, "playerProgressPersistence", persistence);
            Set(flow, "upgradeSelectionController", upgradeUI);
            Call(flow, "OnEnable");
            Call(flow, "Start");
            Call(flow, "DebugSpawnBoss");
        }

        private void ReplacePlayer()
        {
            Call(persistence, "OnDisable");
            UnityEngine.Object.DestroyImmediate(stats.gameObject);
            UnityEngine.Object.DestroyImmediate(upgradeUI.gameObject);
            UnityEngine.Object.DestroyImmediate(upgradePanel);
            Time.timeScale = 1f;
            CreatePlayer();
        }

        private void ChooseUpgrade(int index) => Call(upgradeUI, "Choose", index,
            Enum.Parse(RuntimeType("LostCity.CombatSandbox.CombatUpgradeType"), index == 0 ? "FireRate" : "ProjectileDamage"));
        private ScriptableObject Definition(string type) => Track(ScriptableObject.CreateInstance(RuntimeType("LostCity.CombatSandbox." + type)));
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string type) => Type.GetType(type + ", Assembly-CSharp", true);
        private static FieldInfo StaticField(Type type, string field) => type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static);
        private static void SetBacking(object target, string property, object value) => Set(target, "<" + property + ">k__BackingField", value);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field,
            BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
        private static Array TypedArray(Type type, params object[] items)
        {
            var array = Array.CreateInstance(type, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            return array;
        }
        private static void InitializeHealth(Component health)
        {
            Set(health, "onHealthChanged", new UnityEvent<float, float>());
            Set(health, "onDied", new UnityEvent());
            Call(health, "Awake");
        }
    }
}
