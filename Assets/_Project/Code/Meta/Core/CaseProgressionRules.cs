using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    public static class CaseProgressionRules
    {
        public static CaseState GetCaseState(
            string caseId,
            CaseProgressionInput chapter,
            ISet<string> completedCaseIds)
        {
            if (string.IsNullOrWhiteSpace(caseId) || chapter == null)
            {
                return CaseState.Locked;
            }

            int index = Array.IndexOf(chapter.OrderedCaseIds, caseId);
            if (index < 0)
            {
                return CaseState.Locked;
            }

            completedCaseIds ??= new HashSet<string>();
            if (completedCaseIds.Contains(caseId))
            {
                return CaseState.Completed;
            }

            if (!AreAllCompleted(chapter.RequiredCompletedCaseIds, completedCaseIds))
            {
                return CaseState.Locked;
            }

            return index == 0 || completedCaseIds.Contains(chapter.OrderedCaseIds[index - 1])
                ? CaseState.Available
                : CaseState.Locked;
        }

        public static int GetCurrentChapterIndex(
            IReadOnlyList<CaseProgressionInput> chapters,
            ISet<string> completedCaseIds)
        {
            if (chapters == null || chapters.Count == 0)
            {
                return -1;
            }

            completedCaseIds ??= new HashSet<string>();
            int latestUnlocked = -1;

            for (int i = 0; i < chapters.Count; i++)
            {
                CaseProgressionInput chapter = chapters[i];
                if (chapter == null || chapter.OrderedCaseIds.Length == 0)
                {
                    continue;
                }

                if (!AreAllCompleted(chapter.RequiredCompletedCaseIds, completedCaseIds))
                {
                    continue;
                }

                latestUnlocked = i;
                if (!AreAllCompleted(chapter.OrderedCaseIds, completedCaseIds))
                {
                    return i;
                }
            }

            return latestUnlocked;
        }

        private static bool AreAllCompleted(string[] requiredIds, ISet<string> completedCaseIds)
        {
            for (int i = 0; i < requiredIds.Length; i++)
            {
                if (!completedCaseIds.Contains(requiredIds[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
