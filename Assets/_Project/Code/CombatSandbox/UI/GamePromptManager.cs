using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class GamePromptManager : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float defaultDurationSeconds = 3f;

        private static GamePromptManager instance;
        private float hideTime;

        public static GamePromptManager Instance => instance != null ? instance : FindObjectOfType<GamePromptManager>(includeInactive: true);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Hide();
        }

        private void Update()
        {
            if (panelRoot != null && panelRoot.activeSelf && hideTime > 0f && Time.unscaledTime >= hideTime)
            {
                Hide();
            }
        }

        public void ShowPrompt(string message, PromptType type)
        {
            ShowPrompt(message, type, defaultDurationSeconds);
        }

        public void ShowPrompt(string message, PromptType type, float durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = GetColor(type);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            hideTime = durationSeconds > 0f ? Time.unscaledTime + durationSeconds : 0f;
        }

        public void Hide()
        {
            hideTime = 0f;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private static Color GetColor(PromptType type)
        {
            switch (type)
            {
                case PromptType.Success:
                    return new Color(0.05f, 0.35f, 0.18f, 0.92f);
                case PromptType.Warning:
                    return new Color(0.45f, 0.3f, 0.05f, 0.92f);
                case PromptType.Quest:
                    return new Color(0.12f, 0.2f, 0.38f, 0.92f);
                case PromptType.Clue:
                    return new Color(0.16f, 0.12f, 0.36f, 0.92f);
                case PromptType.Combat:
                    return new Color(0.38f, 0.08f, 0.08f, 0.92f);
                default:
                    return new Color(0.04f, 0.05f, 0.06f, 0.92f);
            }
        }
    }
}
