using System;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class UpgradeSelectionController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Button fireRateButton;
        [SerializeField] private Text fireRateButtonText;
        [SerializeField] private Button damageButton;
        [SerializeField] private Text damageButtonText;
        [SerializeField] private Button droneButton;
        [SerializeField] private Text droneButtonText;
        [SerializeField] private bool pauseWhileChoosing = true;

        private CombatUpgradeStats activeStats;
        private Action choiceCompleted;
        private float previousTimeScale = 1f;

        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (fireRateButton != null)
            {
                fireRateButton.onClick.AddListener(() => Choose(CombatUpgradeType.FireRate));
            }

            if (damageButton != null)
            {
                damageButton.onClick.AddListener(() => Choose(CombatUpgradeType.ProjectileDamage));
            }

            if (droneButton != null)
            {
                droneButton.onClick.AddListener(() => Choose(CombatUpgradeType.DroneProjectile));
            }

            Hide();
        }

        public void Show(CombatUpgradeStats stats, int level, Action onChoiceCompleted)
        {
            if (stats == null || panelRoot == null)
            {
                return;
            }

            activeStats = stats;
            choiceCompleted = onChoiceCompleted;

            if (titleText != null)
            {
                titleText.text = $"LEVEL {level}";
            }

            if (fireRateButtonText != null)
            {
                fireRateButtonText.text = "+20% Fire Rate";
            }

            if (damageButtonText != null)
            {
                damageButtonText.text = "+20% Projectile Damage";
            }

            if (droneButtonText != null)
            {
                droneButtonText.text = "+1 Drone Projectile";
            }

            if (pauseWhileChoosing)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            panelRoot.SetActive(true);
        }

        private void Choose(CombatUpgradeType upgradeType)
        {
            if (activeStats == null)
            {
                return;
            }

            activeStats.ApplyUpgrade(upgradeType);
            Hide();
            choiceCompleted?.Invoke();
            choiceCompleted = null;
            activeStats = null;
        }

        private void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (pauseWhileChoosing && Time.timeScale == 0f)
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
