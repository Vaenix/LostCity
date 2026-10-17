using UnityEngine;

namespace LostCity.Characters
{
    public sealed class ProtagonistPickupAnchor : MonoBehaviour
    {
        [SerializeField] private Transform contactPoint;
        [SerializeField] private Transform visualRoot;
        public Transform ContactPoint => contactPoint;
        public Transform VisualRoot => visualRoot;
    }
}
