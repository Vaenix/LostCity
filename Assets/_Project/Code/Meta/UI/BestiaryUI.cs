using System;
using System.Collections.Generic;
using System.Text;
using LostCity.CombatSandbox;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Meta
{
    public sealed class BestiaryUI : MonoBehaviour
    {
        [SerializeField] private Text documentText;
        private HubViewSupport view;

        private void OnEnable()
        {
            view = new HubViewSupport(Refresh);
            view.Tick();
        }
        private void Update() { view?.Tick(); }
        private void OnDisable() { view?.Dispose(); view = null; }

        public void Refresh()
        {
            if (documentText == null) return;
            if (view == null || !view.Ready) { documentText.text = "怪物图鉴\n\n图鉴服务尚未就绪。"; return; }
            var enemies = new HashSet<string>(view.Save.EncounteredEnemyIds, StringComparer.Ordinal);
            var bosses = new HashSet<string>(view.Save.EncounteredBossIds, StringComparer.Ordinal);
            var text = new StringBuilder("怪物图鉴\n\n敌人\n");
            int enemyCount = 0;
            foreach (EnemyDefinition enemy in view.Registry.Enemies)
            {
                if (enemy == null) continue;
                enemyCount++;
                AppendEntry(text, enemies.Contains(enemy.EnemyId), enemy.DisplayName, enemy.BestiaryDescription);
            }
            if (enemyCount == 0) text.AppendLine("暂无敌人记录。");
            text.AppendLine("\n首领");
            int bossCount = 0;
            foreach (BossDefinition boss in view.Registry.Bosses)
            {
                if (boss == null) continue;
                bossCount++;
                AppendEntry(text, bosses.Contains(boss.BossId), boss.Name, boss.BestiaryDescription);
            }
            if (bossCount == 0) text.AppendLine("暂无首领记录。");
            documentText.text = text.ToString();
        }

        private static void AppendEntry(StringBuilder text, bool discovered, string name, string description)
        {
            if (!discovered) { text.AppendLine("\n· 未知记录"); return; }
            text.Append("\n· ").AppendLine(name);
            text.Append("  ").AppendLine(string.IsNullOrWhiteSpace(description) ? "暂无图鉴描述。" : description);
        }
    }
}
