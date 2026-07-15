using System;
using System.Linq;

namespace AlbaWorld.Core;

/// <summary>JSON-compatible state. Keep fields public for Unity JsonUtility.</summary>
[Serializable]
public sealed class GameSaveData
{
    public int schemaVersion = SaveMigration.CurrentSchemaVersion;
    public string languageCode = "pt-BR";
    public string selectedSkinId = "skin.cream";
    public string selectedHairId = "hair.sunny";
    public string selectedOutfitId = "outfit.pink";
    public string selectedShoesId = "shoes.sun";
    public string selectedAccessoryId = "accessory.star";
    public string selectedPetId = "pet.cat";
    public string selectedPetAccessoryId = "pet.bow";
    public string[] unlockedItemIds = Array.Empty<string>();
    public string[] roomJson = Array.Empty<string>();
    public string lastRewardDate = string.Empty;
    public int rewardsUsedToday;
    public int dailyRewardLimit = 2;
    public CharacterLoadoutData character = new();
    public PetLoadoutData pet = new();
    public PlayerWorldStateData playerWorld = new();
    public string activeRoomId = "room.sunny";
    public RoomLayoutData[] rooms3D = Array.Empty<RoomLayoutData>();
}

public static class SaveMigration
{
    public const int CurrentSchemaVersion = 3;

    public static GameSaveData Upgrade(GameSaveData? input)
    {
        var save = input ?? new GameSaveData();
        var sourceSchemaVersion = save.schemaVersion;
        var migrateLegacySelections = sourceSchemaVersion < 3;
        save.unlockedItemIds ??= Array.Empty<string>();
        save.roomJson ??= Array.Empty<string>();
        save.languageCode = string.IsNullOrWhiteSpace(save.languageCode) ? "pt-BR" : save.languageCode;
        save.selectedSkinId = DefaultIfBlank(save.selectedSkinId, "skin.cream");
        save.selectedHairId = DefaultIfBlank(save.selectedHairId, "hair.sunny");
        save.selectedOutfitId = DefaultIfBlank(save.selectedOutfitId, "outfit.pink");
        save.selectedShoesId = DefaultIfBlank(save.selectedShoesId, "shoes.sun");
        save.selectedAccessoryId = DefaultIfBlank(save.selectedAccessoryId, "accessory.star");
        save.selectedPetId = string.IsNullOrWhiteSpace(save.selectedPetId) ? "pet.cat" : save.selectedPetId;
        save.selectedPetAccessoryId = DefaultIfBlank(save.selectedPetAccessoryId, "pet.bow");
        save.dailyRewardLimit = save.dailyRewardLimit <= 0 ? 2 : save.dailyRewardLimit;
        save.unlockedItemIds = save.unlockedItemIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();

        save.character ??= new CharacterLoadoutData();
        save.pet ??= new PetLoadoutData();
        save.playerWorld ??= new PlayerWorldStateData();

        // Early schema-3 writers persisted only the legacy 2D mirror. Their newly
        // introduced pet field is indistinguishable from the default loadout, so retain
        // a non-cat selectedPetId in that narrow compatibility shape. A genuinely
        // populated pet loadout remains authoritative.
        if (sourceSchemaVersion == CurrentSchemaVersion &&
            IsDefaultPetLoadout(save.pet) &&
            !string.Equals(save.selectedPetId, "pet.cat", StringComparison.Ordinal))
        {
            save.pet.petId = save.selectedPetId;
        }

        if (migrateLegacySelections)
        {
            save.character.skinId = save.selectedSkinId;
            save.character.hairId = save.selectedHairId;
            save.character.outfitId = save.selectedOutfitId;
            save.character.shoesId = save.selectedShoesId;
            save.character.accessoryIds = new[] { save.selectedAccessoryId };
            save.pet.petId = save.selectedPetId;
            save.pet.accessoryIds = new[] { save.selectedPetAccessoryId };
        }

        NormalizeLoadouts(save);
        save.playerWorld.position ??= new SerializableVector3(0f, 0f, 0f);
        save.activeRoomId = DefaultIfBlank(save.activeRoomId, "room.sunny");
        save.rooms3D = (save.rooms3D ?? Array.Empty<RoomLayoutData>())
            .Where(room => room != null && !string.IsNullOrWhiteSpace(room.roomId))
            .GroupBy(room => room.roomId)
            .Select(group => group.First())
            .ToArray();
        foreach (var room in save.rooms3D)
        {
            room.Normalize();
            foreach (var placement in room.placements)
            {
                placement.position ??= new SerializableVector3(0f, 0f, 0f);
                placement.scale ??= new SerializableVector3(1f, 1f, 1f);
                placement.supportInstanceId ??= string.Empty;
                placement.supportPointId ??= string.Empty;
            }
        }

        save.schemaVersion = CurrentSchemaVersion;
        return save;
    }

    private static void NormalizeLoadouts(GameSaveData save)
    {
        save.character.bodyId = DefaultIfBlank(save.character.bodyId, "body.girl");
        save.character.skinId = DefaultIfBlank(save.character.skinId, "skin.cream");
        save.character.faceId = DefaultIfBlank(save.character.faceId, "face.sunny");
        save.character.hairId = DefaultIfBlank(save.character.hairId, "hair.sunny");
        save.character.outfitId = DefaultIfBlank(save.character.outfitId, "outfit.pink");
        save.character.shoesId = DefaultIfBlank(save.character.shoesId, "shoes.sun");
        save.character.accessoryIds = NormalizeIds(save.character.accessoryIds);
        save.pet.petId = DefaultIfBlank(save.pet.petId, "pet.cat");
        save.pet.colorId = DefaultIfBlank(save.pet.colorId, "petcolor.sunny");
        save.pet.accessoryIds = NormalizeIds(save.pet.accessoryIds);
    }

    private static string DefaultIfBlank(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static bool IsDefaultPetLoadout(PetLoadoutData pet) =>
        (string.IsNullOrWhiteSpace(pet.petId) || string.Equals(pet.petId, "pet.cat", StringComparison.Ordinal)) &&
        (string.IsNullOrWhiteSpace(pet.colorId) || string.Equals(pet.colorId, "petcolor.sunny", StringComparison.Ordinal)) &&
        (pet.accessoryIds == null || pet.accessoryIds.Length == 0);

    private static string[] NormalizeIds(string[] ids) => (ids ?? Array.Empty<string>())
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct()
        .ToArray();
}
