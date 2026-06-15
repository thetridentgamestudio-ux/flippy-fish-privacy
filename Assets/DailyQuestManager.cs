using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

public class DailyQuestManager : MonoBehaviour
{
    [System.Serializable]
    public class Quest
    {
        public string id;
        public string title;
        public string description;
        public int targetValue;
        public int currentProgress;
        public int rewardCoins;
        public bool completed;
        public enum QuestType { ScoreGoal, ReachDifficulty, CollectCoins, DistancePipes }
        public QuestType type;
    }

    private static DailyQuestManager instance;
    public static DailyQuestManager Instance => instance; // Expose instance
    private List<Quest> dailyQuests = new List<Quest>();
    private DateTime lastResetTime;
    private const string QUEST_SAVE_KEY = "DailyQuests";
    private const string LAST_RESET_KEY = "LastQuestReset";

    // Auto-create so any script can call DailyQuestManager.Create() safely
    public static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("DailyQuestManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DailyQuestManager>();
        instance.Init();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    void Start() { } // Init() already called in Awake/Create

    void Init()
    {
        if (_initialised) return;
        _initialised = true;
        LoadQuests();
        CheckDailyReset();
        if (dailyQuests.Count == 0) GenerateTodaysQuests();
    }
    bool _initialised;

    public static void GenerateTodaysQuests()
    {
        if (instance == null) return;
        instance.dailyQuests.Clear();

        // Quest 1: Reach score 10
        instance.dailyQuests.Add(new Quest
        {
            id = "quest_score_10",
            title = "Score 10 Points",
            description = "Reach a score of 10 in a single run",
            targetValue = 10,
            currentProgress = 0,
            rewardCoins = 100,
            type = Quest.QuestType.ScoreGoal,
            completed = false
        });

        // Quest 2: Reach difficulty HARD
        instance.dailyQuests.Add(new Quest
        {
            id = "quest_hard_mode",
            title = "Play on Hard",
            description = "Reach score 20+ (Hard difficulty)",
            targetValue = 20,
            currentProgress = 0,
            rewardCoins = 150,
            type = Quest.QuestType.ReachDifficulty,
            completed = false
        });

        // Quest 3: Collect 50 coins
        instance.dailyQuests.Add(new Quest
        {
            id = "quest_coins_50",
            title = "Collect 50 Coins",
            description = "Collect 50 coins across all runs",
            targetValue = 50,
            currentProgress = 0,
            rewardCoins = 200,
            type = Quest.QuestType.CollectCoins,
            completed = false
        });

        instance.SaveQuests();
        Debug.Log("Daily quests generated for today");
    }

    public static void UpdateQuestProgress(Quest.QuestType type, int value)
    {
        if (instance == null) return;
        bool anyChanged = false;

        foreach (var quest in instance.dailyQuests)
        {
            if (quest.type != type || quest.completed) continue;

            // ScoreGoal / ReachDifficulty: track best single-run score (take max, not sum)
            // CollectCoins: cumulative across all runs today
            int newProgress = (type == Quest.QuestType.CollectCoins)
                ? quest.currentProgress + value
                : Mathf.Max(quest.currentProgress, value);

            newProgress = Mathf.Min(newProgress, quest.targetValue);
            if (newProgress == quest.currentProgress) continue;

            quest.currentProgress = newProgress;
            anyChanged = true;

            if (quest.currentProgress >= quest.targetValue)
            {
                quest.completed = true;
                SkinManager.AddCoins(quest.rewardCoins);
                AnalyticsEvents.LogQuestCompleted(quest.id, quest.rewardCoins);

                // Show completion toast so player knows they earned coins
                GameBootstrap.Instance?.ShowToast(
                    $"Quest done!  {quest.title}  +{quest.rewardCoins} 🪙",
                    new Color(0.15f, 0.85f, 0.45f));

                bool allDone = instance.dailyQuests.TrueForAll(q => q.completed);
                if (allDone)
                {
                    AnalyticsEvents.LogAllDailyQuestsCompleted(GetTotalQuestReward());
                    GameBootstrap.Instance?.ShowToast(
                        "All daily quests complete! 🎉", new Color(1f, 0.82f, 0.1f));
                }
            }
        }

        if (anyChanged) instance.SaveQuests();
    }

    public static List<Quest> GetDailyQuests() => instance.dailyQuests;

    public static int GetTotalQuestReward()
    {
        int total = 0;
        foreach (var q in instance.dailyQuests)
            if (q.completed) total += q.rewardCoins;
        return total;
    }

    void CheckDailyReset()
    {
        if (!PlayerPrefs.HasKey(LAST_RESET_KEY))
        {
            ResetDailyQuests();
            return;
        }

        string lastReset = PlayerPrefs.GetString(LAST_RESET_KEY);
        DateTime last = DateTime.Parse(lastReset, null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime now = DateTime.UtcNow;

        if (last.Date < now.Date)
        {
            ResetDailyQuests();
        }
    }

    void ResetDailyQuests()
    {
        lastResetTime = DateTime.UtcNow;
        PlayerPrefs.SetString(LAST_RESET_KEY, lastResetTime.ToString("O")); // ISO 8601 round-trip format
        PlayerPrefs.Save();
        GenerateTodaysQuests();
    }

    void SaveQuests()
    {
        string json = JsonUtility.ToJson(new QuestList { quests = dailyQuests });
        PlayerPrefs.SetString(QUEST_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    void LoadQuests()
    {
        if (PlayerPrefs.HasKey(QUEST_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(QUEST_SAVE_KEY);
            QuestList list = JsonUtility.FromJson<QuestList>(json);
            dailyQuests = list.quests ?? new List<Quest>();
        }
    }

    [System.Serializable]
    public class QuestList
    {
        public List<Quest> quests = new List<Quest>();
    }
}