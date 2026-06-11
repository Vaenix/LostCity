using System;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float weight = 1f;

        public GameObject Prefab => prefab;
        public float Weight => Mathf.Max(0f, weight);
    }
}
