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
    public void UpgradeKeepsFirstValidPlacementPayloadForDuplicateInstanceIds()
    {
        var save = new GameSaveData
        {
            rooms3D = new[]
            {
                new RoomLayoutData
                {
                    placements = new[]
                    {
                        new FurniturePlacementData
                        {
                            instanceId = "shared-1",
                            itemId = "furniture.bed",
                            position = new SerializableVector3(1f, 2f, 3f)
                        },
                        new FurniturePlacementData
                        {
                            instanceId = "shared-1",
                            itemId = "furniture.desk",
                            position = new SerializableVector3(9f, 8f, 7f)
                        }
                    }
                }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);

        var placement = Assert.Single(Assert.Single(upgraded.rooms3D).placements);
        Assert.Equal("furniture.bed", placement.itemId);
        Assert.Equal(1f, placement.position.x);
        Assert.Equal(2f, placement.position.y);
        Assert.Equal(3f, placement.position.z);
    }

    [Fact]
    public void UpgradeKeepsFirstRoomPayloadForDuplicateRoomIds()
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
                        new FurniturePlacementData { instanceId = "bed-1", itemId = "furniture.bed" }
                    }
                },
                new RoomLayoutData
                {
                    roomId = "room.sunny",
                    placements = new[]
                    {
                        new FurniturePlacementData { instanceId = "desk-1", itemId = "furniture.desk" }
                    }
                }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);

        var room = Assert.Single(upgraded.rooms3D);
        var placement = Assert.Single(room.placements);
        Assert.Equal("bed-1", placement.instanceId);
        Assert.Equal("furniture.bed", placement.itemId);
    }

    [Fact]
    public void UpgradePreservesCurrentSchemaThreeLoadoutOverConflictingLegacySelections()
    {
        var save = new GameSaveData
        {
            schemaVersion = 3,
            selectedSkinId = "skin.legacy",
            selectedHairId = "hair.legacy",
            selectedOutfitId = "outfit.legacy",
            selectedShoesId = "shoes.legacy",
            selectedAccessoryId = "accessory.legacy",
            selectedPetId = "pet.legacy",
            selectedPetAccessoryId = "petaccessory.legacy",
            character = new CharacterLoadoutData
            {
                bodyId = "body.current",
                skinId = "skin.current",
                faceId = "face.current",
                hairId = "hair.current",
                outfitId = "outfit.current",
                shoesId = "shoes.current",
                accessoryIds = new[] { "accessory.current", "accessory.current-2" }
            },
            pet = new PetLoadoutData
            {
                petId = "pet.current",
                colorId = "petcolor.current",
                accessoryIds = new[] { "petaccessory.current" }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);

        Assert.Equal("body.current", upgraded.character.bodyId);
        Assert.Equal("skin.current", upgraded.character.skinId);
        Assert.Equal("face.current", upgraded.character.faceId);
        Assert.Equal("hair.current", upgraded.character.hairId);
        Assert.Equal("outfit.current", upgraded.character.outfitId);
        Assert.Equal("shoes.current", upgraded.character.shoesId);
        Assert.Equal(new[] { "accessory.current", "accessory.current-2" }, upgraded.character.accessoryIds);
        Assert.Equal("pet.current", upgraded.pet.petId);
        Assert.Equal("petcolor.current", upgraded.pet.colorId);
        Assert.Equal(new[] { "petaccessory.current" }, upgraded.pet.accessoryIds);
    }

    [Fact]
    public void UpgradeRemovesNullPlacementAndNormalizesValidPlacementPayload()
    {
        var save = new GameSaveData
        {
            rooms3D = new[]
            {
                new RoomLayoutData
                {
                    placements = new FurniturePlacementData[]
                    {
                        null!,
                        new()
                        {
                            instanceId = "bed-1",
                            itemId = "furniture.bed",
                            position = null!,
                            scale = null!,
                            supportInstanceId = null!,
                            supportPointId = null!
                        }
                    }
                }
            }
        };

        var upgraded = SaveMigration.Upgrade(save);

        var placement = Assert.Single(Assert.Single(upgraded.rooms3D).placements);
        Assert.NotNull(placement.position);
        Assert.Equal((0f, 0f, 0f), (placement.position.x, placement.position.y, placement.position.z));
        Assert.NotNull(placement.scale);
        Assert.Equal((1f, 1f, 1f), (placement.scale.x, placement.scale.y, placement.scale.z));
        Assert.Equal(string.Empty, placement.supportInstanceId);
        Assert.Equal(string.Empty, placement.supportPointId);
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
            rewardsUsedToday = 2,
            dailyRewardLimit = 7
        };

        var upgraded = SaveMigration.Upgrade(legacy);

        Assert.Equal("shoes.rainbow", upgraded.character.shoesId);
        Assert.Equal(new[] { "accessory.star" }, upgraded.character.accessoryIds);
        Assert.Equal(new[] { "pet.bow" }, upgraded.pet.accessoryIds);
        Assert.Equal(new[] { "hair.cloud", "outfit.mint" }, upgraded.unlockedItemIds);
        Assert.Equal("2099-12-31", upgraded.lastRewardDate);
        Assert.Equal(2, upgraded.rewardsUsedToday);
        Assert.Equal(7, upgraded.dailyRewardLimit);
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
