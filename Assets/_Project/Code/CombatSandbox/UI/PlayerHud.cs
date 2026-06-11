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
                statsText.text = "等级 -\n生命 -\n攻击 -\n防御 -\n暴击 -\n闪避 -";
                return;
            }

            statsText.text =
                $"等级 {playerExperience.CurrentLevel}\n" +
                $"生命 {Mathf.CeilToInt(playerStats.CurrentHp)}/{Mathf.CeilToInt(playerStats.MaxHp)}\n" +
                $"攻击 x{playerStats.Attack:0.00}\n" +
                $"防御 {playerStats.Defense:0}\n" +
                $"暴击 {playerStats.CritChance * 100f:0}% x{playerStats.CritDamage:0.0}\n" +
                $"闪避 {playerStats.DodgeChance * 100f:0}%";
        }
    }
}
