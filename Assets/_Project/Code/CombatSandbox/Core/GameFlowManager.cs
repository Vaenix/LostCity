using System;
using LostCity.Meta;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private DeductionBoard deductionBoard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private BossSpawnController bossSpawnController;
        [SerializeField] private RewardSelectionUI rewardSelectionUI;
        [SerializeField] private CaseCompletionUI completionUI;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private CombatUpgradeStats combatUpgradeStats;
        [SerializeField] private PlayerExperience playerExperience;
        [SerializeField] private PlayerProgressPersistence playerProgressPersistence;
        [SerializeField] private UpgradeSelectionController upgradeSelectionController;
        private Damageable trackedBossDamageable;
        private RewardDefinition selectedReward;
        private bool rewardApplied;
        private bool completionPersisted;
        private bool settlingBossDeath;
        private bool started;

        public event Action<GameFlowState> StateChanged;
        public GameFlowState CurrentState { get; private set; } = GameFlowState.Investigation;
        public bool CompletionPersisted => completionPersisted;
        private CaseDefinition ActiveCase => investigationProgress != null ? investigationProgress.CurrentCase : null;

        private void Awake() => ResolveReferences();

        private void Start()
        {
            ResolveReferences();
            playerProgressPersistence?.Initialize(GameRuntime.Instance == null ? null : GameRuntime.Instance.SaveGame);
            started = true;
            EnterInvestigationState();
            if (ActiveCase != null) Debug.Log($"Case Started: {ActiveCase.CaseId}");
        }

        private void OnEnable()
        {
            ResolveReferences();
            Unsubscribe();
            if (investigationProgress != null)
            {
                investigationProgress.AllRequiredCluesCollected += EnterDeductionState;
                investigationProgress.CaseSolved += EnterCombatState;
            }
            if (bossSpawnController != null) bossSpawnController.BossSpawned += HandleBossSpawned;
            if (rewardSelectionUI != null) rewardSelectionUI.RewardSelected += HandleRewardSelected;
            if (completionUI != null)
            {
                completionUI.ReturnRequested += HandleReturnRequested;
                completionUI.RetryRequested += RetryCompletion;
            }
            if (playerExperience != null) playerExperience.UpgradeChoicesChanged += TryShowBossReward;
            if (!started) return;
            if (CurrentState == GameFlowState.Combat && bossSpawnController != null) TrackBoss(bossSpawnController.ActiveBoss);
            if (CurrentState == GameFlowState.Reward && selectedReward == null) TryShowBossReward();
            if (CurrentState == GameFlowState.ChapterComplete && completionPersisted) ShowCompletion();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopTrackingBoss();
        }

        private void Unsubscribe()
        {
            if (investigationProgress != null)
            {
                investigationProgress.AllRequiredCluesCollected -= EnterDeductionState;
                investigationProgress.CaseSolved -= EnterCombatState;
            }
            if (bossSpawnController != null) bossSpawnController.BossSpawned -= HandleBossSpawned;
            if (rewardSelectionUI != null) rewardSelectionUI.RewardSelected -= HandleRewardSelected;
            if (completionUI != null)
            {
                completionUI.ReturnRequested -= HandleReturnRequested;
                completionUI.RetryRequested -= RetryCompletion;
            }
            if (playerExperience != null) playerExperience.UpgradeChoicesChanged -= TryShowBossReward;
        }

        public void UnlockAllClues() => investigationProgress?.CollectAllRequiredClues();
        public void DebugSpawnBoss() => EnterCombatState();

        public void DebugKillBoss()
        {
            if (CurrentState != GameFlowState.Combat) return;
            if (trackedBossDamageable == null && bossSpawnController != null) TrackBoss(bossSpawnController.ActiveBoss);
            if (trackedBossDamageable != null && trackedBossDamageable.IsAlive)
                trackedBossDamageable.ApplyDamage(new DamageInfo(99999f, CombatTeam.Player, gameObject, trackedBossDamageable.transform.position));
        }

        private void EnterInvestigationState()
        {
            SetState(GameFlowState.Investigation);
            enemySpawner?.StopAndClearEnemies();
            if (bossSpawnController != null) bossSpawnController.enabled = false;
            deductionBoard?.SetAvailable(true);
            rewardSelectionUI?.Hide();
            completionUI?.Hide();
        }

        private void EnterDeductionState()
        {
            if (CurrentState != GameFlowState.Investigation) return;
            SetState(GameFlowState.Deduction);
            deductionBoard?.SetAvailable(true);
        }

        private void EnterCombatState()
        {
            if (ActiveCase == null || CurrentState == GameFlowState.Combat
                || CurrentState == GameFlowState.Reward || CurrentState == GameFlowState.ChapterComplete) return;
            Debug.Log($"Case Deduction Success: {ActiveCase.CaseId}");
            SetState(GameFlowState.Combat);
            deductionBoard?.SetAvailable(false);
            rewardSelectionUI?.Hide();
            completionUI?.Hide();
            if (enemySpawner != null) enemySpawner.enabled = true;
            if (bossSpawnController != null)
            {
                bossSpawnController.SetBossDefinition(ActiveCase.BossDefinition);
                bossSpawnController.enabled = true;
                TrackBoss(bossSpawnController.SpawnBoss());
            }
        }

        private void HandleBossSpawned(GameObject boss)
        {
            Debug.Log($"Boss Spawned: {ActiveCase?.BossDefinition?.BossId}");
            TrackBoss(boss);
        }

        private void TrackBoss(GameObject boss)
        {
            if (boss == null) return;
            StopTrackingBoss();
            trackedBossDamageable = boss.GetComponent<Damageable>();
            // Boss XP is paid synchronously once before XP intake is sealed, never as a late pickup.
            var dropper = boss.GetComponent<XpDropper>();
            if (dropper != null) dropper.enabled = false;
            if (trackedBossDamageable != null) trackedBossDamageable.Died += HandleBossDied;
        }

        private void StopTrackingBoss()
        {
            if (trackedBossDamageable != null) trackedBossDamageable.Died -= HandleBossDied;
            trackedBossDamageable = null;
        }

        private void HandleBossDied(Damageable source, DamageInfo damageInfo)
        {
            if (CurrentState != GameFlowState.Combat) return;
            StopTrackingBoss();
            Debug.Log($"Boss Defeated: {ActiveCase?.BossDefinition?.BossId}");
            SetState(GameFlowState.Reward);
            settlingBossDeath = true;
            StopCombat();
            if (playerExperience != null)
            {
                playerExperience.AddExperience(ActiveCase != null && ActiveCase.BossDefinition != null ? ActiveCase.BossDefinition.XpReward : 0);
                playerExperience.SetAcceptsExperience(false);
            }
            settlingBossDeath = false;
            TryShowBossReward();
        }

        private void StopCombat()
        {
            enemySpawner?.StopAndClearEnemies();
            if (bossSpawnController != null) bossSpawnController.enabled = false;
            deductionBoard?.SetAvailable(false);
            foreach (var weapon in FindObjectsOfType<PistolWeapon>()) weapon.enabled = false;
            foreach (var weapon in FindObjectsOfType<MemoryOrbWeapon>()) weapon.enabled = false;
            foreach (var projectile in FindObjectsOfType<Projectile>()) RemoveFromCombat(projectile.gameObject);
            foreach (var orb in FindObjectsOfType<XpOrb>()) RemoveFromCombat(orb.gameObject);
            foreach (var member in FindObjectsOfType<TeamMember>())
                if (member.Team == CombatTeam.Enemy) RemoveFromCombat(member.gameObject);
        }

        private static void RemoveFromCombat(GameObject target)
        {
            target.SetActive(false);
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private void TryShowBossReward()
        {
            if (settlingBossDeath || CurrentState != GameFlowState.Reward || selectedReward != null) return;
            if ((playerExperience != null && playerExperience.PendingUpgradeChoices > 0)
                || (upgradeSelectionController != null && upgradeSelectionController.IsShowing)) return;
            if (ActiveCase == null || rewardSelectionUI == null) return;
            rewardSelectionUI.Show(playerStats, ActiveCase.CompletionText, ActiveCase.GetCompletionRewards(), combatUpgradeStats);
        }

        private void HandleRewardSelected(RewardDefinition reward)
        {
            if (CurrentState != GameFlowState.Reward || selectedReward != null || ActiveCase == null || reward == null
                || Array.IndexOf(ActiveCase.GetCompletionRewards(), reward) < 0) return;
            selectedReward = reward;
            TryFinalizeCompletion(retryStorage: false);
        }

        public void RetryCompletion()
        {
            if (completionPersisted) HandleReturnRequested();
            else if (selectedReward != null) TryFinalizeCompletion(retryStorage: true);
        }

        private void TryFinalizeCompletion(bool retryStorage)
        {
            var save = playerProgressPersistence != null ? playerProgressPersistence.SaveGame : null;
            if (save == null)
            {
                completionUI?.ShowError("未找到存档服务，请检查场景配置。");
                return;
            }
            if (retryStorage && save.IsReadOnly) save.Reload();
            if (save.IsReadOnly) { ShowSaveError(); return; }
            if (save.HasUnsavedChanges)
            {
                save.SaveNow();
                if (save.HasUnsavedChanges || save.IsReadOnly) { ShowSaveError(); return; }
            }
            if (!rewardApplied)
            {
                rewardApplied = true;
                selectedReward.Apply(playerStats, combatUpgradeStats);
                Debug.Log($"Reward Selected: {selectedReward.RewardId}");
            }
            if (!playerProgressPersistence.RecordReward(selectedReward)) { ShowSaveError(); return; }
            save.CompleteCase(ActiveCase.CaseId);
            if (save.IsReadOnly || save.HasUnsavedChanges || !save.IsCaseCompleted(ActiveCase.CaseId)) { ShowSaveError(); return; }
            completionPersisted = true;
            SetState(GameFlowState.ChapterComplete);
            rewardSelectionUI?.Hide();
            ShowCompletion();
        }

        private void ShowSaveError()
        {
            var save = playerProgressPersistence.SaveGame;
            completionUI?.ShowError(save.IsReadOnly
                ? "存档只读，案件尚未保存。请使用兼容版本或修复存档后重试。"
                : "保存失败，案件进度尚未写入。修复存储问题后重试不会重复领取奖励。");
        }

        private void ShowCompletion()
        {
            completionUI?.ShowChapterComplete(ActiveCase != null ? ActiveCase.CaseName : "当前案件",
                ActiveCase != null ? ActiveCase.CompletionText : string.Empty);
        }

        private void HandleReturnRequested()
        {
            if (!completionPersisted) return;
            var runtime = GameRuntime.Instance;
            if (runtime == null)
            {
                completionUI?.ShowError("未找到场景服务，请检查场景配置。");
                return;
            }
            Debug.Log($"Case Completed: {ActiveCase?.CaseId}");
            runtime.SceneFlow.LoadHub();
            if (!string.IsNullOrEmpty(runtime.SceneFlow.LastError))
                completionUI?.ShowError("返回大厅失败，请检查场景配置后重试。");
        }

        private void SetState(GameFlowState nextState)
        {
            CurrentState = nextState;
            StateChanged?.Invoke(CurrentState);
        }

        private void ResolveReferences()
        {
            if (investigationProgress == null) investigationProgress = FindObjectOfType<InvestigationProgress>();
            if (deductionBoard == null) deductionBoard = FindObjectOfType<DeductionBoard>(includeInactive: true);
            if (enemySpawner == null) enemySpawner = FindObjectOfType<EnemySpawner>(includeInactive: true);
            if (bossSpawnController == null) bossSpawnController = FindObjectOfType<BossSpawnController>(includeInactive: true);
            if (rewardSelectionUI == null) rewardSelectionUI = FindObjectOfType<RewardSelectionUI>(includeInactive: true);
            if (completionUI == null) completionUI = FindObjectOfType<CaseCompletionUI>(includeInactive: true);
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
            if (combatUpgradeStats == null) combatUpgradeStats = FindObjectOfType<CombatUpgradeStats>();
            if (playerExperience == null) playerExperience = FindObjectOfType<PlayerExperience>();
            if (playerProgressPersistence == null) playerProgressPersistence = FindObjectOfType<PlayerProgressPersistence>();
            if (upgradeSelectionController == null) upgradeSelectionController = FindObjectOfType<UpgradeSelectionController>(includeInactive: true);
        }
    }
}
