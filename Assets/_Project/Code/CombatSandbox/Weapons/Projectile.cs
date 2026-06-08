using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private ProjectileDefinition defaultDefinition;
        [SerializeField] private bool destroyOnNonDamageableHit = true;

        private Rigidbody2D body;
        private ProjectileDefinition activeDefinition;
        private CombatTeam sourceTeam;
        private GameObject owner;
        private bool hasLaunched;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void Launch(ProjectileDefinition definition, CombatTeam team, Vector3 direction, GameObject sourceOwner)
        {
            activeDefinition = definition != null ? definition : defaultDefinition;
            sourceTeam = team;
            owner = sourceOwner;
            hasLaunched = true;

            if (activeDefinition == null)
            {
                Debug.LogWarning($"{name} cannot launch without a ProjectileDefinition.", this);
                Destroy(gameObject);
                return;
            }

            Vector2 launchDirection = new Vector2(direction.x, direction.y);
            Vector2 normalizedDirection = launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : (Vector2)transform.right;
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            body.velocity = normalizedDirection * activeDefinition.Speed;
            Destroy(gameObject, activeDefinition.LifetimeSeconds);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider);
        }

        private void HandleHit(Collider2D other)
        {
            if (!hasLaunched || other == null)
            {
                return;
            }

            if (owner != null && other.transform.IsChildOf(owner.transform))
            {
                return;
            }

            TeamMember hitTeam = other.GetComponentInParent<TeamMember>();
            if (hitTeam != null && hitTeam.Team == sourceTeam)
            {
                return;
            }

            Damageable damageable = other.GetComponentInParent<Damageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.ApplyDamage(new DamageInfo(activeDefinition.Damage, sourceTeam, owner, other.ClosestPoint(transform.position)));
                Destroy(gameObject);
                return;
            }

            if (destroyOnNonDamageableHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
