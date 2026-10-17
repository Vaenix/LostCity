using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostCity.Meta;
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
    // Explicit, additive migration of existing assets. Never call the sandbox/content generators here.
    public static class Phase6ProjectMigration
    {
        private const string CombatScene = "Assets/_Project/Scenes/CombatSandbox.unity";
        private const string HubScene = "Assets/_Project/Scenes/Hub.unity";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/CombatSandbox/Player.prefab";
        private const string InputRoot = "Assets/_Project/Settings/Input/";
        private const string InputPath = InputRoot + "CombatSandbox.inputactions";
        private const string DefinitionRoot = "Assets/_Project/ScriptableObjects/CombatSandbox/";

        [MenuItem("Tools/Lost City/Migrate Existing Project To Phase 6")]
        public static void MigrateExistingProject()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling,
                "请先退出运行模式并等待编译完成。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Require(!SceneManager.GetSceneAt(i).isDirty, "请先保存已打开场景的手工改动，再执行迁移。");
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScene) != null
                && AssetDatabase.LoadAssetAtPath<SceneAsset>(HubScene) != null, "迁移要求已有 CombatSandbox 和 Hub 场景。");
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Require(actions != null, "未找到本地输入配置。");
            foreach (string action in new[] { "Player/Move", "Player/Fire", "UI/Point", "UI/Click", "UI/ScrollWheel", "UI/Navigate", "UI/Submit", "UI/Cancel" })
                FindActionReference(actions, action);

            var protectedFiles = new Dictionary<string, byte[]>();
            foreach (string path in new[] { "Assets/Room.unity", "Assets/Room.unity.meta", InputPath })
                if (File.Exists(path)) protectedFiles.Add(path, File.ReadAllBytes(path));
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            var opened = new List<Scene>();
            try
            {
                Scene combat = Open(CombatScene, opened);
                Scene hub = Open(HubScene, opened);
                // Fail on ambiguous/missing existing content before writing the prefab.
                RequireNoMissingScripts(combat.GetRootGameObjects());
                RequireNoMissingScripts(hub.GetRootGameObjects());
                Unique<GameFlowManager>(combat);
                Unique<InvestigationProgress>(combat);
                Unique<PlayerStats>(combat);
                Unique<CaseCompletionUI>(combat);
                Unique<EventSystem>(combat);
                Unique<EventSystem>(hub);
                var combatState = CaptureHierarchy(combat.GetRootGameObjects());
                var hubState = CaptureHierarchy(hub.GetRootGameObjects());
                MigratePlayerPrefab(actions);
                bool combatChanged = MigrateCombat(combat, actions);
                bool hubChanged = WireInput(Unique<EventSystem>(hub).gameObject, actions);
                AssertHierarchyUnchanged(combatState);
                AssertHierarchyUnchanged(hubState);
                RequireNoMissingScripts(combat.GetRootGameObjects());
                RequireNoMissingScripts(hub.GetRootGameObjects());
                SaveIfChanged(combat, combatChanged);
                SaveIfChanged(hub, hubChanged);
                foreach (var file in protectedFiles)
                    Require(File.ReadAllBytes(file.Key).SequenceEqual(file.Value), "迁移不应修改受保护文件：" + file.Key);
                Phase6EditorValidation.ValidateGeneratedAssets();
                Debug.Log("Phase6 existing-project migration passed. Existing authored transforms and Room bytes were preserved; Play Mode was not entered.");
            }
            finally
            {
                if (setup.Length > 0 && setup.All(item => !string.IsNullOrEmpty(item.path))
                    && setup.Any(item => item.isActive && item.isLoaded))
                {
                    // Do not discard partially edited scenes after a failure; they remain available for inspection.
                    if (!Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                        EditorSceneManager.RestoreSceneManagerSetup(setup);
                }
                else foreach (Scene scene in opened)
                    if (scene.IsValid() && scene.isLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void MigratePlayerPrefab(InputActionAsset actions)
        {
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab) != null, "未找到已有玩家 prefab。");
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                RequireNoMissingScripts(new[] { root });
                var state = CaptureHierarchy(new[] { root });
                bool changed = WirePlayer(root, actions, null, null);
                AssertHierarchyUnchanged(state);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab, out bool success);
                    Require(success, "玩家 prefab 保存失败。");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static bool MigrateCombat(Scene scene, InputActionAsset actions)
        {
            var flow = Unique<GameFlowManager>(scene);
            var progress = Unique<InvestigationProgress>(scene);
            var stats = Unique<PlayerStats>(scene);
            var upgrades = Unique<UpgradeSelectionController>(scene);
            var completion = Unique<CaseCompletionUI>(scene);
            Camera camera = Components<Camera>(scene).Single(item => item.CompareTag("MainCamera"));
            bool changed = WirePlayer(stats.gameObject, actions, upgrades, camera);
            changed |= FillMissing(progress, "caseDefinition", AssetDatabase.LoadAssetAtPath<CaseDefinition>(DefinitionRoot + "Room304_CaseDefinition.asset"));
            changed |= FillMissing(flow, "investigationProgress", progress);
            changed |= FillMissing(flow, "deductionBoard", Unique<DeductionBoard>(scene));
            changed |= FillMissing(flow, "enemySpawner", Unique<EnemySpawner>(scene));
            changed |= FillMissing(flow, "bossSpawnController", Unique<BossSpawnController>(scene));
            changed |= FillMissing(flow, "rewardSelectionUI", Unique<RewardSelectionUI>(scene));
            changed |= FillMissing(flow, "completionUI", completion);
            changed |= FillMissing(flow, "playerStats", stats);
            changed |= FillMissing(flow, "combatUpgradeStats", stats.GetComponent<CombatUpgradeStats>());
            changed |= FillMissing(flow, "playerExperience", stats.GetComponent<PlayerExperience>());
            changed |= FillMissing(flow, "playerProgressPersistence", stats.GetComponent<PlayerProgressPersistence>());
            changed |= FillMissing(flow, "upgradeSelectionController", upgrades);
            changed |= WireAuthoredClues(scene, progress);
            changed |= WireCompletionRetry(completion);
            changed |= WireInput(Unique<EventSystem>(scene).gameObject, actions);
            foreach (Canvas canvas in Components<Canvas>(scene))
                if (canvas.GetComponentsInChildren<Button>(true).Length > 0 && canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    changed = true;
                }
            return changed;
        }

        private static bool WirePlayer(GameObject root, InputActionAsset actions, UpgradeSelectionController upgrades, Camera camera)
        {
            var stats = root.GetComponent<PlayerStats>();
            var experience = root.GetComponent<PlayerExperience>();
            var upgradeStats = root.GetComponent<CombatUpgradeStats>();
            Require(stats != null && experience != null && upgradeStats != null, "现有玩家缺少基础进度组件，不能安全推断配置。");
            bool changed = false;
            var persistence = GetOrAdd<PlayerProgressPersistence>(root, ref changed);
            changed |= FillMissing(persistence, "playerStats", stats);
            changed |= FillMissing(persistence, "playerExperience", experience);
            changed |= FillMissing(persistence, "combatUpgradeStats", upgradeStats);
            var reader = root.GetComponent<PlayerInputReader>();
            var aim = root.GetComponent<PlayerAim>();
            Require(reader != null && aim != null, "现有玩家输入或瞄准组件缺失。");
            foreach (var pair in new[] { ("moveActionReference", "Player/Move"), ("fireActionReference", "Player/Fire") })
            {
                var reference = Reference(reader, pair.Item1) as InputActionReference;
                if (reference == null || reference.action == null)
                    changed |= Assign(reader, pair.Item1, FindActionReference(actions, pair.Item2));
            }
            if (upgrades != null) changed |= FillMissing(experience, "upgradeSelectionController", upgrades);
            if (camera != null) changed |= FillMissing(aim, "aimCamera", camera);
            var pistol = root.GetComponentInChildren<PistolWeapon>(true);
            var orb = root.GetComponentInChildren<MemoryOrbWeapon>(true);
            Require(pistol != null && orb != null, "现有玩家武器缺失，迁移不会重建玩家。");
            changed |= FillMissing(pistol, "inputReader", reader);
            changed |= FillMissing(pistol, "playerAim", aim);
            changed |= FillMissing(pistol, "weaponDefinition", AssetDatabase.LoadAssetAtPath<WeaponDefinition>(DefinitionRoot + "PistolWeaponDefinition.asset"));
            changed |= FillMissing(orb, "weaponDefinition", AssetDatabase.LoadAssetAtPath<WeaponDefinition>(DefinitionRoot + "OrbWeaponDefinition.asset"));
            foreach (Component weapon in new Component[] { pistol, orb })
                if (Reference(weapon, "muzzle") == null)
                    changed |= FillMissing(weapon, "muzzle", weapon.GetComponentsInChildren<Transform>(true).Single(item => item.name == "Muzzle"));
            return changed;
        }

        private static bool WireAuthoredClues(Scene scene, InvestigationProgress progress)
        {
            bool changed = false;
            CaseClueLayout[] layouts = Components<CaseClueLayout>(scene).ToArray();
            Require(layouts.Length <= 1, "存在多个线索布局，不能安全迁移。");
            var layout = layouts.Length == 1 ? layouts[0] : GetOrAdd<CaseClueLayout>(progress.gameObject, ref changed);
            changed |= FillMissing(layout, "investigationProgress", progress);
            var template = Reference(layout, "pickupTemplate") as CluePickup;
            CluePickup[] authored = Components<CluePickup>(scene).Where(item => item != template && item.name != "Phase6PickupTemplate").ToArray();
            Require(authored.All(item => item.Definition != null), "原线索引用缺失，请先修复定义；迁移不会猜测线索内容。");
            foreach (CluePickup pickup in authored) changed |= FillMissing(pickup, "investigationProgress", progress);
            if (template == null)
            {
                template = Components<CluePickup>(scene).SingleOrDefault(item => item.name == "Phase6PickupTemplate");
                if (template == null)
                {
                    Require(authored.Length > 0, "缺少可复用的线索模板，迁移不会重建手工线索。");
                    GameObject clone = UnityEngine.Object.Instantiate(authored[0].gameObject, layout.transform);
                    clone.name = "Phase6PickupTemplate";
                    clone.SetActive(false);
                    template = clone.GetComponent<CluePickup>();
                    changed = true;
                }
                changed |= FillMissing(layout, "pickupTemplate", template);
            }
            var serialized = new SerializedObject(layout);
            SerializedProperty array = serialized.FindProperty("authoredPickups");
            Require(array != null, "CaseClueLayout 尚未提供 authoredPickups，无法保留手工布局。");
            bool same = array.arraySize == authored.Length;
            if (same)
                for (int i = 0; i < authored.Length; i++) same &= array.GetArrayElementAtIndex(i).objectReferenceValue == authored[i];
            if (!same)
            {
                array.arraySize = authored.Length;
                for (int i = 0; i < authored.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = authored[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(layout);
                changed = true;
            }
            return changed;
        }

        private static bool WireCompletionRetry(CaseCompletionUI completion)
        {
            if (Reference(completion, "retryButton") != null) return false;
            GameObject panel = Reference(completion, "panelRoot") as GameObject;
            Require(panel != null && panel.GetComponent<RectTransform>() != null, "完成界面缺少现有面板，迁移不会重建它。");
            Transform existing = panel.transform.Find("Phase6RetryButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (existing != null) Require(button != null, "Phase6RetryButton 名称已被非按钮对象占用。");
            if (button == null)
            {
                var go = new GameObject("Phase6RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panel.transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 8f);
                rect.sizeDelta = new Vector2(240f, 44f);
                Image image = go.GetComponent<Image>();
                image.color = new Color(0.3f, 0.34f, 0.36f);
                button = go.GetComponent<Button>();
                button.targetGraphic = image;
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(go.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 18;
                label.text = "重试保存";
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }
            return FillMissing(completion, "retryButton", button);
        }

        private static bool WireInput(GameObject eventObject, InputActionAsset actions)
        {
            bool changed = false;
            var module = GetOrAdd<InputSystemUIInputModule>(eventObject, ref changed);
            var serialized = new SerializedObject(module);
            var assignments = new Dictionary<string, UnityEngine.Object>
            {
                { "m_ActionsAsset", actions },
                { "m_PointAction", FindActionReference(actions, "UI/Point") },
                { "m_LeftClickAction", FindActionReference(actions, "UI/Click") },
                { "m_ScrollWheelAction", FindActionReference(actions, "UI/ScrollWheel") },
                { "m_MoveAction", FindActionReference(actions, "UI/Navigate") },
                { "m_SubmitAction", FindActionReference(actions, "UI/Submit") },
                { "m_CancelAction", FindActionReference(actions, "UI/Cancel") }
            };
            foreach (var pair in assignments) serialized.FindProperty(pair.Key).objectReferenceValue = pair.Value;
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(module); changed = true; }
            return changed;
        }

        private static InputActionReference FindActionReference(InputActionAsset asset, string actionName)
        {
            InputAction action = asset.FindAction(actionName);
            Require(action != null, "本地输入配置缺少动作：" + actionName);
            string path = InputRoot + "CombatSandbox_" + actionName.Replace('/', '_') + ".inputactionreference.asset";
            if (actionName.StartsWith("Player/", StringComparison.Ordinal)) path = InputRoot + "CombatSandbox_" + action.name + ".inputactionreference.asset";
            var standalone = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (standalone != null && standalone.action == action) return standalone;
            foreach (UnityEngine.Object value in AssetDatabase.LoadAllAssetsAtPath(InputPath))
                if (value is InputActionReference reference && EditorUtility.IsPersistent(reference) && reference.action == action) return reference;
            throw new InvalidOperationException("本地输入动作缺少持久化引用：" + actionName);
        }

        private static bool FillMissing(UnityEngine.Object target, string field, UnityEngine.Object value) =>
            Reference(target, field) == null && Assign(target, field, value);
        private static UnityEngine.Object Reference(UnityEngine.Object target, string field)
        {
            Require(target != null, "迁移引用的现有组件缺失：" + field);
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            Require(property != null, "迁移字段不存在：" + target.GetType().Name + "." + field);
            return property.objectReferenceValue;
        }
        private static bool Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            Require(value != null, "不能安全推断缺失引用：" + target.name + "." + field);
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
            if (changed)
            {
                EditorUtility.SetDirty(target);
                if (target is Component component && component.gameObject.scene.path == CombatScene && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            return changed;
        }
        private static T GetOrAdd<T>(GameObject root, ref bool changed) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null) return component;
            changed = true;
            return root.AddComponent<T>();
        }
        private static IEnumerable<T> Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
        private static T Unique<T>(Scene scene) where T : Component
        {
            T[] values = Components<T>(scene).ToArray();
            Require(values.Length == 1, scene.name + " 必须恰好存在一个 " + typeof(T).Name + "，迁移不会猜测对象。");
            return values[0];
        }
        private static Scene Open(string path, List<Scene> opened)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded) return scene;
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            opened.Add(scene);
            return scene;
        }
        private static void SaveIfChanged(Scene scene, bool changed)
        {
            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, scene.path), "场景保存失败：" + scene.path);
        }
        private static void RequireNoMissingScripts(IEnumerable<GameObject> roots)
        {
            foreach (Transform transform in roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "现有对象存在 Missing Script，迁移不会删除它：" + transform.name);
        }
        private sealed class HierarchyState
        {
            public Transform Transform;
            public Transform Parent;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public bool Active;
        }
        private static HierarchyState[] CaptureHierarchy(IEnumerable<GameObject> roots) =>
            roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(item => new HierarchyState
            {
                Transform = item, Parent = item.parent, Position = item.localPosition,
                Rotation = item.localRotation, Scale = item.localScale, Active = item.gameObject.activeSelf
            }).ToArray();
        private static void AssertHierarchyUnchanged(IEnumerable<HierarchyState> values)
        {
            foreach (HierarchyState state in values)
                Require(state.Transform != null && state.Transform.parent == state.Parent
                    && state.Transform.localPosition.Equals(state.Position) && state.Transform.localRotation.Equals(state.Rotation)
                    && state.Transform.localScale.Equals(state.Scale) && state.Transform.gameObject.activeSelf == state.Active,
                    "迁移意外改变了原对象的层级、Transform 或 active 状态。");
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
