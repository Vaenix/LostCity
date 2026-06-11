using UnityEngine;
using UnityEngine.InputSystem;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CluePickup : MonoBehaviour
    {
        [SerializeField] private ClueDefinition clue;
        [SerializeField] private InvestigationProgress investigationProgress;
        [SerializeField] private GameObject interactionPrompt;

        private bool playerInRange;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            if (!playerInRange)
            {
                return;
            }

            ResolveProgress();
            if (investigationProgress == null || investigationProgress.IsCaseSolved)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Collect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            ResolveProgress();
            playerInRange = true;
            SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInRange = false;
            SetPromptVisible(false);
        }

        private void Collect()
        {
            ResolveProgress();
            if (investigationProgress != null && investigationProgress.TryCollectClue(clue))
            {
                gameObject.SetActive(false);
            }
        }

        private void ResolveProgress()
        {
            if (investigationProgress == null)
            {
                investigationProgress = FindObjectOfType<InvestigationProgress>();
            }
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(visible);
            }
        }

        private static bool IsPlayer(Collider2D other)
        {
            TeamMember teamMember = other.GetComponentInParent<TeamMember>();
            return teamMember != null && teamMember.Team == CombatTeam.Player;
        }
    }
}
