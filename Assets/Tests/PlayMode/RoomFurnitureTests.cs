using System.Collections;
using System.Linq;
using AlbaWorld.Catalog;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class RoomFurnitureTests
{
    [UnityTest]
    public IEnumerator FurnitureMutationsStayBoundedAndPersist()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);

        var root = new GameObject("FurnitureTestRoot");
        var save = new GameSaveData();
        var saveService = new MemorySaveService();
        var controller = root.AddComponent<RoomFurnitureController>();
        controller.Initialize(catalog!, root.transform, save, saveService);

        Assert.That(controller.TryAdd("furniture.bed", new Vector3(999f, 0f, -999f)), Is.True);
        yield return null;

        var placement = controller.ActivePlacements.Single();
        Assert.That(placement.position.x, Is.InRange(-4.4f, 4.1f));
        Assert.That(placement.position.z, Is.InRange(-2.1f, 3.0f));
        Assert.That(controller.TryScale(controller.SelectedInstanceId, 0.1f), Is.True);
        Assert.That(controller.TryMirror(controller.SelectedInstanceId), Is.True);
        Assert.That(save.rooms3D.Single(room => room.roomId == "room.sunny").placements, Has.Length.EqualTo(1));
        controller.SetRoom("room.cozy");
        controller.SetRoom("room.sunny");
        yield return null;
        Assert.That(controller.ActivePlacements.Single().scale.x, Is.LessThan(0f));

        Object.Destroy(root);
    }

    [UnityTest]
    public IEnumerator EachRoomKeepsItsOwnFurnitureLayout()
    {
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);

        var root = new GameObject("FurnitureRoomIsolationTestRoot");
        var save = new GameSaveData();
        var saveService = new MemorySaveService();
        var controller = root.AddComponent<RoomFurnitureController>();
        controller.Initialize(catalog!, root.transform, save, saveService);

        Assert.That(controller.TryAdd("furniture.bed", new Vector3(-2f, 0f, 1f)), Is.True);
        controller.SetRoom("room.cozy");
        yield return null;
        Assert.That(controller.ActivePlacements, Is.Empty);

        Assert.That(controller.TryAdd("furniture.sofa", new Vector3(2f, 0f, 1f)), Is.True);
        controller.SetRoom("room.sunny");
        yield return null;
        Assert.That(controller.ActivePlacements.Single().itemId, Is.EqualTo("furniture.bed"));

        Object.Destroy(root);
    }

    private sealed class MemorySaveService : ISaveService
    {
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) { }
    }
}
