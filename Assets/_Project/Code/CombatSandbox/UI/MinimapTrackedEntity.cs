using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class MinimapTrackedEntity : MonoBehaviour
    {
        [SerializeField] private MinimapEntityType entityType = MinimapEntityType.Enemy;

        public MinimapEntityType EntityType => entityType;
    }
}
