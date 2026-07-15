#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using AlbaWorld.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AlbaWorld.Editor;

public static class AlbaCatalogBuilder
{
    private const string DataDirectory = "Assets/Resources/Data";
    private const string DefinitionDirectory = DataDirectory + "/Definitions";
    private const string VisualDirectory = DataDirectory + "/Visuals";
    private const string CatalogPath = DataDirectory + "/AlbaItemCatalog3D.asset";

    [MenuItem("Alba World/Build 3D Item Catalog")]
    public static void Build()
    {
        EnsureDirectories();

        var specs = BuildSpecs()
            .OrderBy(spec => spec.Id, StringComparer.Ordinal)
            .ToArray();
        var visuals = new List<ItemVisual3D>(specs.Length);
        foreach (var spec in specs)
        {
            var definition = CreateOrUpdateDefinition(spec);
            visuals.Add(CreateOrUpdateVisual(spec, definition));
        }

        var catalog = LoadOrCreate<ItemCatalog3D>(CatalogPath);
        catalog.items.Clear();
        catalog.items.AddRange(visuals);
        EditorUtility.SetDirty(catalog);

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);
        if (errors.Count != 0)
            throw new BuildFailedException(string.Join(Environment.NewLine, errors));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Built deterministic 3D item catalog with {visuals.Count} entries at {CatalogPath}.");
    }

    private static IEnumerable<DefinitionSpec> BuildSpecs()
    {
        var runtime = new RuntimeCatalog();
        foreach (var definition in runtime.All())
        {
            if (definition.category == ItemCategory.Pet)
                continue;

            yield return new DefinitionSpec(
                definition.itemId,
                definition.category,
                definition.displayKey,
                definition.free,
                definition.tint,
                definition.scale,
                definition.layer);
        }

        foreach (var id in KenneyPetIds.All)
        {
            var definition = runtime.Get(id);
            if (definition == null)
                throw new BuildFailedException($"Runtime catalog is missing Kenney pet definition: {id}");

            yield return new DefinitionSpec(
                definition.itemId,
                definition.category,
                definition.displayKey,
                definition.free,
                definition.tint,
                definition.scale,
                definition.layer);
        }

        yield return NewSpec("body.girl", ItemCategory.Body);
        yield return NewSpec("body.boy", ItemCategory.Body);
        yield return NewSpec("face.sunny", ItemCategory.Face);
        yield return NewSpec("face.sparkle", ItemCategory.Face);
        yield return NewSpec("face.calm", ItemCategory.Face);
        yield return NewSpec("face.happy", ItemCategory.Face);
        yield return NewSpec("petcolor.sunny", ItemCategory.PetColor);
        yield return NewSpec("petcolor.cocoa", ItemCategory.PetColor);
    }

    private static DefinitionSpec NewSpec(string id, ItemCategory category) =>
        new(id, category, id, true, Color.white, 1f, 0);

    private static ItemDefinition CreateOrUpdateDefinition(DefinitionSpec spec)
    {
        var definition = LoadOrCreate<ItemDefinition>($"{DefinitionDirectory}/{spec.Id}.asset");
        definition.name = spec.Id;
        definition.itemId = spec.Id;
        definition.category = spec.Category;
        definition.displayKey = spec.DisplayKey;
        definition.free = spec.Free;
        definition.tint = spec.Tint;
        definition.scale = spec.Scale;
        definition.layer = spec.Layer;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static ItemVisual3D CreateOrUpdateVisual(DefinitionSpec spec, ItemDefinition definition)
    {
        var visual = LoadOrCreate<ItemVisual3D>($"{VisualDirectory}/{spec.Id}.asset");
        visual.name = spec.Id;
        visual.definition = definition;
        visual.equipmentSlot = EquipmentSlotFor(spec.Id, spec.Category);
        visual.compatibleBodies = BodyCompatibility.Both;
        if (spec.Category == ItemCategory.Pet)
        {
            visual.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyPetAssetSetup.PrefabPathFor(spec.Id));
            if (visual.prefab == null)
                throw new BuildFailedException($"Missing Kenney pet prefab for {spec.Id}: {KenneyPetAssetSetup.PrefabPathFor(spec.Id)}");
        }
        else if (KenneyFurnitureAssetSetup.AllIds.Contains(spec.Id, StringComparer.Ordinal))
        {
            visual.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyFurnitureAssetSetup.PrefabPathFor(spec.Id));
            if (visual.prefab == null)
                throw new BuildFailedException($"Missing Kenney furniture prefab for {spec.Id}: {KenneyFurnitureAssetSetup.PrefabPathFor(spec.Id)}");
        }
        ApplyPlacementRules(visual, spec.Id, spec.Category);
        EditorUtility.SetDirty(visual);
        return visual;
    }

    private static EquipmentSlot EquipmentSlotFor(string id, ItemCategory category) => category switch
    {
        ItemCategory.Skin => EquipmentSlot.Body,
        ItemCategory.Body => EquipmentSlot.Body,
        ItemCategory.Face => EquipmentSlot.Face,
        ItemCategory.Hair => EquipmentSlot.Hair,
        ItemCategory.Outfit => EquipmentSlot.Outfit,
        ItemCategory.Shoes => EquipmentSlot.Shoes,
        ItemCategory.HumanAccessory when id == "accessory.glasses" => EquipmentSlot.FaceAccessory,
        ItemCategory.HumanAccessory => EquipmentSlot.Head,
        ItemCategory.Pet => EquipmentSlot.Pet,
        ItemCategory.PetColor => EquipmentSlot.Pet,
        ItemCategory.PetAccessory => EquipmentSlot.PetAccessory,
        _ => EquipmentSlot.None
    };

    private static void ApplyPlacementRules(ItemVisual3D visual, string id, ItemCategory category)
    {
        visual.placement ??= new PlacementRules();
        visual.placement.minimumScale = 0.8f;
        visual.placement.maximumScale = 1.2f;
        visual.placement.rotationStep = 45f;
        visual.placement.canHostSmallObjects = false;

        if (category == ItemCategory.Furniture)
        {
            visual.placement.kind = PlacementKind.Floor;
            visual.placement.canHostSmallObjects = id is "furniture.table" or "furniture.shelf";
        }
        else if (category == ItemCategory.Decor)
        {
            visual.placement.kind = id is "furniture.picture" or "furniture.clock"
                ? PlacementKind.Wall
                : id is "furniture.book" or "furniture.cushion" or "furniture.lamp" or "furniture.plant"
                    ? PlacementKind.Surface
                    : PlacementKind.Floor;
        }
        else
        {
            visual.placement.kind = PlacementKind.None;
        }
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            EnsureScriptReference(asset, path);
            return ReloadRequired<T>(path);
        }

        // A command-line build can encounter a stale asset whose script was imported in
        // the same refresh. Recreate that unusable file now that the MonoScript is known.
        if (File.Exists(path) && !AssetDatabase.DeleteAsset(path))
            throw new BuildFailedException($"Could not replace invalid catalog asset: {path}");

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        EnsureScriptReference(asset, path);
        return ReloadRequired<T>(path);
    }

    private static T ReloadRequired<T>(string path) where T : ScriptableObject
    {
        var reloaded = AssetDatabase.LoadAssetAtPath<T>(path);
        if (reloaded == null)
            throw new BuildFailedException($"Could not reload catalog asset: {path}");
        return reloaded;
    }

    private static void EnsureScriptReference(ScriptableObject asset, string path)
    {
        var scriptPath = asset switch
        {
            ItemDefinition => "Assets/Scripts/Core/ItemDefinition.cs",
            ItemVisual3D => "Assets/Scripts/Catalog/ItemVisual3D.cs",
            ItemCatalog3D => "Assets/Scripts/Catalog/ItemCatalog3D.cs",
            _ => string.Empty
        };
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null)
            throw new BuildFailedException($"Could not resolve MonoScript for catalog asset: {path}");

        var serializedAsset = new SerializedObject(asset);
        var scriptProperty = serializedAsset.FindProperty("m_Script");
        if (scriptProperty == null || scriptProperty.objectReferenceValue != null)
            return;

        scriptProperty.objectReferenceValue = script;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory(DefinitionDirectory);
        Directory.CreateDirectory(VisualDirectory);
        AssetDatabase.Refresh();
    }

    private readonly struct DefinitionSpec
    {
        public DefinitionSpec(
            string id,
            ItemCategory category,
            string displayKey,
            bool free,
            Color tint,
            float scale,
            int layer)
        {
            Id = id;
            Category = category;
            DisplayKey = displayKey;
            Free = free;
            Tint = tint;
            Scale = scale;
            Layer = layer;
        }

        public string Id { get; }
        public ItemCategory Category { get; }
        public string DisplayKey { get; }
        public bool Free { get; }
        public Color Tint { get; }
        public float Scale { get; }
        public int Layer { get; }
    }
}
#endif
