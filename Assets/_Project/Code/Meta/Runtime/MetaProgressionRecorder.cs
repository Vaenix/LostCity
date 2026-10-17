using LostCity.CombatSandbox;

namespace LostCity.Meta
{
    public static class MetaProgressionRecorder
    {
        public static void RecordClue(ClueDefinition clue)
        {
            if (clue != null && !string.IsNullOrWhiteSpace(clue.Id)) GameRuntime.Instance?.SaveGame?.DiscoverClue(clue.Id);
        }

        public static void RecordEnemy(EnemyDefinition enemy)
        {
            if (enemy != null && !string.IsNullOrWhiteSpace(enemy.EnemyId)) GameRuntime.Instance?.SaveGame?.EncounterEnemy(enemy.EnemyId);
        }

        public static void RecordBoss(BossDefinition boss)
        {
            if (boss != null && !string.IsNullOrWhiteSpace(boss.BossId)) GameRuntime.Instance?.SaveGame?.EncounterBoss(boss.BossId);
        }
    }
}
