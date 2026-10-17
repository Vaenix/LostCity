using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LostCity.Meta.Core.Tests
{
    public sealed class EvidencePresentationTests
    {
        private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Catalog_MapsStableIdsNotEntryOrder_AndUnknownIdsHaveNoIllustration()
        {
            Sprite bracelet = Sprite();
            Sprite register = Sprite();
            Sprite note = Sprite();
            ScriptableObject catalog = Catalog(new[] { "room304_apology_note", "room304_patient_bracelet", "room304_visitor_log" },
                new[] { note, bracelet, register });
            Assert.That(Call(catalog, "GetIllustration", "room304_patient_bracelet"), Is.SameAs(bracelet));
            Assert.That(Call(catalog, "GetIllustration", "room304_visitor_log"), Is.SameAs(register));
            Assert.That(Call(catalog, "GetIllustration", "room304_apology_note"), Is.SameAs(note));
            foreach (string id in new[] { null, "", " ", "unknown", "ROOM304_PATIENT_BRACELET" })
                Assert.That(Call(catalog, "GetIllustration", id), Is.Null);
        }

        [Test]
        public void Catalog_NullEntriesAndDuplicateIds_FailClosed()
        {
            ScriptableObject empty = Definition("LostCity.EvidencePresentation.EvidenceIllustrationCatalog");
            Set(empty, "entries", null);
            Assert.That(Call(empty, "GetIllustration", "known"), Is.Null);
            ScriptableObject duplicate = Catalog(new[] { "known", "known" }, new[] { Sprite(), Sprite() });
            Assert.That(Call(duplicate, "GetIllustration", "known"), Is.Null);
        }

        [TestCase("panel-paper", 16f)]
        [TestCase("evidence-card-normal", 16f)]
        [TestCase("evidence-card-selected", 16f)]
        [TestCase("button-secondary-normal", 12f)]
        [TestCase("button-secondary-hover", 12f)]
        [TestCase("button-secondary-pressed", 12f)]
        [TestCase("button-secondary-disabled", 12f)]
        [TestCase("icon-close", 0f)]
        public void Importer_WithoutPrivateDesignManifest_UsesExactCommittedBorderSubset(string id, float border)
        {
            Type importer = Type.GetType("LostCity.EvidencePresentation.EditorTools.EvidenceArtImporter, Assembly-CSharp-Editor", true);
            MethodInfo resolve = importer.GetMethod("ResolveBorder", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(resolve.Invoke(null, new object[] { id, null }), Is.EqualTo(new Vector4(border, border, border, border)));
        }

        [Test]
        public void Importer_AbsentSourceAndAbsentCopy_ReportsMissingFileWithoutImportingAnything()
        {
            Type importer = Type.GetType("LostCity.EvidencePresentation.EditorTools.EvidenceArtImporter, Assembly-CSharp-Editor", true);
            MethodInfo copy = importer.GetMethod("CopyAndImport", BindingFlags.NonPublic | BindingFlags.Static);
            string unique = Guid.NewGuid().ToString("N");
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => copy.Invoke(null, new object[]
            {
                Path.Combine(Path.GetTempPath(), "missing-evidence-" + unique + ".png"),
                "Assets/_Project/Art/EvidencePresentation/missing-" + unique + ".png", Vector4.zero
            }));
            Assert.That(exception.InnerException, Is.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void View_FiltersInvalidAndDuplicateCollectedIds_AndNeverSelectsUncollectedEvidence()
        {
            Component view = View(out _, out Component title, out Component body, out _);
            ScriptableObject known = Clue("known", "known_title", "known_body");
            ScriptableObject hidden = Clue("hidden", "uncollected_title", "uncollected_body");
            Refresh(view, new[] { null, known, known, Clue(" ", "invalid", "invalid") });
            Assert.That(Visible(view), Is.EqualTo(new[] { known }));
            Assert.That(Call(view, "SelectClue", "hidden"), Is.False);
            Assert.That(Property(view, "SelectedClue"), Is.SameAs(known));
            Assert.That(TextValue(title), Is.EqualTo("known_title"));
            Assert.That(TextValue(body), Is.EqualTo("known_body"));
            Assert.That(TextValue(title) + TextValue(body), Does.Not.Contain(Property(hidden, "Name").ToString()));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(12)]
        public void View_RendersEntireCollectedListWithoutFixedThreeEntryLimit(int count)
        {
            Component view = View(out Transform rows, out _, out _, out _);
            ScriptableObject[] clues = Enumerable.Range(0, count).Select(i => Clue("clue_" + i, "title_" + i, "body_" + i)).ToArray();
            Refresh(view, clues);
            Assert.That(Visible(view), Is.EqualTo(clues));
            Assert.That(rows.childCount, Is.EqualTo(count));
            if (count == 0) Assert.That(Property(view, "SelectedClue"), Is.Null);
            else Assert.That(Property(view, "SelectedClue"), Is.SameAs(clues[0]));
        }

        [Test]
        public void View_PreservesSelectionByIdAcrossRefreshReorderingAndReplacement()
        {
            Component view = View(out _, out Component title, out Component body, out Component image);
            Sprite sprite = Sprite();
            Set(view, "catalog", Catalog(new[] { "second" }, new[] { sprite }));
            ScriptableObject first = Clue("first", "first_title", "first_body");
            ScriptableObject second = Clue("second", "second_title", "second_body");
            Refresh(view, new[] { first, second });
            Assert.That(Call(view, "SelectClue", "second"), Is.True);
            ScriptableObject replacement = Clue("second", "replacement_title", "replacement_body");
            Refresh(view, new[] { replacement, first, Clue("third", "third_title", "third_body") });
            Assert.That(Property(view, "SelectedClue"), Is.SameAs(replacement));
            Assert.That(TextValue(title), Is.EqualTo("replacement_title"));
            Assert.That(TextValue(body), Is.EqualTo("replacement_body"));
            Assert.That(Property(image, "sprite"), Is.SameAs(sprite));
            Assert.That(Property(image, "preserveAspect"), Is.True);
            Assert.That(Call(view, "SelectClue", "never_collected"), Is.False);
            Assert.That(Property(view, "SelectedClueId"), Is.EqualTo("second"));
        }

        [Test]
        public void View_NullOrEmptyCollection_ClearsStaleImageTextAndSelection()
        {
            Component view = View(out _, out Component title, out Component body, out Component image);
            Set(view, "catalog", Catalog(new[] { "known" }, new[] { Sprite() }));
            ScriptableObject clue = Clue("known", "visible_title", "visible_body");
            Refresh(view, new[] { clue });
            Assert.That(Property(image, "sprite"), Is.Not.Null);
            Refresh(view, null);
            Assert.That(Visible(view), Is.Empty);
            Assert.That(Property(view, "SelectedClue"), Is.Null);
            Assert.That(TextValue(title), Is.Empty);
            Assert.That(TextValue(body), Is.Empty);
            Assert.That(Property(image, "sprite"), Is.Null);
            Assert.That(((Component)Field(view, "emptyText")).gameObject.activeSelf, Is.True);
            Refresh(view, new[] { clue });
            Refresh(view, new ScriptableObject[0]);
            Assert.That(Property(view, "SelectedClue"), Is.Null);
            Assert.That(((GameObject)Field(view, "detailsRoot")).activeSelf, Is.False);
        }

        [Test]
        public void View_MissingCatalogOrSprite_KeepsCollectedTextWithoutUsingDefinitionIcon()
        {
            Component view = View(out _, out Component title, out Component body, out Component image);
            ScriptableObject clue = Clue("known", "known_title", "known_body");
            Set(clue, "icon", Sprite());
            Refresh(view, new[] { clue });
            Assert.That(TextValue(title), Is.EqualTo("known_title"));
            Assert.That(TextValue(body), Is.EqualTo("known_body"));
            Assert.That(Property(image, "sprite"), Is.Null);
            Assert.That(((Component)Field(view, "illustrationPlaceholder")).gameObject.activeSelf, Is.True);
        }

        [Test]
        public void View_ChineseFontIncludesInactiveTemplate_AndNewRowsInheritIt()
        {
            Component view = View(out Transform rows, out Component title, out _, out _);
            Component template = (Component)Field(view, "rowTemplate");
            Component templateTitle = (Component)Field(template, "titleText");
            Assert.That(template.gameObject.activeSelf, Is.False);
            Call(view, "ApplyChineseFont");
            object font = Property(title, "font");
            Assert.That(font, Is.Not.Null);
            Assert.That(Property(templateTitle, "font"), Is.SameAs(font));
            Refresh(view, new[] { Clue("known", "已收集线索", "线索正文") });
            Component clone = rows.GetChild(0).GetComponent(RuntimeType("LostCity.EvidencePresentation.EvidenceThumbnailRow"));
            Assert.That(Property(Field(clone, "titleText"), "font"), Is.SameAs(font));
        }

        [Test]
        public void JournalHook_UsesOnlyCollectedClues_ExposesToggle_AndLeavesLegacyTextFallbackIntact()
        {
            Component view = View(out _, out _, out _, out _);
            ScriptableObject known = Clue("known", "known_title", "known_body");
            ScriptableObject secret = Clue("uncollected", "secret_title", "secret_body");
            ScriptableObject definition = Definition("LostCity.CombatSandbox.CaseDefinition");
            Set(definition, "caseName", "test_case");
            Set(definition, "clues", new[] { known, secret });
            Set(definition, "correctAnswer", "mystery_answer_marker");
            GameObject root = UiObject("Journal fixture");
            root.SetActive(false);
            Component progress = root.AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(progress, "caseDefinition", definition);
            ((IList)Field(progress, "collectedClues")).Add(known);
            Component journal = root.AddComponent(RuntimeType("LostCity.CombatSandbox.EvidenceJournal"));
            GameObject panel = UiObject("Panel", root.transform);
            panel.SetActive(false);
            Component header = UiObject("Header", root.transform).AddComponent(UiType("Text"));
            Component fallback = UiObject("Fallback", root.transform).AddComponent(UiType("Text"));
            Set(journal, "investigationProgress", progress);
            Set(journal, "panelRoot", panel);
            Set(journal, "titleText", header);
            Set(journal, "clueListText", fallback);
            Set(journal, "illustratedView", view);
            Call(journal, "Toggle");
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(Visible(view), Is.EqualTo(new[] { known }));
            Assert.That(TextValue(Field(view, "bodyText")), Does.Not.Contain("mystery_answer_marker").And.Not.Contain("secret_body"));
            Call(journal, "Hide");
            Assert.That(panel.activeSelf, Is.False);
            Set(journal, "illustratedView", null);
            Call(journal, "Refresh");
            Assert.That(TextValue(header), Is.EqualTo("test_case线索"));
            Assert.That(TextValue(fallback), Is.EqualTo("known_title [test_category]\nknown_body\n\n"));
        }

        private Component View(out Transform listRoot, out Component title, out Component body, out Component image)
        {
            GameObject root = UiObject("View fixture");
            root.SetActive(false);
            Component view = root.AddComponent(RuntimeType("LostCity.EvidencePresentation.IllustratedEvidenceJournal"));
            listRoot = UiObject("Rows", root.transform).transform;
            GameObject template = UiObject("Row template", root.transform);
            template.SetActive(false);
            Component row = template.AddComponent(RuntimeType("LostCity.EvidencePresentation.EvidenceThumbnailRow"));
            Set(row, "button", template.AddComponent(UiType("Button")));
            Set(row, "background", template.AddComponent(UiType("Image")));
            Set(row, "thumbnail", UiObject("Thumbnail", template.transform).AddComponent(UiType("Image")));
            Set(row, "titleText", UiObject("Title", template.transform).AddComponent(UiType("Text")));
            Set(row, "categoryText", UiObject("Category", template.transform).AddComponent(UiType("Text")));
            title = UiObject("Title", root.transform).AddComponent(UiType("Text"));
            body = UiObject("Body", root.transform).AddComponent(UiType("Text"));
            image = UiObject("Image", root.transform).AddComponent(UiType("Image"));
            Set(view, "listRoot", listRoot);
            Set(view, "rowTemplate", row);
            Set(view, "clueTitleText", title);
            Set(view, "bodyText", body);
            Set(view, "illustration", image);
            Set(view, "detailsRoot", UiObject("Details", root.transform));
            Set(view, "emptyText", UiObject("Empty", root.transform).AddComponent(UiType("Text")));
            Set(view, "illustrationPlaceholder", UiObject("Placeholder", root.transform).AddComponent(UiType("Text")));
            return view;
        }

        private ScriptableObject Catalog(string[] ids, Sprite[] sprites)
        {
            ScriptableObject catalog = Definition("LostCity.EvidencePresentation.EvidenceIllustrationCatalog");
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("clueId").stringValue = ids[i];
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("illustration").objectReferenceValue = sprites[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }
        private Sprite Sprite()
        {
            Texture2D texture = Track(new Texture2D(4, 4));
            return Track(UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
        }
        private ScriptableObject Clue(string id, string title, string body)
        {
            ScriptableObject clue = Definition("LostCity.CombatSandbox.ClueDefinition");
            Set(clue, "id", id);
            Set(clue, "clueName", title);
            Set(clue, "journalText", body);
            Set(clue, "category", "test_category");
            return clue;
        }
        private static void Refresh(Component view, ScriptableObject[] clues)
        {
            if (clues == null) { Call(view, "Refresh", new object[] { null }); return; }
            Array typed = Array.CreateInstance(RuntimeType("LostCity.CombatSandbox.ClueDefinition"), clues.Length);
            for (int i = 0; i < clues.Length; i++) typed.SetValue(clues[i], i);
            Call(view, "Refresh", (object)typed);
        }
        private GameObject UiObject(string name, Transform parent = null)
        {
            GameObject root = Track(new GameObject(name, typeof(RectTransform)));
            if (parent != null) root.transform.SetParent(parent, false);
            return root;
        }
        private ScriptableObject Definition(string name) => Track(ScriptableObject.CreateInstance(RuntimeType(name)));
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static Type UiType(string name) => Type.GetType("UnityEngine.UI." + name + ", UnityEngine.UI", true);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        private static string TextValue(object text) => (string)Property(text, "text");
        private static UnityEngine.Object[] Visible(Component view) => ((IEnumerable)Property(view, "VisibleClues")).Cast<UnityEngine.Object>().ToArray();
        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (value is Array source && field.FieldType.IsArray)
            {
                Array typed = Array.CreateInstance(field.FieldType.GetElementType(), source.Length);
                for (int i = 0; i < source.Length; i++) typed.SetValue(source.GetValue(i), i);
                value = typed;
            }
            field.SetValue(target, value);
        }
    }
}
