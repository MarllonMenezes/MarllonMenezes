using System;
using AlbaWorld.Core;
using Xunit;

namespace AlbaWorld.CoreTests;

public sealed class RewardLimiterTests
{
    [Fact]
    public void AllowsTwoRewardsOnFirstDayAndRejectsThird()
    {
        var limiter = new DailyRewardLimiter(maxPerDay: 2);
        var day = new DateTime(2026, 7, 12);

        Assert.True(limiter.TryConsume(day));
        Assert.True(limiter.TryConsume(day));
        Assert.False(limiter.TryConsume(day));
        Assert.Equal(0, limiter.Remaining(day));
    }

    [Fact]
    public void ClockRollbackDoesNotResetRewardCount()
    {
        var limiter = new DailyRewardLimiter(maxPerDay: 2, lastRewardDate: new DateTime(2026, 7, 12), usedToday: 2);

        Assert.False(limiter.TryConsume(new DateTime(2026, 7, 11)));
        Assert.Equal(0, limiter.Remaining(new DateTime(2026, 7, 11)));
    }

    [Fact]
    public void ALaterDateResetsTheCounter()
    {
        var limiter = new DailyRewardLimiter(maxPerDay: 2, lastRewardDate: new DateTime(2026, 7, 12), usedToday: 2);

        Assert.Equal(2, limiter.Remaining(new DateTime(2026, 7, 13)));
        Assert.True(limiter.TryConsume(new DateTime(2026, 7, 13)));
    }

    [Fact]
    public void SaveMigrationPreservesUnlocksAndAddsDefaults()
    {
        var old = new GameSaveData { schemaVersion = 1, unlockedItemIds = new[] { "hair.basic" } };

        var migrated = SaveMigration.Upgrade(old);

        Assert.Equal(SaveMigration.CurrentSchemaVersion, migrated.schemaVersion);
        Assert.Contains("hair.basic", migrated.unlockedItemIds);
        Assert.NotNull(migrated.languageCode);
        Assert.Equal(2, migrated.dailyRewardLimit);
    }

    [Fact]
    public void CatalogRulesRejectDuplicateIds()
    {
        var result = CatalogRules.ValidateIds(new[] { "chair.blue", "pet.cat", "chair.blue" });

        Assert.False(result.IsValid);
        Assert.Contains("chair.blue", result.DuplicateIds);
    }
}
