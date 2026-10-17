using System;
using System.Linq;
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
        [SerializeField, TextArea] private string caseSummary;
        [SerializeField] private bool isPlayable = true;

        public string CaseId => caseId;
        public string CaseName => string.IsNullOrWhiteSpace(caseName) ? name : caseName;
        public string Description => description;
        public ClueDefinition[] Clues => clues == null ? Array.Empty<ClueDefinition>() : (ClueDefinition[])clues.Clone();
        public string DeductionQuestion => deductionQuestion;
        public string CorrectAnswer => correctAnswer;
        public BossDefinition BossDefinition => bossDefinition;
        public RewardDefinition[] RewardPool => rewardPool == null ? Array.Empty<RewardDefinition>() : (RewardDefinition[])rewardPool.Clone();
        public string CompletionText => completionText;
        public string CaseSummary => caseSummary;
        public bool IsPlayable => isPlayable;

        public RewardDefinition[] GetCompletionRewards()
        {
            var valid = FilterRewards(rewardPool);
            return valid.Length > 0 ? valid : FilterRewards(bossDefinition == null ? null : bossDefinition.RewardPool);
        }

        private static RewardDefinition[] FilterRewards(RewardDefinition[] pool) => pool == null
            ? Array.Empty<RewardDefinition>() : pool.Where(reward => reward != null && reward.IsApplicable).ToArray();
    }
}
