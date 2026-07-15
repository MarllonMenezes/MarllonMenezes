#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using AlbaWorld.Core;
using AlbaWorld.Catalog;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class CharacterPresetCatalogTests
{
    [Test]
    public void SaveSchemaAddsPresetIdWithoutRemovingLegacyBodyId()
    {
        var save = new GameSaveData { schemaVersion = 3 };
        save.character.bodyId = "body.girl";

        var upgraded = SaveMigration.Upgrade(save);

        Assert.That(SaveMigration.CurrentSchemaVersion, Is.GreaterThanOrEqualTo(4));
        Assert.That(upgraded.character.characterPresetId, Is.EqualTo("cartooncity.char.01"));
        Assert.That(upgraded.character.bodyId, Is.EqualTo("body.girl"));
    }

    [Test]
    public void UnknownPresetIdFallsBackToDefaultAndKeepsLegacySelection()
    {
        var save = new GameSaveData { schemaVersion = 4 };
        save.character.characterPresetId = string.Empty;
        save.character.bodyId = "body.boy";

        var upgraded = SaveMigration.Upgrade(save);

        Assert.That(upgraded.character.characterPresetId, Is.EqualTo("cartooncity.char.01"));
        Assert.That(upgraded.character.bodyId, Is.EqualTo("body.boy"));
    }

    [Test]
    public void CatalogResolvesStableIdsAndDoesNotInventUnknownPresets()
    {
        var catalog = CharacterPresetCatalog.TestOnly("cartooncity.char.01", "cartooncity.char.02");
        try
        {
            Assert.That(catalog.Get("cartooncity.char.01"), Is.Not.Null);
            Assert.That(catalog.Get("cartooncity.char.99"), Is.Null);
            Assert.That(catalog.All.Count(), Is.EqualTo(2));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
        }
    }
}
#endif
