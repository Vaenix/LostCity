using System;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class Room304RewardSelectionUI : MonoBehaviour
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
        [SerializeField] private RewardDefinition[] rewardPool;

        private PlayerStats activePlayerStats;
        private CombatUpgradeStats activeUpgradeStats;
        private RewardDefinition[] activeRewards;
        private bool hasSelected;

        public event Action<RewardDefinition> RewardSelected;

        private void Awake()
        {
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(() => SelectReward(0));
            }

            if (maxHpButton != null)
            {
                maxHpButton.onClick.AddListener(() => SelectReward(1));
            }

            if (critButton != null)
            {
                critButton.onClick.AddListener(() => SelectReward(2));
            }

            Hide();
        }

        public void Show(PlayerStats playerStats)
        {
            Show(playerStats, null, null);
        }

        public void Show(PlayerStats playerStats, RewardDefinition[] rewards, CombatUpgradeStats upgradeStats)
        {
            activePlayerStats = playerStats != null ? playerStats : FindObjectOfType<PlayerStats>();
            activeUpgradeStats = upgradeStats != null ? upgradeStats : FindObjectOfType<CombatUpgradeStats>();
            activeRewards = rewards != null && rewards.Length > 0 ? rewards : rewardPool;
            hasSelected = false;
            SetButtonsInteractable(true);

            if (titleText != null)
            {
                titleText.text = "真相已揭开";
            }

            if (bodyText != null)
            {
                bodyText.text = "304号病房已解封\n选择一项奖励";
            }

            if (attackButtonText != null)
            {
                attackButtonText.text = GetRewardLabel(0);
            }

            if (maxHpButtonText != null)
            {
                maxHpButtonText.text = GetRewardLabel(1);
            }

            if (critButtonText != null)
            {
                critButtonText.text = GetRewardLabel(2);
            }

            SetButtonVisible(attackButton, HasReward(0));
            SetButtonVisible(maxHpButton, HasReward(1));
            SetButtonVisible(critButton, HasReward(2));

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

        private void SelectReward(int index)
        {
            if (hasSelected || activePlayerStats == null)
            {
                return;
            }

            RewardDefinition rewardDefinition = GetReward(index);
            if (rewardDefinition == null)
            {
                return;
            }

            hasSelected = true;
            rewardDefinition.Apply(activePlayerStats, activeUpgradeStats);
            SetButtonsInteractable(false);
            Hide();
            RewardSelected?.Invoke(rewardDefinition);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (attackButton != null)
            {
                attackButton.interactable = interactable;
            }

            if (maxHpButton != null)
            {
                maxHpButton.interactable = interactable;
            }

            if (critButton != null)
            {
                critButton.interactable = interactable;
            }
        }

        private bool HasReward(int index)
        {
            return GetReward(index) != null;
        }

        private RewardDefinition GetReward(int index)
        {
            if (activeRewards == null || index < 0 || index >= activeRewards.Length)
            {
                return null;
            }

            return activeRewards[index];
        }

        private string GetRewardLabel(int index)
        {
            RewardDefinition rewardDefinition = GetReward(index);
            if (rewardDefinition == null)
            {
                return "未配置奖励";
            }

            return string.IsNullOrWhiteSpace(rewardDefinition.Description)
                ? rewardDefinition.DisplayName
                : rewardDefinition.DisplayName + "\n" + rewardDefinition.Description;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
