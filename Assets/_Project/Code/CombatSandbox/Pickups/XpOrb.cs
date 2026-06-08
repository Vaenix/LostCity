using UnityEngine;

namespace LostCity.CombatSandbox
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class XpOrb : MonoBehaviour
    {
        [SerializeField] private int experienceValue = 1;
        [SerializeField] private float attractionRadius = 3.5f;
        [SerializeField] private float moveSpeed = 8f;

        private PlayerExperience target;

        public void Initialize(int value)
        {
            experienceValue = Mathf.Max(1, value);
        }

        private void Awake()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
        }

        private void Update()
        {
            ResolveTarget();

            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            if (toTarget.sqrMagnitude > attractionRadius * attractionRadius)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerExperience playerExperience = other.GetComponentInParent<PlayerExperience>();
            if (playerExperience == null)
            {
                return;
            }

            playerExperience.AddExperience(experienceValue);
            Destroy(gameObject);
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            target = FindObjectOfType<PlayerExperience>();
        }
    }
}
