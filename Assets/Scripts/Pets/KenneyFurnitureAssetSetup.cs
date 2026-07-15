#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Editor;

/// <summary>Imports the approved CC0 Furniture Kit subset into deterministic prefabs.</summary>
public static class KenneyFurnitureAssetSetup
{
    public static readonly string[] AllIds =
    {
        "furniture.bed",
        "furniture.sofa",
        "furniture.table",
        "furniture.chair",
        "furniture.shelf",
        "furniture.lamp",
        "furniture.plant",
        "furniture.rug",
        "furniture.book"
    };

    private const string SourceDirectory = "Assets/Art3D/Furniture/Source/KenneyFurnitureKit";
    private const string PrefabDirectory = "Assets/Art3D/Furniture/Prefabs";
    private const string MaterialPath = "Assets/Art3D/Furniture/Materials/KenneyFurniture.mat";

    private static readonly IReadOnlyDictionary<string, string> SourceFiles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["furniture.bed"] = "bedSingle.fbx",
            ["furniture.sofa"] = "loungeSofa.fbx",
            ["furniture.table"] = "table.fbx",
            ["furniture.chair"] = "chairCushion.fbx",
            ["furniture.shelf"] = "bookcaseOpen.fbx",
            ["furniture.lamp"] = "lampRoundFloor.fbx",
            ["furniture.plant"] = "pottedPlant.fbx",
            ["furniture.rug"] = "rugRound.fbx",
            ["furniture.book"] = "books.fbx"
        };

    private static readonly IReadOnlyDictionary<string, float> TargetSizes =
        new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["furniture.bed"] = 2.7f,
            ["furniture.sofa"] = 2.8f,
            ["furniture.table"] = 1.65f,
            ["furniture.chair"] = 1.1f,
            ["furniture.shelf"] = 2.1f,
            ["furniture.lamp"] = 1.8f,
            ["furniture.plant"] = 1.25f,
            ["furniture.rug"] = 2.7f,
            ["furniture.book"] = 0.55f
        };

    public static string PrefabPathFor(string itemId) =>
        $"{PrefabDirectory}/{itemId}.prefab";

    public static string SourcePathFor(string itemId) =>
        $"{SourceDirectory}/{SourceFiles[itemId]}";

    [MenuItem("Alba World/Setup Kenney Furniture")]
    public static void Setup()
    {
        Directory.CreateDirectory(PrefabDirectory);
        Directory.CreateDirectory("Assets/Art3D/Furniture/Materials");
        AssetDatabase.Refresh();

        var fallbackMaterial = GetOrCreateFallbackMaterial();
        foreach (var id in AllIds)
            CreatePrefab(id, fallbackMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Kenney Furniture setup complete: {AllIds.Length} prefabs.");
    }

    private static void CreatePrefab(string id, Material fallbackMaterial)
    {
        var sourcePath = SourcePathFor(id);
        AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceUpdate);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
            throw new InvalidOperationException($"Furniture source model missing: {sourcePath}");

        var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Could not instantiate furniture source: {sourcePath}");

        try
        {
            instance.name = id;
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (var light in instance.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light.gameObject);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.All(material => material == null))
                    renderer.sharedMaterial = fallbackMaterial;
            }

            NormalizeRoot(instance, id, renderers);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPathFor(id));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void NormalizeRoot(GameObject instance, string id, Renderer[] renderers)
    {
        var bounds = BoundsOf(renderers);
        if (bounds.size.sqrMagnitude <= 0.0001f)
            return;

        var target = TargetSizes[id];
        var largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        instance.transform.localScale = Vector3.one * (target / largest);

        bounds = BoundsOf(renderers);
        instance.transform.position -= new Vector3(0f, bounds.min.y, 0f);
    }

    private static Bounds BoundsOf(Renderer[] renderers)
    {
        var valid = renderers.Where(renderer => renderer != null).ToArray();
        if (valid.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        var bounds = valid[0].bounds;
        for (var index = 1; index < valid.Length; index++)
            bounds.Encapsulate(valid[index].bounds);
        return bounds;
    }

    private static Material GetOrCreateFallbackMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"))
        {
            name = "Kenney Furniture"
        };
        material.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}
#endif
