using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Damageable))]
    public sealed class MemoryFragmentEnemy : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private Transform target;

        private Rigidbody2D body;
        private Damageable damageable;
        private float nextContactDamageTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            damageable = GetComponent<Damageable>();

            if (definition != null)
            {
                damageable.SetMaxHealth(definition.MaxHealth, true);
            }
        }

        private void OnEnable()
        {
            damageable.Died += HandleDied;
        }

        private void OnDisable()
        {
            damageable.Died -= HandleDied;
        }

        private void FixedUpdate()
        {
            if (!damageable.IsAlive)
            {
                return;
            }

            ResolveTarget();

            if (target == null)
            {
                return;
            }

            Vector2 toTarget = (Vector2)target.position - body.position;

            float stoppingDistance = definition != null ? definition.StoppingDistance : 0.75f;
            if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            Vector2 direction = toTarget.normalized;
            float moveSpeed = definition != null ? definition.MoveSpeed : 3.5f;
            body.MovePosition(body.position + direction * (moveSpeed * Time.fixedDeltaTime));
            body.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            PlayerMotor player = FindObjectOfType<PlayerMotor>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (!damageable.IsAlive || Time.time < nextContactDamageTime)
            {
                return;
            }

            TeamMember teamMember = other.GetComponentInParent<TeamMember>();
            if (teamMember == null || teamMember.Team != CombatTeam.Player)
            {
                return;
            }

            Damageable playerDamageable = other.GetComponentInParent<Damageable>();
            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            float contactDamage = definition != null ? definition.ContactDamage : 12f;
            float contactCooldown = definition != null ? definition.ContactCooldownSeconds : 0.75f;
            playerDamageable.ApplyDamage(new DamageInfo(contactDamage, CombatTeam.Enemy, gameObject, other.ClosestPoint(transform.position)));
            nextContactDamageTime = Time.time + contactCooldown;
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            Destroy(gameObject);
        }
    }
}
