using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Game;
using NUnit.Framework;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class ItemCatalog3DTests
{
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

        Object.DestroyImmediate(girl);
        Assert.That(visual.PrefabForBody("body.girl"), Is.SameAs(fallback));
        Object.DestroyImmediate(fallback);
        Object.DestroyImmediate(boy);
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
            Assert.That(actual!.definition.free, Is.EqualTo(expected.free), expected.itemId);
            Assert.That(actual.definition.displayKey, Is.EqualTo(expected.displayKey), expected.itemId);
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
}
