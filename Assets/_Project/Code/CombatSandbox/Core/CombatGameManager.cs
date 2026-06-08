using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostCity.CombatSandbox
{
    public sealed class CombatGameManager : MonoBehaviour
    {
        private static CombatGameManager instance;
        private Coroutine restartRoutine;

        public static CombatGameManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public void RestartCurrentScene(float delaySeconds)
        {
            if (restartRoutine != null)
            {
                return;
            }

            restartRoutine = StartCoroutine(RestartAfterDelay(delaySeconds));
        }

        private IEnumerator RestartAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
