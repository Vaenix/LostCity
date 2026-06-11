using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Clue Definition")]
    public sealed class ClueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string category;
        [SerializeField] private string shortDescription;
        [SerializeField] private string fullDescription;

        public string Id => id;
        public string Title => title;
        public string Category => category;
        public string ShortDescription => shortDescription;
        public string FullDescription => fullDescription;
    }
}
