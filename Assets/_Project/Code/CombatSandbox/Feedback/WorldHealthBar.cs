using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Damageable))]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform barRoot;
        [SerializeField] private Transform fill;
        [SerializeField] private bool hideWhenFull;

        private Damageable damageable;
        private Vector3 fullFillScale = Vector3.one;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();

            if (fill != null)
            {
                fullFillScale = fill.localScale;
            }
        }

        private void OnEnable()
        {
            damageable.HealthChanged += HandleHealthChanged;
            damageable.Died += HandleDied;
        }

        private void Start()
        {
            UpdateFill();
        }

        private void OnDisable()
        {
            damageable.HealthChanged -= HandleHealthChanged;
            damageable.Died -= HandleDied;
        }

        private void HandleHealthChanged(Damageable source)
        {
            UpdateFill();
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(false);
            }
        }

        private void UpdateFill()
        {
            if (fill == null || damageable.MaxHealth <= 0f)
            {
                return;
            }

            float normalizedHealth = Mathf.Clamp01(damageable.CurrentHealth / damageable.MaxHealth);
            fill.localScale = new Vector3(fullFillScale.x * normalizedHealth, fullFillScale.y, fullFillScale.z);

            if (barRoot != null && hideWhenFull)
            {
                barRoot.gameObject.SetActive(normalizedHealth < 0.999f && normalizedHealth > 0f);
            }
        }
    }
}
