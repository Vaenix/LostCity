using System;
using System.Collections.Generic;
using System.Text;
using LostCity.CombatSandbox;
using LostCity.Meta.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Meta
{
    public sealed class InvestigationArchiveUI : MonoBehaviour
    {
        [SerializeField] private Text documentText;
        private HubViewSupport view;

        private void OnEnable()
        {
            view = new HubViewSupport(Refresh);
            view.Tick();
        }
        private void Update() { view?.Tick(); }
        private void OnDisable() { view?.Dispose(); view = null; }

        public void Refresh()
        {
            if (documentText == null) return;
            if (view == null || !view.Ready) { documentText.text = "案件档案\n\n档案服务尚未就绪。"; return; }
            var discovered = new HashSet<string>(view.Save.DiscoveredClueIds, StringComparer.Ordinal);
            var displayed = new HashSet<string>(StringComparer.Ordinal);
            var text = new StringBuilder("案件档案\n\n已完成案件\n");
            IReadOnlyList<CaseRegistryEntry> entries = view.Registry.GetEntries();
            bool anyCompleted = false;
            foreach (CaseRegistryEntry entry in entries)
            {
                if (entry?.Definition == null || entry.State != CaseState.Completed) continue;
                anyCompleted = true;
                CaseDefinition definition = entry.Definition;
                text.Append("\n【").Append(definition.CaseName).AppendLine("】");
                text.AppendLine(string.IsNullOrWhiteSpace(definition.CaseSummary) ? "暂无案件摘要。" : definition.CaseSummary);
                text.AppendLine("已发现线索");
                if (!AppendClues(text, definition.Clues, discovered, displayed)) text.AppendLine("暂无已发现线索。");
            }
            if (!anyCompleted) text.AppendLine("暂无已完成案件。");

            text.AppendLine("\n调查中的已发现线索");
            bool anyOngoing = false;
            foreach (CaseRegistryEntry entry in entries)
            {
                if (entry?.Definition == null || entry.State == CaseState.Completed) continue;
                var clues = new StringBuilder();
                if (!AppendClues(clues, entry.Definition.Clues, discovered, displayed)) continue;
                anyOngoing = true;
                text.Append("\n【").Append(entry.Definition.CaseName).AppendLine("】").Append(clues);
            }
            if (!anyOngoing) text.AppendLine("暂无调查中的已发现线索。");

            var unassigned = new StringBuilder();
            foreach (ClueDefinition clue in view.Registry.Clues)
                if (clue != null && discovered.Contains(clue.Id) && !displayed.Contains(clue.Id))
                    AppendClue(unassigned, clue, displayed);
            if (unassigned.Length > 0) text.AppendLine("\n未归档线索").Append(unassigned);
            documentText.text = text.ToString();
        }

        private static bool AppendClues(StringBuilder text, ClueDefinition[] clues, HashSet<string> discovered, HashSet<string> displayed)
        {
            if (clues == null) return false;
            bool any = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ClueDefinition clue in clues)
            {
                if (clue == null || !discovered.Contains(clue.Id) || !seen.Add(clue.Id)) continue;
                AppendClue(text, clue, displayed);
                any = true;
            }
            return any;
        }

        private static void AppendClue(StringBuilder text, ClueDefinition clue, HashSet<string> displayed)
        {
            displayed.Add(clue.Id);
            text.Append("· ").Append(clue.Name);
            if (!string.IsNullOrWhiteSpace(clue.Category)) text.Append("｜").Append(clue.Category);
            text.AppendLine().Append("  ").AppendLine(clue.JournalText);
        }
    }
}
