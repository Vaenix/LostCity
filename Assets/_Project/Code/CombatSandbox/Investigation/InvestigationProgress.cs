using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class InvestigationProgress : MonoBehaviour
    {
        [SerializeField] private string caseTitle = "304号病房";
        [SerializeField] private string mysteryQuestion = "304号病房为什么被封锁？";
        [SerializeField] private ClueDefinition[] requiredClues;

        private readonly List<ClueDefinition> collectedClues = new List<ClueDefinition>();
        private bool caseSolved;

        public event Action<ClueDefinition> ClueCollected;
        public event Action CaseSolved;

        public string CaseTitle => caseTitle;
        public string MysteryQuestion => mysteryQuestion;
        public IReadOnlyList<ClueDefinition> CollectedClues => collectedClues;
        public bool IsCaseSolved => caseSolved;

        public bool TryCollectClue(ClueDefinition clue)
        {
            if (clue == null || caseSolved || HasClue(clue))
            {
                return false;
            }

            collectedClues.Add(clue);
            ClueCollected?.Invoke(clue);
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
            if (requiredClues == null || requiredClues.Length == 0 || selectedClues == null)
            {
                return false;
            }

            if (selectedClues.Count != requiredClues.Length)
            {
                return false;
            }

            for (int i = 0; i < requiredClues.Length; i++)
            {
                if (!ContainsClue(selectedClues, requiredClues[i]))
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
            if (requiredClues == null)
            {
                return;
            }

            for (int i = 0; i < requiredClues.Length; i++)
            {
                TryCollectClue(requiredClues[i]);
            }
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
