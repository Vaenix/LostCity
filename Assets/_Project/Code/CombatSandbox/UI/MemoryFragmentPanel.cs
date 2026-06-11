using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class MemoryFragmentPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private string title = "记忆片段";
        [SerializeField] private string body = "304号病房被封锁，因为访客从未到来。遗憾属于你。";

        private void Awake()
        {
            Hide();
        }

        public void Show()
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
