using System;
using System.Globalization;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

public sealed class RewardedAdsService : IRewardedAdsService
{
    private readonly GameSaveData _save;
    private readonly DailyRewardLimiter _limiter;
    private readonly bool _simulateInEditor;

    public RewardedAdsService(GameSaveData save, bool simulateInEditor = true)
    {
        _save = save;
        _limiter = new DailyRewardLimiter(save.dailyRewardLimit, ParseDate(save.lastRewardDate), save.rewardsUsedToday);
        _simulateInEditor = simulateInEditor;
    }

    public bool IsAvailable => Application.internetReachability != NetworkReachability.NotReachable || (Application.isEditor && _simulateInEditor);

    public void ShowForItem(string itemId, Action<bool> completed)
    {
        if (!IsAvailable || !_limiter.TryConsume(DateTime.Now))
        {
            completed(false);
            return;
        }

        _save.lastRewardDate = _limiter.LastRewardDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        _save.rewardsUsedToday = _limiter.UsedToday;
        // This editor-safe adapter is replaced by the Google Mobile Ads Unity adapter before release.
        // It intentionally never grants a reward unless the simulated ad completed.
        if (Application.isEditor && _simulateInEditor)
        {
            completed(true);
            return;
        }

        completed(false);
    }

    private static DateTime? ParseDate(string value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
