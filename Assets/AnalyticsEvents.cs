using UnityEngine;

/// <summary>
/// Centralized analytics event tracking for Firebase Analytics
/// Tracks engagement, monetization, retention, feature usage
/// </summary>
public class AnalyticsEvents : MonoBehaviour
{
    private static AnalyticsEvents instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    // Session events
    public static void LogSessionStart()
    {
        Debug.Log("[Analytics] Session Started");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("session_start");
    }

    public static void LogSessionEnd(int durationSeconds)
    {
        Debug.Log($"[Analytics] Session Ended: {durationSeconds}s");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("session_end", new Firebase.Analytics.Parameter("duration_seconds", durationSeconds));
    }

    // Engagement events
    public static void LogGameOver(int score, int coins, float timeAlive)
    {
        Debug.Log($"[Analytics] Game Over: Score={score}, Coins={coins}, Time={timeAlive}s");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("game_over", 
        //     new Firebase.Analytics.Parameter("score", score),
        //     new Firebase.Analytics.Parameter("coins_earned", coins),
        //     new Firebase.Analytics.Parameter("time_alive_seconds", timeAlive)
        // );
    }

    public static void LogLevelReached(int score, string difficulty)
    {
        Debug.Log($"[Analytics] Milestone Reached: Score={score}, Difficulty={difficulty}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("level_reached",
        //     new Firebase.Analytics.Parameter("score", score),
        //     new Firebase.Analytics.Parameter("difficulty", difficulty)
        // );
    }

    // Quest events
    public static void LogQuestCompleted(string questId, int rewardCoins)
    {
        Debug.Log($"[Analytics] Quest Completed: {questId}, +{rewardCoins} coins");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("quest_completed",
        //     new Firebase.Analytics.Parameter("quest_id", questId),
        //     new Firebase.Analytics.Parameter("reward_coins", rewardCoins)
        // );
    }

    public static void LogAllDailyQuestsCompleted(int totalRewardCoins)
    {
        Debug.Log($"[Analytics] All Daily Quests Completed: +{totalRewardCoins} coins");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("daily_quests_all_completed",
        //     new Firebase.Analytics.Parameter("total_reward", totalRewardCoins)
        // );
    }

    // Power-up events
    public static void LogPowerUpCollected(string powerUpType)
    {
        Debug.Log($"[Analytics] Power-up Collected: {powerUpType}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("powerup_collected",
        //     new Firebase.Analytics.Parameter("powerup_type", powerUpType)
        // );
    }

    public static void LogPowerUpUsed(string powerUpType, float duration)
    {
        Debug.Log($"[Analytics] Power-up Used: {powerUpType} ({duration}s)");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("powerup_used",
        //     new Firebase.Analytics.Parameter("powerup_type", powerUpType),
        //     new Firebase.Analytics.Parameter("duration_seconds", duration)
        // );
    }

    // Battle Pass events
    public static void LogBattlePassTierUnlocked(int tierNumber)
    {
        Debug.Log($"[Analytics] Battle Pass Tier Unlocked: {tierNumber}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("battlepass_tier_unlocked",
        //     new Firebase.Analytics.Parameter("tier_number", tierNumber)
        // );
    }

    public static void LogBattlePassPurchased()
    {
        Debug.Log("[Analytics] Battle Pass Premium Purchased");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("battlepass_purchased");
    }

    public static void LogBattlePassRewardClaimed(int tierNumber, string rewardType)
    {
        Debug.Log($"[Analytics] Battle Pass Reward Claimed: Tier {tierNumber}, Type={rewardType}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("battlepass_reward_claimed",
        //     new Firebase.Analytics.Parameter("tier_number", tierNumber),
        //     new Firebase.Analytics.Parameter("reward_type", rewardType)
        // );
    }

    // Achievement events
    public static void LogAchievementUnlocked(string achievementId, int rewardCoins)
    {
        Debug.Log($"[Analytics] Achievement Unlocked: {achievementId}, +{rewardCoins} coins");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("achievement_unlocked",
        //     new Firebase.Analytics.Parameter("achievement_id", achievementId),
        //     new Firebase.Analytics.Parameter("reward_coins", rewardCoins)
        // );
    }

    // Monetization events
    public static void LogAdImpression(string adType, string placement)
    {
        // Debug.Log($"[Analytics] Ad Impression: Type={adType}, Placement={placement}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression",
        //     new Firebase.Analytics.Parameter("ad_type", adType),
        //     new Firebase.Analytics.Parameter("placement", placement)
        // );
    }

    public static void LogAdClicked(string adType, string placement)
    {
        Debug.Log($"[Analytics] Ad Clicked: Type={adType}, Placement={placement}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_clicked",
        //     new Firebase.Analytics.Parameter("ad_type", adType),
        //     new Firebase.Analytics.Parameter("placement", placement)
        // );
    }

    public static void LogInAppPurchase(string product, float price, string currency = "USD")
    {
        Debug.Log($"[Analytics] IAP Purchase: {product}, {price} {currency}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent(Firebase.Analytics.FirebaseAnalytics.EventPurchase,
        //     new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterItemName, product),
        //     new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterValue, price),
        //     new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterCurrency, currency)
        // );
    }

    // Feature engagement events
    public static void LogFeatureOpened(string featureName)
    {
        Debug.Log($"[Analytics] Feature Opened: {featureName}");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("feature_opened",
        //     new Firebase.Analytics.Parameter("feature_name", featureName)
        // );
    }

    public static void LogCosmeticPurchased(string cosmeticType, string cosmeticId, int coinsCost)
    {
        Debug.Log($"[Analytics] Cosmetic Purchased: {cosmeticType}/{cosmeticId}, -{coinsCost} coins");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("cosmetic_purchased",
        //     new Firebase.Analytics.Parameter("cosmetic_type", cosmeticType),
        //     new Firebase.Analytics.Parameter("cosmetic_id", cosmeticId),
        //     new Firebase.Analytics.Parameter("coins_spent", coinsCost)
        // );
    }

    // Retention events
    public static void LogDailyStreak(int dayCount)
    {
        Debug.Log($"[Analytics] Daily Streak: {dayCount} days");
        // Firebase.Analytics.FirebaseAnalytics.LogEvent("daily_streak",
        //     new Firebase.Analytics.Parameter("day_count", dayCount)
        // );
    }
}