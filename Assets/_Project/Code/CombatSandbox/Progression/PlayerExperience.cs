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
            if (amount <= 0)
            {
                return;
            }

            float xpMultiplier = playerStats != null ? playerStats.XpMultiplier : 1f;
            currentExperience += Mathf.Max(1, Mathf.RoundToInt(amount * xpMultiplier));

            while (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                currentLevel += 1;
                experienceToNextLevel = Mathf.Max(experienceToNextLevel + 1, Mathf.CeilToInt(experienceToNextLevel * nextLevelExperienceMultiplier));
                pendingUpgradeChoices += 1;
            }

            TryShowNextUpgradeChoice();
        }

        private void TryShowNextUpgradeChoice()
        {
            if (pendingUpgradeChoices <= 0 || upgradeStats == null || upgradeSelectionController == null || upgradeSelectionController.IsShowing)
            {
                return;
            }

            upgradeSelectionController.Show(upgradeStats, currentLevel, HandleUpgradeChoiceCompleted);
        }

        private void HandleUpgradeChoiceCompleted()
        {
            pendingUpgradeChoices = Mathf.Max(0, pendingUpgradeChoices - 1);
            TryShowNextUpgradeChoice();
        }
    }
}
