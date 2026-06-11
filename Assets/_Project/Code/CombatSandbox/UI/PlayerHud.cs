using UnityEngine;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerExperience playerExperience;
        [SerializeField] private Text statsText;

        private void Awake()
        {
            ResolveReferences();
            Refresh();
        }

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged += Refresh;
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged -= Refresh;
            }
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }

            if (playerExperience == null)
            {
                playerExperience = playerStats != null ? playerStats.GetComponent<PlayerExperience>() : FindObjectOfType<PlayerExperience>();
            }
        }

        private void Refresh()
        {
            if (statsText == null)
            {
                return;
            }

            ResolveReferences();
            if (playerStats == null || playerExperience == null)
            {
                statsText.text = "LV -\nHP -\nATK -\nDEF -\nCRIT -\nDODGE -";
                return;
            }

            statsText.text =
                $"LV {playerExperience.CurrentLevel}\n" +
                $"HP {Mathf.CeilToInt(playerStats.CurrentHp)}/{Mathf.CeilToInt(playerStats.MaxHp)}\n" +
                $"ATK x{playerStats.Attack:0.00}\n" +
                $"DEF {playerStats.Defense:0}\n" +
                $"CRIT {playerStats.CritChance * 100f:0}% x{playerStats.CritDamage:0.0}\n" +
                $"DODGE {playerStats.DodgeChance * 100f:0}%";
        }
    }
}
