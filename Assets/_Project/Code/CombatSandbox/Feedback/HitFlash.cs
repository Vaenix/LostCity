using System.Collections;
using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Damageable))]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashSeconds = 0.08f;

        private Damageable damageable;
        private Color originalColor;
        private Coroutine flashRoutine;

        private void Awake()
        {
            damageable = GetComponent<Damageable>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (targetRenderer != null)
            {
                originalColor = targetRenderer.color;
            }
        }

        private void OnEnable()
        {
            damageable.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            damageable.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(Damageable source, DamageInfo damageInfo)
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            targetRenderer.color = flashColor;
            yield return new WaitForSeconds(flashSeconds);
            targetRenderer.color = originalColor;
            flashRoutine = null;
        }
    }
}
