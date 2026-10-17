using LostCity.Meta.Persistence;
using UnityEngine;

namespace LostCity.Meta
{
    public sealed class GameRuntime : MonoBehaviour
    {
        public static GameRuntime Instance { get; private set; }
        public SaveGameManager SaveGame { get; private set; }
        public CaseRegistry CaseRegistry { get; private set; }
        public CaseSessionManager CaseSession { get; private set; }
        public SceneFlowManager SceneFlow { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveGame = Resolve<SaveGameManager>();
            SaveGame.Initialize();
            CaseRegistry = Resolve<CaseRegistry>();
            CaseRegistry.Initialize(Resources.Load<GameContentCatalog>("GameContentCatalog"), SaveGame);
            CaseSession = Resolve<CaseSessionManager>();
            CaseSession.Initialize(CaseRegistry);
            SceneFlow = Resolve<SceneFlowManager>();
            SceneFlow.Initialize(CaseSession);
        }

        private T Resolve<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

        private void OnApplicationPause(bool paused)
        {
            if (paused && Instance == this && SaveGame != null && SaveGame.HasUnsavedChanges) SaveGame.SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (Instance == this && SaveGame != null && SaveGame.HasUnsavedChanges) SaveGame.SaveNow();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
