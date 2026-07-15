#if UNITY_INCLUDE_TESTS
using System.Collections;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class CharacterMovementTests
{
    private GameObject _root = null!;

    [UnityTest]
    public IEnumerator DestinationNeverLeavesWalkableBounds()
    {
        var fixture = CreateMovementFixture();
        fixture.Controller.SetDestination(new Vector3(99f, 0f, 99f));
        yield return null;

        Assert.That(RoomFurnitureController.DefaultWalkableBounds.Contains(fixture.Controller.transform.localPosition), Is.True);
        Object.Destroy(_root);
    }

    [UnityTest]
    public IEnumerator ArrivalUpdatesPlayerWorldAndSaves()
    {
        var fixture = CreateMovementFixture();
        var destination = new Vector3(1.1f, 0.22f, 0.9f);
        fixture.Controller.SetDestination(destination);

        var timeout = 120;
        while (fixture.Controller.IsMoving && timeout-- > 0)
            yield return null;

        Assert.That(fixture.Controller.IsMoving, Is.False);
        Assert.That(fixture.Save.SaveCount, Is.GreaterThan(0));
        Assert.That(fixture.Save.Last.position.x, Is.EqualTo(destination.x).Within(0.01f));
        Assert.That(fixture.Save.Last.position.z, Is.EqualTo(destination.z).Within(0.01f));
        Object.Destroy(_root);
    }

    [UnityTest]
    public IEnumerator InputDisabledStopsDestinationChanges()
    {
        var fixture = CreateMovementFixture();
        var before = fixture.Controller.transform.localPosition;
        fixture.Controller.SetInputEnabled(false);
        fixture.Controller.SetDestination(new Vector3(1f, 0.22f, 1f));
        yield return null;

        Assert.That(fixture.Controller.transform.localPosition, Is.EqualTo(before));
        Assert.That(fixture.Controller.IsMoving, Is.False);
        Object.Destroy(_root);
    }

    private MovementFixture CreateMovementFixture()
    {
        _root = new GameObject("MovementTestRoot");
        var save = new GameSaveData();
        var service = new MemorySaveService();
        var controller = _root.AddComponent<CharacterMovementController>();
        controller.Initialize(_root.transform, save, service, RoomFurnitureController.DefaultWalkableBounds, 0.22f);
        return new MovementFixture(controller, service, save);
    }

    private sealed class MovementFixture
    {
        public MovementFixture(CharacterMovementController controller, MemorySaveService save, GameSaveData data)
        {
            Controller = controller;
            Save = save;
            Data = data;
        }

        public CharacterMovementController Controller { get; }
        public MemorySaveService Save { get; }
        public GameSaveData Data { get; }
    }

    private sealed class MemorySaveService : ISaveService
    {
        public int SaveCount { get; private set; }
        public PlayerWorldStateData Last { get; private set; } = new();

        public GameSaveData Load() => new();

        public void Save(GameSaveData data)
        {
            SaveCount++;
            Last = new PlayerWorldStateData
            {
                position = new SerializableVector3(data.playerWorld.position.x, data.playerWorld.position.y, data.playerWorld.position.z),
                yaw = data.playerWorld.yaw
            };
        }
    }
}
#endif
