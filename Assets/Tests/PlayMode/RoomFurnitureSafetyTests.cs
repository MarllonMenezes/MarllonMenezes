#if UNITY_INCLUDE_TESTS
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

public sealed class RoomFurnitureSafetyTests
{
    private GameObject _root = null!;

    [UnityTest]
    public IEnumerator FurnitureCannotBeAddedInsideWalkableZone()
    {
        var controller = CreateControllerWithEmptyRoom();
        Assert.That(controller.TryAdd("furniture.bed", new Vector3(0f, 0.22f, 0f)), Is.False);
        Assert.That(controller.ActivePlacements, Is.Empty);
        yield return null;
        Object.Destroy(_root);
    }

    [UnityTest]
    public IEnumerator FurnitureCannotOverlapExistingFurniture()
    {
        var controller = CreateControllerWithEmptyRoom();
        Assert.That(controller.TryAdd("furniture.bed", new Vector3(-3.2f, 0.22f, 2.3f)), Is.True);
        var first = controller.SelectedInstanceId;
        var firstPlacement = controller.ActivePlacements.Single(item => item.instanceId == first);
        Assert.That(controller.TryAdd("furniture.sofa", new Vector3(2.6f, 0.22f, 2.3f)), Is.True);
        var second = controller.SelectedInstanceId;
        var beforeMove = controller.ActivePlacements.Single(item => item.instanceId == second);
        var overlap = new Vector3(firstPlacement.position.x, 0.22f, firstPlacement.position.z);

        Assert.That(controller.TryMove(second, overlap), Is.False);
        var afterMove = controller.ActivePlacements.Single(item => item.instanceId == second);
        Assert.That(afterMove.position.x, Is.EqualTo(beforeMove.position.x).Within(0.001f));
        Assert.That(afterMove.position.z, Is.EqualTo(beforeMove.position.z).Within(0.001f));
        yield return null;
        Object.Destroy(_root);
    }

    [UnityTest]
    public IEnumerator RemoveCanBeUndoneBeforeTimeout()
    {
        var controller = CreateControllerWithEmptyRoom();
        Assert.That(controller.TryAdd("furniture.chair", new Vector3(3.2f, 0.22f, -1.6f)), Is.True);
        var instanceId = controller.SelectedInstanceId;
        Assert.That(controller.TryRemove(instanceId), Is.True);
        Assert.That(controller.TryUndoRemove(), Is.True);
        Assert.That(controller.ActivePlacements.Any(item => item.instanceId == instanceId), Is.True);
        yield return null;
        Object.Destroy(_root);
    }

    private RoomFurnitureController CreateControllerWithEmptyRoom()
    {
        _root = new GameObject("FurnitureSafetyTestRoot");
        var catalog = Resources.Load<ItemCatalog3D>("Data/AlbaItemCatalog3D");
        Assert.That(catalog, Is.Not.Null);
        var controller = _root.AddComponent<RoomFurnitureController>();
        controller.Initialize(catalog!, _root.transform, new GameSaveData(), new MemorySaveService());
        return controller;
    }

    private sealed class MemorySaveService : ISaveService
    {
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) { }
    }
}
#endif
