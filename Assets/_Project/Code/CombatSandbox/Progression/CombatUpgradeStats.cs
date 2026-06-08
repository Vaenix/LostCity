using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class CombatUpgradeStats : MonoBehaviour
    {
        [SerializeField] private float fireRateMultiplier = 1f;
        [SerializeField] private float projectileDamageMultiplier = 1f;
        [SerializeField] private int extraDroneProjectiles;

        public float FireRateMultiplier => Mathf.Max(0.01f, fireRateMultiplier);
        public float ProjectileDamageMultiplier => Mathf.Max(0.01f, projectileDamageMultiplier);
        public int ExtraDroneProjectiles => Mathf.Max(0, extraDroneProjectiles);

        public void ApplyUpgrade(CombatUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case CombatUpgradeType.FireRate:
                    fireRateMultiplier *= 1.2f;
                    break;
                case CombatUpgradeType.ProjectileDamage:
                    projectileDamageMultiplier *= 1.2f;
                    break;
                case CombatUpgradeType.DroneProjectile:
                    extraDroneProjectiles += 1;
                    break;
            }
        }
    }
}
