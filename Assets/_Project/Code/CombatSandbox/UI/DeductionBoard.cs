using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class DeductionBoard : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text questionText;
        [SerializeField] private RectTransform clueButtonRoot;
        [SerializeField] private Button clueButtonTemplate;
        [SerializeField] private Text selectedCluesText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button submitButton;

        private readonly List<ClueDefinition> selectedClues = new List<ClueDefinition>();
        private readonly List<Button> clueButtons = new List<Button>();
        private readonly StringBuilder textBuilder = new StringBuilder();
        private bool isAvailable = true;
        private Coroutine correctDeductionRoutine;

        private void Awake()
        {
            ResolveReferences();
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(Submit);
            }

            if (clueButtonTemplate != null)
            {
                clueButtonTemplate.gameObject.SetActive(false);
            }

            Hide();
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
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
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

        public void SetAvailable(bool available)
        {
            isAvailable = available;
            if (!available)
            {
                Hide();
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
            if (!isAvailable || panelRoot == null || investigationProgress != null && investigationProgress.IsCaseSolved)
            {
                return;
            }

            panelRoot.SetActive(!panelRoot.activeSelf);
            if (panelRoot.activeSelf)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            ResolveReferences();
            ClearClueButtons();

            if (investigationProgress == null)
            {
                return;
            }

            if (questionText != null)
            {
                questionText.text = investigationProgress.MysteryQuestion;
            }

            for (int i = 0; i < investigationProgress.CollectedClues.Count; i++)
            {
                CreateClueButton(investigationProgress.CollectedClues[i]);
            }

            RefreshSelectedText();
            if (feedbackText != null && string.IsNullOrWhiteSpace(feedbackText.text))
            {
                feedbackText.text = "选择能回答问题的证据。";
            }
        }

        private void CreateClueButton(ClueDefinition clue)
        {
            if (clue == null || clueButtonTemplate == null || clueButtonRoot == null)
            {
                return;
            }

            Button button = Instantiate(clueButtonTemplate, clueButtonRoot);
            button.name = clue.Title + "Button";
            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => ToggleClue(clue));

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = clue.Title + "\n" + clue.ShortDescription;
            }

            clueButtons.Add(button);
        }

        private void ClearClueButtons()
        {
            for (int i = 0; i < clueButtons.Count; i++)
            {
                if (clueButtons[i] != null)
                {
                    Destroy(clueButtons[i].gameObject);
                }
            }

            clueButtons.Clear();
        }

        private void ToggleClue(ClueDefinition clue)
        {
            if (selectedClues.Contains(clue))
            {
                selectedClues.Remove(clue);
            }
            else
            {
                selectedClues.Add(clue);
            }

            RefreshSelectedText();
        }

        private void Submit()
        {
            if (investigationProgress == null || correctDeductionRoutine != null)
            {
                return;
            }

            if (investigationProgress.IsCorrectDeduction(selectedClues))
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "真相重现";
                }

                if (submitButton != null)
                {
                    submitButton.interactable = false;
                }

                correctDeductionRoutine = StartCoroutine(CompleteCorrectDeduction());
                return;
            }

            if (feedbackText != null)
            {
                feedbackText.text = "证据还无法成立。";
            }
        }

        private IEnumerator CompleteCorrectDeduction()
        {
            yield return new WaitForSecondsRealtime(0.75f);
            investigationProgress.MarkCaseSolved();
            Hide();
        }

        private void RefreshSelectedText()
        {
            if (selectedCluesText == null)
            {
                return;
            }

            if (selectedClues.Count == 0)
            {
                selectedCluesText.text = "已选择线索：无";
                return;
            }

            textBuilder.Clear();
            textBuilder.Append("已选择线索：\n");
            for (int i = 0; i < selectedClues.Count; i++)
            {
                textBuilder.Append("- ").Append(selectedClues[i].Title).Append('\n');
            }

            selectedCluesText.text = textBuilder.ToString();
        }

        private void HandleClueCollected(ClueDefinition clue)
        {
            if (panelRoot != null && panelRoot.activeSelf)
            {
                Refresh();
            }
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
