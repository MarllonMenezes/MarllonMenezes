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
}

public static class SaveMigration
{
    public const int CurrentSchemaVersion = 2;

    public static GameSaveData Upgrade(GameSaveData? input)
    {
        var save = input ?? new GameSaveData();
        save.unlockedItemIds ??= Array.Empty<string>();
        save.roomJson ??= Array.Empty<string>();
        save.languageCode = string.IsNullOrWhiteSpace(save.languageCode) ? "pt-BR" : save.languageCode;
        save.selectedPetId = string.IsNullOrWhiteSpace(save.selectedPetId) ? "pet.cat" : save.selectedPetId;
        save.dailyRewardLimit = save.dailyRewardLimit <= 0 ? 2 : save.dailyRewardLimit;
        save.schemaVersion = CurrentSchemaVersion;
        save.unlockedItemIds = save.unlockedItemIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        return save;
    }
}
