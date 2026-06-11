using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class Room304CompletionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private bool waitingForContinue;
        private bool hasContinued;

        public event Action ContinueRequested;

        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            if (!waitingForContinue || hasContinued)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                hasContinued = true;
                waitingForContinue = false;
                ContinueRequested?.Invoke();
            }
        }

        public void ShowChapterComplete()
        {
            ShowChapterComplete("304号病房", string.Empty);
        }

        public void ShowChapterComplete(string caseName, string completionText)
        {
            hasContinued = false;
            waitingForContinue = true;

            if (titleText != null)
            {
                titleText.text = "章节完成";
            }

            if (bodyText != null)
            {
                string resolvedCaseName = string.IsNullOrWhiteSpace(caseName) ? "当前案件" : caseName;
                string resolvedCompletionText = string.IsNullOrWhiteSpace(completionText) ? resolvedCaseName : completionText;
                bodyText.text = resolvedCompletionText + "\n按空格继续";
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void ShowNextChapterPlaceholder()
        {
            waitingForContinue = false;

            if (titleText != null)
            {
                titleText.text = "下一章节开发中";
            }

            if (bodyText != null)
            {
                bodyText.text = "感谢测试304号病房原型";
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            waitingForContinue = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
