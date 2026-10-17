using System;
using System.Collections.Generic;
using LostCity.Meta;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class InvestigationProgress : MonoBehaviour
    {
        [SerializeField] private CaseDefinition caseDefinition;
        [SerializeField] private string caseTitle = "当前案件";
        [SerializeField] private string mysteryQuestion = "案件真相是什么？";
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
        public RewardDefinition[] RewardPool => caseDefinition != null ? caseDefinition.GetCompletionRewards() : Array.Empty<RewardDefinition>();
        public IReadOnlyList<ClueDefinition> RequiredClues => GetRequiredClues();
        public IReadOnlyList<ClueDefinition> CollectedClues => collectedClues.ToArray();
        public bool IsCaseSolved => caseSolved;
        public bool HasCollectedAllRequiredClues => AreAllRequiredCluesCollected();

        private void Awake()
        {
            var runtime = GameRuntime.Instance;
            if (runtime != null && runtime.CaseSession.HasActiveCase) caseDefinition = runtime.CaseSession.ActiveCase;
            if (caseDefinition == null)
            {
                Debug.LogError("未找到当前案件配置。", this);
                runtime?.SceneFlow.LoadHub();
            }
        }

        public bool TryCollectClue(ClueDefinition clue)
        {
            if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || caseSolved || HasClue(clue)
                || !ContainsClue(GetRequiredClues(), clue))
            {
                return false;
            }

            collectedClues.Add(clue);
            MetaProgressionRecorder.RecordClue(clue);
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

            if (!HasCollectedAllRequiredClues) return;
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

            return requiredClues == null ? Array.Empty<ClueDefinition>() : (ClueDefinition[])requiredClues.Clone();
        }

        private static bool ContainsClue(IReadOnlyList<ClueDefinition> clues, ClueDefinition target)
        {
            if (target == null)
            {
                return false;
            }

            for (int i = 0; i < clues.Count; i++)
            {
                if (clues[i] != null && (clues[i] == target || clues[i].Id == target.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
