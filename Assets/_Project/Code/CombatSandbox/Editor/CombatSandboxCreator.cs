using System;
using System.Collections.Generic;
using System.IO;
using LostCity.CombatSandbox;
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
        private const string UiPointActionReferencePath = InputRoot + "/CombatSandbox_UI_Point.inputactionreference.asset";
        private const string UiClickActionReferencePath = InputRoot + "/CombatSandbox_UI_Click.inputactionreference.asset";
        private const string UiScrollWheelActionReferencePath = InputRoot + "/CombatSandbox_UI_ScrollWheel.inputactionreference.asset";
        private const string UiNavigateActionReferencePath = InputRoot + "/CombatSandbox_UI_Navigate.inputactionreference.asset";
        private const string UiSubmitActionReferencePath = InputRoot + "/CombatSandbox_UI_Submit.inputactionreference.asset";
        private const string UiCancelActionReferencePath = InputRoot + "/CombatSandbox_UI_Cancel.inputactionreference.asset";

        private const string PlayerPrefabPath = PrefabRoot + "/Player.prefab";
        private const string PlayerProjectilePrefabPath = PrefabRoot + "/Projectile_Player.prefab";
        private const string OrbProjectilePrefabPath = PrefabRoot + "/Projectile_Orb.prefab";
        private const string EnemyProjectilePrefabPath = PrefabRoot + "/Projectile_Enemy.prefab";
        private const string ChaserEnemyPrefabPath = PrefabRoot + "/Enemy_Chaser.prefab";
        private const string ChargerEnemyPrefabPath = PrefabRoot + "/Enemy_Charger.prefab";
        private const string ShooterEnemyPrefabPath = PrefabRoot + "/Enemy_Shooter.prefab";
        private const string TankEnemyPrefabPath = PrefabRoot + "/Enemy_Tank.prefab";
        private const string WardenBossPrefabPath = PrefabRoot + "/Boss_TheWarden.prefab";
        private const string LegacyEnemyPrefabPath = PrefabRoot + "/MemoryFragmentEnemy.prefab";
        private const string XpOrbPrefabPath = PrefabRoot + "/XpOrb.prefab";

        private const string PistolProjectileDefinitionPath = ScriptableObjectRoot + "/PistolProjectileDefinition.asset";
        private const string OrbProjectileDefinitionPath = ScriptableObjectRoot + "/OrbProjectileDefinition.asset";
        private const string EnemyProjectileDefinitionPath = ScriptableObjectRoot + "/EnemyProjectileDefinition.asset";
        private const string PistolWeaponDefinitionPath = ScriptableObjectRoot + "/PistolWeaponDefinition.asset";
        private const string OrbWeaponDefinitionPath = ScriptableObjectRoot + "/OrbWeaponDefinition.asset";
        private const string ChaserEnemyDefinitionPath = ScriptableObjectRoot + "/ChaserEnemyDefinition.asset";
        private const string ChargerEnemyDefinitionPath = ScriptableObjectRoot + "/ChargerEnemyDefinition.asset";
        private const string ShooterEnemyDefinitionPath = ScriptableObjectRoot + "/ShooterEnemyDefinition.asset";
        private const string TankEnemyDefinitionPath = ScriptableObjectRoot + "/TankEnemyDefinition.asset";
        private const string LegacyEnemyDefinitionPath = ScriptableObjectRoot + "/MemoryFragmentEnemyDefinition.asset";

        private const string SquareSpritePath = GeneratedArtRoot + "/SandboxSquare.png";
        private const string CircleSpritePath = GeneratedArtRoot + "/SandboxCircle.png";

        [MenuItem(MenuPath)]
        public static void CreateCombatSandbox()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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

            ProjectileDefinition enemyProjectileDefinition = CreateProjectileDefinition(
                EnemyProjectileDefinitionPath,
                damage: 6f,
                speed: 9f,
                lifetimeSeconds: 4f);

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

            Projectile enemyProjectilePrefab = CreateProjectilePrefab(
                EnemyProjectilePrefabPath,
                "Projectile_Enemy",
                circleSprite,
                new Color(1f, 0.35f, 0.75f, 1f),
                new Vector3(0.18f, 0.18f, 1f),
                radius: 0.1f,
                enemyProjectileDefinition);

            XpOrb xpOrbPrefab = CreateXpOrbPrefab(XpOrbPrefabPath, circleSprite);

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

            DeleteAssetIfExists(LegacyEnemyPrefabPath);
            DeleteAssetIfExists(LegacyEnemyDefinitionPath);

            EnemyDefinition chaserDefinition = CreateEnemyDefinition(
                ChaserEnemyDefinitionPath,
                "Chaser",
                EnemyArchetype.Chaser,
                maxHealth: 42f,
                moveSpeed: 3.8f,
                stoppingDistance: 0.75f,
                contactDamage: 5f,
                contactCooldownSeconds: 0.75f,
                chargeTriggerRange: 0f,
                chargeSpeed: 0f,
                chargeDurationSeconds: 0f,
                chargeCooldownSeconds: 0f,
                shootRange: 0f,
                shootCooldownSeconds: 0f,
                enemyProjectilePrefab,
                enemyProjectileDefinition);

            EnemyDefinition chargerDefinition = CreateEnemyDefinition(
                ChargerEnemyDefinitionPath,
                "Charger",
                EnemyArchetype.Charger,
                maxHealth: 55f,
                moveSpeed: 2.7f,
                stoppingDistance: 0.85f,
                contactDamage: 8f,
                contactCooldownSeconds: 0.75f,
                chargeTriggerRange: 7f,
                chargeSpeed: 10f,
                chargeDurationSeconds: 0.5f,
                chargeCooldownSeconds: 2.6f,
                shootRange: 0f,
                shootCooldownSeconds: 0f,
                enemyProjectilePrefab,
                enemyProjectileDefinition);

            EnemyDefinition shooterDefinition = CreateEnemyDefinition(
                ShooterEnemyDefinitionPath,
                "Shooter",
                EnemyArchetype.Shooter,
                maxHealth: 34f,
                moveSpeed: 2.4f,
                stoppingDistance: 6f,
                contactDamage: 4f,
                contactCooldownSeconds: 0.9f,
                chargeTriggerRange: 0f,
                chargeSpeed: 0f,
                chargeDurationSeconds: 0f,
                chargeCooldownSeconds: 0f,
                shootRange: 10f,
                shootCooldownSeconds: 1.6f,
                enemyProjectilePrefab,
                enemyProjectileDefinition);

            EnemyDefinition tankDefinition = CreateEnemyDefinition(
                TankEnemyDefinitionPath,
                "Tank",
                EnemyArchetype.Tank,
                maxHealth: 120f,
                moveSpeed: 1.8f,
                stoppingDistance: 0.9f,
                contactDamage: 10f,
                contactCooldownSeconds: 0.9f,
                chargeTriggerRange: 0f,
                chargeSpeed: 0f,
                chargeDurationSeconds: 0f,
                chargeCooldownSeconds: 0f,
                shootRange: 0f,
                shootCooldownSeconds: 0f,
                enemyProjectilePrefab,
                enemyProjectileDefinition);

            GameObject chaserPrefab = CreateEnemyPrefab(ChaserEnemyPrefabPath, "Enemy_Chaser", circleSprite, squareSprite, chaserDefinition, xpOrbPrefab, new Color(1f, 0.25f, 0.25f, 1f), 1);
            GameObject chargerPrefab = CreateEnemyPrefab(ChargerEnemyPrefabPath, "Enemy_Charger", circleSprite, squareSprite, chargerDefinition, xpOrbPrefab, new Color(1f, 0.55f, 0.15f, 1f), 2);
            GameObject shooterPrefab = CreateEnemyPrefab(ShooterEnemyPrefabPath, "Enemy_Shooter", circleSprite, squareSprite, shooterDefinition, xpOrbPrefab, new Color(0.95f, 0.25f, 1f, 1f), 2);
            GameObject tankPrefab = CreateEnemyPrefab(TankEnemyPrefabPath, "Enemy_Tank", circleSprite, squareSprite, tankDefinition, xpOrbPrefab, new Color(0.55f, 0.35f, 1f, 1f), 3);
            GameObject wardenBossPrefab = CreateWardenBossPrefab(WardenBossPrefabPath, squareSprite, enemyProjectilePrefab, enemyProjectileDefinition, xpOrbPrefab, new[] { chaserPrefab, chargerPrefab, shooterPrefab });
            GameObject playerPrefab = CreatePlayerPrefab(PlayerPrefabPath, circleSprite, squareSprite, inputReferences, pistolWeaponDefinition, orbWeaponDefinition);

            CreateScene(squareSprite, playerPrefab, wardenBossPrefab, inputReferences, new[]
            {
                new SpawnPrefabWeight(chaserPrefab, 55f),
                new SpawnPrefabWeight(chargerPrefab, 20f),
                new SpawnPrefabWeight(shooterPrefab, 17f),
                new SpawnPrefabWeight(tankPrefab, 8f)
            });

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
            DeleteAssetIfExists(UiPointActionReferencePath);
            DeleteAssetIfExists(UiClickActionReferencePath);
            DeleteAssetIfExists(UiScrollWheelActionReferencePath);
            DeleteAssetIfExists(UiNavigateActionReferencePath);
            DeleteAssetIfExists(UiSubmitActionReferencePath);
            DeleteAssetIfExists(UiCancelActionReferencePath);
            DeleteAssetIfExists(InputActionsPath);

            InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            inputActions.name = "CombatSandbox";

            InputActionMap playerMap = inputActions.AddActionMap("Player");

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

            InputActionMap uiMap = inputActions.AddActionMap("UI");

            InputAction pointAction = uiMap.AddAction(
                "Point",
                InputActionType.PassThrough,
                "<Pointer>/position");
            pointAction.expectedControlType = "Vector2";

            InputAction clickAction = uiMap.AddAction(
                "Click",
                InputActionType.PassThrough,
                "<Mouse>/leftButton");
            clickAction.expectedControlType = "Button";

            InputAction scrollWheelAction = uiMap.AddAction(
                "ScrollWheel",
                InputActionType.PassThrough,
                "<Mouse>/scroll");
            scrollWheelAction.expectedControlType = "Vector2";

            InputAction navigateAction = uiMap.AddAction(
                "Navigate",
                InputActionType.Value);
            navigateAction.expectedControlType = "Vector2";
            navigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            navigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            InputAction submitAction = uiMap.AddAction(
                "Submit",
                InputActionType.Button,
                "<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/space");
            submitAction.expectedControlType = "Button";

            InputAction cancelAction = uiMap.AddAction(
                "Cancel",
                InputActionType.Button,
                "<Keyboard>/escape");
            cancelAction.expectedControlType = "Button";

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
            InputActionReference uiPointReference = CreateActionReference(importedActions, "UI/Point", UiPointActionReferencePath);
            InputActionReference uiClickReference = CreateActionReference(importedActions, "UI/Click", UiClickActionReferencePath);
            InputActionReference uiScrollWheelReference = CreateActionReference(importedActions, "UI/ScrollWheel", UiScrollWheelActionReferencePath);
            InputActionReference uiNavigateReference = CreateActionReference(importedActions, "UI/Navigate", UiNavigateActionReferencePath);
            InputActionReference uiSubmitReference = CreateActionReference(importedActions, "UI/Submit", UiSubmitActionReferencePath);
            InputActionReference uiCancelReference = CreateActionReference(importedActions, "UI/Cancel", UiCancelActionReferencePath);

            return new InputReferences(
                importedActions,
                moveReference,
                fireReference,
                aimReference,
                uiPointReference,
                uiClickReference,
                uiScrollWheelReference,
                uiNavigateReference,
                uiSubmitReference,
                uiCancelReference);
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
            return SaveAndReloadAsset(definition, path);
        }

        private static EnemyDefinition CreateEnemyDefinition(
            string path,
            string displayName,
            EnemyArchetype archetype,
            float maxHealth,
            float moveSpeed,
            float stoppingDistance,
            float contactDamage,
            float contactCooldownSeconds,
            float chargeTriggerRange,
            float chargeSpeed,
            float chargeDurationSeconds,
            float chargeCooldownSeconds,
            float shootRange,
            float shootCooldownSeconds,
            Projectile projectilePrefab,
            ProjectileDefinition projectileDefinition)
        {
            EnemyDefinition definition = CreateScriptableObject<EnemyDefinition>(path);
            SetString(definition, "displayName", displayName);
            SetEnum(definition, "archetype", archetype);
            SetFloat(definition, "maxHealth", maxHealth);
            SetFloat(definition, "moveSpeed", moveSpeed);
            SetFloat(definition, "stoppingDistance", stoppingDistance);
            SetFloat(definition, "contactDamage", contactDamage);
            SetFloat(definition, "contactCooldownSeconds", contactCooldownSeconds);
            SetFloat(definition, "chargeTriggerRange", chargeTriggerRange);
            SetFloat(definition, "chargeSpeed", chargeSpeed);
            SetFloat(definition, "chargeDurationSeconds", chargeDurationSeconds);
            SetFloat(definition, "chargeCooldownSeconds", chargeCooldownSeconds);
            SetFloat(definition, "shootRange", shootRange);
            SetFloat(definition, "shootCooldownSeconds", shootCooldownSeconds);
            SetObject(definition, "projectilePrefab", projectilePrefab);
            SetObject(definition, "projectileDefinition", projectileDefinition);
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
            definition = SaveAndReloadAsset(definition, path);

            if (definition.ProjectilePrefab == null || definition.ProjectileDefinition == null)
            {
                throw new InvalidOperationException($"{path} was created without projectile references.");
            }

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

        private static XpOrb CreateXpOrbPrefab(string path, Sprite sprite)
        {
            DeleteAssetIfExists(path);

            GameObject xpOrbObject = new GameObject("XpOrb");
            xpOrbObject.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

            SpriteRenderer spriteRenderer = xpOrbObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(0.35f, 1f, 0.45f, 1f);
            spriteRenderer.sortingOrder = 4;

            Rigidbody2D body = xpOrbObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D circleCollider = xpOrbObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.35f;

            XpOrb xpOrb = xpOrbObject.AddComponent<XpOrb>();
            SetInt(xpOrb, "experienceValue", 1);
            SetFloat(xpOrb, "attractionRadius", 3.5f);
            SetFloat(xpOrb, "moveSpeed", 8f);

            GameObject prefab = SavePrefab(xpOrbObject, path);
            return prefab.GetComponent<XpOrb>();
        }

        private static GameObject CreateEnemyPrefab(
            string path,
            string objectName,
            Sprite sprite,
            Sprite squareSprite,
            EnemyDefinition definition,
            XpOrb xpOrbPrefab,
            Color color,
            int experienceValue)
        {
            DeleteAssetIfExists(path);

            GameObject enemyObject = new GameObject(objectName);

            SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
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

            MinimapTrackedEntity minimapTrackedEntity = enemyObject.AddComponent<MinimapTrackedEntity>();
            SetEnum(minimapTrackedEntity, "entityType", MinimapEntityType.Enemy);

            Damageable damageable = enemyObject.AddComponent<Damageable>();
            SetFloat(damageable, "maxHealth", definition != null ? definition.MaxHealth : 45f);
            SetBool(damageable, "destroyOnDeath", false);

            MemoryFragmentEnemy enemy = enemyObject.AddComponent<MemoryFragmentEnemy>();
            SetObject(enemy, "definition", definition);
            SetObject(enemy, "target", null);

            HitFlash hitFlash = enemyObject.AddComponent<HitFlash>();
            SetObject(hitFlash, "targetRenderer", spriteRenderer);
            SetColor(hitFlash, "flashColor", Color.white);
            SetFloat(hitFlash, "flashSeconds", 0.08f);

            AddWorldHealthBar(enemyObject, squareSprite, new Vector3(0f, 0.72f, 0f), new Color(1f, 0.2f, 0.2f, 1f), hideWhenFull: false);

            XpDropper xpDropper = enemyObject.AddComponent<XpDropper>();
            SetObject(xpDropper, "xpOrbPrefab", xpOrbPrefab);
            SetInt(xpDropper, "experienceValue", experienceValue);

            return SavePrefab(enemyObject, path);
        }

        private static GameObject CreateWardenBossPrefab(
            string path,
            Sprite squareSprite,
            Projectile burstProjectilePrefab,
            ProjectileDefinition burstProjectileDefinition,
            XpOrb xpOrbPrefab,
            GameObject[] summonPrefabs)
        {
            DeleteAssetIfExists(path);

            GameObject bossObject = new GameObject("Boss_TheWarden");
            bossObject.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            SpriteRenderer spriteRenderer = bossObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = squareSprite;
            spriteRenderer.color = new Color(1f, 0.85f, 0.2f, 1f);
            spriteRenderer.sortingOrder = 2;

            Rigidbody2D body = bossObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D collider = bossObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = new Vector2(0.9f, 0.9f);

            TeamMember teamMember = bossObject.AddComponent<TeamMember>();
            SetEnum(teamMember, "team", CombatTeam.Enemy);

            MinimapTrackedEntity minimapTrackedEntity = bossObject.AddComponent<MinimapTrackedEntity>();
            SetEnum(minimapTrackedEntity, "entityType", MinimapEntityType.Boss);

            Damageable damageable = bossObject.AddComponent<Damageable>();
            SetFloat(damageable, "maxHealth", 320f);
            SetBool(damageable, "destroyOnDeath", false);

            WardenBoss wardenBoss = bossObject.AddComponent<WardenBoss>();
            SetFloat(wardenBoss, "moveSpeed", 2f);
            SetFloat(wardenBoss, "stoppingDistance", 3f);
            SetFloat(wardenBoss, "contactDamage", 12f);
            SetFloat(wardenBoss, "contactCooldownSeconds", 0.8f);
            SetFloat(wardenBoss, "chargeTriggerRange", 10f);
            SetFloat(wardenBoss, "chargeSpeed", 11f);
            SetFloat(wardenBoss, "chargeDurationSeconds", 0.65f);
            SetFloat(wardenBoss, "chargeCooldownSeconds", 4f);
            SetObjectArray(wardenBoss, "summonPrefabs", summonPrefabs);
            SetInt(wardenBoss, "summonCount", 2);
            SetFloat(wardenBoss, "summonRadius", 2.2f);
            SetFloat(wardenBoss, "summonCooldownSeconds", 9f);
            SetObject(wardenBoss, "burstProjectilePrefab", burstProjectilePrefab);
            SetObject(wardenBoss, "burstProjectileDefinition", burstProjectileDefinition);
            SetInt(wardenBoss, "burstProjectileCount", 12);
            SetFloat(wardenBoss, "burstCooldownSeconds", 5f);

            HitFlash hitFlash = bossObject.AddComponent<HitFlash>();
            SetObject(hitFlash, "targetRenderer", spriteRenderer);
            SetColor(hitFlash, "flashColor", Color.white);
            SetFloat(hitFlash, "flashSeconds", 0.08f);

            AddWorldHealthBar(bossObject, squareSprite, new Vector3(0f, 0.8f, 0f), new Color(1f, 0.85f, 0.2f, 1f), hideWhenFull: false, width: 1.4f);

            XpDropper xpDropper = bossObject.AddComponent<XpDropper>();
            SetObject(xpDropper, "xpOrbPrefab", xpOrbPrefab);
            SetInt(xpDropper, "experienceValue", 12);

            return SavePrefab(bossObject, path);
        }

        private static GameObject CreatePlayerPrefab(
            string path,
            Sprite sprite,
            Sprite squareSprite,
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
            SetFloat(damageable, "maxHealth", 150f);
            SetBool(damageable, "destroyOnDeath", false);

            PlayerStats playerStats = playerObject.AddComponent<PlayerStats>();
            SetFloat(playerStats, "maxHp", 150f);
            SetFloat(playerStats, "currentHp", 150f);
            SetFloat(playerStats, "attack", 1f);
            SetFloat(playerStats, "defense", 3f);
            SetFloat(playerStats, "moveSpeed", 7f);
            SetFloat(playerStats, "fireRate", 1f);
            SetFloat(playerStats, "critChance", 0.05f);
            SetFloat(playerStats, "critDamage", 1.5f);
            SetFloat(playerStats, "dodgeChance", 0.05f);
            SetFloat(playerStats, "xpMultiplier", 1f);
            SetFloat(playerStats, "pickupRadius", 3.5f);

            CombatUpgradeStats upgradeStats = playerObject.AddComponent<CombatUpgradeStats>();
            SetFloat(upgradeStats, "fireRateMultiplier", 1f);
            SetFloat(upgradeStats, "projectileDamageMultiplier", 1f);
            SetInt(upgradeStats, "extraDroneProjectiles", 0);

            MinimapTrackedEntity minimapTrackedEntity = playerObject.AddComponent<MinimapTrackedEntity>();
            SetEnum(minimapTrackedEntity, "entityType", MinimapEntityType.Player);

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

            GameObject facingIndicatorObject = new GameObject("FacingIndicator");
            facingIndicatorObject.transform.SetParent(aimRootObject.transform);
            facingIndicatorObject.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            facingIndicatorObject.transform.localScale = new Vector3(0.8f, 0.14f, 1f);

            SpriteRenderer facingIndicatorRenderer = facingIndicatorObject.AddComponent<SpriteRenderer>();
            facingIndicatorRenderer.sprite = squareSprite;
            facingIndicatorRenderer.color = new Color(0.8f, 1f, 1f, 1f);
            facingIndicatorRenderer.sortingOrder = 6;

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

            PlayerExperience playerExperience = playerObject.AddComponent<PlayerExperience>();
            SetObject(playerExperience, "upgradeStats", upgradeStats);
            SetObject(playerExperience, "upgradeSelectionController", null);
            SetInt(playerExperience, "currentLevel", 1);
            SetInt(playerExperience, "currentExperience", 0);
            SetInt(playerExperience, "experienceToNextLevel", 5);
            SetFloat(playerExperience, "nextLevelExperienceMultiplier", 1.35f);

            AddWorldHealthBar(playerObject, squareSprite, new Vector3(0f, 0.78f, 0f), new Color(0.2f, 1f, 0.35f, 1f), hideWhenFull: false);

            return SavePrefab(playerObject, path);
        }

        private static void CreateScene(
            Sprite squareSprite,
            GameObject playerPrefab,
            GameObject bossPrefab,
            InputReferences inputReferences,
            SpawnPrefabWeight[] enemySpawnWeights)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("CombatSandbox");

            GameObject managerObject = new GameObject("CombatGameManager");
            managerObject.transform.SetParent(root.transform);
            managerObject.AddComponent<CombatGameManager>();

            CreateEventSystem(root.transform, inputReferences);

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

            UpgradeSelectionController upgradeSelectionController = CreateUpgradeChoiceUi(root.transform);
            PlayerExperience playerExperience = playerObject.GetComponent<PlayerExperience>();
            SetObject(playerExperience, "upgradeSelectionController", upgradeSelectionController);

            PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
            CreateCombatHud(root.transform, squareSprite, playerStats, playerExperience);

            SimpleTopDownCameraFollow cameraFollow = cameraObject.AddComponent<SimpleTopDownCameraFollow>();
            SetObject(cameraFollow, "target", playerObject.transform);
            SetVector3(cameraFollow, "offset", new Vector3(0f, 0f, -10f));
            SetFloat(cameraFollow, "followSharpness", 10f);

            GameObject spawnerObject = new GameObject("EnemySpawner");
            spawnerObject.transform.SetParent(root.transform);
            spawnerObject.transform.position = Vector3.zero;

            EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
            SetObject(spawner, "enemyPrefab", enemySpawnWeights != null && enemySpawnWeights.Length > 0 ? enemySpawnWeights[0].Prefab : null);
            SetSpawnEntries(spawner, "spawnEntries", enemySpawnWeights);
            SetObject(spawner, "player", playerObject.transform);
            SetObject(spawner, "arenaCenter", spawnerObject.transform);
            SetFloat(spawner, "spawnIntervalSeconds", 1.35f);
            SetInt(spawner, "initialSpawnCount", 2);
            SetInt(spawner, "maxAliveEnemies", 10);
            SetFloat(spawner, "spawnRadius", 18f);
            SetFloat(spawner, "minimumDistanceFromPlayer", 8f);

            GameObject bossSpawnerObject = new GameObject("BossSpawner");
            bossSpawnerObject.transform.SetParent(root.transform);
            bossSpawnerObject.transform.position = new Vector3(0f, 9f, 0f);

            BossSpawnController bossSpawnController = bossSpawnerObject.AddComponent<BossSpawnController>();
            SetObject(bossSpawnController, "bossPrefab", bossPrefab);
            SetObject(bossSpawnController, "spawnPoint", bossSpawnerObject.transform);
            SetObject(bossSpawnController, "player", playerObject.transform);
            SetFloat(bossSpawnController, "spawnDelaySeconds", 2f);
            SetBool(bossSpawnController, "spawnOnStart", true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void CreateEventSystem(Transform root, InputReferences inputReferences)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(root);
            eventSystemObject.AddComponent<EventSystem>();

            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.actionsAsset = inputReferences.Asset;
            inputModule.point = inputReferences.UiPoint;
            inputModule.leftClick = inputReferences.UiClick;
            inputModule.scrollWheel = inputReferences.UiScrollWheel;
            inputModule.move = inputReferences.UiNavigate;
            inputModule.submit = inputReferences.UiSubmit;
            inputModule.cancel = inputReferences.UiCancel;
        }

        private static void CreateCombatHud(Transform root, Sprite squareSprite, PlayerStats playerStats, PlayerExperience playerExperience)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = new GameObject("CombatHUD");
            canvasObject.transform.SetParent(root);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            canvasObject.AddComponent<CanvasScaler>();

            PlayerHud playerHud = canvasObject.AddComponent<PlayerHud>();

            GameObject statsPanel = CreateAnchoredUiRect(
                "StatsPanel",
                canvasObject.transform,
                new Vector2(190f, 150f),
                new Vector2(16f, -16f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Image statsPanelImage = statsPanel.AddComponent<Image>();
            statsPanelImage.sprite = squareSprite;
            statsPanelImage.color = new Color(0.03f, 0.04f, 0.05f, 0.78f);
            statsPanelImage.raycastTarget = false;

            Text statsText = CreateText("StatsText", statsPanel.transform, font, "LV 1", 16, TextAnchor.UpperLeft, new Vector2(160f, 124f), new Vector2(0f, -6f));
            statsText.raycastTarget = false;

            SetObject(playerHud, "playerStats", playerStats);
            SetObject(playerHud, "playerExperience", playerExperience);
            SetObject(playerHud, "statsText", statsText);

            GameObject minimapPanel = CreateAnchoredUiRect(
                "Minimap",
                canvasObject.transform,
                new Vector2(172f, 172f),
                new Vector2(-16f, -16f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));

            Image minimapBackground = minimapPanel.AddComponent<Image>();
            minimapBackground.sprite = squareSprite;
            minimapBackground.color = new Color(0.03f, 0.04f, 0.05f, 0.78f);
            minimapBackground.raycastTarget = false;

            GameObject markerRootObject = CreateAnchoredUiRect(
                "MarkerRoot",
                minimapPanel.transform,
                new Vector2(150f, 150f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));

            Image markerRootImage = markerRootObject.AddComponent<Image>();
            markerRootImage.sprite = squareSprite;
            markerRootImage.color = new Color(0.08f, 0.09f, 0.1f, 0.95f);
            markerRootImage.raycastTarget = false;

            GameObject markerTemplateObject = CreateAnchoredUiRect(
                "MarkerTemplate",
                markerRootObject.transform,
                new Vector2(6f, 6f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));

            Image markerTemplate = markerTemplateObject.AddComponent<Image>();
            markerTemplate.sprite = squareSprite;
            markerTemplate.color = Color.white;
            markerTemplate.raycastTarget = false;
            markerTemplateObject.SetActive(false);

            MinimapController minimapController = minimapPanel.AddComponent<MinimapController>();
            SetObject(minimapController, "markerRoot", markerRootObject.GetComponent<RectTransform>());
            SetObject(minimapController, "markerTemplate", markerTemplate);
            SetVector2(minimapController, "worldHalfExtents", new Vector2(20f, 20f));
            SetColor(minimapController, "playerColor", new Color(0.25f, 0.95f, 1f, 1f));
            SetColor(minimapController, "enemyColor", new Color(1f, 0.25f, 0.25f, 1f));
            SetColor(minimapController, "bossColor", new Color(1f, 0.85f, 0.2f, 1f));
            SetFloat(minimapController, "refreshIntervalSeconds", 0.25f);
        }

        private static void AddWorldHealthBar(GameObject owner, Sprite squareSprite, Vector3 localPosition, Color fillColor, bool hideWhenFull, float width = 1f)
        {
            GameObject barRoot = new GameObject("HealthBar");
            RectTransform canvasRect = barRoot.AddComponent<RectTransform>();
            barRoot.transform.SetParent(owner.transform);
            barRoot.transform.localPosition = localPosition;
            barRoot.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Canvas canvas = barRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            canvasRect.sizeDelta = new Vector2(120f * width, 16f);

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(barRoot.transform, worldPositionStays: false);
            RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.sprite = squareSprite;
            backgroundImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            backgroundImage.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(barRoot.transform, worldPositionStays: false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.04f, 0.25f);
            fillRect.anchorMax = new Vector2(0.96f, 0.75f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.sprite = squareSprite;
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;

            WorldHealthBar healthBar = owner.AddComponent<WorldHealthBar>();
            SetObject(healthBar, "barRoot", barRoot.transform);
            SetObject(healthBar, "fillImage", fillImage);
            SetObject(healthBar, "fill", fillObject.transform);
            SetBool(healthBar, "hideWhenFull", hideWhenFull);
        }

        private static UpgradeSelectionController CreateUpgradeChoiceUi(Transform root)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = new GameObject("UpgradeChoiceUI");
            canvasObject.transform.SetParent(root);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            UpgradeSelectionController controller = canvasObject.AddComponent<UpgradeSelectionController>();

            GameObject panelObject = CreateUiRect("Panel", canvasObject.transform, new Vector2(520f, 260f), Vector2.zero);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.05f, 0.06f, 0.92f);

            Text titleText = CreateText("Title", panelObject.transform, font, "LEVEL UP", 28, TextAnchor.MiddleCenter, new Vector2(420f, 42f), new Vector2(0f, 88f));

            Button fireRateButton = CreateUpgradeButton(panelObject.transform, font, "+20% Fire Rate", new Vector2(0f, 32f), out Text fireRateText);
            Button damageButton = CreateUpgradeButton(panelObject.transform, font, "+20% Projectile Damage", new Vector2(0f, -28f), out Text damageText);
            Button droneButton = CreateUpgradeButton(panelObject.transform, font, "+1 Drone Projectile", new Vector2(0f, -88f), out Text droneText);

            SetObject(controller, "panelRoot", panelObject);
            SetObject(controller, "titleText", titleText);
            SetObject(controller, "fireRateButton", fireRateButton);
            SetObject(controller, "fireRateButtonText", fireRateText);
            SetObject(controller, "damageButton", damageButton);
            SetObject(controller, "damageButtonText", damageText);
            SetObject(controller, "droneButton", droneButton);
            SetObject(controller, "droneButtonText", droneText);
            SetBool(controller, "pauseWhileChoosing", true);

            panelObject.SetActive(false);
            return controller;
        }

        private static Button CreateUpgradeButton(Transform parent, Font font, string label, Vector2 anchoredPosition, out Text labelText)
        {
            GameObject buttonObject = CreateUiRect(label, parent, new Vector2(420f, 46f), anchoredPosition);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.21f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.24f, 0.28f, 0.32f, 1f);
            colors.pressedColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            labelText = CreateText("Label", buttonObject.transform, font, label, 18, TextAnchor.MiddleCenter, new Vector2(400f, 34f), Vector2.zero);
            return button;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            GameObject textObject = CreateUiRect(name, parent, size, anchoredPosition);
            Text uiText = textObject.AddComponent<Text>();
            uiText.font = font;
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = Color.white;
            uiText.raycastTarget = false;
            return uiText;
        }

        private static GameObject CreateAnchoredUiRect(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchoredPosition,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, worldPositionStays: false);

            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            return gameObject;
        }

        private static GameObject CreateUiRect(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, worldPositionStays: false);

            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            return gameObject;
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
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (loadedAsset == null)
            {
                throw new InvalidOperationException($"Failed to create asset at {path}.");
            }

            return loadedAsset;
        }

        private static GameObject SavePrefab(GameObject source, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to save prefab at {path}.");
            }

            return prefab;
        }

        private static T SaveAndReloadAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (loadedAsset == null)
            {
                throw new InvalidOperationException($"Failed to reload asset at {path}.");
            }

            return loadedAsset;
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

        private static void SetVector2(UnityEngine.Object target, string propertyName, Vector2 value)
        {
            SetSerializedProperty(target, propertyName, property => property.vector2Value = value);
        }

        private static void SetColor(UnityEngine.Object target, string propertyName, Color value)
        {
            SetSerializedProperty(target, propertyName, property => property.colorValue = value);
        }

        private static void SetSpawnEntries(UnityEngine.Object target, string propertyName, SpawnPrefabWeight[] entries)
        {
            SetSerializedProperty(target, propertyName, property =>
            {
                property.arraySize = entries != null ? entries.Length : 0;
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty entryProperty = property.GetArrayElementAtIndex(i);
                    entryProperty.FindPropertyRelative("prefab").objectReferenceValue = entries[i].Prefab;
                    entryProperty.FindPropertyRelative("weight").floatValue = entries[i].Weight;
                }
            });
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
                InputActionReference aim,
                InputActionReference uiPoint,
                InputActionReference uiClick,
                InputActionReference uiScrollWheel,
                InputActionReference uiNavigate,
                InputActionReference uiSubmit,
                InputActionReference uiCancel)
            {
                Asset = asset;
                Move = move;
                Fire = fire;
                Aim = aim;
                UiPoint = uiPoint;
                UiClick = uiClick;
                UiScrollWheel = uiScrollWheel;
                UiNavigate = uiNavigate;
                UiSubmit = uiSubmit;
                UiCancel = uiCancel;
            }

            public InputActionAsset Asset { get; }
            public InputActionReference Move { get; }
            public InputActionReference Fire { get; }
            public InputActionReference Aim { get; }
            public InputActionReference UiPoint { get; }
            public InputActionReference UiClick { get; }
            public InputActionReference UiScrollWheel { get; }
            public InputActionReference UiNavigate { get; }
            public InputActionReference UiSubmit { get; }
            public InputActionReference UiCancel { get; }
        }

        private readonly struct SpawnPrefabWeight
        {
            public SpawnPrefabWeight(GameObject prefab, float weight)
            {
                Prefab = prefab;
                Weight = weight;
            }

            public GameObject Prefab { get; }
            public float Weight { get; }
        }
    }
}
