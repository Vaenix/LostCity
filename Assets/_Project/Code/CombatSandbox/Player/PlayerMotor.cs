using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private float moveSpeed = 7f;

        private Rigidbody2D body;

        public float MoveSpeed => moveSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = inputReader != null ? inputReader.Move : Vector2.zero;
            Vector2 moveDirection = moveInput;

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            Vector2 nextPosition = body.position + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            body.MovePosition(nextPosition);
        }
    }
}
