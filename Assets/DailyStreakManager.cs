using UnityEngine;
using System;

/// <summary>
/// Tracks daily login streaks and awards coins for consecutive days.
/// Call CheckStreak() once per session from GameBootstrap.Awake().
/// </summary>
public static class DailyStreakManager
{
    private const string LAST_LOGIN_KEY  = "StreakLastLogin";
    private const string STREAK_KEY      = "StreakDays";
    private const string CLAIMED_TODAY   = "StreakClaimedToday";

    public static int CurrentStreak => PlayerPrefs.GetInt(STREAK_KEY, 0);

    // Returns coins awarded this session (0 if already claimed today)
    public static int CheckStreak()
    {
        string todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Already claimed today — no duplicate reward
        if (PlayerPrefs.GetString(CLAIMED_TODAY, "") == todayStr)
            return 0;

        int streak = PlayerPrefs.GetInt(STREAK_KEY, 0);
        string lastLogin = PlayerPrefs.GetString(LAST_LOGIN_KEY, "");

        if (!string.IsNullOrEmpty(lastLogin))
        {
            DateTime last = DateTime.ParseExact(lastLogin, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);
            double daysDiff = (DateTime.UtcNow.Date - last.Date).TotalDays;

            if (daysDiff <= 0)       return 0;  // same day (shouldn't reach here but guard)
            else if (daysDiff == 1)  streak++;   // consecutive day
            else                     streak = 1; // broke streak
        }
        else
        {
            streak = 1; // first ever login
        }

        // Coin reward scales with streak, caps at 50 per day
        int coins = Mathf.Min(10 * streak, 50);
        SkinManager.AddCoins(coins);
        AnalyticsEvents.LogDailyStreak(streak);

        PlayerPrefs.SetInt(STREAK_KEY, streak);
        PlayerPrefs.SetString(LAST_LOGIN_KEY, todayStr);
        PlayerPrefs.SetString(CLAIMED_TODAY, todayStr);
        PlayerPrefs.Save();

        return coins;
    }

    // Call on game-over screen to show tomorrow's reward preview
    public static string GetTomorrowTeaser()
    {
        int nextStreak = CurrentStreak + 1;
        int nextCoins  = Mathf.Min(10 * nextStreak, 50);
        return $"Come back tomorrow for +{nextCoins} coins! (Day {nextStreak} streak)";
    }
}
