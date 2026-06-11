using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private DeductionBoard deductionBoard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private BossSpawnController bossSpawnController;
        [SerializeField] private Room304RewardSelectionUI rewardSelectionUI;
        [SerializeField] private Room304CompletionUI completionUI;
        [SerializeField] private PlayerStats playerStats;

        private Damageable trackedBossDamageable;

        public event Action<GameFlowState> StateChanged;

        public GameFlowState CurrentState { get; private set; } = GameFlowState.Investigation;

        private void Awake()
        {
            ResolveReferences();
            EnterInvestigationState();
            Debug.Log("Room304 Started");
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (investigationProgress != null)
            {
                investigationProgress.AllRequiredCluesCollected += EnterDeductionState;
                investigationProgress.CaseSolved += EnterCombatState;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.BossSpawned += HandleBossSpawned;
            }

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.RewardSelected += HandleRewardSelected;
            }

            if (completionUI != null)
            {
                completionUI.ContinueRequested += HandleCompletionContinueRequested;
            }
        }

        private void OnDisable()
        {
            if (investigationProgress != null)
            {
                investigationProgress.AllRequiredCluesCollected -= EnterDeductionState;
                investigationProgress.CaseSolved -= EnterCombatState;
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.BossSpawned -= HandleBossSpawned;
            }

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.RewardSelected -= HandleRewardSelected;
            }

            if (completionUI != null)
            {
                completionUI.ContinueRequested -= HandleCompletionContinueRequested;
            }

            StopTrackingBoss();
        }

        public void UnlockAllClues()
        {
            if (investigationProgress == null)
            {
                return;
            }

            investigationProgress.CollectAllRequiredClues();
        }

        public void DebugSpawnBoss()
        {
            EnterCombatState();
        }

        public void DebugKillBoss()
        {
            if (trackedBossDamageable == null)
            {
                GameObject activeBoss = bossSpawnController != null ? bossSpawnController.ActiveBoss : null;
                if (activeBoss != null)
                {
                    TrackBoss(activeBoss);
                }
            }

            if (trackedBossDamageable != null && trackedBossDamageable.IsAlive)
            {
                trackedBossDamageable.ApplyDamage(new DamageInfo(99999f, CombatTeam.Player, gameObject, trackedBossDamageable.transform.position));
            }
        }

        private void EnterInvestigationState()
        {
            SetState(GameFlowState.Investigation);

            if (enemySpawner != null)
            {
                enemySpawner.StopAndClearEnemies();
            }

            if (bossSpawnController != null)
            {
                bossSpawnController.enabled = false;
            }

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(true);
            }

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.Hide();
            }

            if (completionUI != null)
            {
                completionUI.Hide();
            }
        }

        private void EnterDeductionState()
        {
            if (CurrentState != GameFlowState.Investigation)
            {
                return;
            }

            SetState(GameFlowState.Deduction);

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(true);
            }
        }

        private void EnterCombatState()
        {
            if (CurrentState == GameFlowState.Combat || CurrentState == GameFlowState.Reward || CurrentState == GameFlowState.ChapterComplete)
            {
                return;
            }

            Debug.Log("Room304 Deduction Success");
            SetState(GameFlowState.Combat);

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(false);
            }

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.Hide();
            }

            if (completionUI != null)
            {
                completionUI.Hide();
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

        private void EnterRewardState()
        {
            if (CurrentState == GameFlowState.Reward || CurrentState == GameFlowState.ChapterComplete)
            {
                return;
            }

            Debug.Log("Boss Defeated");
            SetState(GameFlowState.Reward);

            if (enemySpawner != null)
            {
                enemySpawner.StopAndClearEnemies();
            }

            RemoveRemainingEnemies();

            if (deductionBoard != null)
            {
                deductionBoard.SetAvailable(false);
            }

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.Show(playerStats);
            }
        }

        private void EnterChapterCompleteState()
        {
            SetState(GameFlowState.ChapterComplete);

            if (rewardSelectionUI != null)
            {
                rewardSelectionUI.Hide();
            }

            if (completionUI != null)
            {
                completionUI.ShowChapterComplete();
            }
        }

        private void HandleBossSpawned(GameObject boss)
        {
            Debug.Log("Boss Spawned");
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
            EnterRewardState();
        }

        private void HandleRewardSelected(Room304RewardType rewardType)
        {
            Debug.Log("Reward Selected");
            EnterChapterCompleteState();
        }

        private void HandleCompletionContinueRequested()
        {
            Debug.Log("Room304 Completed");

            if (completionUI != null)
            {
                completionUI.ShowNextChapterPlaceholder();
            }
        }

        private void RemoveRemainingEnemies()
        {
            TeamMember[] teamMembers = FindObjectsOfType<TeamMember>();
            for (int i = 0; i < teamMembers.Length; i++)
            {
                if (teamMembers[i] != null && teamMembers[i].Team == CombatTeam.Enemy)
                {
                    Destroy(teamMembers[i].gameObject);
                }
            }
        }

        private void SetState(GameFlowState nextState)
        {
            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
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

            if (rewardSelectionUI == null)
            {
                rewardSelectionUI = FindObjectOfType<Room304RewardSelectionUI>(includeInactive: true);
            }

            if (completionUI == null)
            {
                completionUI = FindObjectOfType<Room304CompletionUI>(includeInactive: true);
            }

            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }
        }
    }
}
