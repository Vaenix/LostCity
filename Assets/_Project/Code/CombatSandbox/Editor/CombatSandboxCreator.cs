using System;
using System.Collections.Generic;
using System.IO;
using LostCity.CombatSandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LostCity.CombatSandbox.EditorTools
{
    public static class CombatSandboxCreator
    {
        private const string MenuPath = "Tools/Lost City/Create Combat Sandbox";

        private const string ScenePath = "Assets/_Project/Scenes/CombatSandbox.unity";
        private const string PrefabRoot = "Assets/_Project/Prefabs/CombatSandbox";
        private const string ScriptableObjectRoot = "Assets/_Project/ScriptableObjects/CombatSandbox";
        private const string InputRoot = "Assets/_Project/Settings/Input";
        private const string GeneratedArtRoot = "Assets/_Project/Art/CombatSandbox/Generated";

        private const string InputActionsPath = InputRoot + "/CombatSandbox.inputactions";
        private const string MoveActionReferencePath = InputRoot + "/CombatSandbox_Move.inputactionreference.asset";
        private const string FireActionReferencePath = InputRoot + "/CombatSandbox_Fire.inputactionreference.asset";
        private const string AimActionReferencePath = InputRoot + "/CombatSandbox_Aim.inputactionreference.asset";

        private const string PlayerPrefabPath = PrefabRoot + "/Player.prefab";
        private const string PlayerProjectilePrefabPath = PrefabRoot + "/Projectile_Player.prefab";
        private const string OrbProjectilePrefabPath = PrefabRoot + "/Projectile_Orb.prefab";
        private const string EnemyPrefabPath = PrefabRoot + "/MemoryFragmentEnemy.prefab";

        private const string PistolProjectileDefinitionPath = ScriptableObjectRoot + "/PistolProjectileDefinition.asset";
        private const string OrbProjectileDefinitionPath = ScriptableObjectRoot + "/OrbProjectileDefinition.asset";
        private const string PistolWeaponDefinitionPath = ScriptableObjectRoot + "/PistolWeaponDefinition.asset";
        private const string OrbWeaponDefinitionPath = ScriptableObjectRoot + "/OrbWeaponDefinition.asset";
        private const string EnemyDefinitionPath = ScriptableObjectRoot + "/MemoryFragmentEnemyDefinition.asset";

        private const string SquareSpritePath = GeneratedArtRoot + "/SandboxSquare.png";
        private const string CircleSpritePath = GeneratedArtRoot + "/SandboxCircle.png";

        [MenuItem(MenuPath)]
        public static void CreateCombatSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();

            Sprite squareSprite = CreateSpriteTexture(SquareSpritePath, false);
            Sprite circleSprite = CreateSpriteTexture(CircleSpritePath, true);
            InputReferences inputReferences = CreateInputActions();

            ProjectileDefinition pistolProjectileDefinition = CreateProjectileDefinition(
                PistolProjectileDefinitionPath,
                damage: 18f,
                speed: 22f,
                lifetimeSeconds: 2f);

            ProjectileDefinition orbProjectileDefinition = CreateProjectileDefinition(
                OrbProjectileDefinitionPath,
                damage: 10f,
                speed: 16f,
                lifetimeSeconds: 2.5f);

            EnemyDefinition enemyDefinition = CreateEnemyDefinition(EnemyDefinitionPath);

            Projectile playerProjectilePrefab = CreateProjectilePrefab(
                PlayerProjectilePrefabPath,
                "Projectile_Player",
                circleSprite,
                new Color(1f, 0.86f, 0.25f, 1f),
                new Vector3(0.25f, 0.25f, 1f),
                radius: 0.12f,
                pistolProjectileDefinition);

            Projectile orbProjectilePrefab = CreateProjectilePrefab(
                OrbProjectilePrefabPath,
                "Projectile_Orb",
                circleSprite,
                new Color(0.35f, 0.75f, 1f, 1f),
                new Vector3(0.2f, 0.2f, 1f),
                radius: 0.1f,
                orbProjectileDefinition);

            WeaponDefinition pistolWeaponDefinition = CreateWeaponDefinition(
                PistolWeaponDefinitionPath,
                "Pistol",
                playerProjectilePrefab,
                pistolProjectileDefinition,
                cooldownSeconds: 0.25f,
                range: 14f);

            WeaponDefinition orbWeaponDefinition = CreateWeaponDefinition(
                OrbWeaponDefinitionPath,
                "Memory Orb",
                orbProjectilePrefab,
                orbProjectileDefinition,
                cooldownSeconds: 0.8f,
                range: 12f);

            GameObject enemyPrefab = CreateEnemyPrefab(EnemyPrefabPath, circleSprite, enemyDefinition);
            GameObject playerPrefab = CreatePlayerPrefab(PlayerPrefabPath, circleSprite, inputReferences, pistolWeaponDefinition, orbWeaponDefinition);

            CreateScene(squareSprite, playerPrefab, enemyPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Combat Sandbox Created",
                "CombatSandbox scene, prefabs, ScriptableObjects, generated sprites, and Input Actions were created. Press Play in Assets/_Project/Scenes/CombatSandbox.unity.",
                "OK");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(ScriptableObjectRoot);
            Directory.CreateDirectory(InputRoot);
            Directory.CreateDirectory(GeneratedArtRoot);
            AssetDatabase.Refresh();
        }

        private static InputReferences CreateInputActions()
        {
            DeleteAssetIfExists(MoveActionReferencePath);
            DeleteAssetIfExists(FireActionReferencePath);
            DeleteAssetIfExists(AimActionReferencePath);
            DeleteAssetIfExists(InputActionsPath);

            InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            inputActions.name = "CombatSandbox";

            InputActionMap playerMap = inputActions.AddActionMap("Player");

            // InputAction moveAction = playerMap.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            // moveAction.AddCompositeBinding("2DVector")
            //     .With("Up", "<Keyboard>/w")
            //     .With("Down", "<Keyboard>/s")
            //     .With("Left", "<Keyboard>/a")
            //     .With("Right", "<Keyboard>/d");
            // moveAction.AddCompositeBinding("2DVector")
            //     .With("Up", "<Keyboard>/upArrow")
            //     .With("Down", "<Keyboard>/downArrow")
            //     .With("Left", "<Keyboard>/leftArrow")
            //     .With("Right", "<Keyboard>/rightArrow");

            // playerMap.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton", expectedControlType: "Button");
            // playerMap.AddAction("Aim", InputActionType.Value, "<Mouse>/position", expectedControlType: "Vector2");

            InputAction moveAction = playerMap.AddAction(
                    "Move",
                    InputActionType.Value
                );
            moveAction.expectedControlType = "Vector2";

            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            InputAction fireAction = playerMap.AddAction(
                "Fire",
                InputActionType.Button,
                "<Mouse>/leftButton"
            );
            fireAction.expectedControlType = "Button";

            InputAction aimAction = playerMap.AddAction(
                "Aim",
                InputActionType.Value,
                "<Mouse>/position"
            );
            aimAction.expectedControlType = "Vector2";

            File.WriteAllText(InputActionsPath, inputActions.ToJson());
            UnityEngine.Object.DestroyImmediate(inputActions);

            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
            InputActionAsset importedActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (importedActions == null)
            {
                throw new InvalidOperationException($"Failed to import Input Actions asset at {InputActionsPath}.");
            }

            InputActionReference moveReference = CreateActionReference(importedActions, "Player/Move", MoveActionReferencePath);
            InputActionReference fireReference = CreateActionReference(importedActions, "Player/Fire", FireActionReferencePath);
            InputActionReference aimReference = CreateActionReference(importedActions, "Player/Aim", AimActionReferencePath);

            return new InputReferences(importedActions, moveReference, fireReference, aimReference);
        }

        private static InputActionReference CreateActionReference(InputActionAsset inputActions, string actionPath, string assetPath)
        {
            InputAction action = inputActions.FindAction(actionPath, throwIfNotFound: true);
            InputActionReference reference = InputActionReference.Create(action);
            reference.name = action.name + "Reference";
            AssetDatabase.CreateAsset(reference, assetPath);
            return AssetDatabase.LoadAssetAtPath<InputActionReference>(assetPath);
        }

        private static ProjectileDefinition CreateProjectileDefinition(string path, float damage, float speed, float lifetimeSeconds)
        {
            ProjectileDefinition definition = CreateScriptableObject<ProjectileDefinition>(path);
            SetFloat(definition, "damage", damage);
            SetFloat(definition, "speed", speed);
            SetFloat(definition, "lifetimeSeconds", lifetimeSeconds);
            return definition;
        }

        private static EnemyDefinition CreateEnemyDefinition(string path)
        {
            EnemyDefinition definition = CreateScriptableObject<EnemyDefinition>(path);
            SetFloat(definition, "maxHealth", 45f);
            SetFloat(definition, "moveSpeed", 3.5f);
            SetFloat(definition, "stoppingDistance", 0.75f);
            SetFloat(definition, "contactDamage", 12f);
            SetFloat(definition, "contactCooldownSeconds", 0.75f);
            return definition;
        }

        private static WeaponDefinition CreateWeaponDefinition(
            string path,
            string displayName,
            Projectile projectilePrefab,
            ProjectileDefinition projectileDefinition,
            float cooldownSeconds,
            float range)
        {
            WeaponDefinition definition = CreateScriptableObject<WeaponDefinition>(path);
            SetString(definition, "displayName", displayName);
            SetObject(definition, "projectilePrefab", projectilePrefab);
            SetObject(definition, "projectileDefinition", projectileDefinition);
            SetFloat(definition, "cooldownSeconds", cooldownSeconds);
            SetFloat(definition, "range", range);
            return definition;
        }

        private static Projectile CreateProjectilePrefab(
            string path,
            string objectName,
            Sprite sprite,
            Color color,
            Vector3 scale,
            float radius,
            ProjectileDefinition defaultDefinition)
        {
            DeleteAssetIfExists(path);

            GameObject projectileObject = new GameObject(objectName);
            projectileObject.transform.localScale = scale;

            SpriteRenderer spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 5;

            Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D circleCollider = projectileObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = radius;

            Projectile projectile = projectileObject.AddComponent<Projectile>();
            SetObject(projectile, "defaultDefinition", defaultDefinition);
            SetBool(projectile, "destroyOnNonDamageableHit", true);

            GameObject prefab = SavePrefab(projectileObject, path);
            return prefab.GetComponent<Projectile>();
        }

        private static GameObject CreateEnemyPrefab(string path, Sprite sprite, EnemyDefinition definition)
        {
            DeleteAssetIfExists(path);

            GameObject enemyObject = new GameObject("MemoryFragmentEnemy");

            SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
            spriteRenderer.sortingOrder = 2;

            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D circleCollider = enemyObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = false;
            circleCollider.radius = 0.45f;

            TeamMember teamMember = enemyObject.AddComponent<TeamMember>();
            SetEnum(teamMember, "team", CombatTeam.Enemy);

            Damageable damageable = enemyObject.AddComponent<Damageable>();
            SetFloat(damageable, "maxHealth", 45f);
            SetBool(damageable, "destroyOnDeath", false);

            MemoryFragmentEnemy enemy = enemyObject.AddComponent<MemoryFragmentEnemy>();
            SetObject(enemy, "definition", definition);
            SetObject(enemy, "target", null);

            return SavePrefab(enemyObject, path);
        }

        private static GameObject CreatePlayerPrefab(
            string path,
            Sprite sprite,
            InputReferences inputReferences,
            WeaponDefinition pistolWeaponDefinition,
            WeaponDefinition orbWeaponDefinition)
        {
            DeleteAssetIfExists(path);

            GameObject playerObject = new GameObject("Player");

            SpriteRenderer spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(0.25f, 0.95f, 1f, 1f);
            spriteRenderer.sortingOrder = 3;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D circleCollider = playerObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = false;
            circleCollider.radius = 0.45f;

            TeamMember teamMember = playerObject.AddComponent<TeamMember>();
            SetEnum(teamMember, "team", CombatTeam.Player);

            Damageable damageable = playerObject.AddComponent<Damageable>();
            SetFloat(damageable, "maxHealth", 100f);
            SetBool(damageable, "destroyOnDeath", false);

            PlayerInputReader inputReader = playerObject.AddComponent<PlayerInputReader>();
            SetObject(inputReader, "moveActionReference", inputReferences.Move);
            SetObject(inputReader, "fireActionReference", inputReferences.Fire);

            PlayerMotor playerMotor = playerObject.AddComponent<PlayerMotor>();
            SetObject(playerMotor, "inputReader", inputReader);
            SetFloat(playerMotor, "moveSpeed", 7f);

            GameObject aimRootObject = new GameObject("AimRoot");
            aimRootObject.transform.SetParent(playerObject.transform);
            aimRootObject.transform.localPosition = Vector3.zero;

            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(aimRootObject.transform);
            muzzleObject.transform.localPosition = new Vector3(0.8f, 0f, 0f);

            PlayerAim playerAim = playerObject.AddComponent<PlayerAim>();
            SetObject(playerAim, "yawRoot", aimRootObject.transform);
            SetObject(playerAim, "aimCamera", null);
            SetFloat(playerAim, "turnSpeedDegrees", 1080f);

            PistolWeapon pistolWeapon = aimRootObject.AddComponent<PistolWeapon>();
            SetObject(pistolWeapon, "inputReader", inputReader);
            SetObject(pistolWeapon, "playerAim", playerAim);
            SetObject(pistolWeapon, "muzzle", muzzleObject.transform);
            SetObject(pistolWeapon, "weaponDefinition", pistolWeaponDefinition);
            SetEnum(pistolWeapon, "sourceTeam", CombatTeam.Player);

            GameObject orbObject = new GameObject("MemoryOrb");
            orbObject.transform.SetParent(playerObject.transform);
            orbObject.transform.localPosition = new Vector3(1.25f, 1.25f, 0f);

            SpriteRenderer orbRenderer = orbObject.AddComponent<SpriteRenderer>();
            orbRenderer.sprite = sprite;
            orbRenderer.color = new Color(0.45f, 0.65f, 1f, 1f);
            orbRenderer.sortingOrder = 4;

            MemoryOrbWeapon orbWeapon = orbObject.AddComponent<MemoryOrbWeapon>();
            SetObject(orbWeapon, "weaponDefinition", orbWeaponDefinition);
            SetObject(orbWeapon, "muzzle", orbObject.transform);
            SetVector3(orbWeapon, "localFollowOffset", new Vector3(1.25f, 1.25f, 0f));
            SetFloat(orbWeapon, "followSharpness", 14f);
            SetEnum(orbWeapon, "sourceTeam", CombatTeam.Player);

            PlayerDeathHandler deathHandler = playerObject.AddComponent<PlayerDeathHandler>();
            SetFloat(deathHandler, "restartDelaySeconds", 1.25f);
            SetObjectArray(deathHandler, "disableOnDeath", new UnityEngine.Object[] { playerMotor, pistolWeapon, orbWeapon });

            return SavePrefab(playerObject, path);
        }

        private static void CreateScene(Sprite squareSprite, GameObject playerPrefab, GameObject enemyPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("CombatSandbox");

            GameObject managerObject = new GameObject("CombatGameManager");
            managerObject.transform.SetParent(root.transform);
            managerObject.AddComponent<CombatGameManager>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);

            GameObject arenaObject = new GameObject("Arena");
            arenaObject.transform.SetParent(root.transform);
            arenaObject.transform.position = Vector3.zero;
            arenaObject.transform.localScale = new Vector3(40f, 40f, 1f);

            SpriteRenderer arenaRenderer = arenaObject.AddComponent<SpriteRenderer>();
            arenaRenderer.sprite = squareSprite;
            arenaRenderer.color = new Color(0.18f, 0.18f, 0.2f, 1f);
            arenaRenderer.sortingOrder = -10;

            GameObject playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerObject.name = "Player";
            playerObject.transform.SetParent(root.transform);
            playerObject.transform.position = Vector3.zero;

            PlayerAim playerAim = playerObject.GetComponent<PlayerAim>();
            SetObject(playerAim, "aimCamera", camera);

            SimpleTopDownCameraFollow cameraFollow = cameraObject.AddComponent<SimpleTopDownCameraFollow>();
            SetObject(cameraFollow, "target", playerObject.transform);
            SetVector3(cameraFollow, "offset", new Vector3(0f, 0f, -10f));
            SetFloat(cameraFollow, "followSharpness", 10f);

            GameObject spawnerObject = new GameObject("EnemySpawner");
            spawnerObject.transform.SetParent(root.transform);
            spawnerObject.transform.position = Vector3.zero;

            EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
            SetObject(spawner, "enemyPrefab", enemyPrefab);
            SetObject(spawner, "player", playerObject.transform);
            SetObject(spawner, "arenaCenter", spawnerObject.transform);
            SetFloat(spawner, "spawnIntervalSeconds", 1.25f);
            SetInt(spawner, "initialSpawnCount", 4);
            SetInt(spawner, "maxAliveEnemies", 12);
            SetFloat(spawner, "spawnRadius", 18f);
            SetFloat(spawner, "minimumDistanceFromPlayer", 8f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static Sprite CreateSpriteTexture(string path, bool circle)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            Color32[] pixels = new Color32[size * size];

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool filled = !circle || Vector2.Distance(new Vector2(x, y), center) <= radius;
                    pixels[y * size + x] = filled ? Color.white : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static T CreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            DeleteAssetIfExists(path);
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static GameObject SavePrefab(GameObject source, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);
            return prefab;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, enabled: true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, enabled: true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SetSerializedProperty(target, propertyName, property => property.objectReferenceValue = value);
        }

        private static void SetObjectArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            SetSerializedProperty(target, propertyName, property =>
            {
                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            SetSerializedProperty(target, propertyName, property => property.stringValue = value);
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SetSerializedProperty(target, propertyName, property => property.floatValue = value);
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            SetSerializedProperty(target, propertyName, property => property.intValue = value);
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            SetSerializedProperty(target, propertyName, property => property.boolValue = value);
        }

        private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
        {
            SetSerializedProperty(target, propertyName, property => property.vector3Value = value);
        }

        private static void SetEnum<TEnum>(UnityEngine.Object target, string propertyName, TEnum value) where TEnum : Enum
        {
            SetSerializedProperty(target, propertyName, property => property.enumValueIndex = Convert.ToInt32(value));
        }

        private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, Action<SerializedProperty> apply)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), $"Cannot assign serialized field {propertyName} on a null target.");
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            apply(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private readonly struct InputReferences
        {
            public InputReferences(
                InputActionAsset asset,
                InputActionReference move,
                InputActionReference fire,
                InputActionReference aim)
            {
                Asset = asset;
                Move = move;
                Fire = fire;
                Aim = aim;
            }

            public InputActionAsset Asset { get; }
            public InputActionReference Move { get; }
            public InputActionReference Fire { get; }
            public InputActionReference Aim { get; }
        }
    }
}
