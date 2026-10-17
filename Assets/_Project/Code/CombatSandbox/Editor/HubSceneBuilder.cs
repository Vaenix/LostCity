using System;
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
    public static class HubSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Hub.unity";
        private const string ProjectInputPath = "Assets/_Project/Settings/Input/CombatSandbox.inputactions";
        private const string DefaultInputPath = "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";
        private static readonly Color Background = new Color(0.13f, 0.14f, 0.15f);
        private static readonly Color Surface = new Color(0.21f, 0.22f, 0.23f);
        private static readonly Color Foreground = new Color(0.94f, 0.94f, 0.94f);

        [MenuItem("Tools/Lost City/Create Hub Scene")]
        public static void CreateHubScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出运行模式，再生成大厅场景。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("请先保存已打开的场景，再生成大厅场景。");

            string inputPath = ProjectInputPath;
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
            if (actions == null || actions.FindAction("UI/Point") == null || actions.FindAction("UI/Click") == null)
            {
                inputPath = DefaultInputPath;
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
            }
            InputActionReference point = ActionReference(inputPath, actions, "UI/Point");
            InputActionReference click = ActionReference(inputPath, actions, "UI/Click");
            InputActionReference scroll = ActionReference(inputPath, actions, "UI/ScrollWheel");
            InputActionReference move = ActionReference(inputPath, actions, "UI/Navigate");
            InputActionReference submit = ActionReference(inputPath, actions, "UI/Submit");
            InputActionReference cancel = ActionReference(inputPath, actions, "UI/Cancel");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            InputSystemUIInputModule input = eventObject.GetComponent<InputSystemUIInputModule>();
            // Runtime property setters do not reliably persist these Editor-generated assignments.
            var serializedInput = new SerializedObject(input);
            serializedInput.FindProperty("m_ActionsAsset").objectReferenceValue = actions;
            serializedInput.FindProperty("m_PointAction").objectReferenceValue = point;
            serializedInput.FindProperty("m_LeftClickAction").objectReferenceValue = click;
            serializedInput.FindProperty("m_ScrollWheelAction").objectReferenceValue = scroll;
            serializedInput.FindProperty("m_MoveAction").objectReferenceValue = move;
            serializedInput.FindProperty("m_SubmitAction").objectReferenceValue = submit;
            serializedInput.FindProperty("m_CancelAction").objectReferenceValue = cancel;
            serializedInput.FindProperty("m_MiddleClickAction").objectReferenceValue = null;
            serializedInput.FindProperty("m_RightClickAction").objectReferenceValue = null;
            serializedInput.FindProperty("m_TrackedDevicePositionAction").objectReferenceValue = null;
            serializedInput.FindProperty("m_TrackedDeviceOrientationAction").objectReferenceValue = null;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(input);

            var canvasObject = new GameObject("HubCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform background = Rect("Background", canvasObject.transform);
            Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.gameObject.AddComponent<Image>().color = Background;
            background.GetComponent<Image>().raycastTarget = false;

            Text title = Label("Title", canvasObject.transform, font, "迷城大厅", 30);
            Top(title.rectTransform, 36f, 36f, 20f, 42f);
            Text chapter = Label("CurrentChapter", canvasObject.transform, font, "章节进度：暂无章节", 20);
            Top(chapter.rectTransform, 36f, 36f, 68f, 34f);

            RectTransform tabs = Rect("Tabs", canvasObject.transform);
            Top(tabs, 36f, 36f, 120f, 44f);
            HorizontalLayoutGroup tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.childControlWidth = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandHeight = true;
            Button casesButton = Button("CasesTab", tabs, font, "案件选择", out _);
            Button archiveButton = Button("ArchiveTab", tabs, font, "案件档案", out _);
            Button bestiaryButton = Button("BestiaryTab", tabs, font, "怪物图鉴", out _);

            RectTransform casesPanel = ScrollPanel("CasesPanel", canvasObject.transform, out RectTransform caseList);
            HubCaseRow rowTemplate = CaseRow(caseList, font);
            rowTemplate.gameObject.SetActive(false);
            RectTransform archivePanel = ScrollPanel("ArchivePanel", canvasObject.transform, out RectTransform archiveContent);
            Text archiveText = Document(archiveContent, font, "案件档案");
            InvestigationArchiveUI archive = archivePanel.gameObject.AddComponent<InvestigationArchiveUI>();
            Set(archive, "documentText", archiveText);
            RectTransform bestiaryPanel = ScrollPanel("BestiaryPanel", canvasObject.transform, out RectTransform bestiaryContent);
            Text bestiaryText = Document(bestiaryContent, font, "怪物图鉴");
            BestiaryUI bestiary = bestiaryPanel.gameObject.AddComponent<BestiaryUI>();
            Set(bestiary, "documentText", bestiaryText);
            archivePanel.gameObject.SetActive(false);
            bestiaryPanel.gameObject.SetActive(false);

            Text status = Label("Status", canvasObject.transform, font, string.Empty, 18);
            Stretch(status.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(36f, 16f), new Vector2(-226f, 64f));
            status.color = new Color(1f, 0.79f, 0.55f);
            Button retrySaveButton = Button("RetrySaveButton", canvasObject.transform, font, "重试存档", out _);
            RectTransform retryRect = (RectTransform)retrySaveButton.transform;
            Stretch(retryRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-206f, 16f), new Vector2(-36f, 64f));
            retrySaveButton.gameObject.SetActive(false);
            HubController controller = canvasObject.AddComponent<HubController>();
            Set(controller, "currentChapterText", chapter);
            Set(controller, "statusText", status);
            Set(controller, "caseListRoot", caseList);
            Set(controller, "rowTemplate", rowTemplate);
            Set(controller, "archiveButton", archiveButton);
            Set(controller, "bestiaryButton", bestiaryButton);
            Set(controller, "casesButton", casesButton);
            Set(controller, "casesPanel", casesPanel.gameObject);
            Set(controller, "archiveUI", archive);
            Set(controller, "bestiaryUI", bestiary);
            Set(controller, "retrySaveButton", retrySaveButton);
            eventObject.GetComponent<EventSystem>().firstSelectedGameObject = archiveButton.gameObject;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("大厅场景保存失败：" + ScenePath);
            ValidateSavedInputBindings(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        }

        private static void ValidateSavedInputBindings(Scene scene)
        {
            InputSystemUIInputModule module = null;
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (InputSystemUIInputModule candidate in root.GetComponentsInChildren<InputSystemUIInputModule>(true))
                {
                    module = candidate;
                    count++;
                }
            if (count != 1 || module == null || !module.enabled || module.actionsAsset == null
                || !EditorUtility.IsPersistent(module.actionsAsset))
                throw new InvalidOperationException("大厅保存重开验证失败：界面输入模块或动作资源丢失。");
            var serialized = new SerializedObject(module);
            foreach (string field in new[] { "m_PointAction", "m_LeftClickAction", "m_ScrollWheelAction", "m_MoveAction", "m_SubmitAction", "m_CancelAction" })
            {
                var reference = serialized.FindProperty(field).objectReferenceValue as InputActionReference;
                if (reference == null || !EditorUtility.IsPersistent(reference) || reference.action == null
                    || reference.action.actionMap?.asset != module.actionsAsset)
                    throw new InvalidOperationException("大厅保存重开验证失败：输入动作引用丢失或不匹配：" + field);
            }
        }

        private static InputActionReference ActionReference(string path, InputActionAsset asset, string actionName)
        {
            InputAction action = asset != null ? asset.FindAction(actionName) : null;
            if (action != null)
                foreach (UnityEngine.Object child in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (child is InputActionReference reference && EditorUtility.IsPersistent(reference)
                        && reference.action != null && reference.action.id == action.id)
                        return reference;
            throw new InvalidOperationException("缺少可持久化的界面输入动作：" + actionName);
        }

        private static RectTransform ScrollPanel(string name, Transform parent, out RectTransform content)
        {
            RectTransform panel = Rect(name, parent);
            Stretch(panel, Vector2.zero, Vector2.one, new Vector2(36f, 80f), new Vector2(-36f, -184f));
            Image surface = panel.gameObject.AddComponent<Image>();
            surface.color = Surface;
            surface.raycastTarget = false;
            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            RectTransform viewport = Rect("Viewport", panel);
            Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-24f, -8f));
            viewport.gameObject.AddComponent<RectMask2D>();
            // This transparent hit area receives scroll/drag on empty space; decorative backgrounds never intercept buttons.
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            RectTransform track = Rect("Scrollbar", panel);
            Stretch(track, new Vector2(1f, 0f), Vector2.one, new Vector2(-18f, 8f), new Vector2(-6f, -8f));
            Image trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = Background;
            RectTransform handle = Rect("Handle", track);
            Stretch(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.55f, 0.57f, 0.59f);
            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return panel;
        }

        private static HubCaseRow CaseRow(Transform parent, Font font)
        {
            RectTransform root = Rect("CaseRowTemplate", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 152f;
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.26f, 0.27f, 0.28f);
            image.raycastTarget = false;
            Text title = Label("CaseName", root, font, string.Empty, 22);
            Top(title.rectTransform, 18f, 210f, 10f, 32f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 14;
            title.resizeTextMaxSize = 22;
            Text description = Label("Description", root, font, string.Empty, 18);
            Top(description.rectTransform, 18f, 210f, 50f, 60f);
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 14;
            description.resizeTextMaxSize = 18;
            Text state = Label("State", root, font, string.Empty, 16);
            Stretch(state.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(18f, 12f), new Vector2(-210f, 38f));
            Button start = Button("StartButton", root, font, "进入案件", out Text startLabel);
            RectTransform startRect = (RectTransform)start.transform;
            startRect.anchorMin = startRect.anchorMax = new Vector2(1f, 0.5f);
            startRect.pivot = new Vector2(1f, 0.5f);
            startRect.sizeDelta = new Vector2(170f, 48f);
            startRect.anchoredPosition = new Vector2(-18f, 0f);
            HubCaseRow row = root.gameObject.AddComponent<HubCaseRow>();
            Set(row, "caseNameText", title);
            Set(row, "descriptionText", description);
            Set(row, "stateText", state);
            Set(row, "startButton", start);
            Set(row, "startButtonText", startLabel);
            return row;
        }

        private static Text Document(Transform parent, Font font, string initial)
        {
            Text text = Label("Document", parent, font, initial, 20);
            text.lineSpacing = 1.2f;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button Button(string name, Transform parent, Font font, string caption, out Text label)
        {
            RectTransform root = Rect(name, parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.35f, 0.37f, 0.38f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.84f, 0.91f, 0.89f);
            colors.pressedColor = new Color(0.6f, 0.78f, 0.72f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
            button.colors = colors;
            label = Label("Label", root, font, caption, 20);
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return button;
        }

        private static Text Label(string name, Transform parent, Font font, string value, int size)
        {
            Text text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Foreground;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Top(RectTransform rect, float left, float right, float top, float height)
        {
            Stretch(rect, new Vector2(0f, 1f), Vector2.one, new Vector2(left, -top - height), new Vector2(-right, -top));
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException("缺少界面序列化字段：" + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
