using System;
using System.IO;
using System.Linq;
using AlbaWorld.Catalog;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AlbaWorld.Editor;

public static class CartoonCityCharacterAssetSetup
{
    public const string PilotId = "cartooncity.char.01";
    public const string PilotSourceFile = "Character_1_2_2.fbx";
    public const string PilotModelPath = "Assets/Art3D/Characters/Source/RGPolyCartoonCity/FBX/Unity FBX/Character_1_2_2.fbx";
    public const string IdleAnimationPath = "Assets/Art3D/Characters/Source/RGPolyCartoonCity/FBX/Unity FBX/Animations/Idle_A.fbx";
    public const string PilotPrefabPath = "Assets/Art3D/Characters/Prefabs/CartoonCityChar01.prefab";
    public const string ControllerPath = "Assets/Art3D/Characters/Controllers/CartoonCityIdle.controller";
    public const string DefinitionPath = "Assets/Resources/Data/CharacterPresets/CartoonCityChar01.asset";
    public const string CatalogPath = "Assets/Resources/Data/CartoonCityCharacterPresets.asset";

    [MenuItem("Alba World/Build Cartoon City Pilot")]
    public static void BuildPilot()
    {
        RequireFile(PilotModelPath);
        RequireFile(IdleAnimationPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(PilotModelPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(IdleAnimationPath, ImportAssetOptions.ForceSynchronousImport);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(PilotModelPath);
        if (model == null)
            throw new InvalidOperationException($"Cartoon City pilot model did not import: {PilotModelPath}");

        var instance = UnityEngine.Object.Instantiate(model);
        try
        {
            instance.name = "CartoonCityChar01";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            RemoveSceneOnlyComponents(instance);

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.runtimeAnimatorController = BuildIdleController();

            EnsureAssetFolder(Path.GetDirectoryName(PilotPrefabPath)!.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PilotPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to create pilot prefab: {PilotPrefabPath}");

            var definition = LoadOrCreateDefinition();
            definition.presetId = PilotId;
            definition.displayKey = "character.preset.cartooncity.01";
            definition.sourceAsset = PilotSourceFile;
            definition.prefab = prefab;
            definition.free = true;
            definition.sortOrder = 10;
            definition.palettes = new[]
            {
                new CharacterPresetPalette
                {
                    paletteId = "default",
                    displayKey = "character.palette.default",
                    skinTint = Color.white,
                    hairTint = Color.white,
                    outfitTint = Color.white,
                    shoesTint = Color.white
                },
                new CharacterPresetPalette
                {
                    paletteId = "pastel",
                    displayKey = "character.palette.pastel",
                    skinTint = Color.white,
                    hairTint = Color.white,
                    outfitTint = new Color(1f, 0.55f, 0.75f, 1f),
                    shoesTint = Color.white
                }
            };
            definition.compatibleAccessoryIds = Array.Empty<string>();
            EditorUtility.SetDirty(definition);

            var catalog = LoadOrCreateCatalog();
            catalog.presets.RemoveAll(entry => entry == null || entry.presetId == PilotId);
            catalog.presets.Add(definition);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Built Cartoon City pilot {PilotId}: {PilotPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static CharacterPresetDefinition LoadOrCreateDefinition()
    {
        EnsureAssetFolder("Assets/Resources/Data/CharacterPresets");
        var definition = AssetDatabase.LoadAssetAtPath<CharacterPresetDefinition>(DefinitionPath);
        if (definition != null)
            return definition;
        if (File.Exists(DefinitionPath))
            AssetDatabase.DeleteAsset(DefinitionPath);
        definition = ScriptableObject.CreateInstance<CharacterPresetDefinition>();
        AssetDatabase.CreateAsset(definition, DefinitionPath);
        return definition;
    }

    private static CharacterPresetCatalog LoadOrCreateCatalog()
    {
        EnsureAssetFolder("Assets/Resources/Data");
        var catalog = AssetDatabase.LoadAssetAtPath<CharacterPresetCatalog>(CatalogPath);
        if (catalog != null)
            return catalog;
        if (File.Exists(CatalogPath))
            AssetDatabase.DeleteAsset(CatalogPath);
        catalog = ScriptableObject.CreateInstance<CharacterPresetCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        return catalog;
    }

    private static RuntimeAnimatorController BuildIdleController()
    {
        EnsureAssetFolder("Assets/Art3D/Characters/Controllers");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        var clip = AssetDatabase.LoadAllAssetsAtPath(IdleAnimationPath).OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
        if (clip == null)
            return controller;

        var machine = controller.layers[0].stateMachine;
        var idle = machine.states.FirstOrDefault(state => state.state != null && state.state.name == "Idle").state;
        if (idle == null)
            idle = machine.AddState("Idle");
        idle.motion = clip;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void RemoveSceneOnlyComponents(GameObject root)
    {
        foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(camera);
        foreach (var light in root.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(light);
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required Cartoon City source is missing: {path}", path);
    }

    private static void EnsureAssetFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(folder);
    }
}
