using System;
using System.Linq;
using System.Reflection;
using AlbaWorld.Catalog;
using AlbaWorld.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class ItemCatalog3DTests
{
    private const string VisualPath = "Assets/Resources/Data/Visuals/outfit.sun.asset";

    [Test]
    public void ValidationRejectsDuplicateIds()
    {
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(ItemVisual3D.TestOnly("hair.sunny"));
        catalog.items.Add(ItemVisual3D.TestOnly("hair.sunny"));

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);

        Assert.That(errors.Any(error => error.Contains("Duplicate item ID: hair.sunny")), Is.True);
    }

    [Test]
    public void ValidationReportsMissingDefinitionsAndBlankIds()
    {
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(ScriptableObject.CreateInstance<ItemVisual3D>());
        catalog.items.Add(ItemVisual3D.TestOnly("  "));

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);

        Assert.That(errors.Any(error => error.Contains("Missing definition")), Is.True);
        Assert.That(errors.Any(error => error.Contains("Blank item ID")), Is.True);
    }

    [Test]
    public void ValidationReportsInvalidPlacementRangesAndRotationSteps()
    {
        var invalidRange = ItemVisual3D.TestOnly("decor.range");
        invalidRange.placement.minimumScale = 2f;
        invalidRange.placement.maximumScale = 1f;
        var invalidRotation = ItemVisual3D.TestOnly("decor.rotation");
        invalidRotation.placement.rotationStep = 50f;
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(invalidRange);
        catalog.items.Add(invalidRotation);

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);

        Assert.That(errors.Any(error => error.Contains("Invalid scale range: decor.range")), Is.True);
        Assert.That(errors.Any(error => error.Contains("Invalid rotation step: decor.rotation")), Is.True);
    }

    [TestCase(359.99f)]
    [TestCase(360.01f)]
    [TestCase(45.0001f)]
    [TestCase(0.000011f)]
    [TestCase(float.Epsilon)]
    [TestCase(float.MaxValue)]
    public void ValidationRejectsNearAndVerySmallNonDivisorRotationSteps(float rotationStep)
    {
        var visual = ItemVisual3D.TestOnly("decor.rotation");
        visual.placement.rotationStep = rotationStep;
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(visual);

        var errors = CatalogValidation.Validate(catalog, requirePrefabs: false);

        Assert.That(errors.Any(error => error.Contains("Invalid rotation step: decor.rotation")), Is.True);
    }

    [TestCase(360f)]
    [TestCase(180f)]
    [TestCase(90f)]
    [TestCase(45f)]
    [TestCase(22.5f)]
    [TestCase(7.2f)]
    [TestCase(1f)]
    [TestCase(0.5f)]
    [TestCase(0.1f)]
    [TestCase(1e-27f)]
    [TestCase(1e-28f)]
    public void ValidationAcceptsInspectorRepresentableRotationDivisors(float rotationStep)
    {
        var visual = ItemVisual3D.TestOnly("decor.rotation");
        visual.placement.rotationStep = rotationStep;
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(visual);

        Assert.That(CatalogValidation.Validate(catalog, requirePrefabs: false), Is.Empty);
    }

    [Test]
    public void ValidationOnlyRequiresPrefabsInStrictMode()
    {
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(ItemVisual3D.TestOnly("hair.sunny"));

        Assert.That(CatalogValidation.Validate(catalog, requirePrefabs: false), Is.Empty);
        Assert.That(
            CatalogValidation.Validate(catalog, requirePrefabs: true)
                .Any(error => error.Contains("Missing prefab: hair.sunny")),
            Is.True);
    }

    [Test]
    public void CatalogLookupUsesFirstDuplicateWithoutSilentlyOverwritingIt()
    {
        var first = ItemVisual3D.TestOnly("hair.sunny");
        var second = ItemVisual3D.TestOnly("hair.sunny");
        var catalog = ScriptableObject.CreateInstance<ItemCatalog3D>();
        catalog.items.Add(first);
        catalog.items.Add(second);

        Assert.That(catalog.GetVisual("hair.sunny"), Is.SameAs(first));
    }

    [Test]
    public void PrefabForBodyUsesMatchingOverrideAndFallsBackToBasePrefab()
    {
        var visual = ItemVisual3D.TestOnly("outfit.sun");
        var fallback = new GameObject("fallback");
        var girl = new GameObject("girl");
        var boy = new GameObject("boy");
        visual.prefab = fallback;
        visual.girlPrefabOverride = girl;
        visual.boyPrefabOverride = boy;

        Assert.That(visual.PrefabForBody("body.girl"), Is.SameAs(girl));
        Assert.That(visual.PrefabForBody("body.boy"), Is.SameAs(boy));
        Assert.That(visual.PrefabForBody("body.unknown"), Is.SameAs(fallback));

        UnityEngine.Object.DestroyImmediate(girl);
        Assert.That(visual.PrefabForBody("body.girl"), Is.SameAs(fallback));
        UnityEngine.Object.DestroyImmediate(fallback);
        UnityEngine.Object.DestroyImmediate(boy);
    }

    [Test]
    public void GeneratedCatalogKeepsIdsFlagsAndTranslationKeys()
    {
        var runtime = new AlbaWorld.Runtime.RuntimeCatalog();
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");

        Assert.That(catalog, Is.Not.Null);
        foreach (var expected in runtime.All())
        {
            var actual = catalog.GetVisual(expected.itemId);
            Assert.That(actual, Is.Not.Null, expected.itemId);
            Assert.That(actual!.definition.itemId, Is.EqualTo(expected.itemId), expected.itemId);
            Assert.That(actual!.definition.free, Is.EqualTo(expected.free), expected.itemId);
            Assert.That(actual.definition.displayKey, Is.EqualTo(expected.displayKey), expected.itemId);
            Assert.That(actual.definition.tint, Is.EqualTo(expected.tint), expected.itemId);
            Assert.That(actual.definition.scale, Is.EqualTo(expected.scale), expected.itemId);
            Assert.That(actual.definition.layer, Is.EqualTo(expected.layer), expected.itemId);
        }
    }

    [Test]
    public void BuilderPreservesAssignedPrefabAndBodyOverrides()
    {
        var temporaryFolder = $"Assets/Tests/TempCatalogBuilder-{Guid.NewGuid():N}";
        var visual = AssetDatabase.LoadAssetAtPath<ItemVisual3D>(VisualPath);
        Assert.That(visual, Is.Not.Null);
        var originalPrefab = visual.prefab;
        var originalGirlOverride = visual.girlPrefabOverride;
        var originalBoyOverride = visual.boyPrefabOverride;

        AssetDatabase.CreateFolder("Assets/Tests", temporaryFolder[(temporaryFolder.LastIndexOf('/') + 1)..]);
        try
        {
            var prefab = CreateTemporaryPrefab(temporaryFolder, "base");
            var girlOverride = CreateTemporaryPrefab(temporaryFolder, "girl");
            var boyOverride = CreateTemporaryPrefab(temporaryFolder, "boy");
            visual.prefab = prefab;
            visual.girlPrefabOverride = girlOverride;
            visual.boyPrefabOverride = boyOverride;
            EditorUtility.SetDirty(visual);
            AssetDatabase.SaveAssets();

            InvokeCatalogBuilder();

            visual = AssetDatabase.LoadAssetAtPath<ItemVisual3D>(VisualPath);
            Assert.That(visual.prefab, Is.SameAs(prefab));
            Assert.That(visual.girlPrefabOverride, Is.SameAs(girlOverride));
            Assert.That(visual.boyPrefabOverride, Is.SameAs(boyOverride));
        }
        finally
        {
            visual = AssetDatabase.LoadAssetAtPath<ItemVisual3D>(VisualPath);
            if (visual != null)
            {
                visual.prefab = originalPrefab;
                visual.girlPrefabOverride = originalGirlOverride;
                visual.boyPrefabOverride = originalBoyOverride;
                EditorUtility.SetDirty(visual);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.DeleteAsset(temporaryFolder);
            AssetDatabase.Refresh();
        }
    }

    [Test]
    public void GeneratedCatalogKeepsFreeAndRewardedMinimums()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.items.Count(item => item.definition.free), Is.GreaterThanOrEqualTo(32));
        Assert.That(catalog.items.Count(item => !item.definition.free), Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void ItemCategoryKeepsExistingSerializedValuesAndAppends3dCategories()
    {
        Assert.That((int)ItemCategory.Decor, Is.EqualTo(8));
        Assert.That((int)ItemCategory.Body, Is.EqualTo(9));
        Assert.That((int)ItemCategory.Face, Is.EqualTo(10));
        Assert.That((int)ItemCategory.PetColor, Is.EqualTo(11));
    }

    private static GameObject CreateTemporaryPrefab(string folder, string name)
    {
        var source = new GameObject(name);
        try
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, $"{folder}/{name}.prefab");
            Assert.That(prefab, Is.Not.Null);
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static void InvokeCatalogBuilder()
    {
        var builderType = Type.GetType("AlbaWorld.Editor.AlbaCatalogBuilder, Assembly-CSharp-Editor");
        Assert.That(builderType, Is.Not.Null);
        var build = builderType!.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        Assert.That(build, Is.Not.Null);
        build!.Invoke(null, null);
    }
}
