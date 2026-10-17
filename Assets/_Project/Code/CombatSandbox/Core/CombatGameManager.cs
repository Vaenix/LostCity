using System.Collections;
using System;
using LostCity.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostCity.CombatSandbox
{
    public sealed class CombatGameManager : MonoBehaviour
    {
        private static CombatGameManager instance;
        private Coroutine restartRoutine;
        private CaseCompletionUI errorUI;
        private float failedRestartDelay;

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
            Time.timeScale = 1f;

            var persistence = FindObjectOfType<PlayerProgressPersistence>();
            bool captured = persistence != null && persistence.Capture();
            if (!captured)
            {
                Debug.LogError("重进前保存失败，暂不销毁未写入的玩家进度。", this);
                failedRestartDelay = delaySeconds;
                if (errorUI == null) errorUI = FindObjectOfType<CaseCompletionUI>(includeInactive: true);
                if (errorUI != null)
                {
                    errorUI.RetryRequested -= RetryRestart;
                    errorUI.RetryRequested += RetryRestart;
                    errorUI.ShowError("重进前保存失败，未写入的进度仍保留在当前场景。修复存档后重试。");
                }
                return;
            }
            if (errorUI != null)
            {
                errorUI.RetryRequested -= RetryRestart;
                errorUI.Hide();
            }
            restartRoutine = StartCoroutine(RestartAfterDelay(delaySeconds));
        }

        private IEnumerator RestartAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));
            Scene activeScene = SceneManager.GetActiveScene();
            Time.timeScale = 1f;
            try
            {
                // Keep CaseSession intact: the selected definition must survive a failed run.
                SceneManager.LoadScene(activeScene.path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"重进场景失败：{activeScene.path}，{exception.Message}", this);
            }
            finally { restartRoutine = null; }
        }

        private void RetryRestart()
        {
            var save = FindObjectOfType<PlayerProgressPersistence>()?.SaveGame;
            if (save != null)
            {
                if (save.IsReadOnly) save.Reload();
                else if (save.HasUnsavedChanges) save.SaveNow();
            }
            RestartCurrentScene(failedRestartDelay);
        }

        private void OnDestroy()
        {
            if (errorUI != null) errorUI.RetryRequested -= RetryRestart;
            if (instance == this) instance = null;
        }
    }
}
