using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    /// <summary>Deprecated serialization shell for untouched legacy scenes; not a Phase 6 game flow.</summary>
    [Obsolete("Legacy serialization compatibility only. Use GameFlowManager for Phase 6.")]
    [AddComponentMenu("")]
    public sealed class Room304GameStateController : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private DeductionBoard deductionBoard;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private BossSpawnController bossSpawnController;
        [SerializeField] private MemoryFragmentPanel memoryFragmentPanel;

        private void Start()
        {
            if (!Application.isPlaying) return;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                if (root.GetComponentInChildren<GameFlowManager>(true) != null) return;
            Debug.LogWarning("此旧场景仅保留序列化兼容数据，不提供 Phase 6 游戏流程。请从大厅进入。", this);
        }
    }
}
