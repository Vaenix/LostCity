using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Damageable))]
    public sealed class WardenBoss : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 3f;
        [SerializeField] private float contactDamage = 18f;
        [SerializeField] private float contactCooldownSeconds = 0.8f;
        [SerializeField] private float chargeTriggerRange = 10f;
        [SerializeField] private float chargeSpeed = 11f;
        [SerializeField] private float chargeDurationSeconds = 0.65f;
        [SerializeField] private float chargeCooldownSeconds = 4f;
        [SerializeField] private GameObject[] summonPrefabs;
        [SerializeField] private int summonCount = 3;
        [SerializeField] private float summonRadius = 2.2f;
        [SerializeField] private float summonCooldownSeconds = 7f;
        [SerializeField] private Projectile burstProjectilePrefab;
        [SerializeField] private ProjectileDefinition burstProjectileDefinition;
        [SerializeField] private int burstProjectileCount = 12;
        [SerializeField] private float burstCooldownSeconds = 5f;

        private Rigidbody2D body;
        private Damageable damageable;
        private float nextContactDamageTime;
        private float nextChargeTime;
        private float chargeEndTime;
        private float nextSummonTime;
        private float nextBurstTime;
        private Vector2 chargeDirection;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            damageable = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            damageable.Died += HandleDied;
            nextSummonTime = Time.time + summonCooldownSeconds;
            nextBurstTime = Time.time + burstCooldownSeconds;
        }

        private void Update()
        {
            if (!damageable.IsAlive)
            {
                return;
            }

            ResolveTarget();
            TrySummon();
            TryBurst();
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

            if (IsCharging())
            {
                body.MovePosition(body.position + chargeDirection * (chargeSpeed * Time.fixedDeltaTime));
                body.MoveRotation(Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg);
                return;
            }

            Vector2 toTarget = (Vector2)target.position - body.position;
            TryStartCharge(toTarget);

            if (IsCharging() || toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            Vector2 direction = toTarget.normalized;
            body.MovePosition(body.position + direction * (moveSpeed * Time.fixedDeltaTime));
            body.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void OnDisable()
        {
            damageable.Died -= HandleDied;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ApplyDefinition(BossDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            moveSpeed = definition.MoveSpeed;
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

        private bool IsCharging()
        {
            return Time.time < chargeEndTime;
        }

        private void TryStartCharge(Vector2 toTarget)
        {
            if (Time.time < nextChargeTime || toTarget.sqrMagnitude > chargeTriggerRange * chargeTriggerRange || toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            chargeDirection = toTarget.normalized;
            chargeEndTime = Time.time + chargeDurationSeconds;
            nextChargeTime = Time.time + chargeCooldownSeconds;
        }

        private void TrySummon()
        {
            if (Time.time < nextSummonTime || summonPrefabs == null || summonPrefabs.Length == 0)
            {
                return;
            }

            for (int i = 0; i < summonCount; i++)
            {
                GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
                if (prefab == null)
                {
                    continue;
                }

                Vector2 offset = Random.insideUnitCircle.normalized * summonRadius;
                Instantiate(prefab, transform.position + new Vector3(offset.x, offset.y, 0f), Quaternion.identity);
            }

            nextSummonTime = Time.time + summonCooldownSeconds;
        }

        private void TryBurst()
        {
            if (Time.time < nextBurstTime || burstProjectilePrefab == null || burstProjectileDefinition == null)
            {
                return;
            }

            int projectileCount = Mathf.Max(1, burstProjectileCount);
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = (360f / projectileCount) * i;
                Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                Projectile projectile = Instantiate(burstProjectilePrefab, transform.position, Quaternion.identity);
                projectile.Launch(burstProjectileDefinition, CombatTeam.Enemy, direction, gameObject);
            }

            nextBurstTime = Time.time + burstCooldownSeconds;
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

            playerDamageable.ApplyDamage(new DamageInfo(contactDamage, CombatTeam.Enemy, gameObject, other.ClosestPoint(transform.position)));
            nextContactDamageTime = Time.time + contactCooldownSeconds;
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            Destroy(gameObject);
        }
    }
}
