using UnityEngine;

// ── ANALYTICS STATUS ─────────────────────────────────────────────────────────
// Firebase Analytics SDK (FirebaseAnalytics.unitypackage) is NOT yet imported.
// All LogEvent calls below are compiled out via #if to keep the project building.
// To activate:
//   1. Import the Firebase Analytics SDK into Assets/Firebase/
//   2. Replace "#if FIREBASE_ANALYTICS_ENABLED" guards with "#if true" or remove them
//   3. Add "FIREBASE_ANALYTICS_ENABLED" to Player Settings → Scripting Define Symbols
// ─────────────────────────────────────────────────────────────────────────────

public class AnalyticsEvents : MonoBehaviour
{
    private static AnalyticsEvents instance;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // ── ACTIVE EVENTS (fire when FIREBASE_ANALYTICS_ENABLED is defined) ───────

    public static void LogRunStart(string skinId)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("run_start",
            new Firebase.Analytics.Parameter("skin_id", skinId));
#endif
    }

    public static void LogGameOver(int score, int coins, float timeAlive)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("game_over",
            new Firebase.Analytics.Parameter("score",              score),
            new Firebase.Analytics.Parameter("coins_earned",       coins),
            new Firebase.Analytics.Parameter("time_alive_seconds", (long)timeAlive));
#endif
    }

    public static void LogPipeMilestone(int score)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("pipe_milestone",
            new Firebase.Analytics.Parameter("score", score));
#endif
    }

    public static void LogPowerUpCollected(string powerUpType)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("powerup_collected",
            new Firebase.Analytics.Parameter("powerup_type", powerUpType));
#endif
    }

    public static void LogPowerUpUsed(string powerUpType, float duration)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("powerup_used",
            new Firebase.Analytics.Parameter("powerup_type",     powerUpType),
            new Firebase.Analytics.Parameter("duration_seconds", (long)duration));
#endif
    }

    public static void LogQuestCompleted(string questId, int rewardCoins)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("quest_completed",
            new Firebase.Analytics.Parameter("quest_id",     questId),
            new Firebase.Analytics.Parameter("reward_coins", rewardCoins));
#endif
    }

    public static void LogAllDailyQuestsCompleted(int totalRewardCoins)
    {
#if FIREBASE_ANALYTICS_ENABLED
        Firebase.Analytics.FirebaseAnalytics.LogEvent("daily_quests_all_completed",
            new Firebase.Analytics.Parameter("total_reward", totalRewardCoins));
#endif
    }

    // ── INACTIVE / FUTURE EVENTS ─────────────────────────────────────────────

    public static void LogSessionStart() { }
    public static void LogSessionEnd(int durationSeconds) { }
    public static void LogLevelReached(int score, string difficulty) { }
    public static void LogBattlePassTierUnlocked(int tierNumber) { }
    public static void LogBattlePassPurchased() { }
    public static void LogBattlePassRewardClaimed(int tierNumber, string rewardType) { }
    public static void LogAchievementUnlocked(string achievementId, int rewardCoins) { }
    public static void LogAdImpression(string adType, string placement) { }
    public static void LogAdClicked(string adType, string placement) { }
    public static void LogInAppPurchase(string product, float price, string currency = "USD") { }
    public static void LogFeatureOpened(string featureName) { }
    public static void LogCosmeticPurchased(string cosmeticType, string cosmeticId, int coinsCost) { }
    public static void LogDailyStreak(int dayCount) { }
}
