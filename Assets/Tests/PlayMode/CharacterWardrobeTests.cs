#if UNITY_INCLUDE_TESTS
using System.Collections;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class CharacterWardrobeTests
{
    private GameObject _character = null!;
    private GameObject _root = null!;

    [UnityTest]
    public IEnumerator ApplyingHairUpdatesCharacterLoadoutAndSaves()
    {
        var save = new GameSaveData();
        var fixture = CreateWardrobe(save);

        Assert.That(fixture.Controller.TryApply("hair.cloud"), Is.True);
        Assert.That(save.character.hairId, Is.EqualTo("hair.cloud"));
        Assert.That(fixture.Save.SaveCount, Is.GreaterThan(0));
        yield return null;
        Teardown();
    }

    [UnityTest]
    public IEnumerator LockedOrUnknownItemIsRejected()
    {
        var fixture = CreateWardrobe(new GameSaveData());

        Assert.That(fixture.Controller.TryApply("hair.unknown"), Is.False);
        Assert.That(fixture.Controller.TryApply("hair.mint"), Is.False);
        Assert.That(fixture.Save.SaveCount, Is.EqualTo(0));
        yield return null;
        Teardown();
    }

    [UnityTest]
    public IEnumerator ApplyingAccessoryReplacesTheSavedAccessorySlot()
    {
        var save = new GameSaveData();
        var fixture = CreateWardrobe(save);

        Assert.That(fixture.Controller.TryApply("accessory.flower"), Is.True);
        Assert.That(save.character.accessoryIds, Is.EqualTo(new[] { "accessory.flower" }));
        Assert.That(save.selectedAccessoryId, Is.EqualTo("accessory.flower"));
        yield return null;
        Teardown();
    }

    private WardrobeFixture CreateWardrobe(GameSaveData save)
    {
        _root = new GameObject("WardrobeTestRoot");
        var prefab = Resources.Load<GameObject>("Characters/BodyGirl");
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(catalog, Is.Not.Null);
        _character = Object.Instantiate(prefab!, _root.transform, false);
        var service = new MemorySaveService();
        var controller = _root.AddComponent<CharacterWardrobeController>();
        controller.Initialize(catalog!, _character.transform, save, service);
        return new WardrobeFixture(controller, service);
    }

    private void Teardown()
    {
        if (_root != null)
            Object.Destroy(_root);
    }

    private readonly struct WardrobeFixture
    {
        public WardrobeFixture(CharacterWardrobeController controller, MemorySaveService save)
        {
            Controller = controller;
            Save = save;
        }

        public CharacterWardrobeController Controller { get; }
        public MemorySaveService Save { get; }
    }

    private sealed class MemorySaveService : ISaveService
    {
        public int SaveCount { get; private set; }
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) => SaveCount++;
    }
}
#endif
