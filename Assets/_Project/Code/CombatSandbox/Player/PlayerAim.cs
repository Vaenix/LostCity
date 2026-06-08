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
            float cameraToRootDistance = Mathf.Abs(aimCamera.transform.position.z - yawRoot.position.z);
            Vector3 mouseWorldPosition = aimCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraToRootDistance));
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
