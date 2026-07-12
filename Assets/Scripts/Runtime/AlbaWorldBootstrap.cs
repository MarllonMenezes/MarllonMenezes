using UnityEngine;

namespace AlbaWorld.Runtime;

public static class AlbaWorldBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureApp()
    {
        if (Object.FindFirstObjectByType<global::AlbaWorld.AlbaWorldApp>() != null)
            return;

        var app = new GameObject("Alba World App");
        app.AddComponent<global::AlbaWorld.AlbaWorldApp>();
    }
}
