using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Case Definition")]
    public sealed class CaseDefinition : ScriptableObject
    {
        [SerializeField] private string caseId;
        [SerializeField] private string caseName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ClueDefinition[] clues;
        [SerializeField, TextArea] private string deductionQuestion;
        [SerializeField, TextArea] private string correctAnswer;
        [SerializeField] private BossDefinition bossDefinition;
        [SerializeField] private RewardDefinition[] rewardPool;
        [SerializeField, TextArea] private string completionText;

        public string CaseId => caseId;
        public string CaseName => string.IsNullOrWhiteSpace(caseName) ? name : caseName;
        public string Description => description;
        public ClueDefinition[] Clues => clues;
        public string DeductionQuestion => deductionQuestion;
        public string CorrectAnswer => correctAnswer;
        public BossDefinition BossDefinition => bossDefinition;
        public RewardDefinition[] RewardPool => rewardPool;
        public string CompletionText => completionText;
    }
}
