using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] private float cooldownSeconds = 0.25f;
        [SerializeField] private float range = 12f;

        public string DisplayName => displayName;
        public Projectile ProjectilePrefab => projectilePrefab;
        public ProjectileDefinition ProjectileDefinition => projectileDefinition;
        public float CooldownSeconds => cooldownSeconds;
        public float Range => range;
    }
}
