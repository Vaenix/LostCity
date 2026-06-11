using UnityEngine;
using UnityEngine.InputSystem;

namespace LostCity.CombatSandbox
{
    public sealed class Room304DebugTools : MonoBehaviour
    {
        [SerializeField] private GameFlowManager gameFlowManager;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            ResolveReferences();

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                gameFlowManager?.UnlockAllClues();
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                gameFlowManager?.DebugSpawnBoss();
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                gameFlowManager?.DebugKillBoss();
            }
        }

        private void ResolveReferences()
        {
            if (gameFlowManager == null)
            {
                gameFlowManager = FindObjectOfType<GameFlowManager>();
            }
        }
    }
}
