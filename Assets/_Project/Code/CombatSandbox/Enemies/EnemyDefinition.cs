using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Memory Fragment";
        [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Chaser;
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float stoppingDistance = 0.75f;
        [SerializeField] private float contactDamage = 12f;
        [SerializeField] private float contactCooldownSeconds = 0.75f;
        [SerializeField] private float chargeTriggerRange = 6f;
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeDurationSeconds = 0.45f;
        [SerializeField] private float chargeCooldownSeconds = 2.4f;
        [SerializeField] private float shootRange = 9f;
        [SerializeField] private float shootCooldownSeconds = 1.6f;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private ProjectileDefinition projectileDefinition;

        public string DisplayName => displayName;
        public EnemyArchetype Archetype => archetype;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StoppingDistance => stoppingDistance;
        public float ContactDamage => contactDamage;
        public float ContactCooldownSeconds => contactCooldownSeconds;
        public float ChargeTriggerRange => chargeTriggerRange;
        public float ChargeSpeed => chargeSpeed;
        public float ChargeDurationSeconds => chargeDurationSeconds;
        public float ChargeCooldownSeconds => chargeCooldownSeconds;
        public float ShootRange => shootRange;
        public float ShootCooldownSeconds => shootCooldownSeconds;
        public Projectile ProjectilePrefab => projectilePrefab;
        public ProjectileDefinition ProjectileDefinition => projectileDefinition;
    }
}
