using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class PistolWeapon : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerAim playerAim;
        [SerializeField] private Transform muzzle;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private CombatTeam sourceTeam = CombatTeam.Player;

        private float nextAllowedFireTime;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponentInParent<PlayerInputReader>();
            }

            if (playerAim == null)
            {
                playerAim = GetComponentInParent<PlayerAim>();
            }

            if (muzzle == null)
            {
                muzzle = transform;
            }
        }

        private void Update()
        {
            if (inputReader != null && inputReader.FirePressedThisFrame)
            {
                TryFire();
            }
        }

        public bool TryFire()
        {
            if (weaponDefinition == null || weaponDefinition.ProjectilePrefab == null)
            {
                Debug.LogWarning($"{name} needs a WeaponDefinition with a projectile prefab.", this);
                return false;
            }

            if (Time.time < nextAllowedFireTime)
            {
                return false;
            }

            Vector3 fireDirection = playerAim != null ? playerAim.AimDirection : muzzle.right;
            Projectile projectile = Instantiate(weaponDefinition.ProjectilePrefab, muzzle.position, Quaternion.identity);
            projectile.Launch(weaponDefinition.ProjectileDefinition, sourceTeam, fireDirection, gameObject);
            nextAllowedFireTime = Time.time + weaponDefinition.CooldownSeconds;
            return true;
        }
    }
}
