using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Clue Definition")]
    public sealed class ClueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string clueName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string title;
        [SerializeField] private string category;
        [SerializeField] private ClueType type;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string journalText;
        [SerializeField] private string shortDescription;
        [SerializeField] private string fullDescription;

        public string Id => id;
        public string Name => FirstNonEmpty(clueName, title, name);
        public string Description => FirstNonEmpty(description, shortDescription);
        public string Category => category;
        public ClueType Type => type;
        public Sprite Icon => icon;
        public string JournalText => FirstNonEmpty(journalText, fullDescription, description, shortDescription);

        public string Title => Name;
        public string ShortDescription => Description;
        public string FullDescription => JournalText;

        private static string FirstNonEmpty(params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return string.Empty;
        }
    }
}
