using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Damageable))]
    public sealed class XpDropper : MonoBehaviour
    {
        [SerializeField] private XpOrb xpOrbPrefab;
        [SerializeField] private int experienceValue = 1;

        private Damageable damageable;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            damageable.Died += HandleDied;
        }

        private void OnDisable()
        {
            damageable.Died -= HandleDied;
        }

        public void SetExperienceValue(int value)
        {
            experienceValue = Mathf.Max(0, value);
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            if (xpOrbPrefab == null)
            {
                return;
            }

            XpOrb xpOrb = Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            xpOrb.Initialize(experienceValue);
        }
    }
}
