using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class InvestigationProgress : MonoBehaviour
    {
        [SerializeField] private CaseDefinition caseDefinition;
        [SerializeField] private string caseTitle = "304号病房";
        [SerializeField] private string mysteryQuestion = "304号病房为什么被封锁？";
        [SerializeField] private ClueDefinition[] requiredClues;

        private readonly List<ClueDefinition> collectedClues = new List<ClueDefinition>();
        private bool caseSolved;
        private bool allRequiredCluesCollected;

        public event Action<ClueDefinition> ClueCollected;
        public event Action AllRequiredCluesCollected;
        public event Action CaseSolved;

        public CaseDefinition CurrentCase => caseDefinition;
        public string CaseTitle => caseDefinition != null ? caseDefinition.CaseName : caseTitle;
        public string CaseDescription => caseDefinition != null ? caseDefinition.Description : string.Empty;
        public string MysteryQuestion => caseDefinition != null ? caseDefinition.DeductionQuestion : mysteryQuestion;
        public string CorrectAnswer => caseDefinition != null ? caseDefinition.CorrectAnswer : string.Empty;
        public string CompletionText => caseDefinition != null ? caseDefinition.CompletionText : string.Empty;
        public BossDefinition BossDefinition => caseDefinition != null ? caseDefinition.BossDefinition : null;
        public RewardDefinition[] RewardPool => caseDefinition != null ? caseDefinition.RewardPool : null;
        public IReadOnlyList<ClueDefinition> RequiredClues => GetRequiredClues();
        public IReadOnlyList<ClueDefinition> CollectedClues => collectedClues;
        public bool IsCaseSolved => caseSolved;
        public bool HasCollectedAllRequiredClues => AreAllRequiredCluesCollected();

        public bool TryCollectClue(ClueDefinition clue)
        {
            if (clue == null || caseSolved || HasClue(clue))
            {
                return false;
            }

            collectedClues.Add(clue);
            ClueCollected?.Invoke(clue);
            NotifyAllRequiredCluesCollectedIfReady();
            return true;
        }

        public bool HasClue(ClueDefinition clue)
        {
            if (clue == null)
            {
                return false;
            }

            for (int i = 0; i < collectedClues.Count; i++)
            {
                if (collectedClues[i] == clue || collectedClues[i].Id == clue.Id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCorrectDeduction(IReadOnlyList<ClueDefinition> selectedClues)
        {
            IReadOnlyList<ClueDefinition> activeRequiredClues = GetRequiredClues();
            if (activeRequiredClues.Count == 0 || selectedClues == null)
            {
                return false;
            }

            if (selectedClues.Count != activeRequiredClues.Count)
            {
                return false;
            }

            for (int i = 0; i < activeRequiredClues.Count; i++)
            {
                if (!ContainsClue(selectedClues, activeRequiredClues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void MarkCaseSolved()
        {
            if (caseSolved)
            {
                return;
            }

            caseSolved = true;
            CaseSolved?.Invoke();
        }

        public void CollectAllRequiredClues()
        {
            IReadOnlyList<ClueDefinition> activeRequiredClues = GetRequiredClues();
            for (int i = 0; i < activeRequiredClues.Count; i++)
            {
                TryCollectClue(activeRequiredClues[i]);
            }
        }

        private void NotifyAllRequiredCluesCollectedIfReady()
        {
            if (allRequiredCluesCollected || !AreAllRequiredCluesCollected())
            {
                return;
            }

            allRequiredCluesCollected = true;
            AllRequiredCluesCollected?.Invoke();
        }

        private bool AreAllRequiredCluesCollected()
        {
            IReadOnlyList<ClueDefinition> activeRequiredClues = GetRequiredClues();
            if (activeRequiredClues.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < activeRequiredClues.Count; i++)
            {
                if (!HasClue(activeRequiredClues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyList<ClueDefinition> GetRequiredClues()
        {
            if (caseDefinition != null && caseDefinition.Clues != null)
            {
                return caseDefinition.Clues;
            }

            return requiredClues ?? Array.Empty<ClueDefinition>();
        }

        private static bool ContainsClue(IReadOnlyList<ClueDefinition> clues, ClueDefinition target)
        {
            if (target == null)
            {
                return false;
            }

            for (int i = 0; i < clues.Count; i++)
            {
                if (clues[i] == target || clues[i].Id == target.Id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
