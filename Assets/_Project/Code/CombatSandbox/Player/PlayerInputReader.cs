using UnityEngine;
using UnityEngine.InputSystem;

namespace LostCity.CombatSandbox
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference fireActionReference;

        private InputAction runtimeMoveAction;
        private InputAction runtimeFireAction;

        public Vector2 Move => MoveAction != null ? MoveAction.ReadValue<Vector2>() : Vector2.zero;
        public bool FirePressedThisFrame => FireAction != null && FireAction.WasPressedThisFrame();
        public bool FireHeld => FireAction != null && FireAction.IsPressed();

        private InputAction MoveAction => moveActionReference != null ? moveActionReference.action : runtimeMoveAction;
        private InputAction FireAction => fireActionReference != null ? fireActionReference.action : runtimeFireAction;

        private void Awake()
        {
            CreateRuntimeFallbackActions();
        }

        private void OnEnable()
        {
            MoveAction?.Enable();
            FireAction?.Enable();
        }

        private void OnDisable()
        {
            MoveAction?.Disable();
            FireAction?.Disable();
        }

        private void CreateRuntimeFallbackActions()
        {
            runtimeMoveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            runtimeMoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            runtimeFireAction = new InputAction("Fire", InputActionType.Button);
            runtimeFireAction.AddBinding("<Mouse>/leftButton");
        }
    }
}
