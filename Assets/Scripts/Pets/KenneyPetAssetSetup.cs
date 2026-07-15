#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlbaWorld.Pets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlbaWorld.Editor;

/// <summary>
/// Deterministically converts the staged Kenney FBX files into reusable pet prefabs.
/// This editor-only implementation lives in the runtime assembly so the editor test
/// assembly can exercise the real asset pipeline without referencing Assembly-CSharp-Editor.
/// </summary>
public static class KenneyPetAssetSetup
{
    private const string SourceRoot = "Assets/Art3D/Pets/Source/KenneyCubePets";
    private const string TexturePath = "Assets/Art3D/Pets/Textures/colormap.png";
    private const string MaterialRoot = "Assets/Art3D/Pets/Materials";
    private const string MaterialPath = MaterialRoot + "/KenneyPets.mat";
    private const string PrefabRoot = "Assets/Art3D/Pets/Prefabs";

    // The source package models are already authored at the desired size. Keeping the
    // values in one explicit table makes the import contract stable if a source FBX is
    // replaced later, and gives every animal a deterministic root transform/pivot.
    private static readonly Dictionary<string, PetImportRule> ImportRules = new(StringComparer.Ordinal)
    {
        ["pet.beaver"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.bee"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.bunny"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.cat"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.caterpillar"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.chick"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.cow"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.crab"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.deer"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.dog"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.elephant"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.fish"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.fox"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.giraffe"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.hog"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.koala"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.lion"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.monkey"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.panda"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.parrot"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.penguin"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.pig"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.polar"] = new PetImportRule(Vector3.one, Vector3.zero),
        ["pet.tiger"] = new PetImportRule(Vector3.one, Vector3.zero),
    };

    public static string PrefabPathFor(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId) || !animalId.StartsWith("pet.", StringComparison.Ordinal))
            throw new ArgumentException("Animal id must use the pet.* form.", nameof(animalId));

        return $"{PrefabRoot}/{animalId.Substring("pet.".Length)}.prefab";
    }

    [MenuItem("Alba World/Build Kenney Pet Prefabs")]
    public static void Setup()
    {
        var manifest = KenneySourceManifest.LoadForTests();
        if (!KenneyPetIds.All.All(id => manifest.AnimalIds.Contains(id, StringComparer.Ordinal)))
            throw new InvalidDataException("Kenney source manifest does not contain all authoritative pet IDs.");

        EnsureAssetFolder(MaterialRoot);
        EnsureAssetFolder(PrefabRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var material = CreateOrUpdateMaterial();
        foreach (var animalId in KenneyPetIds.All)
        {
            var sourcePath = SourcePathFor(animalId);
            ConfigureModelImporter(sourcePath);
            CreateOrUpdatePrefab(animalId, sourcePath, material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Built {KenneyPetIds.All.Length} deterministic Kenney pet prefabs.");
    }

    private static string SourcePathFor(string animalId)
    {
        var animalName = animalId.Substring("pet.".Length);
        return $"{SourceRoot}/animal-{animalName}.fbx";
    }

    private static void ConfigureModelImporter(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Missing Kenney pet source: {sourcePath}", sourcePath);

        var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter
            ?? throw new InvalidOperationException($"No ModelImporter found for {sourcePath}");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importConstraints = false;
        importer.preserveHierarchy = true;
        importer.useFileUnits = true;
        importer.globalScale = 1f;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.isReadable = false;
        importer.addCollider = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? throw new InvalidOperationException("Universal Render Pipeline/Simple Lit is unavailable.");
        var colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath)
            ?? throw new FileNotFoundException($"Kenney colormap did not import: {TexturePath}", TexturePath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "KenneyPets" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.name = "KenneyPets";
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_ZWrite", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.35f);
        material.SetTexture("_BaseMap", colormap);
        material.SetTexture("_MainTex", colormap);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.color = Color.white;
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateOrUpdatePrefab(string animalId, string sourcePath, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)
            ?? throw new InvalidOperationException($"Model did not import: {sourcePath}");
        if (!ImportRules.TryGetValue(animalId, out var rule))
            throw new InvalidOperationException($"No deterministic import rule exists for {animalId}.");

        var instance = UnityEngine.Object.Instantiate(model);
        try
        {
            instance.name = animalId.Substring("pet.".Length);
            instance.transform.localScale = rule.RootScale;
            instance.transform.localPosition = rule.Pivot;
            RemoveImportedCamerasAndLights(instance);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException($"No renderers imported from {sourcePath}");
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                    slots = new[] { material };
                else
                    for (var index = 0; index < slots.Length; index++)
                        slots[index] = material;
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            var prefabPath = PrefabPathFor(animalId);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Could not save pet prefab: {prefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void RemoveImportedCamerasAndLights(GameObject instance)
    {
        foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(camera);
        foreach (var light in instance.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(light);
    }

    private static void EnsureAssetFolder(string folder)
    {
        var normalized = folder.Replace('\\', '/');
        var parts = normalized.Split('/');
        var current = parts[0];
        for (var index = 1; index < parts.Length; index++)
        {
            var next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    private sealed class PetImportRule
    {
        public PetImportRule(Vector3 rootScale, Vector3 pivot)
        {
            RootScale = rootScale;
            Pivot = pivot;
        }

        public Vector3 RootScale { get; }
        public Vector3 Pivot { get; }
    }
}

/// <summary>Editor-only mesh metrics used by prefab validation tests.</summary>
public static class MeshMetrics
{
    public static int TriangleCount(GameObject prefab)
    {
        if (prefab == null)
            return 0;
        return prefab.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null)
            .Sum(filter => filter.sharedMesh.triangles.Length / 3);
    }
}
#endif
