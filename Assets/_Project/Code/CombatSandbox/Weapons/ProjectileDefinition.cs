using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Projectile Definition")]
    public sealed class ProjectileDefinition : ScriptableObject
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifetimeSeconds = 2f;

        public float Damage => damage;
        public float Speed => speed;
        public float LifetimeSeconds => lifetimeSeconds;
    }
}
