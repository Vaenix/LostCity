using System;
using LostCity.CombatSandbox;
using UnityEngine;

namespace LostCity.Meta
{
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private CaseDefinition[] cases = new CaseDefinition[0];
        [SerializeField] private ChapterDefinition[] chapters = new ChapterDefinition[0];
        [SerializeField] private ClueDefinition[] clues = new ClueDefinition[0];
        [SerializeField] private EnemyDefinition[] enemies = new EnemyDefinition[0];
        [SerializeField] private BossDefinition[] bosses = new BossDefinition[0];

        public CaseDefinition[] Cases => Copy(cases);
        public ChapterDefinition[] Chapters => Copy(chapters);
        public ClueDefinition[] Clues => Copy(clues);
        public EnemyDefinition[] Enemies => Copy(enemies);
        public BossDefinition[] Bosses => Copy(bosses);

        private static T[] Copy<T>(T[] source) => source == null ? Array.Empty<T>() : (T[])source.Clone();
    }
}
