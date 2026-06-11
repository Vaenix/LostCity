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

        private PlayerStats activePlayerStats;
        private bool hasSelected;

        public event Action<Room304RewardType> RewardSelected;

        private void Awake()
        {
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(() => SelectReward(Room304RewardType.Attack));
            }

            if (maxHpButton != null)
            {
                maxHpButton.onClick.AddListener(() => SelectReward(Room304RewardType.MaxHp));
            }

            if (critButton != null)
            {
                critButton.onClick.AddListener(() => SelectReward(Room304RewardType.CritChance));
            }

            Hide();
        }

        public void Show(PlayerStats playerStats)
        {
            activePlayerStats = playerStats != null ? playerStats : FindObjectOfType<PlayerStats>();
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
                attackButtonText.text = "+10%攻击力";
            }

            if (maxHpButtonText != null)
            {
                maxHpButtonText.text = "+10%生命值";
            }

            if (critButtonText != null)
            {
                critButtonText.text = "+5%暴击率";
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

        private void SelectReward(Room304RewardType rewardType)
        {
            if (hasSelected || activePlayerStats == null)
            {
                return;
            }

            hasSelected = true;
            ApplyReward(rewardType);
            SetButtonsInteractable(false);
            Hide();
            RewardSelected?.Invoke(rewardType);
        }

        private void ApplyReward(Room304RewardType rewardType)
        {
            switch (rewardType)
            {
                case Room304RewardType.Attack:
                    activePlayerStats.MultiplyAttack(1.1f);
                    break;
                case Room304RewardType.MaxHp:
                    activePlayerStats.MultiplyMaxHp(1.1f);
                    break;
                case Room304RewardType.CritChance:
                    activePlayerStats.AddCritChance(0.05f);
                    break;
            }
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
    }
}
