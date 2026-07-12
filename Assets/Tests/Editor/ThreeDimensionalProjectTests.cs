using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AlbaWorld.Tests;

public sealed class ThreeDimensionalProjectTests
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [Test]
    public void UrpAndThreeDimensionalSettingsArePresent()
    {
        var manifest = File.ReadAllText("Packages/manifest.json");
        StringAssert.Contains("\"com.unity.render-pipelines.universal\": \"17.3.0\"", manifest);
        Assert.That(File.Exists("Assets/Settings/AlbaWorldURP.asset"), Is.True);
        Assert.That(File.Exists("Assets/Settings/AlbaWorldRenderer.asset"), Is.True);
        Assert.That(File.Exists(MainScenePath), Is.True);
    }

    [Test]
    public void StartupRegistersNonDestructiveConfiguration()
    {
        var setupSource = File.ReadAllText("Assets/Editor/ProjectSetup.cs");

        StringAssert.Contains("delayCall += EnsureProjectConfiguration", setupSource);
        StringAssert.DoesNotContain("delayCall += EnsureDemoScene", setupSource);
    }

    [Test]
    public void AutomaticConfigurationPreservesTheCurrentScene()
    {
        var configurationSnapshot = ProjectConfigurationSnapshot.Capture();
        var activeScene = SceneManager.GetActiveScene();
        var loadedSceneHandles = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(index => SceneManager.GetSceneAt(index).handle)
            .ToArray();
        var mainSceneContents = File.ReadAllBytes(MainScenePath);
        var mainSceneWriteTime = File.GetLastWriteTimeUtc(MainScenePath);

        try
        {
            InvokeEditorMethod("AlbaWorld.Editor.ProjectSetup", "EnsureProjectConfiguration");

            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(activeScene.handle));
            Assert.That(
                Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index).handle),
                Is.EqualTo(loadedSceneHandles));
            CollectionAssert.AreEqual(mainSceneContents, File.ReadAllBytes(MainScenePath));
            Assert.That(File.GetLastWriteTimeUtc(MainScenePath), Is.EqualTo(mainSceneWriteTime));
        }
        finally
        {
            configurationSnapshot.Restore();
        }
    }

    [Test]
    public void AutomaticConfigurationPreservesBuildScenesAndAddsMainWhenAbsent()
    {
        var originalScenes = EditorBuildSettings.scenes;

        try
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Existing.unity", false),
            };

            InvokeEditorMethod("AlbaWorld.Editor.ProjectSetup", "EnsureProjectConfiguration");

            var configuredScenes = EditorBuildSettings.scenes;
            Assert.That(configuredScenes.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Scenes/Existing.unity",
                MainScenePath,
            }));
            Assert.That(configuredScenes[0].enabled, Is.False);
            Assert.That(configuredScenes[1].enabled, Is.True);
        }
        finally
        {
            EditorBuildSettings.scenes = originalScenes;
        }
    }

    [Test]
    public void AutomaticConfigurationEnablesMainWithoutReorderingBuildScenes()
    {
        var originalScenes = EditorBuildSettings.scenes;

        try
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Existing.unity", false),
                new EditorBuildSettingsScene(MainScenePath, false),
            };

            InvokeEditorMethod("AlbaWorld.Editor.ProjectSetup", "EnsureProjectConfiguration");

            var configuredScenes = EditorBuildSettings.scenes;
            Assert.That(configuredScenes.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Scenes/Existing.unity",
                MainScenePath,
            }));
            Assert.That(configuredScenes[0].enabled, Is.False);
            Assert.That(configuredScenes[1].enabled, Is.True);
        }
        finally
        {
            EditorBuildSettings.scenes = originalScenes;
        }
    }

    [Test]
    public void UrpConfigurationRepairsDeterministicValuesAndRenderer()
    {
        var pipeline = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/AlbaWorldURP.asset");
        var renderer = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/AlbaWorldRenderer.asset");
        Assert.That(pipeline, Is.Not.Null);
        Assert.That(renderer, Is.Not.Null);

        var serializedPipeline = new SerializedObject(pipeline);
        var supportsHdr = serializedPipeline.FindProperty("m_SupportsHDR");
        var msaa = serializedPipeline.FindProperty("m_MSAA");
        var shadowDistance = serializedPipeline.FindProperty("m_ShadowDistance");
        var renderScale = serializedPipeline.FindProperty("m_RenderScale");
        var renderers = serializedPipeline.FindProperty("m_RendererDataList");
        var defaultRendererIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");
        var originalHdr = supportsHdr.boolValue;
        var originalMsaa = msaa.intValue;
        var originalShadowDistance = shadowDistance.floatValue;
        var originalRenderScale = renderScale.floatValue;
        var originalDefaultRendererIndex = defaultRendererIndex.intValue;
        var originalRenderers = Enumerable.Range(0, renderers.arraySize)
            .Select(index => renderers.GetArrayElementAtIndex(index).objectReferenceValue)
            .ToArray();

        try
        {
            supportsHdr.boolValue = true;
            msaa.intValue = 8;
            shadowDistance.floatValue = 99f;
            renderScale.floatValue = 0.73f;
            renderers.arraySize = 2;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = null;
            renderers.GetArrayElementAtIndex(1).objectReferenceValue = renderer;
            defaultRendererIndex.intValue = 1;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

            InvokeEditorMethod("AlbaWorld.Editor.UrpProjectSetup", "Configure");

            serializedPipeline.Update();
            Assert.That(supportsHdr.boolValue, Is.False);
            Assert.That(msaa.intValue, Is.EqualTo(2));
            Assert.That(shadowDistance.floatValue, Is.EqualTo(20f));
            Assert.That(renderScale.floatValue, Is.EqualTo(0.73f).Within(0.001f));
            Assert.That(renderers.arraySize, Is.EqualTo(2));
            Assert.That(
                AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(0).objectReferenceValue),
                Is.EqualTo("Assets/Settings/AlbaWorldRenderer.asset"));
            Assert.That(renderers.GetArrayElementAtIndex(1).objectReferenceValue, Is.EqualTo(renderer));
            Assert.That(defaultRendererIndex.intValue, Is.Zero);
        }
        finally
        {
            serializedPipeline.Update();
            supportsHdr.boolValue = originalHdr;
            msaa.intValue = originalMsaa;
            shadowDistance.floatValue = originalShadowDistance;
            renderScale.floatValue = originalRenderScale;
            renderers.arraySize = originalRenderers.Length;
            for (var index = 0; index < originalRenderers.Length; index++)
                renderers.GetArrayElementAtIndex(index).objectReferenceValue = originalRenderers[index];
            defaultRendererIndex.intValue = originalDefaultRendererIndex;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }
    }

    [Test]
    public void MainSceneContainsTheRequiredThreeDimensionalFoundation()
    {
        var scene = SceneManager.GetSceneByPath(MainScenePath);
        var openedForTest = !scene.isLoaded;
        if (openedForTest)
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

        try
        {
            AssertRequiredFoundation(scene);
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void FoundationValidationAllowsFutureWorldContent()
    {
        var scene = EditorSceneManager.NewPreviewScene();

        try
        {
            CreateFoundationScene(scene);
            var worldRoot = scene.GetRootGameObjects().Single(root => root.name == "WorldRoot");
            var futureContent = new GameObject("Future Room Content");
            SceneManager.MoveGameObjectToScene(futureContent, scene);
            futureContent.transform.SetParent(worldRoot.transform, false);
            futureContent.AddComponent<MeshFilter>();
            futureContent.AddComponent<MeshRenderer>();
            futureContent.AddComponent<BoxCollider>();
            var localLight = futureContent.AddComponent<Light>();
            localLight.type = LightType.Point;

            AssertRequiredFoundation(scene);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void AssertRequiredFoundation(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var cameraObjects = roots.Where(root => root.name == "Main Camera").ToArray();
        var lightObjects = roots.Where(root => root.name == "Directional Light").ToArray();
        var volumeObjects = roots.Where(root => root.name == "Global Volume").ToArray();
        var worldRoots = roots.Where(root => root.name == "WorldRoot").ToArray();
        Assert.That(cameraObjects, Has.Length.EqualTo(1));
        Assert.That(lightObjects, Has.Length.EqualTo(1));
        Assert.That(volumeObjects, Has.Length.EqualTo(1));
        Assert.That(worldRoots, Has.Length.EqualTo(1));

        var camera = cameraObjects[0].GetComponent<Camera>();
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.CompareTag("MainCamera"), Is.True);
        Assert.That(camera.orthographic, Is.False);

        var directionalLight = lightObjects[0].GetComponent<Light>();
        Assert.That(directionalLight, Is.Not.Null);
        Assert.That(directionalLight.type, Is.EqualTo(LightType.Directional));

        var volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
        Assert.That(volumeType, Is.Not.Null);
        var volume = volumeObjects[0].GetComponent(volumeType);
        Assert.That(volume, Is.Not.Null);
        Assert.That((bool)volumeType.GetProperty("isGlobal").GetValue(volume), Is.True);
        var profile = (UnityEngine.Object)volumeType.GetField("sharedProfile").GetValue(volume);
        Assert.That(AssetDatabase.GetAssetPath(profile), Is.EqualTo("Assets/Settings/AlbaWorldPostProcess.asset"));

        foreach (var foundationObject in cameraObjects.Concat(lightObjects).Concat(volumeObjects).Concat(worldRoots))
        {
            Assert.That(foundationObject.GetComponent<MeshRenderer>(), Is.Null);
            Assert.That(foundationObject.GetComponent<MeshFilter>(), Is.Null);
            Assert.That(foundationObject.GetComponent<Collider>(), Is.Null);
        }
    }

    private static void CreateFoundationScene(Scene scene)
    {
        var cameraObject = CreateRootObject(scene, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>().orthographic = false;

        var lightObject = CreateRootObject(scene, "Directional Light");
        lightObject.AddComponent<Light>().type = LightType.Directional;

        var volumeObject = CreateRootObject(scene, "Global Volume");
        var volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
        Assert.That(volumeType, Is.Not.Null);
        var volume = volumeObject.AddComponent(volumeType);
        volumeType.GetProperty("isGlobal").SetValue(volume, true);
        volumeType.GetField("sharedProfile").SetValue(
            volume,
            AssetDatabase.LoadMainAssetAtPath("Assets/Settings/AlbaWorldPostProcess.asset"));

        CreateRootObject(scene, "WorldRoot");
    }

    private static GameObject CreateRootObject(Scene scene, string name)
    {
        var gameObject = new GameObject(name);
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        return gameObject;
    }

    private static void InvokeEditorMethod(string typeName, string methodName)
    {
        var type = Type.GetType($"{typeName}, Assembly-CSharp-Editor");
        Assert.That(type, Is.Not.Null, $"Editor type {typeName} must exist.");
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, $"{typeName}.{methodName} must be public and static.");
        method.Invoke(null, null);
    }

    private sealed class ProjectConfigurationSnapshot
    {
        private readonly EditorBuildSettingsScene[] buildScenes;
        private readonly RenderPipelineAsset defaultPipeline;
        private readonly RenderPipelineAsset qualityPipeline;
        private readonly UnityEngine.Object pipelineAsset;
        private readonly string pipelineJson;
        private readonly string companyName;
        private readonly string productName;
        private readonly string androidIdentifier;
        private readonly string standaloneIdentifier;
        private readonly AndroidSdkVersions minimumSdk;
        private readonly AndroidSdkVersions targetSdk;
        private readonly AndroidArchitecture architectures;
        private readonly ScriptingImplementation scriptingBackend;
        private readonly bool portrait;
        private readonly bool portraitUpsideDown;
        private readonly bool landscapeLeft;
        private readonly bool landscapeRight;

        private ProjectConfigurationSnapshot()
        {
            buildScenes = EditorBuildSettings.scenes
                .Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled))
                .ToArray();
            defaultPipeline = GraphicsSettings.defaultRenderPipeline;
            qualityPipeline = QualitySettings.renderPipeline;
            pipelineAsset = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/AlbaWorldURP.asset");
            pipelineJson = pipelineAsset == null ? null : EditorJsonUtility.ToJson(pipelineAsset);
            companyName = PlayerSettings.companyName;
            productName = PlayerSettings.productName;
            androidIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            standaloneIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone);
            minimumSdk = PlayerSettings.Android.minSdkVersion;
            targetSdk = PlayerSettings.Android.targetSdkVersion;
            architectures = PlayerSettings.Android.targetArchitectures;
            scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            portrait = PlayerSettings.allowedAutorotateToPortrait;
            portraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
        }

        public static ProjectConfigurationSnapshot Capture() => new();

        public void Restore()
        {
            EditorBuildSettings.scenes = buildScenes;
            if (pipelineAsset != null)
            {
                EditorJsonUtility.FromJsonOverwrite(pipelineJson, pipelineAsset);
                EditorUtility.SetDirty(pipelineAsset);
            }

            GraphicsSettings.defaultRenderPipeline = defaultPipeline;
            QualitySettings.renderPipeline = qualityPipeline;
            PlayerSettings.companyName = companyName;
            PlayerSettings.productName = productName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, androidIdentifier);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, standaloneIdentifier);
            PlayerSettings.Android.minSdkVersion = minimumSdk;
            PlayerSettings.Android.targetSdkVersion = targetSdk;
            PlayerSettings.Android.targetArchitectures = architectures;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, scriptingBackend);
            PlayerSettings.allowedAutorotateToPortrait = portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = portraitUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
            AssetDatabase.SaveAssets();
        }
    }
}
