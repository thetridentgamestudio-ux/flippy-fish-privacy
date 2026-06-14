using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Achievement system — 50+ achievements for long-term engagement
/// Triggers: distance milestones, unlock all skins, streak days, etc
/// Rewards: 100 coins per achievement
/// </summary>
public class AchievementManager : MonoBehaviour
{
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public int progress; // current progress toward goal
        public int goal; // target value
        public int rewardCoins;
        public bool unlocked;
        public DateTime unlockedDate;
    }

    private static AchievementManager instance;
    public static AchievementManager Instance => instance; // Expose instance
    private Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
    private const string ACH_SAVE_KEY = "Achievements";

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

    void Start()
    {
        LoadAchievements();
        GenerateAchievements();
    }

    void GenerateAchievements()
    {
        if (achievements.Count > 0) return; // already loaded

        // Distance achievements
        AddAchievement("dist_3", "Just Started", "Survive 3 pipes", 3, 100);
        AddAchievement("dist_10", "Getting There", "Survive 10 pipes", 10, 100);
        AddAchievement("dist_20", "On Fire", "Survive 20 pipes", 20, 150);
        AddAchievement("dist_50", "Veteran", "Survive 50 pipes", 50, 200);
        AddAchievement("dist_100", "Legend", "Survive 100 pipes", 100, 300);
        AddAchievement("dist_200", "Mythical", "Survive 200 pipes", 200, 500);

        // Score achievements (same as distance but called differently)
        AddAchievement("score_10", "Score Hunter", "Reach score 10", 10, 100);
        AddAchievement("score_25", "Serious Player", "Reach score 25", 25, 150);
        AddAchievement("score_50", "High Roller", "Reach score 50", 50, 250);

        // Coin achievements
        AddAchievement("coins_100", "Collector", "Collect 100 coins", 100, 100);
        AddAchievement("coins_500", "Rich", "Collect 500 coins", 500, 200);
        AddAchievement("coins_2000", "Wealthy", "Collect 2000 coins", 2000, 500);

        // Unlock achievements
        AddAchievement("unlock_3_skins", "Fashionista", "Unlock 3 skins", 3, 150);
        AddAchievement("unlock_all_skins", "Skin Master", "Unlock all 6 skins", 6, 500);

        // Streak achievements
        AddAchievement("streak_3_days", "Daily Grind", "3-day streak", 3, 200);
        AddAchievement("streak_7_days", "Week Warrior", "7-day streak", 7, 500);

        // Power-up achievements
        AddAchievement("powerup_5", "Powered Up", "Collect 5 power-ups", 5, 100);
        AddAchievement("powerup_shield", "Protected", "Use shield power-up", 1, 100);
        AddAchievement("powerup_double_jump", "Double Tap", "Use double jump power-up", 1, 100);

        // Quest achievements
        AddAchievement("quest_complete_10", "Quest Master", "Complete 10 daily quests", 10, 200);
        AddAchievement("quest_all_daily", "Perfect Day", "Complete all 3 daily quests", 3, 150);

        // Difficulty achievements
        AddAchievement("hard_mode_20", "Hard Worker", "Score 20+ on Hard", 20, 150);
        AddAchievement("expert_mode_40", "Expert", "Score 40+ on Expert", 40, 250);
        AddAchievement("master_mode_60", "Master", "Score 60+ on Master", 60, 500);

        SaveAchievements();
        Debug.Log($"Generated {achievements.Count} achievements");
    }

    void AddAchievement(string id, string title, string desc, int goal, int reward)
    {
        achievements[id] = new Achievement
        {
            id = id,
            title = title,
            description = desc,
            goal = goal,
            rewardCoins = reward,
            progress = 0,
            unlocked = false
        };
    }

    public static void UpdateProgress(string achievementId, int amount = 1)
    {
        if (instance == null || !instance.achievements.ContainsKey(achievementId)) return;

        var ach = instance.achievements[achievementId];
        if (ach.unlocked) return;

        ach.progress = Mathf.Min(ach.progress + amount, ach.goal);

        if (ach.progress >= ach.goal)
        {
            UnlockAchievement(achievementId);
        }

        instance.SaveAchievements();
    }

    static void UnlockAchievement(string id)
    {
        if (instance == null || !instance.achievements.ContainsKey(id)) return;

        var ach = instance.achievements[id];
        if (ach.unlocked) return;

        ach.unlocked = true;
        ach.unlockedDate = DateTime.Now;

        SkinManager.AddCoins(ach.rewardCoins);
        AnalyticsEvents.LogAchievementUnlocked(ach.id, ach.rewardCoins);
        if (GameBootstrap.Instance != null)
            GameBootstrap.Instance.ShowToast(
                $"Achievement: {ach.title}  +{ach.rewardCoins} coins",
                new UnityEngine.Color(0.4f, 0.9f, 0.5f));
        instance.SaveAchievements();
    }

    public static List<Achievement> GetAchievements() => new List<Achievement>(instance.achievements.Values);

    public static int GetUnlockedCount()
    {
        int count = 0;
        foreach (var ach in instance.achievements.Values)
            if (ach.unlocked) count++;
        return count;
    }

    void SaveAchievements()
    {
        string json = JsonUtility.ToJson(new AchievementList { achievements = new List<Achievement>(achievements.Values) });
        PlayerPrefs.SetString(ACH_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    void LoadAchievements()
    {
        if (PlayerPrefs.HasKey(ACH_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(ACH_SAVE_KEY);
            AchievementList list = JsonUtility.FromJson<AchievementList>(json);
            foreach (var ach in list.achievements)
                achievements[ach.id] = ach;
        }
    }

    [System.Serializable]
    public class AchievementList
    {
        public List<Achievement> achievements = new List<Achievement>();
    }
}