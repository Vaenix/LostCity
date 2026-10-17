using System;
using LostCity.CombatSandbox;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostCity.Meta
{
    public sealed class SceneFlowManager : MonoBehaviour
    {
        [SerializeField] private string hubSceneName = "Hub";
        [SerializeField] private string investigationSceneName = "CombatSandbox";
        private CaseSessionManager session;
        public string LastError { get; private set; } = string.Empty;
        public bool IsLoading { get; private set; }

        public void Initialize(CaseSessionManager caseSession) => session = caseSession;

        public void LoadHub()
        {
            if (IsLoading) return;
            session?.EndCase();
            TryLoad(hubSceneName);
        }

        public bool StartCase(CaseDefinition definition)
        {
            if (IsLoading || session == null || !session.BeginCase(definition)) return false;
            return TryLoad(investigationSceneName);
        }

        private bool TryLoad(string sceneName)
        {
            IsLoading = true;
            Time.timeScale = 1f;
            try
            {
                if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
                    throw new InvalidOperationException("Scene is not available in Build Settings.");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                LastError = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                session?.EndCase();
                Time.timeScale = 1f;
                LastError = $"Scene load failed for '{sceneName}': {exception.Message}";
                Debug.LogError(LastError, this);
                return false;
            }
            finally { IsLoading = false; }
        }
    }
}
