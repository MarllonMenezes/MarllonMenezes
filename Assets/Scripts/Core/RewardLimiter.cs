using System;

namespace AlbaWorld.Core;

/// <summary>Offline-safe daily counter. A clock rollback never grants additional rewards.</summary>
public sealed class DailyRewardLimiter
{
    public int MaxPerDay { get; }
    public DateTime? LastRewardDate { get; private set; }
    public int UsedToday { get; private set; }

    public DailyRewardLimiter(int maxPerDay, DateTime? lastRewardDate = null, int usedToday = 0)
    {
        MaxPerDay = Math.Max(0, maxPerDay);
        LastRewardDate = lastRewardDate?.Date;
        UsedToday = Math.Max(0, usedToday);
    }

    public int Remaining(DateTime now)
    {
        var date = now.Date;
        if (!LastRewardDate.HasValue || date > LastRewardDate.Value.Date)
            return MaxPerDay;

        return Math.Max(0, MaxPerDay - UsedToday);
    }

    public bool TryConsume(DateTime now)
    {
        var date = now.Date;
        if (!LastRewardDate.HasValue || date > LastRewardDate.Value.Date)
        {
            LastRewardDate = date;
            UsedToday = 0;
        }

        if (date < LastRewardDate.Value.Date || UsedToday >= MaxPerDay)
            return false;

        UsedToday++;
        return true;
    }
}
