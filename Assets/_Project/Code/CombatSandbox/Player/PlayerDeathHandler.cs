using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Damageable))]
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private float restartDelaySeconds = 1.25f;
        [SerializeField] private MonoBehaviour[] disableOnDeath;

        private Damageable damageable;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            damageable = GetComponent<Damageable>();
            damageable.Died -= HandleDied;
            damageable.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (damageable != null) damageable.Died -= HandleDied;
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            GetComponent<PlayerExperience>()?.SetAcceptsExperience(false);
            Time.timeScale = 1f;
            for (int i = 0; disableOnDeath != null && i < disableOnDeath.Length; i++)
            {
                if (disableOnDeath[i] != null)
                {
                    disableOnDeath[i].enabled = false;
                }
            }

            if (CombatGameManager.Instance != null)
            {
                CombatGameManager.Instance.RestartCurrentScene(restartDelaySeconds);
            }
        }
    }
}
