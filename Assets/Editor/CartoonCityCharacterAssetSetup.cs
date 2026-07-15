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
        BuildPreset(PilotId, PilotSourceFile, PilotPrefabPath, DefinitionPath);
    }

    [MenuItem("Alba World/Build All Cartoon City Characters")]
    public static void BuildAll()
    {
        var files = new[]
        {
            "Character_1_2_2.fbx", "Character_2_1_3.fbx", "Character_3_2_3.fbx", "Character_4_1_1.fbx",
            "Character_5_2_3.fbx", "Character_5_3_1.fbx", "Character_6_2_2.fbx", "Character_8_3_1.fbx",
            "Character_9_3_4.fbx", "Character_9_5_7.fbx", "Character_10_4_3.fbx", "Character_11_3_1.fbx",
            "Character_B_1.fbx", "Character_Z_4.fbx", "Character_Z_9.fbx", "PoliceMan_A_4_1.fbx"
        };
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (var index = 0; index < files.Length; index++)
        {
            var id = $"cartooncity.char.{index + 1:00}";
            BuildPreset(id, files[index], PrefabPathFor(index + 1), DefinitionPathFor(index + 1));
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Built {files.Length} free Cartoon City character presets.");
    }

    private static void BuildPreset(string presetId, string sourceFile, string prefabPath, string definitionPath)
    {
        var modelPath = $"Assets/Art3D/Characters/Source/RGPolyCartoonCity/FBX/Unity FBX/{sourceFile}";
        RequireFile(modelPath);
        RequireFile(IdleAnimationPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(IdleAnimationPath, ImportAssetOptions.ForceSynchronousImport);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
            throw new InvalidOperationException($"Cartoon City model did not import: {modelPath}");

        var instance = UnityEngine.Object.Instantiate(model);
        try
        {
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);
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

            EnsureAssetFolder(Path.GetDirectoryName(prefabPath)!.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to create Cartoon City prefab: {prefabPath}");

            var definition = LoadOrCreateDefinition(definitionPath);
            definition.presetId = presetId;
            definition.displayKey = $"character.preset.cartooncity.{presetId.Substring("cartooncity.char.".Length)}";
            definition.sourceAsset = sourceFile;
            definition.prefab = prefab;
            definition.free = true;
            definition.sortOrder = int.Parse(presetId[^2..]);
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
            catalog.presets.RemoveAll(entry => entry == null || entry.presetId == presetId);
            catalog.presets.Add(definition);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Built Cartoon City preset {presetId}: {prefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static CharacterPresetDefinition LoadOrCreateDefinition(string path)
    {
        EnsureAssetFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));
        var definition = AssetDatabase.LoadAssetAtPath<CharacterPresetDefinition>(path);
        if (definition != null)
            return definition;
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
        definition = ScriptableObject.CreateInstance<CharacterPresetDefinition>();
        AssetDatabase.CreateAsset(definition, path);
        return definition;
    }

    public static string PrefabPathFor(int index) => $"Assets/Art3D/Characters/Prefabs/CartoonCityChar{index:00}.prefab";
    public static string DefinitionPathFor(int index) => $"Assets/Resources/Data/CharacterPresets/CartoonCityChar{index:00}.asset";

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
