using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostCity.Meta
{
    public static class ContentDefinitionIndex
    {
        public static Dictionary<string, T> Build<T>(IEnumerable<T> definitions, Func<T, string> getId)
            where T : UnityEngine.Object
        {
            var groups = new Dictionary<string, List<T>>(StringComparer.Ordinal);
            foreach (T definition in definitions)
            {
                if (definition == null) continue;
                string id = getId(definition);
                if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                {
                    Debug.LogError($"Invalid {typeof(T).Name} ID on '{definition.name}': IDs must be nonblank and trimmed.", definition);
                    continue;
                }
                if (!groups.TryGetValue(id, out var group)) groups.Add(id, group = new List<T>());
                group.Add(definition);
            }

            var index = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var pair in groups)
            {
                if (pair.Value.Count == 1) index.Add(pair.Key, pair.Value[0]);
                else foreach (T definition in pair.Value)
                    Debug.LogError($"Duplicate {typeof(T).Name} ID '{pair.Key}' on '{definition.name}'; all ambiguous entries excluded.", definition);
            }
            return index;
        }
    }
}
