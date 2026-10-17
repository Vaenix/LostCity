using System;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class RewardSelectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button attackButton;
        [SerializeField] private Text attackButtonText;
        [SerializeField] private Button maxHpButton;
        [SerializeField] private Text maxHpButtonText;
        [SerializeField] private Button critButton;
        [SerializeField] private Text critButtonText;
        private RewardDefinition[] activeRewards = Array.Empty<RewardDefinition>();
        private bool hasSelected;

        public event Action<RewardDefinition> RewardSelected;
        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (attackButton != null) attackButton.onClick.AddListener(() => SelectReward(0));
            if (maxHpButton != null) maxHpButton.onClick.AddListener(() => SelectReward(1));
            if (critButton != null) critButton.onClick.AddListener(() => SelectReward(2));
            Hide();
        }

        public void Show(PlayerStats playerStats, string completionText, RewardDefinition[] rewards, CombatUpgradeStats upgradeStats)
        {
            // The caller supplies CaseDefinition.GetCompletionRewards(), shared with start validation.
            activeRewards = rewards == null ? Array.Empty<RewardDefinition>() : (RewardDefinition[])rewards.Clone();
            hasSelected = false;
            if (titleText != null) titleText.text = "真相已揭开";
            if (bodyText != null) bodyText.text = completionText + "\n选择一项奖励";
            BindButton(attackButton, attackButtonText, 0);
            BindButton(maxHpButton, maxHpButtonText, 1);
            BindButton(critButton, critButtonText, 2);
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void SelectReward(int index)
        {
            if (hasSelected || index < 0 || index >= activeRewards.Length || activeRewards[index] == null) return;
            hasSelected = true;
            var selected = activeRewards[index];
            Hide();
            // Application and durable completion are owned by GameFlowManager, not presentation.
            RewardSelected?.Invoke(selected);
        }

        private void BindButton(Button button, Text label, int index)
        {
            var reward = index < activeRewards.Length ? activeRewards[index] : null;
            if (button != null)
            {
                button.gameObject.SetActive(reward != null);
                button.interactable = reward != null;
            }
            if (label != null) label.text = reward == null ? string.Empty
                : string.IsNullOrWhiteSpace(reward.Description) ? reward.DisplayName : reward.DisplayName + "\n" + reward.Description;
        }
    }
}
