using UnityEngine;
using UnityEngine.InputSystem;

namespace LostCity.CombatSandbox
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private Transform yawRoot;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float turnSpeedDegrees = 1080f;

        public Vector3 AimDirection => yawRoot != null ? yawRoot.right : transform.right;

        public static bool TryGetAimPoint(Camera camera, Vector2 screenPosition, float planeZ, out Vector3 point)
        {
            point = Vector3.zero;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            // A grazing ray gives an unstable, effectively unbounded aim point.
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
            {
                return false;
            }

            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        private void Awake()
        {
            if (yawRoot == null)
            {
                yawRoot = transform;
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (Mouse.current == null || aimCamera == null || yawRoot == null)
            {
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (!TryGetAimPoint(aimCamera, mousePosition, yawRoot.position.z, out Vector3 mouseWorldPosition))
            {
                return;
            }

            Vector2 aimDirection = mouseWorldPosition - yawRoot.position;

            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float targetAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            float currentAngle = yawRoot.eulerAngles.z;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeedDegrees * Time.deltaTime);
            yawRoot.rotation = Quaternion.Euler(0f, 0f, nextAngle);
        }
    }
}
