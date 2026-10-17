using System;
using UnityEditor;

namespace LostCity.CombatSandbox.EditorTools
{
    public sealed class GameContentCatalogPostprocessor : AssetPostprocessor
    {
        private static bool pending;
        private static bool rebuilding;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (pending || rebuilding || !(Relevant(imported) || Relevant(deleted) || Relevant(moved) || Relevant(movedFrom))) return;
            pending = true;
            EditorApplication.delayCall += Rebuild;
        }

        private static bool Relevant(string[] paths)
        {
            foreach (string path in paths)
                if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(path, GameContentCatalogBuilder.CatalogPath, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void Rebuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Rebuild;
                return;
            }
            pending = false;
            rebuilding = true;
            try { GameContentCatalogBuilder.Rebuild(); }
            finally { rebuilding = false; }
        }
    }
}
