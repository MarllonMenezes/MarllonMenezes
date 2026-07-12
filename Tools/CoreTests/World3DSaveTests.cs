using System;
using System.Text.Json;
using AlbaWorld.Core;
using Xunit;

public sealed class World3DSaveTests
{
    [Fact]
    public void UpgradeMapsLegacySelectionsIntoThreeDimensionalLoadout()
    {
        var legacy = new GameSaveData
        {
            schemaVersion = 2,
            selectedSkinId = "skin.honey",
            selectedHairId = "hair.cloud",
            selectedOutfitId = "outfit.mint",
            selectedPetId = "pet.dog"
        };

        var upgraded = SaveMigration.Upgrade(legacy);

        Assert.Equal(3, upgraded.schemaVersion);
        Assert.Equal("body.girl", upgraded.character.bodyId);
        Assert.Equal("skin.honey", upgraded.character.skinId);
        Assert.Equal("hair.cloud", upgraded.character.hairId);
        Assert.Equal("outfit.mint", upgraded.character.outfitId);
        Assert.Equal("pet.dog", upgraded.pet.petId);
        Assert.Equal("room.sunny", upgraded.activeRoomId);
    }

    [Fact]
    public void UpgradeRemovesInvalidPlacementsWithoutDroppingTheRoom()
    {
        var save = new GameSaveData
        {
            rooms3D = new[]
            {
                new RoomLayoutData
                {
                    roomId = "room.sunny",
                    placements = new[]
                    {
                        new FurniturePlacementData { instanceId = "", itemId = "furniture.bed" },
                        new FurniturePlacementData { instanceId = "bed-1", itemId = "furniture.bed" }
                    }
                }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);
        Assert.Single(upgraded.rooms3D);
        Assert.Single(upgraded.rooms3D[0].placements);
        Assert.Equal("bed-1", upgraded.rooms3D[0].placements[0].instanceId);
    }

    [Fact]
    public void UpgradeNormalizesMalformedThreeDimensionalStateAndIsIdempotent()
    {
        var save = new GameSaveData
        {
            schemaVersion = 3,
            character = null!,
            pet = null!,
            playerWorld = null!,
            activeRoomId = null!,
            rooms3D = new RoomLayoutData[]
            {
                null!,
                new() { roomId = "", placements = null! },
                new() { roomId = "room.sunny", placements = null! },
                new() { roomId = "room.sunny" }
            }
        };

        var options = new JsonSerializerOptions { IncludeFields = true };
        var upgraded = SaveMigration.Upgrade(save);
        var firstUpgradeJson = JsonSerializer.Serialize(upgraded, options);
        var upgradedAgain = SaveMigration.Upgrade(upgraded);
        var secondUpgradeJson = JsonSerializer.Serialize(upgradedAgain, options);

        Assert.Same(upgraded, upgradedAgain);
        Assert.Equal(firstUpgradeJson, secondUpgradeJson);
        Assert.NotNull(upgraded.character);
        Assert.NotNull(upgraded.pet);
        Assert.NotNull(upgraded.playerWorld);
        Assert.Equal("room.sunny", upgraded.activeRoomId);
        Assert.Single(upgraded.rooms3D);
        Assert.Equal("room.sunny", upgraded.rooms3D[0].roomId);
        Assert.Empty(upgraded.rooms3D[0].placements);
    }

    [Fact]
    public void UpgradeMapsAllLegacySelectionsWithoutChangingProgressOrRewardClock()
    {
        var legacy = new GameSaveData
        {
            schemaVersion = 2,
            selectedSkinId = "skin.honey",
            selectedHairId = "hair.cloud",
            selectedOutfitId = "outfit.mint",
            selectedShoesId = "shoes.rainbow",
            selectedAccessoryId = "accessory.star",
            selectedPetId = "pet.dog",
            selectedPetAccessoryId = "pet.bow",
            unlockedItemIds = new[] { "hair.cloud", "hair.cloud", "outfit.mint" },
            lastRewardDate = "2099-12-31",
            rewardsUsedToday = 2
        };

        var upgraded = SaveMigration.Upgrade(legacy);

        Assert.Equal("shoes.rainbow", upgraded.character.shoesId);
        Assert.Equal(new[] { "accessory.star" }, upgraded.character.accessoryIds);
        Assert.Equal(new[] { "pet.bow" }, upgraded.pet.accessoryIds);
        Assert.Equal(new[] { "hair.cloud", "outfit.mint" }, upgraded.unlockedItemIds);
        Assert.Equal("2099-12-31", upgraded.lastRewardDate);
        Assert.Equal(2, upgraded.rewardsUsedToday);
    }

    [Fact]
    public void ThreeDimensionalSaveDataRoundTripsThroughFieldBasedJson()
    {
        var save = new GameSaveData
        {
            character = new CharacterLoadoutData { hairId = "hair.cloud" },
            playerWorld = new PlayerWorldStateData
            {
                position = new SerializableVector3(1f, 2f, 3f),
                yaw = 45f
            },
            rooms3D = new[]
            {
                new RoomLayoutData
                {
                    placements = new[]
                    {
                        new FurniturePlacementData
                        {
                            instanceId = "bed-1",
                            itemId = "furniture.bed",
                            scale = new SerializableVector3(2f, 1f, 1f)
                        }
                    }
                }
            }
        };
        var options = new JsonSerializerOptions { IncludeFields = true };

        var json = JsonSerializer.Serialize(save, options);
        var restored = JsonSerializer.Deserialize<GameSaveData>(json, options)!;

        Assert.Equal("hair.cloud", restored.character.hairId);
        Assert.Equal(3f, restored.playerWorld.position.z);
        Assert.Equal(45f, restored.playerWorld.yaw);
        Assert.Equal("bed-1", restored.rooms3D[0].placements[0].instanceId);
        Assert.Equal(2f, restored.rooms3D[0].placements[0].scale.x);
    }
}
