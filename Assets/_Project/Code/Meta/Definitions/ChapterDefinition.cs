using System;
using LostCity.CombatSandbox;
using UnityEngine;

namespace LostCity.Meta
{
    [CreateAssetMenu(menuName = "Lost City/Meta/Chapter Definition")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        [SerializeField] private string chapterId;
        [SerializeField] private string chapterName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private CaseDefinition[] cases = new CaseDefinition[0];
        [SerializeField] private string[] requiredCompletedCaseIds = new string[0];

        public string ChapterId => chapterId;
        public string ChapterName => string.IsNullOrWhiteSpace(chapterName) ? name : chapterName;
        public string Description => description;
        public CaseDefinition[] Cases => cases == null ? Array.Empty<CaseDefinition>() : (CaseDefinition[])cases.Clone();
        public string[] RequiredCompletedCaseIds => requiredCompletedCaseIds == null
            ? Array.Empty<string>() : (string[])requiredCompletedCaseIds.Clone();
    }
}
