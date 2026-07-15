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

    static ProjectSetup() => EditorApplication.delayCall += EnsureProjectConfiguration;

    public static void EnsureProjectConfiguration()
    {
        UrpProjectSetup.Configure();
        EnsureMainSceneInBuildSettings();
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Alba World/Generate Demo Scene")]
    public static void EnsureDemoScene()
    {
        EnsureProjectConfiguration();

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
        // Recreate this generated root so old prototype components cannot survive a
        // regeneration (Unity keeps missing MonoBehaviour slots in the scene YAML).
        var previousComposition = FindRootObject(scene, "Alba World 3D");
        if (previousComposition != null)
            Object.DestroyImmediate(previousComposition);
        var composition = EnsureRootObject(scene, "Alba World 3D");
        var app = composition.AddComponent<global::AlbaWorld.Runtime.AlbaWorld3DApp>();
        var serializedApp = new SerializedObject(app);
        serializedApp.FindProperty("_girlPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art3D/Characters/Prefabs/BodyGirl.prefab");
        serializedApp.FindProperty("_boyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art3D/Characters/Prefabs/BodyBoy.prefab");
        serializedApp.FindProperty("_petCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<global::AlbaWorld.Catalog.ItemCatalog3D>("Assets/Resources/Data/AlbaItemCatalog3D.asset");
        serializedApp.FindProperty("_itemCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<global::AlbaWorld.Catalog.ItemCatalog3D>("Assets/Resources/Data/AlbaItemCatalog3D.asset");
        serializedApp.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Alba World demo scene generated.");
    }

    private static void EnsureMainSceneInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        for (var index = 0; index < scenes.Length; index++)
        {
            if (scenes[index].path != ScenePath)
                continue;

            if (!scenes[index].enabled)
            {
                scenes[index].enabled = true;
                EditorBuildSettings.scenes = scenes;
            }

            return;
        }

        var updatedScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, updatedScenes, scenes.Length);
        updatedScenes[updatedScenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
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
        var app = GameObject.Find("Alba World 3D");
        Debug.Log($"Demo scene loaded={scene.IsValid()} app={(app != null)} component={(app != null && app.GetComponent<global::AlbaWorld.Runtime.AlbaWorld3DApp>() != null)}");
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
