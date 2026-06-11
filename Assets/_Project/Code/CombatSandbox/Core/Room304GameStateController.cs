using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class Room304GameStateController : MonoBehaviour
    {
        private enum GameState
        {
            Investigation,
            Combat,
            Resolution
        }

        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private DeductionBoard deductionBoard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private BossSpawnController bossSpawnController;
        [SerializeField] private MemoryFragmentPanel memoryFragmentPanel;

        private GameState currentState = GameState.Investigation;
        private Damageable trackedBossDamageable;

        private void Awake()
        {
            ResolveReferences();
            EnterInvestigationState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (investigationProgress != null)
            {
                investigationProgress.CaseSolved += EnterCombatState;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.BossSpawned += HandleBossSpawned;
            }
        }

        private void OnDisable()
        {
            if (investigationProgress != null)
            {
                investigationProgress.CaseSolved -= EnterCombatState;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.BossSpawned -= HandleBossSpawned;
            }

            StopTrackingBoss();
        }

        private void EnterInvestigationState()
        {
            currentState = GameState.Investigation;

            if (enemySpawner != null)
            {
                enemySpawner.enabled = false;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.enabled = false;
            }

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(true);
            }

            if (memoryFragmentPanel != null)
            {
                memoryFragmentPanel.Hide();
            }
        }

        private void EnterCombatState()
        {
            if (currentState != GameState.Investigation)
            {
                return;
            }

            currentState = GameState.Combat;

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(false);
            }

            if (enemySpawner != null)
            {
                enemySpawner.enabled = true;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.enabled = true;
                TrackBoss(bossSpawnController.SpawnBoss());
            }
        }

        private void EnterResolutionState()
        {
            if (currentState == GameState.Resolution)
            {
                return;
            }

            currentState = GameState.Resolution;

            if (enemySpawner != null)
            {
                enemySpawner.enabled = false;
            }

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(false);
            }

            if (memoryFragmentPanel != null)
            {
                memoryFragmentPanel.Show();
            }
        }

        private void HandleBossSpawned(GameObject boss)
        {
            TrackBoss(boss);
        }

        private void TrackBoss(GameObject boss)
        {
            if (boss == null)
            {
                return;
            }

            StopTrackingBoss();
            trackedBossDamageable = boss.GetComponent<Damageable>();
            if (trackedBossDamageable != null)
            {
                trackedBossDamageable.Died += HandleBossDied;
            }
        }

        private void StopTrackingBoss()
        {
            if (trackedBossDamageable != null)
            {
                trackedBossDamageable.Died -= HandleBossDied;
                trackedBossDamageable = null;
            }
        }

        private void HandleBossDied(Damageable source, DamageInfo damageInfo)
        {
            StopTrackingBoss();
            EnterResolutionState();
        }

        private void ResolveReferences()
        {
            if (investigationProgress == null)
            {
                investigationProgress = FindObjectOfType<InvestigationProgress>();
            }

            if (deductionBoard == null)
            {
                deductionBoard = FindObjectOfType<DeductionBoard>(includeInactive: true);
            }

            if (enemySpawner == null)
            {
                enemySpawner = FindObjectOfType<EnemySpawner>(includeInactive: true);
            }

            if (bossSpawnController == null)
            {
                bossSpawnController = FindObjectOfType<BossSpawnController>(includeInactive: true);
            }

            if (memoryFragmentPanel == null)
            {
                memoryFragmentPanel = FindObjectOfType<MemoryFragmentPanel>(includeInactive: true);
            }
        }
    }
}
