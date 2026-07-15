#if UNITY_EDITOR
using NUnit.Framework;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class CharacterPresetControllerTests
{
    [Test]
    public void SelectingPresetUpdatesSaveAndAppliesPaletteWithoutRebuildingObject()
    {
        var catalog = CharacterPresetCatalog.TestOnly("cartooncity.char.01", "cartooncity.char.02");
        var character = new GameObject("Character");
        var save = new GameSaveData();
        var saveService = new MemorySaveService();
        var controller = character.AddComponent<CharacterPresetController>();
        try
        {
            controller.Initialize(catalog, character.transform, save, saveService);

            Assert.That(controller.TrySelect("cartooncity.char.02"), Is.True);
            Assert.That(save.character.characterPresetId, Is.EqualTo("cartooncity.char.02"));
            Assert.That(controller.CurrentPresetId, Is.EqualTo("cartooncity.char.02"));
        }
        finally
        {
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(catalog);
        }
    }

    private sealed class MemorySaveService : ISaveService
    {
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) { }
    }
}
#endif
