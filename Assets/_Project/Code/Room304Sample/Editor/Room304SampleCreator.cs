using System;
using System.Collections.Generic;
using System.IO;
using LostCity.CombatSandbox;
using LostCity.EvidencePresentation.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LostCity.Room304Sample.EditorTools
{
    public static class Room304SampleCreator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Room304Greybox.unity";
        public const string InputActionsPath = "Assets/_Project/Settings/Input/Room304Sample.inputactions";
        public const float PlayerRadius = 0.35f;
        public const float ClueRadius = 1.1f;
        public const float DoorClearWidth = 2.2f;
        public const float MinimumAspect = 4f / 3f;
        private const string DefinitionRoot = "Assets/_Project/ScriptableObjects/CombatSandbox/";

        [MenuItem("Tools/Lost City/Create Room 304 2.5D Sample")]
        public static void CreateSample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出运行模式，再生成样例场景。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("请先保存已打开的场景，再生成样例场景。");

            // Read the original assets; never seed, clone, or rewrite case definitions or player prefabs.
            CaseDefinition caseDefinition = RequiredAsset<CaseDefinition>(DefinitionRoot + "Room304_CaseDefinition.asset");
            ClueDefinition bracelet = Clue("Room304_PatientBracelet.asset", "room304_patient_bracelet");
            ClueDefinition register = Clue("Room304_VisitorLog.asset", "room304_visitor_log");
            ClueDefinition note = Clue("Room304_CrumpledApologyNote.asset", "room304_apology_note");
            VerifyCaseClues(caseDefinition, bracelet, register, note);
            Room304SampleGeometry.EnsureFolder("Assets/_Project/Scenes");
            Room304SampleGeometry.EnsureFolder("Assets/_Project/Settings/Input");
            InputReferences input = CreateInputActions();
            var geometry = new Room304SampleGeometry();
            var palette = new Palette(geometry);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject("Room304Sample").transform;
            CreateWorld(root, geometry, palette);
            Camera camera = CreateCamera(root);
            CreateLighting(root);
            InvestigationProgress progress = Child("InvestigationProgress", root).AddComponent<InvestigationProgress>();
            SetObject(progress, "caseDefinition", caseDefinition);
            CreatePlayer(root, camera, input, geometry, palette);
            CreateClues(root, progress, bracelet, register, note, geometry, palette);
            FrameWorld(camera, root);
            CreateUi(root, progress, input);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("样例场景保存失败：" + ScenePath);
            AppendBuildScene();
            Debug.Log("Room 304 2.5D sample created: " + ScenePath);
        }

        private static void CreateWorld(Transform root, Room304SampleGeometry g, Palette p)
        {
            Transform world = Child("World", root).transform;
            Transform floors = Room304SampleGeometry.VisualRoot(Child("Floors", world).transform);
            // Exact names and XY bounds are the static walkable-domain validation contract.
            g.Box("RoomFloor", floors, new Vector3(0f, -0.1f, 4f), new Vector3(10f, 0.2f, 8f), p.RoomFloor);
            g.Box("CorridorFloor", floors, new Vector3(0f, -0.1f, -1.5f), new Vector3(14f, 0.2f, 3f), p.CorridorFloor);
            for (int x = -4; x <= 4; x++)
                g.Box("RoomTileJoint_X" + x, floors, new Vector3(x, 0.002f, 4f), new Vector3(0.014f, 0.004f, 8f), p.Grout);
            for (int y = 1; y <= 7; y++)
                g.Box("RoomTileJoint_Y" + y, floors, new Vector3(0f, 0.002f, y), new Vector3(10f, 0.004f, 0.014f), p.Grout);
            g.Box("CorridorGuide", floors, new Vector3(0f, 0.005f, -2.4f), new Vector3(13.8f, 0.01f, 0.08f), p.Teal);
            g.Box("DoorThreshold", floors, new Vector3(0f, 0.012f, 0f), new Vector3(DoorClearWidth, 0.024f, 0.2f), p.Metal);

            Transform walls = Child("Walls", world).transform;
            Solid("BackWall", walls, new Vector2(0f, 8f), new Vector2(10.2f, 0.2f), 2.65f, g, p.Wall);
            Solid("WestWall", walls, new Vector2(-5f, 4f), new Vector2(0.2f, 8f), 1.6f, g, p.Wall);
            Solid("EastWall", walls, new Vector2(5f, 4f), new Vector2(0.2f, 8f), 1.6f, g, p.Wall);
            // Low near walls reveal the floor and leave an actual open doorway, not an invisible blocker.
            Solid("FrontWall_West_Cutaway", walls, new Vector2(-3.1f, 0f), new Vector2(4f, 0.2f), 0.55f, g, p.Wall);
            Solid("FrontWall_East_Cutaway", walls, new Vector2(3.1f, 0f), new Vector2(4f, 0.2f), 0.55f, g, p.Wall);
            Solid("CorridorFront_Cutaway", walls, new Vector2(0f, -3f), new Vector2(14.2f, 0.2f), 0.28f, g, p.Wall);
            Solid("CorridorWestEnd", walls, new Vector2(-7f, -1.5f), new Vector2(0.2f, 3f), 0.6f, g, p.Wall);
            Solid("CorridorEastEnd", walls, new Vector2(7f, -1.5f), new Vector2(0.2f, 3f), 0.6f, g, p.Wall);
            Solid("CorridorBack_West", walls, new Vector2(-6.05f, 0f), new Vector2(1.9f, 0.2f), 1.6f, g, p.Wall);
            Solid("CorridorBack_East", walls, new Vector2(6.05f, 0f), new Vector2(1.9f, 0.2f), 1.6f, g, p.Wall);

            Transform fixtures = Room304SampleGeometry.VisualRoot(Child("WallFixtures", world).transform);
            g.Box("BackWallRail", fixtures, new Vector3(0f, 0.9f, 7.87f), new Vector3(9.8f, 0.12f, 0.06f), p.Teal);
            g.Box("WindowFrame", fixtures, new Vector3(0.3f, 1.8f, 7.86f), new Vector3(2.8f, 1.1f, 0.08f), p.Metal);
            g.Box("WindowGlass", fixtures, new Vector3(0.3f, 1.8f, 7.8f), new Vector3(2.6f, 0.9f, 0.04f), p.Glass);
            g.Box("WindowMullion", fixtures, new Vector3(0.3f, 1.8f, 7.76f), new Vector3(0.06f, 0.9f, 0.04f), p.Metal);

            // The parked leaf stands parallel to the east jamb, entirely outside the 2.2 m aperture.
            Solid("Door_OpenLeaf", world, new Vector2(1.27f, 0.95f), new Vector2(0.12f, 1.9f), 1.45f, g, p.Door);
            Solid("DoorJamb_West", world, new Vector2(-1.2f, 0f), new Vector2(0.2f, 0.24f), 1.7f, g, p.Metal);
            Solid("DoorJamb_East", world, new Vector2(1.2f, 0f), new Vector2(0.2f, 0.24f), 1.7f, g, p.Metal);
            g.Box("DoorHandle", fixtures, new Vector3(1.17f, 0.95f, 1.65f), new Vector3(0.13f, 0.09f, 0.24f), p.Metal);

            Transform bed = Solid("HospitalBed", world, new Vector2(-3.35f, 5.1f), new Vector2(1.8f, 3.1f), 0.46f, g, p.Metal);
            g.Box("Mattress", bed, new Vector3(0f, 0.6f, 0f), new Vector3(1.7f, 0.25f, 2.95f), p.Linen);
            g.Box("Pillow", bed, new Vector3(0f, 0.79f, 0.98f), new Vector3(1.28f, 0.18f, 0.55f), p.Paper);
            g.Box("Blanket", bed, new Vector3(0f, 0.75f, -0.35f), new Vector3(1.73f, 0.08f, 1.9f), p.Teal);
            g.Box("Headboard", bed, new Vector3(0f, 0.9f, 1.5f), new Vector3(1.8f, 0.8f, 0.1f), p.Metal);
            g.Box("Footboard", bed, new Vector3(0f, 0.68f, -1.5f), new Vector3(1.8f, 0.5f, 0.1f), p.Metal);
            for (int side = -1; side <= 1; side += 2)
                g.Box("BedSideRail" + side, bed, new Vector3(side * 0.86f, 0.88f, 0.5f), new Vector3(0.06f, 0.07f, 1.4f), p.Metal);

            Table("BedsideCabinet", world, new Vector2(-1.5f, 5.8f), new Vector2(0.9f, 0.9f), 0.82f, g, p);
            Table("WritingTable", world, new Vector2(3f, 4.3f), new Vector2(2f, 1.25f), 0.86f, g, p);
            Table("VisitorRegisterStand", world, new Vector2(-3.9f, -1f), new Vector2(1.25f, 0.85f), 0.88f, g, p);
        }

        private static Transform Solid(string name, Transform parent, Vector2 center, Vector2 footprint, float height,
            Room304SampleGeometry g, Material material)
        {
            GameObject solid = Child(name, parent);
            solid.transform.localPosition = new Vector3(center.x, center.y, 0f);
            BoxCollider2D collider = solid.AddComponent<BoxCollider2D>();
            collider.size = footprint;
            Transform visual = Room304SampleGeometry.VisualRoot(solid.transform);
            g.Box(name + "Mesh", visual, new Vector3(0f, height * 0.5f, 0f), new Vector3(footprint.x, height, footprint.y), material);
            return visual;
        }

        private static void Table(string name, Transform parent, Vector2 center, Vector2 size, float height,
            Room304SampleGeometry g, Palette p)
        {
            GameObject table = Child(name, parent);
            table.transform.localPosition = new Vector3(center.x, center.y, 0f);
            table.AddComponent<BoxCollider2D>().size = size;
            Transform visual = Room304SampleGeometry.VisualRoot(table.transform);
            g.Box("TableTop", visual, new Vector3(0f, height - 0.05f, 0f), new Vector3(size.x, 0.1f, size.y), p.Furniture);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    g.Box("Leg_" + x + "_" + z, visual,
                        new Vector3(x * (size.x * 0.5f - 0.09f), (height - 0.1f) * 0.5f, z * (size.y * 0.5f - 0.09f)),
                        new Vector3(0.1f, height - 0.1f, 0.1f), p.Metal);
        }

        private static Camera CreateCamera(Transform root)
        {
            GameObject cameraObject = Child("MainCamera", root);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.17f, 0.18f);
            camera.transform.position = new Vector3(0f, -16f, -21f);
            camera.transform.LookAt(new Vector3(0f, 2f, -0.7f), Vector3.up);
            return camera;
        }

        private static void FrameWorld(Camera camera, Transform root)
        {
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                Bounds bounds = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                            Vector3 projected = camera.transform.InverseTransformPoint(corner);
                            min = Vector2.Min(min, new Vector2(projected.x, projected.y));
                            max = Vector2.Max(max, new Vector2(projected.x, projected.y));
                        }
            }
            Vector2 center = (min + max) * 0.5f;
            camera.transform.position += camera.transform.right * center.x + camera.transform.up * center.y;
            Vector2 half = (max - min) * 0.5f;
            // Fixed 4:3 fit also contains the whole footprint at 16:9 and 21:9; no follow script.
            camera.orthographicSize = Mathf.Max(half.y / 0.68f, half.x / (MinimumAspect * 0.88f)) + 0.15f;
            camera.transform.position -= camera.transform.up * (camera.orthographicSize * 0.1f);
        }

        private static void CreateLighting(Transform root)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.68f, 0.7f);
            RenderSettings.fog = false;
            Light key = Child("Daylight", root).AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 0.95f;
            key.color = new Color(1f, 0.96f, 0.9f);
            key.transform.rotation = Quaternion.LookRotation(new Vector3(0.4f, 0.3f, 1f));
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.55f;
            RenderSettings.sun = key;
        }

        private static void CreatePlayer(Transform root, Camera camera, InputReferences input, Room304SampleGeometry g, Palette p)
        {
            GameObject player = Child("Player", root);
            player.tag = "Player";
            player.transform.localPosition = new Vector3(0f, -1.6f, 0f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = PlayerRadius;
            TeamMember team = player.AddComponent<TeamMember>();
            SetInt(team, "team", (int)CombatTeam.Player);
            PlayerInputReader reader = player.AddComponent<PlayerInputReader>();
            SetObject(reader, "moveActionReference", input.Move);
            SetObject(reader, "fireActionReference", input.Fire);
            PlayerMotor motor = player.AddComponent<PlayerMotor>();
            SetObject(motor, "inputReader", reader);
            SetFloat(motor, "moveSpeed", 3.2f);
            Transform yaw = Child("AimRoot", player.transform).transform;
            PlayerAim aim = player.AddComponent<PlayerAim>();
            SetObject(aim, "yawRoot", yaw);
            SetObject(aim, "aimCamera", camera);
            Transform visual = Room304SampleGeometry.VisualRoot(yaw);
            g.Cylinder("PlayerBody", visual, new Vector3(0f, 0.43f, 0f), new Vector3(0.48f, 0.7f, 0.48f), p.Player);
            g.Cylinder("PlayerHead", visual, new Vector3(0f, 0.92f, 0f), new Vector3(0.35f, 0.3f, 0.35f), p.Skin);
            g.Box("FacingMarker", visual, new Vector3(0.26f, 0.64f, 0f), new Vector3(0.22f, 0.1f, 0.12f), p.Brass);
            g.Box("LeftFoot", visual, new Vector3(0f, 0.06f, -0.14f), new Vector3(0.44f, 0.12f, 0.16f), p.Ink);
            g.Box("RightFoot", visual, new Vector3(0f, 0.06f, 0.14f), new Vector3(0.44f, 0.12f, 0.16f), p.Ink);
        }

        private static void CreateClues(Transform root, InvestigationProgress progress, ClueDefinition bracelet,
            ClueDefinition register, ClueDefinition note, Room304SampleGeometry g, Palette p)
        {
            Transform clues = Child("Clues", root).transform;
            Transform bandVisual = Pickup("Clue_PatientBracelet", clues, new Vector2(-1.5f, 5.8f), bracelet, progress);
            g.Bracelet(bandVisual, new Vector3(0f, 0.83f, 0f), p.Brass);
            g.Box("BraceletIdPlate", bandVisual, new Vector3(0f, 0.98f, -0.25f), new Vector3(0.4f, 0.045f, 0.18f), p.Paper);
            for (int i = 0; i < 5; i++)
                g.Box("BraceletIdMark" + i, bandVisual, new Vector3(-0.12f + i * 0.055f, 1.005f, -0.25f),
                    new Vector3(0.02f, 0.006f, 0.09f), p.Ink);

            Transform logVisual = Pickup("Clue_VisitorRegister", clues, new Vector2(-3.9f, -1f), register, progress);
            g.Box("RegisterCover", logVisual, new Vector3(0f, 0.925f, 0f), new Vector3(1.05f, 0.09f, 0.72f), p.Door);
            g.Box("RegisterPageBlock", logVisual, new Vector3(0.02f, 0.99f, 0f), new Vector3(0.94f, 0.04f, 0.64f), p.Paper);
            g.Box("RegisterSpine", logVisual, new Vector3(-0.47f, 1.02f, 0f), new Vector3(0.06f, 0.06f, 0.72f), p.Metal);
            g.Box("RegisterHeader", logVisual, new Vector3(0.02f, 1.016f, 0.22f), new Vector3(0.72f, 0.009f, 0.045f), p.Teal);
            for (int i = 0; i < 5; i++)
                g.Box("RegisterRow" + i, logVisual, new Vector3(0.02f, 1.016f, -0.23f + i * 0.09f),
                    new Vector3(0.78f, 0.009f, 0.012f), p.Ink);
            for (int i = 0; i < 2; i++)
                g.Box("RegisterColumn" + i, logVisual, new Vector3(-0.18f + i * 0.34f, 1.016f, -0.03f),
                    new Vector3(0.012f, 0.009f, 0.47f), p.Ink);

            Transform noteVisual = Pickup("Clue_ApologyNote", clues, new Vector2(3f, 4.3f), note, progress);
            g.FoldedPaper(noteVisual, new Vector3(0f, 0.88f, 0f), p.Paper);
            // These strokes lie just above one triangular fold (dh/dx = .01/.46, dh/dz = .12/.40).
            for (int i = 0; i < 3; i++)
            {
                float z = -0.33f + i * 0.075f;
                float height = 0.88f + 0.02f + (0.31f / 0.46f) * 0.01f + (z + 0.4f) * 0.3f + 0.008f;
                GameObject ink = g.Box("NoteWriting" + i, noteVisual, new Vector3(-0.15f, height, z),
                    new Vector3(0.22f - i * 0.025f, 0.008f, 0.022f), p.Ink);
                ink.transform.localRotation = Quaternion.Euler(-Mathf.Atan(0.3f) * Mathf.Rad2Deg, 0f,
                    Mathf.Atan(0.01f / 0.46f) * Mathf.Rad2Deg);
            }
        }

        private static Transform Pickup(string name, Transform parent, Vector2 center, ClueDefinition definition, InvestigationProgress progress)
        {
            GameObject root = Child(name, parent);
            root.transform.localPosition = new Vector3(center.x, center.y, 0f);
            CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
            trigger.radius = ClueRadius;
            trigger.isTrigger = true;
            root.AddComponent<CluePickup>().Initialize(definition, progress);
            return Room304SampleGeometry.VisualRoot(root.transform);
        }

        private static void CreateUi(Transform root, InvestigationProgress progress, InputReferences input)
        {
            GameObject eventObject = Child("EventSystem", root);
            eventObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
            // Use serialized assignments, which persist reliably in editor-generated scene YAML.
            SetObject(module, "m_ActionsAsset", input.Asset);
            SetObject(module, "m_PointAction", input.UiPoint);
            SetObject(module, "m_LeftClickAction", input.UiClick);
            SetObject(module, "m_ScrollWheelAction", input.UiScroll);
            SetObject(module, "m_MoveAction", input.UiNavigate);
            SetObject(module, "m_SubmitAction", input.UiSubmit);
            SetObject(module, "m_CancelAction", input.UiCancel);
            foreach (string field in new[] { "m_MiddleClickAction", "m_RightClickAction", "m_TrackedDevicePositionAction", "m_TrackedDeviceOrientationAction" })
                SetObject(module, field, null);

            var canvasObject = new GameObject("Room304Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform toolbar = UiRect("Toolbar", canvasObject.transform);
            Stretch(toolbar, new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -64f), new Vector2(-16f, -16f));
            Text title = Label("RoomTitle", toolbar, font, "304号病房", 23);
            Stretch(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-330f, 0f));
            title.alignment = TextAnchor.MiddleLeft;
            Button journalButton = UiButton("JournalButton", toolbar, font, "线索档案  J");
            PlaceRight((RectTransform)journalButton.transform, -156f, 144f);
            Button returnButton = UiButton("ReturnToHubButton", toolbar, font, "返回大厅");
            PlaceRight((RectTransform)returnButton.transform, 0f, 144f);
            Text status = Label("StatusText", canvasObject.transform, font, string.Empty, 18);
            Stretch(status.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 16f), new Vector2(-32f, 54f));
            status.alignment = TextAnchor.MiddleCenter;
            status.color = new Color(1f, 0.77f, 0.6f);
            Room304SampleController controller = canvasObject.AddComponent<Room304SampleController>();
            SetObject(controller, "statusText", status);
            UnityEventTools.AddPersistentListener(returnButton.onClick, controller.ReturnToHub);

            GameObject promptRoot = Child("GamePromptManager", canvasObject.transform);
            GamePromptManager prompt = promptRoot.AddComponent<GamePromptManager>();
            RectTransform promptPanel = UiRect("InteractionPrompt", canvasObject.transform);
            promptPanel.anchorMin = promptPanel.anchorMax = new Vector2(0.5f, 0f);
            promptPanel.pivot = new Vector2(0.5f, 0f);
            promptPanel.anchoredPosition = new Vector2(0f, 58f);
            promptPanel.sizeDelta = new Vector2(420f, 86f);
            Image promptBackground = promptPanel.gameObject.AddComponent<Image>();
            promptBackground.color = new Color(0.05f, 0.08f, 0.08f, 0.94f);
            promptBackground.raycastTarget = false;
            Text promptText = Label("Message", promptPanel, font, string.Empty, 19);
            Stretch(promptText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            promptText.alignment = TextAnchor.MiddleCenter;
            SetObject(prompt, "panelRoot", promptPanel.gameObject);
            SetObject(prompt, "messageText", promptText);
            SetObject(prompt, "backgroundImage", promptBackground);
            promptPanel.gameObject.SetActive(false);

            EvidenceJournal journal = EvidencePresentationBuilder.CreateJournal(canvasObject.transform, progress);
            if (journal == null) throw new InvalidOperationException("线索档案工厂未返回 EvidenceJournal。");
            UnityEventTools.AddPersistentListener(journalButton.onClick, journal.Toggle);
        }

        private static InputReferences CreateInputActions()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                if (File.Exists(InputActionsPath))
                    throw new InvalidOperationException("样例输入资源无法读取：" + InputActionsPath);
                var generated = ScriptableObject.CreateInstance<InputActionAsset>();
                generated.name = "Room304Sample";
                InputActionMap player = generated.AddActionMap("Player");
                InputAction move = player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                move.AddBinding("<Gamepad>/leftStick");
                player.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton", expectedControlLayout: "Button");
                player.AddAction("Aim", InputActionType.Value, "<Pointer>/position", expectedControlLayout: "Vector2");
                InputActionMap ui = generated.AddActionMap("UI");
                ui.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
                InputAction click = ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
                click.AddBinding("<Touchscreen>/primaryTouch/press");
                ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
                InputAction navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
                navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                navigate.AddBinding("<Gamepad>/dpad");
                InputAction submit = ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter", expectedControlLayout: "Button");
                submit.AddBinding("<Gamepad>/buttonSouth");
                InputAction cancel = ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape", expectedControlLayout: "Button");
                cancel.AddBinding("<Gamepad>/buttonEast");
                File.WriteAllText(InputActionsPath, generated.ToJson());
                Object.DestroyImmediate(generated);
                AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
                asset = RequiredAsset<InputActionAsset>(InputActionsPath);
            }
            // On regeneration keep action IDs, and refresh only our own persistent references.
            return new InputReferences
            {
                Asset = asset,
                Move = ActionReference(asset, "Player/Move"),
                Fire = ActionReference(asset, "Player/Fire"),
                UiPoint = ActionReference(asset, "UI/Point"),
                UiClick = ActionReference(asset, "UI/Click"),
                UiScroll = ActionReference(asset, "UI/ScrollWheel"),
                UiNavigate = ActionReference(asset, "UI/Navigate"),
                UiSubmit = ActionReference(asset, "UI/Submit"),
                UiCancel = ActionReference(asset, "UI/Cancel")
            };
        }

        private static InputActionReference ActionReference(InputActionAsset asset, string actionPath)
        {
            InputAction action = asset.FindAction(actionPath, throwIfNotFound: true);
            string path = "Assets/_Project/Settings/Input/Room304Sample_" + actionPath.Replace('/', '_') + ".inputactionreference.asset";
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (reference == null)
            {
                reference = InputActionReference.Create(action);
                AssetDatabase.CreateAsset(reference, path);
            }
            else reference.Set(action);
            reference.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(reference);
            AssetDatabase.SaveAssetIfDirty(reference);
            return reference;
        }

        private static void AppendBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
                if (scene.path == ScenePath) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static ClueDefinition Clue(string file, string stableId)
        {
            ClueDefinition clue = RequiredAsset<ClueDefinition>(DefinitionRoot + file);
            if (clue.Id != stableId) throw new InvalidOperationException("原始线索 ID 不匹配：" + file);
            return clue;
        }

        private static void VerifyCaseClues(CaseDefinition definition, params ClueDefinition[] clues)
        {
            foreach (ClueDefinition clue in clues)
            {
                bool found = false;
                foreach (ClueDefinition required in definition.Clues)
                    if (required == clue) found = true;
                if (!found) throw new InvalidOperationException("原始案件未引用线索：" + clue.Id);
            }
        }

        private static T RequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("缺少样例所需资源：" + path);
            return asset;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static RectTransform UiRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text Label(string name, Transform parent, Font font, string value, int size)
        {
            Text text = UiRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.94f, 0.96f, 0.96f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        private static Button UiButton(string name, Transform parent, Font font, string caption)
        {
            RectTransform rect = UiRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.29f, 0.29f, 0.97f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text label = Label("Label", rect, font, caption, 18);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return button;
        }

        private static void PlaceRight(RectTransform rect, float right, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(right, 0f);
            rect.sizeDelta = new Vector2(width, 48f);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetObject(Object target, string field, Object value)
            => Set(target, field, property => property.objectReferenceValue = value);

        private static void SetInt(Object target, string field, int value)
            => Set(target, field, property => property.intValue = value);

        private static void SetFloat(Object target, string field, float value)
            => Set(target, field, property => property.floatValue = value);

        private static void Set(Object target, string field, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException("缺少序列化字段：" + target.GetType().Name + "." + field);
            assign(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class InputReferences
        {
            internal InputActionAsset Asset;
            internal InputActionReference Move;
            internal InputActionReference Fire;
            internal InputActionReference UiPoint;
            internal InputActionReference UiClick;
            internal InputActionReference UiScroll;
            internal InputActionReference UiNavigate;
            internal InputActionReference UiSubmit;
            internal InputActionReference UiCancel;
        }

        private sealed class Palette
        {
            internal readonly Material RoomFloor, CorridorFloor, Grout, Wall, Metal, Teal, Glass, Door,
                Linen, Paper, Furniture, Player, Skin, Ink, Brass;

            internal Palette(Room304SampleGeometry g)
            {
                RoomFloor = g.Material("RoomFloor", new Color(0.7f, 0.76f, 0.74f));
                CorridorFloor = g.Material("CorridorFloor", new Color(0.55f, 0.6f, 0.62f));
                Grout = g.Material("Grout", new Color(0.57f, 0.64f, 0.62f));
                Wall = g.Material("Wall", new Color(0.82f, 0.84f, 0.83f));
                Metal = g.Material("Metal", new Color(0.53f, 0.58f, 0.59f), 0.45f);
                Teal = g.Material("Teal", new Color(0.19f, 0.48f, 0.45f));
                Glass = g.Material("Window", new Color(0.46f, 0.67f, 0.75f));
                Door = g.Material("DoorAndRegister", new Color(0.48f, 0.22f, 0.2f));
                Linen = g.Material("Linen", new Color(0.84f, 0.88f, 0.89f));
                Paper = g.Material("Paper", new Color(0.95f, 0.95f, 0.9f));
                Furniture = g.Material("Furniture", new Color(0.66f, 0.69f, 0.64f));
                Player = g.Material("Player", new Color(0.24f, 0.32f, 0.45f));
                Skin = g.Material("Skin", new Color(0.82f, 0.67f, 0.56f));
                Ink = g.Material("Ink", new Color(0.12f, 0.15f, 0.16f));
                Brass = g.Material("Bracelet", new Color(0.91f, 0.68f, 0.2f), 0.15f);
            }
        }
    }
}
