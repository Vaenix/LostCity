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
            damageable.Died += HandleDied;
        }

        private void OnDisable()
        {
            damageable.Died -= HandleDied;
        }

        private void HandleDied(Damageable source, DamageInfo damageInfo)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
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
