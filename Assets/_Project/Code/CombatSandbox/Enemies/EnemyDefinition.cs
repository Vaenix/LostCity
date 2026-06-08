using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float stoppingDistance = 0.75f;
        [SerializeField] private float contactDamage = 12f;
        [SerializeField] private float contactCooldownSeconds = 0.75f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StoppingDistance => stoppingDistance;
        public float ContactDamage => contactDamage;
        public float ContactCooldownSeconds => contactCooldownSeconds;
    }
}
