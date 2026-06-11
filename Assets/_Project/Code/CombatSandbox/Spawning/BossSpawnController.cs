using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class BossSpawnController : MonoBehaviour
    {
        [SerializeField] private BossDefinition bossDefinition;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnDelaySeconds = 8f;
        [SerializeField] private bool spawnOnStart = true;

        private GameObject activeBoss;
        private float spawnTime;
        private bool hasSpawned;

        public event Action<GameObject> BossSpawned;

        public GameObject ActiveBoss => activeBoss;

        public void SetBossDefinition(BossDefinition definition)
        {
            if (definition != null)
            {
                bossDefinition = definition;
            }
        }

        private void Start()
        {
            ResolveReferences();
            spawnTime = Time.time + Mathf.Max(0f, spawnDelaySeconds);
        }

        private void Update()
        {
            if (!spawnOnStart || hasSpawned || activeBoss != null || Time.time < spawnTime)
            {
                return;
            }

            SpawnBoss();
        }

        public GameObject SpawnBoss()
        {
            if (activeBoss != null)
            {
                return activeBoss;
            }

            GameObject prefab = bossDefinition != null && bossDefinition.BossPrefab != null
                ? bossDefinition.BossPrefab
                : bossPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"{name} needs a boss prefab.", this);
                return null;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            activeBoss = Instantiate(prefab, position, Quaternion.identity);
            hasSpawned = true;

            ApplyBossDefinition(activeBoss);

            WardenBoss wardenBoss = activeBoss.GetComponent<WardenBoss>();
            if (wardenBoss != null && player != null)
            {
                wardenBoss.SetTarget(player);
            }

            BossSpawned?.Invoke(activeBoss);
            return activeBoss;
        }

        private void ApplyBossDefinition(GameObject boss)
        {
            if (bossDefinition == null || boss == null)
            {
                return;
            }

            boss.name = bossDefinition.Name;

            Damageable damageable = boss.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.SetMaxHealth(bossDefinition.Health, resetCurrentHealth: true);
            }

            WardenBoss wardenBoss = boss.GetComponent<WardenBoss>();
            if (wardenBoss != null)
            {
                wardenBoss.ApplyDefinition(bossDefinition);
            }

            XpDropper xpDropper = boss.GetComponent<XpDropper>();
            if (xpDropper != null)
            {
                xpDropper.SetExperienceValue(bossDefinition.XpReward);
            }
        }

        private void ResolveReferences()
        {
            if (spawnPoint == null)
            {
                spawnPoint = transform;
            }

            if (player == null)
            {
                PlayerMotor playerMotor = FindObjectOfType<PlayerMotor>();
                if (playerMotor != null)
                {
                    player = playerMotor.transform;
                }
            }
        }

    }
}
