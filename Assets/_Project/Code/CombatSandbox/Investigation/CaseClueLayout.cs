using System.Collections.Generic;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class CaseClueLayout : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private CluePickup pickupTemplate;
        [SerializeField] private Vector2 center;
        [SerializeField, Min(1f)] private float radius = 6f;

        private readonly List<CluePickup> spawnedPickups = new List<CluePickup>();

        public IReadOnlyList<CluePickup> SpawnedPickups => spawnedPickups.AsReadOnly();

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            if (investigationProgress == null || pickupTemplate == null)
            {
                Debug.LogError("案件线索布局缺少调查进度或拾取模板，请重新生成场景。", this);
                return;
            }

            pickupTemplate.gameObject.SetActive(false);
            foreach (CluePickup pickup in spawnedPickups)
            {
                if (pickup == null) continue;
                pickup.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(pickup.gameObject);
                else DestroyImmediate(pickup.gameObject);
            }
            spawnedPickups.Clear();

            IReadOnlyList<ClueDefinition> clues = investigationProgress.RequiredClues;
            HashSet<string> placedIds = new HashSet<string>();
            for (int i = 0; i < clues.Count; i++)
            {
                ClueDefinition clue = clues[i];
                if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || !placedIds.Add(clue.Id)) continue;

                CluePickup pickup = Instantiate(pickupTemplate, transform);
                pickup.name = "Clue_" + clue.Id;
                pickup.Initialize(clue, investigationProgress);
                float angle = (i + 0.5f) * Mathf.PI * 2f / clues.Count;
                Vector2 offset = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                pickup.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                pickup.gameObject.SetActive(true);
                spawnedPickups.Add(pickup);
            }
        }
    }
}
