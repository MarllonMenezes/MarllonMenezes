using System;
using System.Linq;

namespace AlbaWorld.Core;

[Serializable]
public sealed class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3() { }
    public SerializableVector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);
}

[Serializable]
public sealed class CharacterLoadoutData
{
    public string bodyId = "body.girl";
    public string skinId = "skin.cream";
    public string faceId = "face.sunny";
    public string hairId = "hair.sunny";
    public string outfitId = "outfit.pink";
    public string shoesId = "shoes.sun";
    public string[] accessoryIds = Array.Empty<string>();
}

[Serializable]
public sealed class PetLoadoutData
{
    public string petId = "pet.cat";
    public string colorId = "petcolor.sunny";
    public string[] accessoryIds = Array.Empty<string>();
}

[Serializable]
public sealed class PlayerWorldStateData
{
    public SerializableVector3 position = new(0f, 0f, 0f);
    public float yaw;
}

[Serializable]
public sealed class FurniturePlacementData
{
    public string instanceId = string.Empty;
    public string itemId = string.Empty;
    public SerializableVector3 position = new(0f, 0f, 0f);
    public SerializableVector3 scale = new(1f, 1f, 1f);
    public float yaw;
    public string supportInstanceId = string.Empty;
    public string supportPointId = string.Empty;
}

[Serializable]
public sealed class RoomLayoutData
{
    public string roomId = "room.sunny";
    public FurniturePlacementData[] placements = Array.Empty<FurniturePlacementData>();

    public void Normalize() => placements = (placements ?? Array.Empty<FurniturePlacementData>())
        .Where(p => p != null && !string.IsNullOrWhiteSpace(p.instanceId) && !string.IsNullOrWhiteSpace(p.itemId))
        .GroupBy(p => p.instanceId)
        .Select(group => group.First())
        .ToArray();
}
