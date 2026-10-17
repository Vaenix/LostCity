using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostCity.CombatSandbox;
using LostCity.EvidencePresentation;
using LostCity.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostCity.Room304Sample.EditorTools
{
    public static class Room304SampleValidation
    {
        private const string ScenePath = "Assets/_Project/Scenes/Room304Greybox.unity";
        private const string ArtRoot = "Assets/_Project/Art/EvidencePresentation";

        [MenuItem("Tools/Lost City/Validate Room 304 2.5D Sample")]
        public static void ValidateSample()
        {
            WithSample(scene =>
            {
                ValidateReferences(scene);
                ValidateNavigation(scene);
                ValidateArt();
                ValidateFraming(scene);
            });
            Debug.Log("Room304 2.5D sample validation passed: references, XY physics, routes, art and camera framing. No Play Mode was entered.");
        }

        public static void RenderSample()
        {
            WithSample(scene =>
            {
                ValidateReferences(scene);
                Camera camera = All<Camera>(scene).Single(item => item.CompareTag("MainCamera"));
                string directory = Path.Combine(Path.GetTempPath(), "lost-city-room304-sample");
                Directory.CreateDirectory(directory);
                foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(1680, 720) })
                    Render(camera, size, Path.Combine(directory, "Room304-" + size.x + "x" + size.y + ".png"));
                Debug.Log("Room304 offline Editor renders saved to " + directory + ". These are not Play Mode screenshots.");
            });
        }

        private static void ValidateReferences(Scene scene)
        {
            foreach (Transform transform in All<Transform>(scene))
            {
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "Missing script: " + transform.name);
                foreach (Component component in transform.GetComponents<Component>())
                    for (Type type = component.GetType(); type != null; type = type.BaseType)
                        Require(type.FullName != "UnityEngine.Collider" && type.FullName != "UnityEngine.Rigidbody",
                            "Sample must not introduce 3D physics: " + transform.name);
            }
            PlayerMotor player = All<PlayerMotor>(scene).Single();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Collider2D collider = player.GetComponent<Collider2D>();
            Require(body != null && body.bodyType == RigidbodyType2D.Dynamic && body.gravityScale == 0f,
                "Player must use dynamic zero-gravity Rigidbody2D.");
            Require((body.constraints & RigidbodyConstraints2D.FreezeRotation) != 0 && collider != null && !collider.isTrigger,
                "Player must retain rotation lock and a solid footprint.");
            Require(Mathf.Abs(player.transform.position.z) < .001f && player.GetComponent<TeamMember>().Team == CombatTeam.Player,
                "Player must stay on the gameplay plane and use the player team.");
            Require(player.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer),
                "Missing real 3D player geometry.");
            var reader = new SerializedObject(player.GetComponent<PlayerInputReader>());
            var move = reader.FindProperty("moveActionReference").objectReferenceValue as InputActionReference;
            var fire = reader.FindProperty("fireActionReference").objectReferenceValue as InputActionReference;
            Require(move != null && move.action != null && fire != null && fire.action != null, "Missing player actions.");
            foreach (string binding in new[] { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d", "<Keyboard>/upArrow", "<Keyboard>/leftArrow", "<Keyboard>/downArrow", "<Keyboard>/rightArrow" })
                Require(move.action.bindings.Any(item => item.path == binding), "Missing move binding: " + binding);
            Camera camera = All<Camera>(scene).Single(item => item.CompareTag("MainCamera"));
            Require(camera.enabled && camera.orthographic && All<SimpleTopDownCameraFollow>(scene).Count() == 0,
                "Sample requires an enabled fixed orthographic camera.");
            Require(new SerializedObject(player.GetComponent<PlayerAim>()).FindProperty("aimCamera").objectReferenceValue == camera,
                "PlayerAim must use the sample camera.");
            InvestigationProgress progress = All<InvestigationProgress>(scene).Single();
            Require(progress.CurrentCase != null && progress.CurrentCase.CaseId == "room_304", "Sample must use the existing Room 304 case.");
            CluePickup[] pickups = All<CluePickup>(scene).ToArray();
            Require(pickups.Length == 3 && pickups.Select(item => item.Definition).Distinct().Count() == 3,
                "Expected exactly three unique existing clue definitions.");
            foreach (CluePickup pickup in pickups)
            {
                Require(progress.RequiredClues.Contains(pickup.Definition), "Pickup points outside the case: " + pickup.name);
                Collider2D trigger = pickup.GetComponent<Collider2D>();
                Require(trigger != null && trigger.isTrigger && Mathf.Abs(trigger.transform.position.z) < .001f,
                    "Clue trigger must stay grounded in XY: " + pickup.name);
                Require(trigger.bounds.size.x >= 1.5f && trigger.bounds.size.y >= 1.5f, "Clue pickup range is too small.");
                Require(pickup.GetComponentsInChildren<MeshRenderer>(true).Length > 0, "Clue needs a 3D mesh, not its UI illustration.");
                Require(new SerializedObject(pickup).FindProperty("investigationProgress").objectReferenceValue == progress,
                    "Pickup must share the scene investigation state.");
            }
            EventSystem events = All<EventSystem>(scene).Single();
            InputSystemUIInputModule input = events.GetComponent<InputSystemUIInputModule>();
            Require(input != null && input.enabled && input.actionsAsset != null, "Missing UI input module.");
            foreach (InputActionReference action in new[] { input.point, input.leftClick, input.move, input.submit, input.cancel, input.scrollWheel })
                Require(action != null && action.action != null && action.action.actionMap.asset == input.actionsAsset,
                    "Unresolved or mismatched UI action reference.");
            EvidenceJournal journal = All<EvidenceJournal>(scene).Single();
            Require(new SerializedObject(journal).FindProperty("investigationProgress").objectReferenceValue == progress,
                "Journal must share collected clue state.");
            ValidatePresentation(scene, journal, pickups);
            foreach (Canvas canvas in All<Canvas>(scene))
                if (canvas.GetComponentsInChildren<Button>(true).Length > 0)
                    Require(canvas.GetComponent<GraphicRaycaster>() != null, "Interactive Canvas needs a raycaster.");
            Require(All<GameFlowManager>(scene).Count() == 0 && All<EnemySpawner>(scene).Count() == 0,
                "This sample must not run a second combat/case-completion flow.");
            Require(All<PlayerProgressPersistence>(scene).Count() == 0 && All<PlayerStats>(scene).Count() == 0,
                "The minimal sample player must not overwrite saved combat progression.");
            string[] build = EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).ToArray();
            Require(build.Length >= 3 && build[0] == "Assets/_Project/Scenes/Hub.unity"
                && build[1] == "Assets/_Project/Scenes/CombatSandbox.unity" && build.Contains(ScenePath), "Build order must preserve Phase 6 entry scenes.");
        }

        private static void ValidatePresentation(Scene scene, EvidenceJournal journal, CluePickup[] pickups)
        {
            IllustratedEvidenceJournal view = All<IllustratedEvidenceJournal>(scene).Single();
            var serialized = new SerializedObject(view);
            foreach (string field in new[] { "journal", "catalog", "listRoot", "rowTemplate", "detailsRoot", "emptyText",
                "illustration", "illustrationPlaceholder", "clueTitleText", "categoryText", "bodyText", "closeButton", "bodyScroll" })
                Require(serialized.FindProperty(field).objectReferenceValue != null, "Missing illustrated journal reference: " + field);
            Require(serialized.FindProperty("journal").objectReferenceValue == journal
                && new SerializedObject(journal).FindProperty("illustratedView").objectReferenceValue == view,
                "Illustrated journal hook is not reciprocal.");
            var catalog = serialized.FindProperty("catalog").objectReferenceValue as EvidenceIllustrationCatalog;
            foreach (CluePickup pickup in pickups)
                Require(catalog.GetIllustration(pickup.Definition.Id) != null, "Missing stable-ID illustration: " + pickup.Definition.Id);
            var row = serialized.FindProperty("rowTemplate").objectReferenceValue as EvidenceThumbnailRow;
            Require(!row.gameObject.activeSelf, "Row template must be inactive.");
            var rowFields = new SerializedObject(row);
            foreach (string field in new[] { "button", "background", "thumbnail", "titleText", "categoryText", "normalSprite", "selectedSprite" })
                Require(rowFields.FindProperty(field).objectReferenceValue != null, "Missing row reference: " + field);
            Image illustration = serialized.FindProperty("illustration").objectReferenceValue as Image;
            Require(illustration.preserveAspect && !illustration.raycastTarget, "Large illustration must preserve aspect and not block input.");
            foreach (string name in new[] { "JournalButton", "ReturnToHubButton" })
            {
                Button button = All<Button>(scene).Single(item => item.name == name);
                Require(button.interactable && button.targetGraphic != null && button.targetGraphic.raycastTarget
                    && button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) != null,
                    "Toolbar button is not wired: " + name);
            }
        }

        private static void ValidateNavigation(Scene scene)
        {
            Physics2D.SyncTransforms();
            PlayerMotor player = All<PlayerMotor>(scene).Single();
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            float radius = Mathf.Max(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y) + .03f;
            Bounds[] floors = new[] { "RoomFloor", "CorridorFloor" }.Select(name =>
                All<MeshRenderer>(scene).Single(renderer => renderer.name == name).bounds).ToArray();
            Collider2D[] obstacles = All<Collider2D>(scene).Where(item => !item.isTrigger && item != playerCollider).ToArray();
            Require(obstacles.Length >= 5, "Missing solid wall/furniture footprints.");
            Require(Physics2D.OverlapPointAll(obstacles[0].bounds.center).Contains(obstacles[0]), "Editor physics query has not synchronized.");
            Vector2 origin = player.transform.position;
            bool Free(Vector2 point) => floors.Any(floor => point.x >= floor.min.x && point.x <= floor.max.x
                && point.y >= floor.min.y && point.y <= floor.max.y)
                && !Physics2D.OverlapCircleAll(point, radius).Any(item => !item.isTrigger && item != playerCollider);
            Require(Free(origin), "Player spawns inside an obstacle or outside the floor.");
            var queue = new Queue<Vector2Int>();
            var checkedCells = new HashSet<Vector2Int> { Vector2Int.zero };
            var reached = new HashSet<CluePickup>();
            CluePickup[] clues = All<CluePickup>(scene).ToArray();
            Vector2Int[] steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            queue.Enqueue(Vector2Int.zero);
            int reachableCells = 0;
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                Vector2 point = origin + (Vector2)cell * .25f;
                reachableCells++;
                foreach (CluePickup clue in clues)
                    if (clue.GetComponent<Collider2D>().OverlapPoint(point)) reached.Add(clue);
                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = cell + step;
                    if (checkedCells.Add(next) && Free(origin + (Vector2)next * .25f)) queue.Enqueue(next);
                }
                Require(checkedCells.Count < 100000, "Floor domain is unexpectedly large.");
            }
            Require(reached.Count == clues.Length, "A clue has no player-width route from spawn: "
                + string.Join(", ", clues.Where(item => !reached.Contains(item)).Select(item => item.Definition.Id)));
            Debug.Log("Sample static clearance passed: " + reachableCells + " reachable grid cells; all clue triggers approachable. Not a movement/play test.");
        }

        private static void ValidateArt()
        {
            foreach (string name in new[] { "patient-bracelet.png", "visitor-register.png", "apology-note.png" })
            {
                string assetPath = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }).Select(AssetDatabase.GUIDToAssetPath)
                    .Single(path => Path.GetFileName(path) == name);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Require(importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                    && settings.spriteMeshType == SpriteMeshType.FullRect && !importer.mipmapEnabled
                    && importer.textureCompression == TextureImporterCompression.Uncompressed && importer.maxTextureSize >= 2048
                    && importer.sRGBTexture && importer.alphaIsTransparency && importer.npotScale == TextureImporterNPOTScale.None,
                    "Evidence import settings differ from source contract: " + name);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                Require(texture.width == 1254 && texture.height == 1254, "Evidence was downsampled.");
                string original = "Design/2026-10-17-production-kit-v1/evidence/" + name;
                if (File.Exists(original)) Require(File.ReadAllBytes(assetPath).SequenceEqual(File.ReadAllBytes(original)), "Evidence pixels/source file were modified.");
            }
        }

        private static void ValidateFraming(Scene scene)
        {
            Camera camera = All<Camera>(scene).Single(item => item.CompareTag("MainCamera"));
            float original = camera.aspect;
            try
            {
                Component[] targets = All<CluePickup>(scene).Cast<Component>().Concat(All<PlayerMotor>(scene)).ToArray();
                foreach (float aspect in new[] { 16f / 9f, 4f / 3f, 21f / 9f })
                {
                    camera.aspect = aspect;
                    foreach (Component target in targets)
                        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                        {
                            Bounds b = renderer.bounds;
                            if (renderer is SkinnedMeshRenderer skin)
                            {
                                Mesh mesh = new Mesh();
                                try
                                {
                                    skin.BakeMesh(mesh);
                                    Vector3[] points = mesh.vertices.Select(vertex => skin.transform.TransformPoint(vertex)).ToArray();
                                    Require(points.Length > 0, "Missing posed skin: " + skin.name);
                                    b = new Bounds(points[0], Vector3.zero);
                                    foreach (Vector3 point in points.Skip(1)) b.Encapsulate(point);
                                }
                                finally { UnityEngine.Object.DestroyImmediate(mesh); }
                            }
                            foreach (float x in new[] { b.min.x, b.max.x })
                                foreach (float y in new[] { b.min.y, b.max.y })
                                    foreach (float z in new[] { b.min.z, b.max.z })
                                    {
                                        Vector3 viewport = camera.WorldToViewportPoint(new Vector3(x, y, z));
                                        Require(viewport.z > camera.nearClipPlane && viewport.x >= .02f && viewport.x <= .98f
                                            && viewport.y >= .02f && viewport.y <= .98f, "Camera clips " + target.name + " at aspect " + aspect);
                                    }
                        }
                }
            }
            finally { camera.aspect = original; }
        }

        private static void Render(Camera camera, Vector2Int size, string path)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            float aspect = camera.aspect;
            RenderTexture target = RenderTexture.GetTemporary(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            try
            {
                camera.aspect = (float)size.x / size.y;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                image.Apply();
                Color32[] pixels = image.GetPixels32();
                int distinct = pixels.Select(color => (color.r << 16) | (color.g << 8) | color.b).Distinct().Count();
                int magenta = pixels.Count(color => color.r > 220 && color.b > 220 && color.g < 40);
                Require(distinct > 32 && magenta < pixels.Length / 100, "Blank render or unsupported material shader: " + path);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("Offline sample render: " + path + "; colors=" + distinct + "; shader-error pixels=" + magenta);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = aspect;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

        private static void WithSample(Action<Scene> action)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode before editor validation.");
            for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "Save authored changes before validation.");
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try { action(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)); }
            finally
            {
                if (setup.All(item => !string.IsNullOrEmpty(item.path)) && setup.Any(item => item.isActive && item.isLoaded))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
