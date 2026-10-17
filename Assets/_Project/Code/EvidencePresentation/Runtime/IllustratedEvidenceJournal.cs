using System;
using System.Collections.Generic;
using LostCity.CombatSandbox;
using LostCity.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.EvidencePresentation
{
    public sealed class IllustratedEvidenceJournal : MonoBehaviour
    {
        [SerializeField] private EvidenceJournal journal;
        [SerializeField] private EvidenceIllustrationCatalog catalog;
        [SerializeField] private Transform listRoot;
        [SerializeField] private EvidenceThumbnailRow rowTemplate;
        [SerializeField] private GameObject detailsRoot;
        [SerializeField] private Text emptyText;
        [SerializeField] private Image illustration;
        [SerializeField] private Text illustrationPlaceholder;
        [SerializeField] private Text clueTitleText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect bodyScroll;
        private readonly List<ClueDefinition> visibleClues = new List<ClueDefinition>();
        private readonly List<EvidenceThumbnailRow> rows = new List<EvidenceThumbnailRow>();

        public IReadOnlyList<ClueDefinition> VisibleClues => visibleClues.AsReadOnly();
        public ClueDefinition SelectedClue { get; private set; }
        public string SelectedClueId => SelectedClue != null ? SelectedClue.Id : null;

        private void Awake() { ApplyChineseFont(); }
        public void ApplyChineseFont() { HubViewSupport.ApplyChineseFont(transform); }
        private void OnEnable()
        {
            if (closeButton == null) return;
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
        private void OnDisable() { if (closeButton != null) closeButton.onClick.RemoveListener(Close); }

        public void Refresh(IEnumerable<ClueDefinition> collectedClues)
        {
            string selectedId = SelectedClueId;
            visibleClues.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (collectedClues != null)
                foreach (ClueDefinition clue in collectedClues)
                    if (clue != null && !string.IsNullOrWhiteSpace(clue.Id) && seen.Add(clue.Id)) visibleClues.Add(clue);
            SelectedClue = visibleClues.Find(clue => string.Equals(clue.Id, selectedId, StringComparison.Ordinal));
            if (SelectedClue == null && visibleClues.Count > 0) SelectedClue = visibleClues[0];
            RebuildRows();
            RenderSelection();
            if (!string.Equals(selectedId, SelectedClueId, StringComparison.Ordinal) && bodyScroll != null)
                bodyScroll.verticalNormalizedPosition = 1f;
        }

        public bool SelectClue(string clueId)
        {
            ClueDefinition clue = visibleClues.Find(item => string.Equals(item.Id, clueId, StringComparison.Ordinal));
            if (clue == null) return false;
            bool changed = SelectedClue != clue;
            SelectedClue = clue;
            RenderSelection();
            if (changed && bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
            return true;
        }

        private void RebuildRows()
        {
            foreach (EvidenceThumbnailRow row in rows)
            {
                if (row == null) continue;
                row.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(row.gameObject);
                else DestroyImmediate(row.gameObject);
            }
            rows.Clear();
            if (listRoot == null || rowTemplate == null) return;
            rowTemplate.gameObject.SetActive(false);
            foreach (ClueDefinition clue in visibleClues)
            {
                EvidenceThumbnailRow row = Instantiate(rowTemplate, listRoot);
                row.Bind(clue, catalog != null ? catalog.GetIllustration(clue.Id) : null, Choose);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
        }

        private void RenderSelection()
        {
            bool hasSelection = SelectedClue != null;
            if (detailsRoot != null) detailsRoot.SetActive(hasSelection);
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(!hasSelection);
                emptyText.text = "尚未收集线索。";
            }
            if (clueTitleText != null) clueTitleText.text = hasSelection ? SelectedClue.Name : string.Empty;
            if (categoryText != null) categoryText.text = hasSelection ? SelectedClue.Category : string.Empty;
            if (bodyText != null) bodyText.text = hasSelection ? SelectedClue.JournalText : string.Empty;
            Sprite sprite = hasSelection && catalog != null ? catalog.GetIllustration(SelectedClue.Id) : null;
            if (illustration != null)
            {
                illustration.sprite = sprite;
                illustration.preserveAspect = true;
                illustration.enabled = sprite != null;
            }
            if (illustrationPlaceholder != null)
            {
                illustrationPlaceholder.text = "暂无图示";
                illustrationPlaceholder.gameObject.SetActive(hasSelection && sprite == null);
            }
            for (int i = 0; i < rows.Count; i++) rows[i].SetSelected(visibleClues[i] == SelectedClue);
        }

        private void Choose(string id) { SelectClue(id); }
        private void Close() { if (journal != null) journal.Hide(); }
    }
}
