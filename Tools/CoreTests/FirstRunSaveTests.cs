using AlbaWorld.Core;
using Xunit;

public sealed class FirstRunSaveTests
{
    [Fact]
    public void NewSaveStartsWithWelcomePending()
    {
        var save = SaveMigration.Upgrade(null);

        Assert.False(save.onboardingCompleted);
        Assert.Equal(5, save.schemaVersion);
    }

    [Fact]
    public void SchemaFourMigrationPreservesProgressAndIsIdempotent()
    {
        var old = new GameSaveData
        {
            schemaVersion = 4,
            onboardingCompleted = true,
            activeRoomId = "room.cozy",
            unlockedItemIds = new[] { "hair.sunny" }
        };

        var upgraded = SaveMigration.Upgrade(old);
        var again = SaveMigration.Upgrade(upgraded);

        Assert.True(upgraded.onboardingCompleted);
        Assert.Equal("room.cozy", upgraded.activeRoomId);
        Assert.Equal(new[] { "hair.sunny" }, upgraded.unlockedItemIds);
        Assert.Equal(5, again.schemaVersion);
        Assert.True(again.onboardingCompleted);
    }
}
