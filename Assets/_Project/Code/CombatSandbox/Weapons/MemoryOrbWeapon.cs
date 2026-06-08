using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class MemoryOrbWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Vector3 localFollowOffset = new Vector3(1.25f, 1.25f, 0f);
        [SerializeField] private float followSharpness = 14f;
        [SerializeField] private CombatTeam sourceTeam = CombatTeam.Player;

        private Transform owner;
        private float nextAllowedFireTime;

        private void Awake()
        {
            owner = transform.parent;

            if (muzzle == null)
            {
                muzzle = transform;
            }
        }

        private void LateUpdate()
        {
            FollowOwner();
        }

        private void Update()
        {
            if (weaponDefinition == null || weaponDefinition.ProjectilePrefab == null)
            {
                return;
            }

            if (Time.time < nextAllowedFireTime)
            {
                return;
            }

            Damageable target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            Vector3 direction = target.transform.position - muzzle.position;
            direction.z = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Projectile projectile = Instantiate(weaponDefinition.ProjectilePrefab, muzzle.position, Quaternion.identity);
            projectile.Launch(weaponDefinition.ProjectileDefinition, sourceTeam, direction, gameObject);
            nextAllowedFireTime = Time.time + weaponDefinition.CooldownSeconds;
        }

        private void FollowOwner()
        {
            if (owner == null)
            {
                return;
            }

            Vector3 targetPosition = owner.TransformPoint(localFollowOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        }

        private Damageable FindNearestEnemy()
        {
            float range = weaponDefinition.Range;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            Damageable nearest = null;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                TeamMember teamMember = hits[i].GetComponentInParent<TeamMember>();
                if (teamMember == null || teamMember.Team != CombatTeam.Enemy)
                {
                    continue;
                }

                Damageable damageable = hits[i].GetComponentInParent<Damageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                float distanceSqr = (damageable.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearest = damageable;
                    nearestDistanceSqr = distanceSqr;
                }
            }

            return nearest;
        }
    }
}
