using System;
using UnityEngine;

namespace LostCity.EvidencePresentation
{
    public sealed class EvidenceIllustrationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string clueId;
            [SerializeField] private Sprite illustration;
            public string ClueId => clueId;
            public Sprite Illustration => illustration;
        }

        [SerializeField] private Entry[] entries = new Entry[0];
        public Entry[] Entries => entries == null ? new Entry[0] : (Entry[])entries.Clone();

        public Sprite GetIllustration(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId) || entries == null) return null;
            Sprite result = null;
            bool found = false;
            foreach (Entry entry in entries)
            {
                if (entry == null || !string.Equals(entry.ClueId, clueId, StringComparison.Ordinal)) continue;
                if (found) return null; // Ambiguous IDs must never pair evidence with the wrong image.
                found = true;
                result = entry.Illustration;
            }
            return result;
        }
    }
}
