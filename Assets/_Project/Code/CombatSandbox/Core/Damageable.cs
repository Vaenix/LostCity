using System;
using UnityEngine;
using UnityEngine.Events;

namespace LostCity.CombatSandbox
{
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private UnityEvent<float, float> onHealthChanged;
        [SerializeField] private UnityEvent onDied;

        private float currentHealth;
        private bool isDead;

        public event Action<Damageable, DamageInfo> Damaged;
        public event Action<Damageable, DamageInfo> Died;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => !isDead;

        private void Awake()
        {
            ResetHealth();
        }

        public void SetMaxHealth(float value, bool resetCurrentHealth)
        {
            maxHealth = Mathf.Max(1f, value);

            if (resetCurrentHealth)
            {
                ResetHealth();
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                onHealthChanged.Invoke(currentHealth, maxHealth);
            }
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = Mathf.Max(1f, maxHealth);
            onHealthChanged.Invoke(currentHealth, maxHealth);
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (isDead || damageInfo.Amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount);
            Damaged?.Invoke(this, damageInfo);
            onHealthChanged.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die(damageInfo);
            }
        }

        private void Die(DamageInfo damageInfo)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Died?.Invoke(this, damageInfo);
            onDied.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }
}
