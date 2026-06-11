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
        private CombatUpgradeStats upgradeStats;

        private void Awake()
        {
            owner = transform.parent;

            if (muzzle == null)
            {
                muzzle = transform;
            }

            upgradeStats = GetComponentInParent<CombatUpgradeStats>();
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

            FireVolley(direction);

            float fireRateMultiplier = upgradeStats != null ? upgradeStats.FireRateMultiplier : 1f;
            nextAllowedFireTime = Time.time + weaponDefinition.CooldownSeconds / fireRateMultiplier;
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

        private void FireVolley(Vector3 baseDirection)
        {
            int projectileCount = 1 + (upgradeStats != null ? upgradeStats.ExtraDroneProjectiles : 0);

            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 shotDirection = GetVolleyDirection(baseDirection, i, projectileCount);
                Projectile projectile = Instantiate(weaponDefinition.ProjectilePrefab, muzzle.position, Quaternion.identity);
                float damageMultiplier = upgradeStats != null ? upgradeStats.RollProjectileDamageMultiplier() : 1f;
                projectile.Launch(weaponDefinition.ProjectileDefinition, sourceTeam, shotDirection, gameObject, damageMultiplier);
            }
        }

        private static Vector3 GetVolleyDirection(Vector3 baseDirection, int index, int projectileCount)
        {
            if (projectileCount <= 1)
            {
                return baseDirection;
            }

            float totalSpreadDegrees = Mathf.Min(60f, 12f * (projectileCount - 1));
            float step = totalSpreadDegrees / (projectileCount - 1);
            float angle = -totalSpreadDegrees * 0.5f + step * index;
            return Quaternion.Euler(0f, 0f, angle) * baseDirection;
        }
    }
}
