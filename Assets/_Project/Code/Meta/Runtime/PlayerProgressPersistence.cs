using System.Collections.Generic;
using LostCity.CombatSandbox;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using UnityEngine;

namespace LostCity.Meta
{
    [RequireComponent(typeof(PlayerStats), typeof(PlayerExperience), typeof(CombatUpgradeStats))]
    public sealed class PlayerProgressPersistence : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerExperience playerExperience;
        [SerializeField] private CombatUpgradeStats combatUpgradeStats;
        private SaveGameManager save;
        private bool subscribed;
        private int mutationDepth;
        private readonly List<string> deferredUnlocks = new List<string>();
        public bool IsInitialized { get; private set; }
        public bool CanPersist => save != null && !save.IsReadOnly;
        public SaveGameManager SaveGame => save;

        private void Start() => Initialize(GameRuntime.Instance == null ? null : GameRuntime.Instance.SaveGame);
        private void OnEnable() { if (IsInitialized) Subscribe(); }
        private void OnDisable() => Unsubscribe();

        public void Initialize(SaveGameManager saveGame)
        {
            if (IsInitialized || saveGame == null) return;
            save = saveGame;
            playerStats = playerStats != null ? playerStats : GetComponent<PlayerStats>();
            playerExperience = playerExperience != null ? playerExperience : GetComponent<PlayerExperience>();
            combatUpgradeStats = combatUpgradeStats != null ? combatUpgradeStats : GetComponent<CombatUpgradeStats>();
            var progress = save.GetPlayerProgress();
            // Start runs after every Awake: Damageable can no longer reset restored maximum HP.
            playerStats.ApplyPermanentProgress(progress);
            playerExperience.ApplySavedProgress(progress);
            combatUpgradeStats.SetExtraDroneProjectiles(progress.DroneProjectileBonus);
            IsInitialized = true;
            if (isActiveAndEnabled) Subscribe();
            playerExperience.ResumePendingChoices();
        }

        public bool Capture()
        {
            if (!IsInitialized || !CanPersist || mutationDepth > 0) return false;
            var progress = new PlayerProgressData();
            playerExperience.WriteProgressTo(progress);
            progress.AttackMultiplier = playerStats.AttackMultiplier;
            progress.MaxHpMultiplier = playerStats.MaxHpMultiplier;
            progress.FireRateMultiplier = playerStats.FireRateMultiplier;
            progress.CritChanceBonus = playerStats.CritChanceBonus;
            progress.DodgeChanceBonus = playerStats.DodgeChanceBonus;
            combatUpgradeStats.WriteProgressTo(progress);
            save.SetPlayerProgressAndUnlocks(progress, deferredUnlocks);
            bool durable = !save.IsReadOnly && !save.HasUnsavedChanges;
            if (durable) deferredUnlocks.Clear();
            return durable;
        }

        public bool RecordReward(RewardDefinition reward)
        {
            if (reward != null && reward.RewardType == RewardType.Unlock)
                SaveDataSanitizer.AddUnique(deferredUnlocks, reward.UnlockId?.Trim());
            return mutationDepth > 0 ? CanPersist : Capture();
        }

        public void BeginProgressMutation() => mutationDepth++;

        public void EndProgressMutation()
        {
            if (mutationDepth <= 0) return;
            mutationDepth--;
            if (mutationDepth == 0) Capture();
        }

        private void HandleProgressChanged() { if (mutationDepth == 0) Capture(); }

        private void Subscribe()
        {
            if (subscribed) return;
            playerStats.PermanentProgressChanged += HandleProgressChanged;
            playerExperience.ProgressChanged += HandleProgressChanged;
            combatUpgradeStats.PermanentProgressChanged += HandleProgressChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (playerStats != null) playerStats.PermanentProgressChanged -= HandleProgressChanged;
            if (playerExperience != null) playerExperience.ProgressChanged -= HandleProgressChanged;
            if (combatUpgradeStats != null) combatUpgradeStats.PermanentProgressChanged -= HandleProgressChanged;
            subscribed = false;
        }
    }
}
