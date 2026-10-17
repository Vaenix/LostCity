using LostCity.CombatSandbox;
using UnityEngine;

namespace LostCity.Characters
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class ProtagonistCluePresentation : MonoBehaviour
    {
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private ProtagonistVisualMotion motion;
        [SerializeField] private CluePickup[] pickups;
        private GameObject proxy;
        private Vector3 initialProxyPosition, contactPosition;
        private Quaternion initialProxyRotation;

        private void OnEnable()
        {
            if (investigationProgress != null) investigationProgress.ClueCollected += OnClueCollected;
        }
        private void OnDisable()
        {
            if (investigationProgress != null) investigationProgress.ClueCollected -= OnClueCollected;
            if (motion != null) motion.CancelPickup();
            ClearProxy();
        }
        private void OnClueCollected(ClueDefinition clue)
        {
            if (motion == null || pickups == null || clue == null) return;
            foreach (CluePickup pickup in pickups)
            {
                if (pickup == null || !pickup.gameObject.activeInHierarchy || pickup.Definition != clue) continue;
                var anchor = pickup.GetComponent<ProtagonistPickupAnchor>();
                if (anchor == null || anchor.ContactPoint == null || anchor.VisualRoot == null) return;
                if (!motion.TryPresentPickup(anchor.ContactPoint.position, pickup)) return;
                ClearProxy();
                // Copy only render components, never interaction scripts, triggers or save owners.
                proxy = new GameObject("CollectedClueVisual");
                proxy.transform.SetPositionAndRotation(anchor.VisualRoot.position, anchor.VisualRoot.rotation);
                // Each copied mesh already carries its world scale; keep its parent neutral.
                proxy.transform.localScale = Vector3.one;
                foreach (MeshFilter source in anchor.VisualRoot.GetComponentsInChildren<MeshFilter>(false))
                {
                    MeshRenderer renderer = source.GetComponent<MeshRenderer>();
                    if (source.sharedMesh == null || renderer == null || !renderer.enabled) continue;
                    var part = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                    part.transform.SetParent(proxy.transform, false);
                    part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                    part.transform.localScale = source.transform.lossyScale;
                    part.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                    part.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                }
                initialProxyPosition = proxy.transform.position;
                initialProxyRotation = proxy.transform.rotation;
                contactPosition = anchor.ContactPoint.position;
                return;
            }
        }
        private void LateUpdate()
        {
            if (proxy == null) return;
            if (motion == null || !motion.isActiveAndEnabled || !motion.IsPickingUp) { ClearProxy(); return; }
            if (motion.PickupProgress >= .65f / 1.2f && motion.LeftHand != null)
            {
                Vector3 grip = motion.VisualGripPoint;
                proxy.transform.SetPositionAndRotation(initialProxyPosition + grip - contactPosition, initialProxyRotation);
            }
        }
        private void ClearProxy()
        {
            if (proxy == null) return;
            if (Application.isPlaying) Destroy(proxy); else DestroyImmediate(proxy);
            proxy = null;
        }
    }
}
