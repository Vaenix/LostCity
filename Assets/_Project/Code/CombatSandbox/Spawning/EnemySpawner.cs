using System.Collections.Generic;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemySpawnEntry[] spawnEntries;
        [SerializeField] private Transform player;
        [SerializeField] private Transform arenaCenter;
        [SerializeField] private float spawnIntervalSeconds = 1.25f;
        [SerializeField] private int initialSpawnCount = 4;
        [SerializeField] private int maxAliveEnemies = 12;
        [SerializeField] private float spawnRadius = 18f;
        [SerializeField] private float minimumDistanceFromPlayer = 8f;

        private readonly List<GameObject> aliveEnemies = new List<GameObject>();
        private float nextSpawnTime;

        private void Start()
        {
            ResolveReferences();

            for (int i = 0; i < initialSpawnCount; i++)
            {
                TrySpawnEnemy();
            }

            nextSpawnTime = Time.time + spawnIntervalSeconds;
        }

        private void Update()
        {
            CleanupDestroyedEnemies();

            if (Time.time < nextSpawnTime || aliveEnemies.Count >= maxAliveEnemies)
            {
                return;
            }

            TrySpawnEnemy();
            nextSpawnTime = Time.time + spawnIntervalSeconds;
        }

        private void ResolveReferences()
        {
            if (arenaCenter == null)
            {
                arenaCenter = transform;
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

        private void TrySpawnEnemy()
        {
            GameObject prefab = SelectEnemyPrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"{name} needs at least one enemy prefab.", this);
                return;
            }

            Vector3 spawnPosition = FindSpawnPosition();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            aliveEnemies.Add(enemy);
        }

        private GameObject SelectEnemyPrefab()
        {
            if (spawnEntries == null || spawnEntries.Length == 0)
            {
                return enemyPrefab;
            }

            float totalWeight = 0f;
            for (int i = 0; i < spawnEntries.Length; i++)
            {
                if (spawnEntries[i] != null && spawnEntries[i].Prefab != null)
                {
                    totalWeight += spawnEntries[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return enemyPrefab;
            }

            float roll = Random.Range(0f, totalWeight);
            for (int i = 0; i < spawnEntries.Length; i++)
            {
                EnemySpawnEntry entry = spawnEntries[i];
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                roll -= entry.Weight;
                if (roll <= 0f)
                {
                    return entry.Prefab;
                }
            }

            return enemyPrefab;
        }

        private Vector3 FindSpawnPosition()
        {
            Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;

            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(minimumDistanceFromPlayer, spawnRadius);
                Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);

                if (player == null || Vector3.Distance(candidate, player.position) >= minimumDistanceFromPlayer)
                {
                    return candidate;
                }
            }

            Vector2 fallbackOffset = Random.insideUnitCircle.normalized * spawnRadius;
            return center + new Vector3(fallbackOffset.x, fallbackOffset.y, 0f);
        }

        private void CleanupDestroyedEnemies()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                }
            }
        }
    }
}
