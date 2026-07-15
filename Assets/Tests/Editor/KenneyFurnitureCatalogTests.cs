#if UNITY_EDITOR
using AlbaWorld.Catalog;
using AlbaWorld.Editor;
using AlbaWorld.Game;
using NUnit.Framework;
using UnityEditor;

namespace AlbaWorld.Tests;

public sealed class KenneyFurnitureCatalogTests
{
    [Test]
    public void CatalogLinksEveryApprovedFurniturePrefab()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog3D>("Assets/Resources/Data/AlbaItemCatalog3D.asset");
        Assert.That(catalog, Is.Not.Null);

        foreach (var id in KenneyFurnitureAssetSetup.AllIds)
        {
            var visual = catalog!.GetVisual(id);
            Assert.That(visual, Is.Not.Null, id);
            Assert.That(visual!.definition.category, Is.EqualTo(ItemCategory.Furniture).Or.EqualTo(ItemCategory.Decor), id);
            Assert.That(visual.prefab, Is.Not.Null, id);
            Assert.That(visual.prefab, Is.EqualTo(AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(KenneyFurnitureAssetSetup.PrefabPathFor(id))), id);
        }
    }
}
#endif
