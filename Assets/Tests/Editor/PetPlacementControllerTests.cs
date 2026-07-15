#if UNITY_EDITOR
using NUnit.Framework;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class PetPlacementControllerTests
{
    [Test]
    public void ManualPetPlacementDisablesFollowAndPersistsClampedPosition()
    {
        var root = new GameObject("Pet");
        var follow = root.AddComponent<PetFollowController>();
        var placement = root.AddComponent<PetPlacementController>();
        var save = new GameSaveData();
        var saveService = new MemorySaveService();
        try
        {
            placement.Initialize(root.transform, save, saveService, new Bounds(Vector3.zero, new Vector3(4f, 1f, 4f)), 0.2f, follow);

            Assert.That(placement.SetManualPosition(new Vector3(9f, 3f, -9f)), Is.True);
            Assert.That(save.pet.followCharacter, Is.False);
            Assert.That(save.pet.position.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(save.pet.position.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(save.pet.position.z, Is.EqualTo(-2f).Within(0.001f));
            Assert.That(follow.FollowEnabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private sealed class MemorySaveService : ISaveService
    {
        public GameSaveData Load() => new();
        public void Save(GameSaveData data) { }
    }
}
#endif
