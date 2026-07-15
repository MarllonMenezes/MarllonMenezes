using System;
using System.Collections;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlbaWorld.Tests;

public sealed class PetPersistenceTests
{
    [Test]
    public void PetSelectionSurvivesSaveMigration()
    {
        var input = new GameSaveData { schemaVersion = SaveMigration.CurrentSchemaVersion };
        input.pet.petId = "pet.panda";

        var output = SaveMigration.Upgrade(input);

        Assert.That(output.pet.petId, Is.EqualTo("pet.panda"));
    }

    [UnityTest]
    public IEnumerator ValidSelectionSavesOnlyAfterAssemblyApply()
    {
        using var fixture = PetTestFactory.Create();
        var save = new GameSaveData();
        var service = new RecordingSaveService();
        var flow = new PetPersistenceCoordinator(save, service, fixture.Controller);

        Assert.That(flow.TrySelect("pet.panda"), Is.True);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.panda"));
        Assert.That(flow.ActivePetRoot, Is.SameAs(fixture.Controller.ActiveInstance));
        Assert.That(save.pet.petId, Is.EqualTo("pet.panda"));
        Assert.That(save.selectedPetId, Is.EqualTo("pet.panda"));
        Assert.That(service.SaveCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator FailedSelectionDoesNotSaveOrReplaceTheActivePet()
    {
        using var fixture = PetTestFactory.Create();
        var save = new GameSaveData();
        var service = new RecordingSaveService();
        var flow = new PetPersistenceCoordinator(save, service, fixture.Controller);

        Assert.That(flow.TrySelect("pet.dog"), Is.True);
        yield return null;
        var previous = fixture.Controller.ActiveInstance;
        service.Reset();

        Assert.That(flow.TrySelect("pet.missing"), Is.False);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.dog"));
        Assert.That(fixture.Controller.ActiveInstance, Is.SameAs(previous));
        Assert.That(save.pet.petId, Is.EqualTo("pet.dog"));
        Assert.That(service.SaveCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator RestoreUnknownPetFallsBackToCatAndKeepsInMemorySaveValid()
    {
        using var fixture = PetTestFactory.Create();
        var save = new GameSaveData
        {
            selectedPetId = "pet.missing",
            pet = new PetLoadoutData { petId = "pet.missing" }
        };
        var service = new RecordingSaveService();
        var flow = new PetPersistenceCoordinator(save, service, fixture.Controller);

        Assert.That(flow.Restore(), Is.True);
        yield return null;

        Assert.That(fixture.Controller.ActivePetId, Is.EqualTo("pet.cat"));
        Assert.That(flow.ActivePetRoot, Is.Not.Null);
        Assert.That(save.pet.petId, Is.EqualTo("pet.cat"));
        Assert.That(save.selectedPetId, Is.EqualTo("pet.cat"));
        Assert.That(service.SaveCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator HouseAndPhotoContextsReuseActivePetAndClampRoomPlacement()
    {
        using var fixture = PetTestFactory.Create();
        var save = new GameSaveData();
        var service = new RecordingSaveService();
        var flow = new PetPersistenceCoordinator(save, service, fixture.Controller);
        Assert.That(flow.TrySelect("pet.fox"), Is.True);
        yield return null;

        var room = new GameObject("RoomContext");
        var photo = new GameObject("PhotoContext");
        try
        {
            Assert.That(flow.TryPrepareHouse(room.transform, new Vector3(9f, 3f, -9f),
                new Vector3(-2f, 0f, -2f), new Vector3(2f, 1f, 2f)), Is.True);
            Assert.That(flow.ActivePetRoot!.transform.parent, Is.SameAs(room.transform));
            Assert.That(flow.ActivePetRoot.transform.localPosition, Is.EqualTo(new Vector3(2f, 1f, -2f)));

            Assert.That(flow.TryPreparePhoto(photo.transform, new Vector3(0.25f, 0.4f, 0.75f)), Is.True);
            Assert.That(flow.ActivePetRoot.transform.parent, Is.SameAs(photo.transform));
            Assert.That(flow.ActivePetRoot.transform.localPosition, Is.EqualTo(new Vector3(0.25f, 0.4f, 0.75f)));
        }
        finally
        {
            UnityEngine.Object.Destroy(room);
            UnityEngine.Object.Destroy(photo);
        }
    }

    private sealed class RecordingSaveService : ISaveService
    {
        public int SaveCount { get; private set; }

        public GameSaveData Load() => throw new NotSupportedException();

        public void Save(GameSaveData data) => SaveCount++;

        public void Reset() => SaveCount = 0;
    }
}
