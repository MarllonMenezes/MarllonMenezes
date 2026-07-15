using UnityEngine;

namespace AlbaWorld.Runtime;

public static class AlbaWorldBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureApp()
    {
        if (Object.FindFirstObjectByType<global::AlbaWorld.Runtime.AlbaWorld3DApp>() != null)
            return;

        var app = new GameObject("Alba World 3D");
        app.AddComponent<global::AlbaWorld.Runtime.AlbaWorld3DApp>();
    }
}
