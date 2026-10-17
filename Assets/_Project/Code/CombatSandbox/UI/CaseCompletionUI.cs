using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class CaseCompletionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button retryButton;
        private float remainingSeconds;
        private bool waitingForReturn;
        private bool waitingForRetry;
        public event Action ReturnRequested;
        public event Action RetryRequested;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(RequestRetry);
            Hide();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (waitingForRetry) RequestRetry();
                else if (waitingForReturn) RequestReturn();
            }
            Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float elapsedSeconds)
        {
            if (!waitingForReturn) return;
            remainingSeconds -= Mathf.Max(0f, elapsedSeconds);
            if (remainingSeconds <= 0f) RequestReturn();
        }

        public void ShowChapterComplete(string caseName, string completionText)
        {
            waitingForRetry = false;
            waitingForReturn = true;
            remainingSeconds = 3f;
            if (titleText != null) titleText.text = "案件完成";
            if (bodyText != null) bodyText.text = (string.IsNullOrWhiteSpace(completionText) ? caseName : completionText)
                + "\n正在返回大厅";
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void ShowError(string message)
        {
            waitingForReturn = false;
            waitingForRetry = true;
            if (titleText != null) titleText.text = "暂时无法完成";
            if (bodyText != null) bodyText.text = message + "\n按空格重试";
            if (retryButton != null) retryButton.gameObject.SetActive(true);
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            waitingForReturn = false;
            waitingForRetry = false;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void RequestReturn()
        {
            if (!waitingForReturn) return;
            waitingForReturn = false;
            ReturnRequested?.Invoke();
        }

        private void RequestRetry()
        {
            if (!waitingForRetry) return;
            waitingForRetry = false;
            RetryRequested?.Invoke();
        }
    }
}
