using System.Collections.Generic;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class CaseClueLayout : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private CluePickup pickupTemplate;
        [SerializeField] private CluePickup[] authoredPickups = new CluePickup[0];
        [SerializeField] private Vector2 center;
        [SerializeField, Min(1f)] private float radius = 6f;

        private readonly List<CluePickup> spawnedPickups = new List<CluePickup>();
        private readonly List<CluePickup> generatedPickups = new List<CluePickup>();

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
            foreach (CluePickup pickup in generatedPickups)
            {
                if (pickup == null) continue;
                pickup.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(pickup.gameObject);
                else DestroyImmediate(pickup.gameObject);
            }
            spawnedPickups.Clear();
            generatedPickups.Clear();

            IReadOnlyList<ClueDefinition> clues = investigationProgress.RequiredClues;
            HashSet<string> placedIds = new HashSet<string>();
            HashSet<CluePickup> usedAuthored = new HashSet<CluePickup>();
            for (int i = 0; i < clues.Count; i++)
            {
                ClueDefinition clue = clues[i];
                if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || !placedIds.Add(clue.Id)) continue;

                CluePickup pickup = FindAuthored(clue.Id, usedAuthored);
                if (pickup == null)
                {
                    pickup = Instantiate(pickupTemplate, transform);
                    pickup.name = "Clue_" + clue.Id;
                    float angle = (i + 0.5f) * Mathf.PI * 2f / clues.Count;
                    Vector2 offset = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    pickup.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                    generatedPickups.Add(pickup);
                }
                else usedAuthored.Add(pickup);
                pickup.Initialize(clue, investigationProgress);
                pickup.gameObject.SetActive(true);
                spawnedPickups.Add(pickup);
            }
            // Authored objects remain in their original hierarchy; only their runtime visibility changes.
            if (authoredPickups != null)
                foreach (CluePickup pickup in authoredPickups)
                    if (pickup != null && pickup != pickupTemplate && !usedAuthored.Contains(pickup))
                        pickup.gameObject.SetActive(false);
        }

        private CluePickup FindAuthored(string id, HashSet<CluePickup> used)
        {
            if (authoredPickups == null) return null;
            foreach (CluePickup pickup in authoredPickups)
                if (pickup != null && pickup != pickupTemplate && !used.Contains(pickup)
                    && pickup.Definition != null && pickup.Definition.Id == id)
                    return pickup;
            return null;
        }
    }
}
