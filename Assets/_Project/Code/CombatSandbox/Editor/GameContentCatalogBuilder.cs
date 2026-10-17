using System;
using System.Collections.Generic;
using System.Linq;
using LostCity.Meta;
using UnityEditor;
using UnityEngine;

namespace LostCity.CombatSandbox.EditorTools
{
    public static class GameContentCatalogBuilder
    {
        public const string CatalogPath = "Assets/_Project/Resources/GameContentCatalog.asset";

        [MenuItem("Tools/Lost City/Rebuild Content Catalog")]
        public static GameContentCatalog Rebuild()
        {
            var cases = Find<CaseDefinition>();
            var chapters = Find<ChapterDefinition>();
            var clues = Find<ClueDefinition>();
            var enemies = Find<EnemyDefinition>();
            var bosses = Find<BossDefinition>();
            ContentDefinitionIndex.Build(cases, value => value.CaseId);
            ContentDefinitionIndex.Build(chapters, value => value.ChapterId);
            ContentDefinitionIndex.Build(clues, value => value.Id);
            ContentDefinitionIndex.Build(enemies, value => value.EnemyId);
            ContentDefinitionIndex.Build(bosses, value => value.BossId);
            foreach (var chapter in chapters)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var definition in chapter.Cases)
                    if (definition != null && !string.IsNullOrWhiteSpace(definition.CaseId) && !seen.Add(definition.CaseId))
                        Debug.LogError($"Chapter '{chapter.ChapterId}' references case '{definition.CaseId}' more than once.", chapter);
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var serialized = new SerializedObject(catalog);
            bool changed = Assign(serialized, "cases", cases);
            changed |= Assign(serialized, "chapters", chapters);
            changed |= Assign(serialized, "clues", clues);
            changed |= Assign(serialized, "enemies", enemies);
            changed |= Assign(serialized, "bosses", bosses);
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
            return catalog;
        }

        private static T[] Find<T>() where T : UnityEngine.Object
        {
            // Include sub-assets and break path ties by local file ID, not FindAssets enumeration order.
            var paths = AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(path => path, StringComparer.Ordinal);
            return paths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<T>()
                .OrderBy(value => LocalId(value))).ToArray();
        }

        private static long LocalId(UnityEngine.Object value)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string _, out long id);
            return id;
        }

        private static bool Assign<T>(SerializedObject target, string field, T[] values) where T : UnityEngine.Object
        {
            var property = target.FindProperty(field);
            bool same = property.arraySize == values.Length;
            if (same)
                for (int i = 0; i < values.Length; i++)
                    same &= property.GetArrayElementAtIndex(i).objectReferenceValue == values[i];
            if (same) return false;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            return true;
        }
    }
}
