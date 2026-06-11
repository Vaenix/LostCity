using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class CombatUpgradeStats : MonoBehaviour
    {
        [SerializeField] private float fireRateMultiplier = 1f;
        [SerializeField] private float projectileDamageMultiplier = 1f;
        [SerializeField] private int extraDroneProjectiles;

        private PlayerStats playerStats;

        public float FireRateMultiplier => playerStats != null ? playerStats.FireRate : Mathf.Max(0.01f, fireRateMultiplier);
        public float ProjectileDamageMultiplier => playerStats != null ? playerStats.Attack : Mathf.Max(0.01f, projectileDamageMultiplier);
        public int ExtraDroneProjectiles => Mathf.Max(0, extraDroneProjectiles);

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        public void ApplyUpgrade(CombatUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case CombatUpgradeType.FireRate:
                    fireRateMultiplier *= 1.2f;
                    playerStats?.MultiplyFireRate(1.2f);
                    break;
                case CombatUpgradeType.ProjectileDamage:
                    projectileDamageMultiplier *= 1.2f;
                    playerStats?.MultiplyAttack(1.2f);
                    break;
                case CombatUpgradeType.DroneProjectile:
                    extraDroneProjectiles += 1;
                    break;
            }
        }

        public void ApplyReward(RewardDefinition rewardDefinition)
        {
            if (rewardDefinition == null)
            {
                return;
            }

            rewardDefinition.Apply(playerStats, this);
        }

        public void AddDroneProjectiles(int amount)
        {
            extraDroneProjectiles += Mathf.Max(0, amount);
        }

        public float RollProjectileDamageMultiplier()
        {
            return playerStats != null ? playerStats.RollOutgoingDamageMultiplier() : ProjectileDamageMultiplier;
        }
    }
}
