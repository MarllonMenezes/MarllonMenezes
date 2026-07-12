#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
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
        UrpProjectSetup.Configure();

        Scene scene;
        if (System.IO.File.Exists(ScenePath))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
        {
            if (!System.IO.Directory.Exists("Assets/Scenes"))
                System.IO.Directory.CreateDirectory("Assets/Scenes");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        var legacyRuntime = FindRootObject(scene, "Alba World Runtime");
        if (legacyRuntime != null)
            Object.DestroyImmediate(legacyRuntime);

        ConfigureCamera(scene);
        ConfigureDirectionalLight(scene);
        ConfigureGlobalVolume(scene);
        EnsureRootObject(scene, "WorldRoot");

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("Alba World demo scene generated.");
    }

    private static void ConfigureCamera(Scene scene)
    {
        var cameraObject = EnsureRootObject(scene, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 4f, -8f), Quaternion.Euler(20f, 0f, 0f));
        var camera = cameraObject.GetComponent<Camera>();
        if (camera == null)
            camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 45f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
    }

    private static void ConfigureDirectionalLight(Scene scene)
    {
        var lightObject = EnsureRootObject(scene, "Directional Light");
        lightObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(50f, -30f, 0f));
        var light = lightObject.GetComponent<Light>();
        if (light == null)
            light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        RenderSettings.sun = light;
    }

    private static void ConfigureGlobalVolume(Scene scene)
    {
        var volumeObject = EnsureRootObject(scene, "Global Volume");
        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
            volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(UrpProjectSetup.VolumeProfilePath);
    }

    private static GameObject EnsureRootObject(Scene scene, string objectName)
    {
        var gameObject = FindRootObject(scene, objectName);
        if (gameObject != null)
            return gameObject;

        gameObject = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        foreach (var gameObject in scene.GetRootGameObjects())
        {
            if (gameObject.name == objectName)
                return gameObject;
        }

        return null;
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
