using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Damageable))]
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp = 100f;
        [SerializeField] private float attack = 1f;
        [SerializeField] private float defense;
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float critChance;
        [SerializeField] private float critDamage = 1.5f;
        [SerializeField] private float dodgeChance;
        [SerializeField] private float xpMultiplier = 1f;
        [SerializeField] private float pickupRadius = 3.5f;

        private Damageable damageable;

        public event Action StatsChanged;

        public float MaxHp => Mathf.Max(1f, maxHp);
        public float CurrentHp => Mathf.Clamp(currentHp, 0f, MaxHp);
        public float Attack => Mathf.Max(0.01f, attack);
        public float Defense => Mathf.Max(0f, defense);
        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);
        public float FireRate => Mathf.Max(0.01f, fireRate);
        public float CritChance => Mathf.Clamp01(critChance);
        public float CritDamage => Mathf.Max(1f, critDamage);
        public float DodgeChance => Mathf.Clamp01(dodgeChance);
        public float XpMultiplier => Mathf.Max(0.01f, xpMultiplier);
        public float PickupRadius => Mathf.Max(0.1f, pickupRadius);

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
            maxHp = MaxHp;
            currentHp = Mathf.Clamp(currentHp <= 0f ? maxHp : currentHp, 0f, maxHp);
            damageable.SetMaxHealth(maxHp, resetCurrentHealth: true);
            currentHp = damageable.CurrentHealth;
        }

        private void OnEnable()
        {
            damageable.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            damageable.HealthChanged -= HandleHealthChanged;
        }

        public void MultiplyAttack(float multiplier)
        {
            attack = Mathf.Max(0.01f, attack * Mathf.Max(0.01f, multiplier));
            NotifyChanged();
        }

        public void MultiplyMaxHp(float multiplier)
        {
            maxHp = Mathf.Max(1f, maxHp * Mathf.Max(0.01f, multiplier));
            if (damageable != null)
            {
                damageable.SetMaxHealth(maxHp, resetCurrentHealth: true);
                currentHp = damageable.CurrentHealth;
            }

            NotifyChanged();
        }

        public void MultiplyFireRate(float multiplier)
        {
            fireRate = Mathf.Max(0.01f, fireRate * Mathf.Max(0.01f, multiplier));
            NotifyChanged();
        }

        public void AddCritChance(float amount)
        {
            critChance = Mathf.Clamp01(critChance + amount);
            NotifyChanged();
        }

        public void AddDodgeChance(float amount)
        {
            dodgeChance = Mathf.Clamp01(dodgeChance + amount);
            NotifyChanged();
        }

        public float RollOutgoingDamageMultiplier()
        {
            float multiplier = Attack;
            if (CritChance > 0f && UnityEngine.Random.value < CritChance)
            {
                multiplier *= CritDamage;
            }

            return multiplier;
        }

        public float ModifyIncomingDamage(DamageInfo damageInfo)
        {
            if (damageInfo.SourceTeam != CombatTeam.Enemy)
            {
                return damageInfo.Amount;
            }

            if (DodgeChance > 0f && UnityEngine.Random.value < DodgeChance)
            {
                return 0f;
            }

            return Mathf.Max(1f, damageInfo.Amount - Defense);
        }

        private void HandleHealthChanged(Damageable source)
        {
            currentHp = source.CurrentHealth;
            maxHp = source.MaxHealth;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            StatsChanged?.Invoke();
        }
    }
}
