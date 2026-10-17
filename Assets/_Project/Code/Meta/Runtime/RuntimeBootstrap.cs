using UnityEngine;

namespace LostCity.Meta
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (GameRuntime.Instance != null) return;
            var root = new GameObject("LostCityRuntime");
            root.AddComponent<GameRuntime>();
        }
    }
}
