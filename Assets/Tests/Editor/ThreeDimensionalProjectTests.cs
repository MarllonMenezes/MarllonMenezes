using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        var activeScene = SceneManager.GetActiveScene();
        var loadedSceneHandles = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(index => SceneManager.GetSceneAt(index).handle)
            .ToArray();
        var mainSceneContents = File.ReadAllBytes(MainScenePath);
        var mainSceneWriteTime = File.GetLastWriteTimeUtc(MainScenePath);

        InvokeEditorMethod("AlbaWorld.Editor.ProjectSetup", "EnsureProjectConfiguration");

        Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(activeScene.handle));
        Assert.That(
            Enumerable.Range(0, SceneManager.sceneCount).Select(index => SceneManager.GetSceneAt(index).handle),
            Is.EqualTo(loadedSceneHandles));
        CollectionAssert.AreEqual(mainSceneContents, File.ReadAllBytes(MainScenePath));
        Assert.That(File.GetLastWriteTimeUtc(MainScenePath), Is.EqualTo(mainSceneWriteTime));
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
        var renderers = serializedPipeline.FindProperty("m_RendererDataList");
        var originalHdr = supportsHdr.boolValue;
        var originalMsaa = msaa.intValue;
        var originalShadowDistance = shadowDistance.floatValue;
        var originalRenderer = renderers.GetArrayElementAtIndex(0).objectReferenceValue;

        try
        {
            supportsHdr.boolValue = true;
            msaa.intValue = 8;
            shadowDistance.floatValue = 99f;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = null;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

            InvokeEditorMethod("AlbaWorld.Editor.UrpProjectSetup", "Configure");

            serializedPipeline.Update();
            Assert.That(supportsHdr.boolValue, Is.False);
            Assert.That(msaa.intValue, Is.EqualTo(2));
            Assert.That(shadowDistance.floatValue, Is.EqualTo(20f));
            Assert.That(
                AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(0).objectReferenceValue),
                Is.EqualTo("Assets/Settings/AlbaWorldRenderer.asset"));
        }
        finally
        {
            serializedPipeline.Update();
            supportsHdr.boolValue = originalHdr;
            msaa.intValue = originalMsaa;
            shadowDistance.floatValue = originalShadowDistance;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = originalRenderer;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }
    }

    [Test]
    public void MainSceneContainsOnlyTheRequiredThreeDimensionalFoundation()
    {
        var scene = SceneManager.GetSceneByPath(MainScenePath);
        var openedForTest = !scene.isLoaded;
        if (openedForTest)
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

        try
        {
            var roots = scene.GetRootGameObjects();
            var components = roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var cameras = components.OfType<Camera>().ToArray();
            var directionalLights = components.OfType<Light>().Where(light => light.type == LightType.Directional).ToArray();
            var volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            Assert.That(volumeType, Is.Not.Null);
            var volumes = components.Where(component => volumeType.IsInstanceOfType(component)).ToArray();

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].CompareTag("MainCamera"), Is.True);
            Assert.That(cameras[0].orthographic, Is.False);
            Assert.That(directionalLights, Has.Length.EqualTo(1));
            Assert.That(components.OfType<Light>().Count(), Is.EqualTo(1));
            Assert.That(volumes, Has.Length.EqualTo(1));
            Assert.That((bool)volumeType.GetProperty("isGlobal").GetValue(volumes[0]), Is.True);
            var profile = (UnityEngine.Object)volumeType.GetField("sharedProfile").GetValue(volumes[0]);
            Assert.That(AssetDatabase.GetAssetPath(profile), Is.EqualTo("Assets/Settings/AlbaWorldPostProcess.asset"));
            Assert.That(roots.Count(root => root.name == "WorldRoot"), Is.EqualTo(1));
            Assert.That(components.OfType<MeshRenderer>(), Is.Empty);
            Assert.That(components.OfType<MeshFilter>(), Is.Empty);
            Assert.That(components.OfType<Collider>(), Is.Empty);
        }
        finally
        {
            if (openedForTest)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void InvokeEditorMethod(string typeName, string methodName)
    {
        var type = Type.GetType($"{typeName}, Assembly-CSharp-Editor");
        Assert.That(type, Is.Not.Null, $"Editor type {typeName} must exist.");
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, $"{typeName}.{methodName} must be public and static.");
        method.Invoke(null, null);
    }
}
