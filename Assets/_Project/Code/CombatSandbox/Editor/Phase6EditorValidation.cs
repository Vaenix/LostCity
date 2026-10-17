using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostCity.Meta;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostCity.CombatSandbox.EditorTools
{
    public static class Phase6EditorValidation
    {
        public static void GenerateAndValidate()
        {
            CombatSandboxCreator.CreateCombatSandbox();
            ValidateGeneratedAssets();
            ValidateMetaContracts();
        }

        [MenuItem("Tools/Lost City/Validate Meta Contracts")]
        public static void ValidateMetaContracts()
        {
            Require(!EditorApplication.isPlaying, "请退出运行模式后验证数据契约。");
            GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(
                "Assets/_Project/Resources/GameContentCatalog.asset");
            Require(catalog != null, "请先生成内容目录。");
            string directory = Path.Combine(Path.GetTempPath(), "LostCity-contracts-" + Guid.NewGuid().ToString("N"));
            GameObject root = new GameObject("MetaContractValidation");
            var temporaryAssets = new List<UnityEngine.Object>();
            try
            {
                var save = root.AddComponent<SaveGameManager>();
                string savePath = Path.Combine(directory, "save.json");
                save.Initialize(savePath);
                var registry = root.AddComponent<CaseRegistry>();
                registry.Initialize(catalog, save);
                ChapterDefinition chapter = registry.GetCurrentChapter();
                Require(chapter != null && chapter.Cases.Length >= 2, "缺少验证章节与后续占位案件。");
                CaseDefinition first = chapter.Cases[0];
                CaseDefinition next = chapter.Cases[1];
                Require(registry.CanStart(first, out _), "首个案件应当可开始。");
                Require(registry.GetCaseState(next) == CaseState.Locked, "后续案件应当尚未解锁。");
                var session = root.AddComponent<CaseSessionManager>();
                session.Initialize(registry);
                Require(session.BeginCase(first) && session.ActiveCase == first, "所选案件未写入会话。");
                Require(!session.BeginCase(first), "运行中的案件不能被重复启动。");
                session.EndCase();

                var progress = new PlayerProgressData
                {
                    Level = 3, CurrentExperience = 2, ExperienceToNextLevel = 10, PendingUpgradeChoices = 1,
                    AttackMultiplier = 1.1f, MaxHpMultiplier = 1.1f, FireRateMultiplier = 1.2f,
                    CritChanceBonus = .05f, DodgeChanceBonus = .1f, DroneProjectileBonus = 1
                };
                save.SetPlayerProgress(progress);
                save.DiscoverClue(first.Clues[0].Id);
                save.EncounterBoss(first.BossDefinition.BossId);
                if (registry.Enemies.Count > 0) save.EncounterEnemy(registry.Enemies[0].EnemyId);
                save.AddPermanentUnlock("validation_unlock");
                Require(save.CompleteCase(first.CaseId), "案件完成未被记录。");
                Require(!save.CompleteCase(first.CaseId), "重复完成不能重复写入。");
                Require(registry.GetCaseState(first) == CaseState.Completed, "首个案件未显示已完成。");
                Require(registry.GetCaseState(next) == CaseState.Available, "完成案件后未解锁后续案件。");
                Require(!registry.CanStart(next, out _), "开发中占位案件不能进入调查场景。");
                Require(registry.CanStart(first, out _), "已完成案件应支持重新调查。");
                var loaded = root.AddComponent<SaveGameManager>();
                loaded.Initialize(savePath);
                Require(JsonUtility.ToJson(loaded.GetSnapshot()) == JsonUtility.ToJson(save.GetSnapshot()),
                    "存档重载丢失了进度、线索或图鉴数据。");
                Require(!loaded.HasUnsavedChanges && !loaded.IsReadOnly, "临时测试存档未正常保存。");

                ValidateVariableClueLayout(root, temporaryAssets);
                Debug.Log("Phase6 meta contracts passed: session, unlock, replay, native JSON reload, variable clue layout.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var asset in temporaryAssets) UnityEngine.Object.DestroyImmediate(asset);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void ValidateVariableClueLayout(GameObject root, List<UnityEngine.Object> temporaryAssets)
        {
            var definition = ScriptableObject.CreateInstance<CaseDefinition>();
            temporaryAssets.Add(definition);
            var progress = root.AddComponent<InvestigationProgress>();
            Assign(progress, "caseDefinition", definition);
            var templateObject = new GameObject("ClueTemplate", typeof(BoxCollider2D));
            templateObject.transform.SetParent(root.transform);
            templateObject.SetActive(false);
            var template = templateObject.AddComponent<CluePickup>();
            var layout = root.AddComponent<CaseClueLayout>();
            Assign(layout, "investigationProgress", progress);
            Assign(layout, "pickupTemplate", template);
            foreach (int count in new[] { 1, 5 })
            {
                var serialized = new SerializedObject(definition);
                var clues = serialized.FindProperty("clues");
                clues.arraySize = count;
                for (int i = 0; i < count; i++)
                {
                    var clue = ScriptableObject.CreateInstance<ClueDefinition>();
                    temporaryAssets.Add(clue);
                    var clueData = new SerializedObject(clue);
                    clueData.FindProperty("id").stringValue = "validation_clue_" + count + "_" + i;
                    clueData.ApplyModifiedPropertiesWithoutUndo();
                    clues.GetArrayElementAtIndex(i).objectReferenceValue = clue;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                layout.Rebuild();
                Require(layout.SpawnedPickups.Count == count, "线索布局仍依赖固定线索数量。");
                for (int i = 0; i < count; i++)
                    Require(layout.SpawnedPickups[i].Definition == definition.Clues[i], "拾取对象绑定了错误案件线索。");
            }
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Tools/Lost City/Validate Meta Framework")]
        public static void ValidateGeneratedAssets()
        {
            string[] requiredScenes =
            {
                "Assets/_Project/Scenes/Hub.unity",
                "Assets/_Project/Scenes/CombatSandbox.unity"
            };
            string[] buildScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Require(buildScenes.Length >= 2 && buildScenes[0] == requiredScenes[0] && buildScenes[1] == requiredScenes[1],
                "Build Settings 必须以 Hub、CombatSandbox 开头。");
            foreach (string scene in requiredScenes)
                Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) != null, "缺少场景：" + scene);

            Require(AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Resources/GameContentCatalog.asset") != null,
                "缺少自动内容目录。");
            Require(AssetDatabase.LoadMainAssetAtPath("Assets/_Project/ScriptableObjects/Meta/Chapter_01.asset") != null,
                "缺少第一章配置。");

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            bool canRestore = previousSetup.All(item => !string.IsNullOrEmpty(item.path))
                && previousSetup.Any(item => item.isLoaded && item.isActive);
            Require(Application.isBatchMode || canRestore, "请先保存或打开一个已有场景，再进行验证。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Require(!SceneManager.GetSceneAt(i).isDirty, "请先保存场景再进行验证。");
            try
            {
                foreach (string path in requiredScenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ValidateScene(scene);
                }
            }
            finally
            {
                if (canRestore) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("Phase6 generated scene validation passed. Play Mode flow is a separate check.");
        }

        private static void ValidateScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Transform[] transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach (Transform transform in transforms)
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    scene.name + " 存在丢失脚本：" + transform.name);

            Camera[] cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
            Require(cameras.Any(camera => camera.enabled && camera.CompareTag("MainCamera")), scene.name + " 缺少主相机。");
            EventSystem[] eventSystems = roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).ToArray();
            Require(eventSystems.Length == 1, scene.name + " 必须包含一个 EventSystem。");
            InputSystemUIInputModule input = eventSystems[0].GetComponent<InputSystemUIInputModule>();
            Require(input != null && input.enabled, scene.name + " 缺少 Input System UI 模块。");
            Require(input.point != null && input.point.action != null && input.leftClick != null && input.leftClick.action != null,
                scene.name + " UI 指针或点击动作未绑定。");
            foreach (Canvas canvas in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
            {
                if (canvas.GetComponentsInChildren<Button>(true).Length > 0)
                    Require(canvas.GetComponent<GraphicRaycaster>() != null, canvas.name + " 缺少 GraphicRaycaster。");
            }

            MonoBehaviour[] behaviours = roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            if (scene.name == "Hub")
            {
                RequireReferences(Find(behaviours, "HubController"), "currentChapterText", "statusText", "caseListRoot",
                    "rowTemplate", "archiveButton", "bestiaryButton", "casesButton", "casesPanel", "archiveUI", "bestiaryUI", "retrySaveButton");
                Require(roots.SelectMany(root => root.GetComponentsInChildren<ScrollRect>(true)).Count() >= 3,
                    "大厅、档案与图鉴需要可滚动内容区。");
            }
            else
            {
                RequireReferences(Find(behaviours, "GameFlowManager"), "investigationProgress", "deductionBoard", "enemySpawner",
                    "bossSpawnController", "rewardSelectionUI", "completionUI", "playerStats", "combatUpgradeStats",
                    "playerExperience", "playerProgressPersistence", "upgradeSelectionController");
                RequireReferences(Find(behaviours, "CaseClueLayout"), "investigationProgress", "pickupTemplate");
                RequireReferences(Find(behaviours, "PlayerProgressPersistence"), "playerStats", "playerExperience", "combatUpgradeStats");
                RequireReferences(Find(behaviours, "CaseCompletionUI"), "panelRoot", "titleText", "bodyText", "retryButton");
                RequireReferences(Find(behaviours, "PistolWeapon"), "inputReader", "playerAim", "muzzle", "weaponDefinition");
                RequireReferences(Find(behaviours, "MemoryOrbWeapon"), "muzzle", "weaponDefinition");
                var reader = new SerializedObject(Find(behaviours, "PlayerInputReader"));
                foreach (string field in new[] { "moveActionReference", "fireActionReference" })
                {
                    var action = reader.FindProperty(field).objectReferenceValue as InputActionReference;
                    Require(action != null && action.action != null, "玩家输入动作无法解析：" + field);
                }
                foreach (string className in new[] { "PistolWeapon", "MemoryOrbWeapon" })
                {
                    var serialized = new SerializedObject(Find(behaviours, className));
                    var weapon = serialized.FindProperty("weaponDefinition").objectReferenceValue as WeaponDefinition;
                    Require(weapon != null && weapon.ProjectilePrefab != null && weapon.ProjectileDefinition != null,
                        className + " 缺少投射物资源。");
                    Require(weapon.CooldownSeconds > 0f && weapon.Range > 0f
                        && weapon.ProjectileDefinition.Damage > 0f && weapon.ProjectileDefinition.Speed > 0f
                        && weapon.ProjectileDefinition.LifetimeSeconds > 0f, className + " 射击参数无效。");
                    Require(weapon.ProjectilePrefab.GetComponent<Rigidbody2D>() != null
                        && weapon.ProjectilePrefab.GetComponent<Collider2D>() != null, className + " 投射物缺少 2D 物理组件。");
                }
            }
        }

        private static MonoBehaviour Find(MonoBehaviour[] behaviours, string className)
        {
            MonoBehaviour result = behaviours.FirstOrDefault(item => item != null && item.GetType().Name == className);
            Require(result != null, "缺少组件：" + className);
            return result;
        }

        private static void RequireReferences(UnityEngine.Object target, params string[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (string field in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                Require(property != null && property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue != null, target.name + " 缺少引用：" + field);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
