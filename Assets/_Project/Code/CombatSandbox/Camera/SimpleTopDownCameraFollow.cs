using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class SimpleTopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private float followSharpness = 10f;

        private void Start()
        {
            if (target == null)
            {
                PlayerMotor player = FindObjectOfType<PlayerMotor>();
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        }
    }
}
