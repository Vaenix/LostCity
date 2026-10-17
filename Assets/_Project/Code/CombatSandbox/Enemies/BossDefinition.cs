using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    [CreateAssetMenu(menuName = "Lost City/Combat Sandbox/Boss Definition")]
    public sealed class BossDefinition : ScriptableObject
    {
        [SerializeField] private string bossId;
        [SerializeField, TextArea] private string bestiaryDescription;
        [SerializeField] private string bossName;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private float health = 300f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private BossSkillDefinition[] skills;
        [SerializeField] private int xpReward = 12;
        [SerializeField] private RewardDefinition[] rewardPool;

        public string Name => string.IsNullOrWhiteSpace(bossName) ? name : bossName;
        public string BossId => bossId;
        public string BestiaryDescription => bestiaryDescription;
        public GameObject BossPrefab => bossPrefab;
        public float Health => Mathf.Max(1f, health);
        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);
        public BossSkillDefinition[] Skills => skills == null ? Array.Empty<BossSkillDefinition>() : (BossSkillDefinition[])skills.Clone();
        public int XpReward => Mathf.Max(0, xpReward);
        public RewardDefinition[] RewardPool => rewardPool == null ? Array.Empty<RewardDefinition>() : (RewardDefinition[])rewardPool.Clone();
    }
}
