#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlbaWorld.Editor;

public static class PlayModeSmokeTest
{
    private static double _startedAt;
    private static bool _reported;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Tick;
        _startedAt = EditorApplication.timeSinceStartup;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
            _startedAt = EditorApplication.timeSinceStartup;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (_reported)
                EditorApplication.Exit(0);
            return;
        }

        if (EditorApplication.timeSinceStartup - _startedAt < 3.0)
            return;

        var app = UnityEngine.Object.FindFirstObjectByType<global::AlbaWorld.Runtime.AlbaWorld3DApp>();
        var pet = UnityEngine.Object.FindFirstObjectByType<global::AlbaWorld.Pets.PetAssemblyController>();
        var furniture = UnityEngine.Object.FindFirstObjectByType<global::AlbaWorld.Runtime.RoomFurnitureController>();
        var hud = GameObject.Find("Alba World HUD");
        Debug.Log($"Alba World smoke test: app={(app != null)} pet={(pet != null && pet.ActiveInstance != null)} furniture={(furniture != null && furniture.ActivePlacements.Count > 0)} hud={(hud != null)}");
        _reported = true;
        EditorApplication.isPlaying = false;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
}
#endif
