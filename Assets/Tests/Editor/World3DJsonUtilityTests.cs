using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AlbaWorld.Tests;

public sealed class World3DJsonUtilityTests
{
    [Test]
    public void SchemaThreeSaveRoundTripsNestedFieldsPlacementsAndArrays()
    {
        var saveType = RequiredCoreType("GameSaveData");
        var characterType = RequiredCoreType("CharacterLoadoutData");
        var petType = RequiredCoreType("PetLoadoutData");
        var playerType = RequiredCoreType("PlayerWorldStateData");
        var vectorType = RequiredCoreType("SerializableVector3");
        var roomType = RequiredCoreType("RoomLayoutData");
        var placementType = RequiredCoreType("FurniturePlacementData");

        var save = Create(saveType);
        SetField(save, "schemaVersion", 3);
        SetField(save, "unlockedItemIds", new[] { "hair.cloud", "furniture.bed" });

        var character = Create(characterType);
        SetField(character, "bodyId", "body.girl");
        SetField(character, "hairId", "hair.cloud");
        SetField(character, "accessoryIds", new[] { "accessory.star", "accessory.bow" });
        SetField(save, "character", character);

        var pet = Create(petType);
        SetField(pet, "petId", "pet.dog");
        SetField(pet, "accessoryIds", new[] { "pet.bow" });
        SetField(save, "pet", pet);

        var player = Create(playerType);
        SetField(player, "position", Vector(vectorType, 1f, 2f, 3f));
        SetField(player, "yaw", 45f);
        SetField(save, "playerWorld", player);
        SetField(save, "activeRoomId", "room.sunny");

        var placement = Create(placementType);
        SetField(placement, "instanceId", "bed-1");
        SetField(placement, "itemId", "furniture.bed");
        SetField(placement, "position", Vector(vectorType, 4f, 5f, 6f));
        SetField(placement, "scale", Vector(vectorType, 2f, 1f, 1f));
        SetField(placement, "yaw", 90f);
        SetField(placement, "supportInstanceId", "rug-1");
        SetField(placement, "supportPointId", "top");

        var room = Create(roomType);
        SetField(room, "roomId", "room.sunny");
        SetField(room, "placements", ArrayOf(placementType, placement));
        SetField(save, "rooms3D", ArrayOf(roomType, room));

        var json = JsonUtility.ToJson(save);
        var restored = JsonUtility.FromJson(json, saveType);

        Assert.That(Field<int>(restored, "schemaVersion"), Is.EqualTo(3));
        Assert.That(Field<string[]>(restored, "unlockedItemIds"),
            Is.EqualTo(new[] { "hair.cloud", "furniture.bed" }));

        var restoredCharacter = Field<object>(restored, "character");
        Assert.That(Field<string>(restoredCharacter, "bodyId"), Is.EqualTo("body.girl"));
        Assert.That(Field<string>(restoredCharacter, "hairId"), Is.EqualTo("hair.cloud"));
        Assert.That(Field<string[]>(restoredCharacter, "accessoryIds"),
            Is.EqualTo(new[] { "accessory.star", "accessory.bow" }));

        var restoredPet = Field<object>(restored, "pet");
        Assert.That(Field<string>(restoredPet, "petId"), Is.EqualTo("pet.dog"));
        Assert.That(Field<string[]>(restoredPet, "accessoryIds"), Is.EqualTo(new[] { "pet.bow" }));

        var restoredPlayer = Field<object>(restored, "playerWorld");
        AssertVector(Field<object>(restoredPlayer, "position"), 1f, 2f, 3f);
        Assert.That(Field<float>(restoredPlayer, "yaw"), Is.EqualTo(45f));
        Assert.That(Field<string>(restored, "activeRoomId"), Is.EqualTo("room.sunny"));

        var restoredRooms = Field<Array>(restored, "rooms3D");
        Assert.That(restoredRooms, Has.Length.EqualTo(1));
        var restoredRoom = restoredRooms.GetValue(0);
        Assert.That(Field<string>(restoredRoom, "roomId"), Is.EqualTo("room.sunny"));
        var restoredPlacements = Field<Array>(restoredRoom, "placements");
        Assert.That(restoredPlacements, Has.Length.EqualTo(1));
        var restoredPlacement = restoredPlacements.GetValue(0);
        Assert.That(Field<string>(restoredPlacement, "instanceId"), Is.EqualTo("bed-1"));
        Assert.That(Field<string>(restoredPlacement, "itemId"), Is.EqualTo("furniture.bed"));
        AssertVector(Field<object>(restoredPlacement, "position"), 4f, 5f, 6f);
        AssertVector(Field<object>(restoredPlacement, "scale"), 2f, 1f, 1f);
        Assert.That(Field<float>(restoredPlacement, "yaw"), Is.EqualTo(90f));
        Assert.That(Field<string>(restoredPlacement, "supportInstanceId"), Is.EqualTo("rug-1"));
        Assert.That(Field<string>(restoredPlacement, "supportPointId"), Is.EqualTo("top"));
    }

    private static Type RequiredCoreType(string typeName)
    {
        var type = Type.GetType($"AlbaWorld.Core.{typeName}, AlbaWorld.Runtime") ??
                   Type.GetType($"AlbaWorld.Core.{typeName}, Assembly-CSharp");
        Assert.That(type, Is.Not.Null, $"Core type AlbaWorld.Core.{typeName} must exist.");
        return type;
    }

    private static object Create(Type type) => Activator.CreateInstance(type);

    private static object Vector(Type vectorType, float x, float y, float z)
    {
        var vector = Create(vectorType);
        SetField(vector, "x", x);
        SetField(vector, "y", y);
        SetField(vector, "z", z);
        return vector;
    }

    private static Array ArrayOf(Type elementType, object value)
    {
        var array = Array.CreateInstance(elementType, 1);
        array.SetValue(value, 0);
        return array;
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        var field = RequiredField(instance, fieldName);
        field.SetValue(instance, value);
    }

    private static T Field<T>(object instance, string fieldName) =>
        (T)RequiredField(instance, fieldName).GetValue(instance);

    private static FieldInfo RequiredField(object instance, string fieldName)
    {
        Assert.That(instance, Is.Not.Null);
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, $"{instance.GetType().Name}.{fieldName} must be a public field.");
        return field;
    }

    private static void AssertVector(object vector, float x, float y, float z)
    {
        Assert.That(Field<float>(vector, "x"), Is.EqualTo(x));
        Assert.That(Field<float>(vector, "y"), Is.EqualTo(y));
        Assert.That(Field<float>(vector, "z"), Is.EqualTo(z));
    }
}
