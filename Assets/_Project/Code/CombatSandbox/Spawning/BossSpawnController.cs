using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class BossSpawnController : MonoBehaviour
    {
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnDelaySeconds = 8f;
        [SerializeField] private bool spawnOnStart = true;

        private GameObject activeBoss;
        private float spawnTime;
        private bool hasSpawned;

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

        public void SpawnBoss()
        {
            if (bossPrefab == null)
            {
                Debug.LogWarning($"{name} needs a boss prefab.", this);
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            activeBoss = Instantiate(bossPrefab, position, Quaternion.identity);
            hasSpawned = true;

            WardenBoss wardenBoss = activeBoss.GetComponent<WardenBoss>();
            if (wardenBoss != null && player != null)
            {
                wardenBoss.SetTarget(player);
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
