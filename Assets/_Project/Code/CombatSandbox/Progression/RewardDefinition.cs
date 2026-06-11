using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Reward Definition")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private RewardType rewardType = RewardType.Attribute;
        [SerializeField] private RewardStatType statType = RewardStatType.None;
        [SerializeField] private float multiplier = 1f;
        [SerializeField] private float additiveValue;
        [SerializeField] private int integerValue;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private string unlockId;

        public string RewardId => rewardId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public RewardType RewardType => rewardType;
        public RewardStatType StatType => statType;
        public float Multiplier => multiplier;
        public float AdditiveValue => additiveValue;
        public int IntegerValue => integerValue;
        public WeaponDefinition WeaponDefinition => weaponDefinition;
        public string UnlockId => unlockId;

        public void Apply(PlayerStats playerStats, CombatUpgradeStats upgradeStats)
        {
            switch (rewardType)
            {
                case RewardType.Attribute:
                case RewardType.Passive:
                    ApplyStatReward(playerStats, upgradeStats);
                    break;
                case RewardType.Weapon:
                    ApplyWeaponReward(upgradeStats);
                    break;
                case RewardType.Unlock:
                    Debug.Log($"解锁奖励：{DisplayName} ({unlockId})");
                    break;
            }
        }

        private void ApplyStatReward(PlayerStats playerStats, CombatUpgradeStats upgradeStats)
        {
            switch (statType)
            {
                case RewardStatType.AttackMultiplier:
                    playerStats?.MultiplyAttack(SafeMultiplier());
                    break;
                case RewardStatType.MaxHpMultiplier:
                    playerStats?.MultiplyMaxHp(SafeMultiplier());
                    break;
                case RewardStatType.FireRateMultiplier:
                    playerStats?.MultiplyFireRate(SafeMultiplier());
                    break;
                case RewardStatType.CritChanceAdd:
                    playerStats?.AddCritChance(additiveValue);
                    break;
                case RewardStatType.DodgeChanceAdd:
                    playerStats?.AddDodgeChance(additiveValue);
                    break;
                case RewardStatType.DroneProjectileAdd:
                    upgradeStats?.AddDroneProjectiles(Mathf.Max(1, integerValue));
                    break;
            }
        }

        private void ApplyWeaponReward(CombatUpgradeStats upgradeStats)
        {
            if (statType == RewardStatType.DroneProjectileAdd)
            {
                upgradeStats?.AddDroneProjectiles(Mathf.Max(1, integerValue));
            }
        }

        private float SafeMultiplier()
        {
            return Mathf.Max(0.01f, multiplier);
        }
    }
}
