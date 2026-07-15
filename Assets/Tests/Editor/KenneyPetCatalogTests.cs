using AlbaWorld.Catalog;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using NUnit.Framework;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class KenneyPetCatalogTests
{
    [Test]
    public void CatalogContainsEveryKenneyPetWithBothTranslations()
    {
        var catalog = LoadCatalogForTests();
        foreach (var id in KenneyPetIds.All)
        {
            var visual = catalog.GetVisual(id);
            Assert.That(visual, Is.Not.Null, id);
            Assert.That(visual!.definition.category, Is.EqualTo(ItemCategory.Pet), id);
            Assert.That(visual.equipmentSlot, Is.EqualTo(EquipmentSlot.Pet), id);
            Assert.That(visual.prefab, Is.Not.Null, id);
            Assert.That(LocalizationTestData.Has("pt-BR", visual.definition.displayKey), Is.True, id);
            Assert.That(LocalizationTestData.Has("en", visual.definition.displayKey), Is.True, id);
        }
    }

    private static ItemCatalog3D LoadCatalogForTests()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null, "Generated 3D catalog is missing.");
        return catalog!;
    }
}
