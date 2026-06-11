using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class EvidenceJournal : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text clueListText;

        private readonly StringBuilder textBuilder = new StringBuilder();

        private void Awake()
        {
            ResolveReferences();
            Hide();
            Refresh();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (investigationProgress != null)
            {
                investigationProgress.ClueCollected += HandleClueCollected;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void OnDisable()
        {
            if (investigationProgress != null)
            {
                investigationProgress.ClueCollected -= HandleClueCollected;
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Toggle()
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(!panelRoot.activeSelf);
            Refresh();
        }

        private void HandleClueCollected(ClueDefinition clue)
        {
            Refresh();
        }

        private void Refresh()
        {
            ResolveReferences();
            if (investigationProgress == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = investigationProgress.CaseTitle + "线索";
            }

            if (clueListText == null)
            {
                return;
            }

            if (investigationProgress.CollectedClues.Count == 0)
            {
                clueListText.text = "尚未收集线索。";
                return;
            }

            textBuilder.Clear();
            for (int i = 0; i < investigationProgress.CollectedClues.Count; i++)
            {
                ClueDefinition clue = investigationProgress.CollectedClues[i];
                textBuilder
                    .Append(clue.Name)
                    .Append(" [")
                    .Append(clue.Category)
                    .Append("]\n")
                    .Append(clue.JournalText)
                    .Append("\n\n");
            }

            clueListText.text = textBuilder.ToString();
        }

        private void ResolveReferences()
        {
            if (investigationProgress == null)
            {
                investigationProgress = FindObjectOfType<InvestigationProgress>();
            }
        }
    }
}
