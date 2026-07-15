#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AlbaWorld.Editor;

public static class UrpProjectSetup
{
    public const string PipelinePath = "Assets/Settings/AlbaWorldURP.asset";
    public const string RendererPath = "Assets/Settings/AlbaWorldRenderer.asset";
    public const string VolumeProfilePath = "Assets/Settings/AlbaWorldPostProcess.asset";

    public static void Configure()
    {
        EnsureFolder("Assets/Settings");

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        EnsureRenderer(pipeline, renderer);
        pipeline.name = "AlbaWorldURP";
        pipeline.shadowDistance = 20f;
        pipeline.supportsHDR = false;
        pipeline.msaaSampleCount = 2;
        EditorUtility.SetDirty(pipeline);

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;

        var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (volumeProfile == null)
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(volumeProfile, VolumeProfilePath);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureRenderer(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
    {
        var rendererDataList = pipeline.rendererDataList;
        var serializedPipeline = new SerializedObject(pipeline);
        var serializedRenderers = serializedPipeline.FindProperty("m_RendererDataList");
        var serializedDefaultRendererIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");
        if (rendererDataList.Length > 0 &&
            rendererDataList[0] == renderer &&
            serializedDefaultRendererIndex.intValue == 0)
            return;

        if (serializedRenderers.arraySize == 0)
            serializedRenderers.arraySize = 1;
        serializedRenderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serializedDefaultRendererIndex.intValue = 0;
        serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }
}
#endif
