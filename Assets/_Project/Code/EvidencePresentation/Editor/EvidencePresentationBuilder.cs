using System;
using LostCity.CombatSandbox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.EvidencePresentation.EditorTools
{
    public static class EvidencePresentationBuilder
    {
        private static readonly Color Ink = new Color(0.11f, 0.17f, 0.17f);

        public static EvidenceJournal CreateJournal(Transform parent, InvestigationProgress progress)
        {
            Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || progress == null)
                throw new ArgumentException("图文线索档案需要已有 Overlay Canvas 和调查进度。");
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            EvidenceIllustrationCatalog catalog = EvidenceArtImporter.ImportAssets();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = Rect("IllustratedEvidenceJournal", parent);
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            EvidenceJournal journal = root.gameObject.AddComponent<EvidenceJournal>();
            IllustratedEvidenceJournal view = root.gameObject.AddComponent<IllustratedEvidenceJournal>();
            RectTransform panel = Rect("Panel", root);
            Stretch(panel, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
            Image paper = panel.gameObject.AddComponent<Image>();
            paper.sprite = EvidenceArtImporter.UiSprite("panel-paper");
            paper.type = Image.Type.Sliced;
            paper.raycastTarget = false;
            Text header = Text("Header", panel, font, "线索档案", 28);
            Stretch(header.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -70f), new Vector2(-92f, -22f));
            header.alignment = TextAnchor.MiddleLeft;
            header.resizeTextForBestFit = true;
            header.resizeTextMinSize = 18;
            header.resizeTextMaxSize = 28;
            Button close = CloseButton(panel);

            RectTransform list = Rect("ClueList", panel);
            Stretch(list, new Vector2(0.025f, 0.04f), new Vector2(0.31f, 1f), Vector2.zero, new Vector2(0f, -90f));
            ScrollRect listScroll = Scroll(list, out RectTransform listContent);
            listScroll.scrollSensitivity = 30f;
            EvidenceThumbnailRow row = Row(listContent, font);
            row.gameObject.SetActive(false);
            Text empty = Text("EmptyState", panel, font, "尚未收集线索。", 22);
            Stretch(empty.rectTransform, new Vector2(0.34f, 0.04f), new Vector2(0.97f, 1f), Vector2.zero, new Vector2(0f, -90f));
            empty.alignment = TextAnchor.MiddleCenter;
            RectTransform details = Rect("ClueDetails", panel);
            Stretch(details, new Vector2(0.34f, 0.04f), new Vector2(0.97f, 1f), Vector2.zero, new Vector2(0f, -90f));
            Text title = Text("ClueTitle", details, font, string.Empty, 24);
            Stretch(title.rectTransform, new Vector2(0f, 0.91f), Vector2.one, Vector2.zero, Vector2.zero);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 24;
            Text category = Text("Category", details, font, string.Empty, 16);
            Stretch(category.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 0.91f), Vector2.zero, Vector2.zero);
            RectTransform art = Rect("Illustration", details);
            Stretch(art, new Vector2(0.02f, 0.34f), new Vector2(0.98f, 0.83f), Vector2.zero, Vector2.zero);
            Image illustration = art.gameObject.AddComponent<Image>();
            illustration.preserveAspect = true;
            illustration.raycastTarget = false;
            Text placeholder = Text("IllustrationPlaceholder", details, font, "暂无图示", 18);
            Stretch(placeholder.rectTransform, art.anchorMin, art.anchorMax, Vector2.zero, Vector2.zero);
            placeholder.alignment = TextAnchor.MiddleCenter;
            RectTransform bodyArea = Rect("BodyScroll", details);
            Stretch(bodyArea, Vector2.zero, new Vector2(1f, 0.32f), Vector2.zero, Vector2.zero);
            ScrollRect bodyScroll = Scroll(bodyArea, out RectTransform bodyContent);
            Text body = Text("Body", bodyContent, font, string.Empty, 20);
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.lineSpacing = 1.2f;

            Set(journal, "investigationProgress", progress);
            Set(journal, "panelRoot", panel.gameObject);
            Set(journal, "titleText", header);
            Set(journal, "illustratedView", view);
            Set(view, "journal", journal);
            Set(view, "catalog", catalog);
            Set(view, "listRoot", listContent);
            Set(view, "rowTemplate", row);
            Set(view, "detailsRoot", details.gameObject);
            Set(view, "emptyText", empty);
            Set(view, "illustration", illustration);
            Set(view, "illustrationPlaceholder", placeholder);
            Set(view, "clueTitleText", title);
            Set(view, "categoryText", category);
            Set(view, "bodyText", body);
            Set(view, "closeButton", close);
            Set(view, "bodyScroll", bodyScroll);
            details.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            return journal;
        }

        private static EvidenceThumbnailRow Row(Transform parent, Font font)
        {
            RectTransform root = Rect("ClueRowTemplate", parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
            Image background = root.gameObject.AddComponent<Image>();
            background.sprite = EvidenceArtImporter.UiSprite("evidence-card-normal");
            background.type = Image.Type.Sliced;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            RectTransform thumbRect = Rect("Thumbnail", root);
            thumbRect.anchorMin = thumbRect.anchorMax = new Vector2(0f, 0.5f);
            thumbRect.pivot = new Vector2(0f, 0.5f);
            thumbRect.anchoredPosition = new Vector2(10f, 0f);
            thumbRect.sizeDelta = new Vector2(72f, 88f);
            Image thumbnail = thumbRect.gameObject.AddComponent<Image>();
            thumbnail.preserveAspect = true;
            thumbnail.raycastTarget = false;
            Text title = Text("Title", root, font, string.Empty, 18);
            Stretch(title.rectTransform, new Vector2(0f, 0.45f), Vector2.one, new Vector2(90f, 0f), new Vector2(-12f, -12f));
            title.alignment = TextAnchor.MiddleLeft;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 12;
            title.resizeTextMaxSize = 18;
            Text category = Text("Category", root, font, string.Empty, 14);
            Stretch(category.rectTransform, Vector2.zero, new Vector2(1f, 0.43f), new Vector2(90f, 12f), new Vector2(-12f, 0f));
            category.resizeTextForBestFit = true;
            category.resizeTextMinSize = 11;
            category.resizeTextMaxSize = 14;
            EvidenceThumbnailRow row = root.gameObject.AddComponent<EvidenceThumbnailRow>();
            Set(row, "button", button);
            Set(row, "background", background);
            Set(row, "thumbnail", thumbnail);
            Set(row, "titleText", title);
            Set(row, "categoryText", category);
            Set(row, "normalSprite", EvidenceArtImporter.UiSprite("evidence-card-normal"));
            Set(row, "selectedSprite", EvidenceArtImporter.UiSprite("evidence-card-selected"));
            return row;
        }

        private static Button CloseButton(Transform panel)
        {
            RectTransform root = Rect("CloseButton", panel);
            Stretch(root, Vector2.one, Vector2.one, new Vector2(-78f, -68f), new Vector2(-26f, -20f));
            Image background = root.gameObject.AddComponent<Image>();
            background.sprite = EvidenceArtImporter.UiSprite("button-secondary-normal");
            background.type = Image.Type.Sliced;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = EvidenceArtImporter.UiSprite("button-secondary-hover"),
                selectedSprite = EvidenceArtImporter.UiSprite("button-secondary-hover"),
                pressedSprite = EvidenceArtImporter.UiSprite("button-secondary-pressed"),
                disabledSprite = EvidenceArtImporter.UiSprite("button-secondary-disabled")
            };
            RectTransform icon = Rect("Icon", root);
            Stretch(icon, Vector2.zero, Vector2.one, new Vector2(12f, 10f), new Vector2(-12f, -10f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = EvidenceArtImporter.UiSprite("icon-close");
            image.color = Ink;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return button;
        }

        private static ScrollRect Scroll(RectTransform root, out RectTransform content)
        {
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            RectTransform viewport = Rect("Viewport", root);
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Only the viewport's input area catches empty-space scrolling; decorative images do not.
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static Text Text(string name, Transform parent, Font font, string value, int size)
        {
            Text text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Ink;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
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
            if (property == null) throw new InvalidOperationException("图文档案字段缺失：" + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
