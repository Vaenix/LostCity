using System;
using LostCity.Meta.Core;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class PlayerExperience : MonoBehaviour
    {
        [SerializeField] private CombatUpgradeStats upgradeStats;
        [SerializeField] private UpgradeSelectionController upgradeSelectionController;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExperience;
        [SerializeField] private int experienceToNextLevel = 5;
        [SerializeField] private float nextLevelExperienceMultiplier = 1.35f;

        private int pendingUpgradeChoices;
        private PlayerStats playerStats;

        public int CurrentLevel => currentLevel;
        public int CurrentExperience => currentExperience;
        public int ExperienceToNextLevel => experienceToNextLevel;
        public int PendingUpgradeChoices => pendingUpgradeChoices;
        public bool AcceptsExperience { get; private set; } = true;
        public event Action ProgressChanged;
        public event Action UpgradeChoicesChanged;

        private void Awake()
        {
            if (upgradeStats == null)
            {
                upgradeStats = GetComponent<CombatUpgradeStats>();
            }

            if (upgradeSelectionController == null)
            {
                upgradeSelectionController = FindObjectOfType<UpgradeSelectionController>(includeInactive: true);
            }

            playerStats = GetComponent<PlayerStats>();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || !AcceptsExperience)
            {
                return;
            }

            float xpMultiplier = playerStats != null ? playerStats.XpMultiplier : 1f;
            currentExperience = (int)Math.Min(int.MaxValue, (long)currentExperience
                + Mathf.Max(1, Mathf.RoundToInt(amount * xpMultiplier)));
            experienceToNextLevel = Mathf.Max(1, experienceToNextLevel);

            while (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                currentLevel = currentLevel == int.MaxValue ? int.MaxValue : currentLevel + 1;
                double growth = nextLevelExperienceMultiplier;
                if (double.IsNaN(growth) || double.IsInfinity(growth) || growth < 1d) growth = 1.35d;
                experienceToNextLevel = (int)Math.Min(int.MaxValue, Math.Max((long)experienceToNextLevel + 1,
                    Math.Ceiling(experienceToNextLevel * growth)));
                pendingUpgradeChoices += 1;
            }

            ProgressChanged?.Invoke();
            TryShowNextUpgradeChoice();
            UpgradeChoicesChanged?.Invoke();
        }

        public void SetAcceptsExperience(bool accepts) => AcceptsExperience = accepts;

        public void ApplySavedProgress(PlayerProgressData progress)
        {
            if (progress == null) return;
            currentLevel = Mathf.Max(1, progress.Level);
            currentExperience = Mathf.Max(0, progress.CurrentExperience);
            experienceToNextLevel = progress.ExperienceToNextLevel > 0 ? progress.ExperienceToNextLevel : 5;
            pendingUpgradeChoices = Mathf.Max(0, progress.PendingUpgradeChoices);
            ProgressChanged?.Invoke();
        }

        public void WriteProgressTo(PlayerProgressData progress)
        {
            progress.Level = currentLevel;
            progress.CurrentExperience = currentExperience;
            progress.ExperienceToNextLevel = experienceToNextLevel;
            progress.PendingUpgradeChoices = pendingUpgradeChoices;
        }

        public void ResumePendingChoices()
        {
            TryShowNextUpgradeChoice();
            UpgradeChoicesChanged?.Invoke();
        }

        private void TryShowNextUpgradeChoice()
        {
            if (pendingUpgradeChoices <= 0 || upgradeStats == null || upgradeSelectionController == null || upgradeSelectionController.IsShowing)
            {
                return;
            }

            upgradeSelectionController.Show(upgradeStats, currentLevel - pendingUpgradeChoices + 1, HandleUpgradeChoiceCompleted);
        }

        private void HandleUpgradeChoiceCompleted()
        {
            pendingUpgradeChoices = Mathf.Max(0, pendingUpgradeChoices - 1);
            ProgressChanged?.Invoke();
            TryShowNextUpgradeChoice();
            UpgradeChoicesChanged?.Invoke();
        }
    }
}
