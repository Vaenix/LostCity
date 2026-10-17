using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LostCity.Meta.Core.Tests
{
    public sealed class Phase6MigrationTests
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
        public void AuthoredLayout_ReusesIdentityAndTransform_GeneratesOnlyMissingClues_WithoutDuplicates()
        {
            Component layout = Layout(out Component progress, out ScriptableObject definition);
            ScriptableObject known = Clue("authored");
            ScriptableObject missing = Clue("missing");
            ScriptableObject other = Clue("other_case");
            Transform parent = Track(new GameObject("Authored parent")).transform;
            Component original = Pickup("Original", parent, known, progress);
            original.transform.localPosition = new Vector3(4.25f, -3f, 2f);
            original.transform.localRotation = Quaternion.Euler(0f, 0f, 23f);
            original.transform.localScale = new Vector3(0.55f, 0.7f, 1f);
            BoxCollider2D collider = original.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1.8f, 2.1f);
            Component duplicate = Pickup("Duplicate", parent, known, progress);
            Component unrelated = Pickup("Other case", parent, other, progress);
            Set(layout, "authoredPickups", new[] { original, duplicate, unrelated });
            Set(definition, "clues", new[] { known, missing, known });

            Call(layout, "Rebuild");

            Component[] active = Spawned(layout);
            Assert.That(active.Length, Is.EqualTo(2));
            Assert.That(active[0], Is.SameAs(original));
            Assert.That(duplicate.gameObject.activeSelf, Is.False);
            Assert.That(unrelated.gameObject.activeSelf, Is.False);
            Assert.That(original.transform.parent, Is.SameAs(parent));
            Assert.That(original.transform.localPosition, Is.EqualTo(new Vector3(4.25f, -3f, 2f)));
            Assert.That(original.transform.localRotation, Is.EqualTo(Quaternion.Euler(0f, 0f, 23f)));
            Assert.That(original.transform.localScale, Is.EqualTo(new Vector3(0.55f, 0.7f, 1f)));
            Assert.That(original.GetComponent<BoxCollider2D>(), Is.SameAs(collider));
            Assert.That(collider.size, Is.EqualTo(new Vector2(1.8f, 2.1f)));
            Component previousClone = active[1];

            Call(layout, "Rebuild");

            Assert.That(previousClone == null, Is.True, "Only the generated clone should be destroyed on rebuild.");
            Assert.That(original != null && duplicate != null && unrelated != null, Is.True);
            Assert.That(Spawned(layout).Length, Is.EqualTo(2));
            Assert.That(Spawned(layout)[0], Is.SameAs(original));
            Assert.That(parent.childCount, Is.EqualTo(3), "Generated clones must not enter or replace the authored hierarchy.");
        }

        [Test]
        public void AuthoredLayout_SwitchingCases_ReactivatesOriginalObjectsWithoutMovingOrDeletingThem()
        {
            Component layout = Layout(out Component progress, out ScriptableObject definition);
            ScriptableObject first = Clue("first");
            ScriptableObject second = Clue("second");
            Transform parent = Track(new GameObject("Authored parent")).transform;
            Component a = Pickup("A", parent, first, progress);
            Component b = Pickup("B", parent, second, progress);
            a.transform.localPosition = new Vector3(-6f, -2.2f, 0f);
            b.transform.localPosition = new Vector3(7f, 4f, 0f);
            Set(layout, "authoredPickups", new[] { a, b });
            Set(definition, "clues", new[] { first });
            Call(layout, "Rebuild");
            Assert.That(a.gameObject.activeSelf, Is.True);
            Assert.That(b.gameObject.activeSelf, Is.False);

            Set(definition, "clues", new[] { second });
            Call(layout, "Rebuild");
            Assert.That(Spawned(layout), Is.EqualTo(new[] { b }));
            Assert.That(a.gameObject.activeSelf, Is.False);
            Assert.That(b.gameObject.activeSelf, Is.True);

            Set(definition, "clues", new[] { first });
            Call(layout, "Rebuild");
            Assert.That(Spawned(layout), Is.EqualTo(new[] { a }));
            Assert.That(a.transform.parent, Is.SameAs(parent));
            Assert.That(b.transform.parent, Is.SameAs(parent));
            Assert.That(a.transform.localPosition, Is.EqualTo(new Vector3(-6f, -2.2f, 0f)));
            Assert.That(b.transform.localPosition, Is.EqualTo(new Vector3(7f, 4f, 0f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyOrNullAuthoredList_PreservesGeneratedLayoutContract(bool nullArray)
        {
            Component layout = Layout(out _, out ScriptableObject definition);
            Set(layout, "authoredPickups", nullArray ? null : new Component[0]);
            ScriptableObject first = Clue("first");
            ScriptableObject second = Clue("second");
            Set(definition, "clues", new[] { first, second });
            Call(layout, "Rebuild");
            Component[] initial = Spawned(layout);
            Assert.That(initial.Length, Is.EqualTo(2));
            Assert.That(Property(initial[0], "Definition"), Is.SameAs(first));
            Assert.That(Property(initial[1], "Definition"), Is.SameAs(second));
            foreach (Component pickup in initial)
            {
                Assert.That(pickup.transform.parent, Is.SameAs(layout.transform));
                Assert.That(pickup.transform.localPosition.magnitude, Is.EqualTo(6f).Within(0.001f));
            }
            Set(definition, "clues", new[] { first });
            Call(layout, "Rebuild");
            Assert.That(initial.All(item => item == null), Is.True);
            Assert.That(Spawned(layout).Length, Is.EqualTo(1));
        }

        [Test]
        public void LegacyShell_PreservesFiveSerializedFieldsAndGuid_WithoutGameplayEvents()
        {
            Type type = RuntimeType("LostCity.CombatSandbox.Room304GameStateController");
            Assert.That(type.GetCustomAttribute<ObsoleteAttribute>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<AddComponentMenu>().componentMenu, Is.Empty);
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.That(fields.Select(field => field.Name), Is.EquivalentTo(new[]
                { "investigationProgress", "deductionBoard", "enemySpawner", "bossSpawnController", "memoryFragmentPanel" }));
            Assert.That(fields.All(field => field.GetCustomAttribute<SerializeField>() != null), Is.True);
            Assert.That(type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly), Is.Empty);
            Assert.That(type.GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/_Project/Code/CombatSandbox/Core/Room304GameStateController.cs"),
                Is.EqualTo("27ac01993cbdc491a8e2bc6b3666343a"));
        }

        [Test]
        public void MigrationFillMissing_PreservesValidExistingReferences_AndIsIdempotent()
        {
            ScriptableObject definition = Definition("LostCity.CombatSandbox.CaseDefinition");
            ScriptableObject original = Definition("LostCity.CombatSandbox.BossDefinition");
            ScriptableObject replacement = Definition("LostCity.CombatSandbox.BossDefinition");
            Type migration = Type.GetType("LostCity.CombatSandbox.EditorTools.Phase6ProjectMigration, Assembly-CSharp-Editor", true);
            MethodInfo fill = migration.GetMethod("FillMissing", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(fill.Invoke(null, new object[] { definition, "bossDefinition", original }), Is.True);
            Assert.That(fill.Invoke(null, new object[] { definition, "bossDefinition", replacement }), Is.False);
            Assert.That(Property(definition, "BossDefinition"), Is.SameAs(original));
            Assert.That(fill.Invoke(null, new object[] { definition, "bossDefinition", original }), Is.False);
        }

        private Component Layout(out Component progress, out ScriptableObject definition)
        {
            GameObject root = Track(new GameObject("Layout fixture"));
            root.SetActive(false);
            definition = Definition("LostCity.CombatSandbox.CaseDefinition");
            progress = root.AddComponent(RuntimeType("LostCity.CombatSandbox.InvestigationProgress"));
            Set(progress, "caseDefinition", definition);
            Component template = Pickup("Template", root.transform, null, progress);
            template.gameObject.SetActive(false);
            Component layout = root.AddComponent(RuntimeType("LostCity.CombatSandbox.CaseClueLayout"));
            Set(layout, "investigationProgress", progress);
            Set(layout, "pickupTemplate", template);
            return layout;
        }
        private Component Pickup(string name, Transform parent, ScriptableObject clue, Component progress)
        {
            GameObject root = Track(new GameObject(name, typeof(BoxCollider2D)));
            root.transform.SetParent(parent, false);
            Component pickup = root.AddComponent(RuntimeType("LostCity.CombatSandbox.CluePickup"));
            Call(pickup, "Initialize", clue, progress);
            return pickup;
        }
        private ScriptableObject Clue(string id)
        {
            ScriptableObject clue = Definition("LostCity.CombatSandbox.ClueDefinition");
            Set(clue, "id", id);
            return clue;
        }
        private ScriptableObject Definition(string name) => Track(ScriptableObject.CreateInstance(RuntimeType(name)));
        private T Track<T>(T value) where T : UnityEngine.Object { objects.Add(value); return value; }
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
        private static Component[] Spawned(Component layout) => ((IEnumerable)Property(layout, "SpawnedPickups")).Cast<Component>().ToArray();
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
