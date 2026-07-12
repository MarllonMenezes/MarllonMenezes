#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlbaWorld.Editor;

[InitializeOnLoad]
public static class ProjectSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    static ProjectSetup() => EditorApplication.delayCall += EnsureDemoScene;

    [MenuItem("Alba World/Generate Demo Scene")]
    public static void EnsureDemoScene()
    {
        if (System.IO.File.Exists(ScenePath))
        {
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayer();
            return;
        }
        if (!System.IO.Directory.Exists("Assets/Scenes")) System.IO.Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var app = new GameObject("Alba World Runtime");
        SceneManager.MoveGameObjectToScene(app, scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("Alba World demo scene generated.");
    }

    public static void ValidateDemoScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var app = GameObject.Find("Alba World App");
        Debug.Log($"Demo scene loaded={scene.IsValid()} app={(app != null)} component={(app != null && app.GetComponent<global::AlbaWorld.AlbaWorldApp>() != null)}");
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Alba World Games";
        PlayerSettings.productName = "Alba World";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.albaworldgames.albaworld");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.albaworldgames.albaworld");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }
}
#endif
