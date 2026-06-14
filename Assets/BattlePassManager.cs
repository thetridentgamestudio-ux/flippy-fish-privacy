using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Battle Pass System — Free + Premium tracks, 30 tiers, seasonal
/// Tier rewards: cosmetics, coins, premium currency
/// </summary>
public class BattlePassManager : MonoBehaviour
{
    [System.Serializable]
    public class BattlePassTier
    {
        public int tierNumber; // 1-30
        public string name;
        public int xpRequired; // cumulative XP to unlock
        
        [System.Serializable]
        public class Reward
        {
            public enum RewardType { Coins, Gems, Skin, Trail, Background }
            public RewardType type;
            public int amount; // coins/gems count or cosmetic ID
            public string cosmetic; // "Trail_Fire", "Skin_Shark", etc
        }

        public Reward freeReward; // all players get this
        public Reward premiumReward; // premium-track exclusive
        public bool unlocked;
        public bool rewardClaimed;
    }

    [System.Serializable]
    public class Season
    {
        public int seasonNumber;
        public string name;
        public DateTime startDate;
        public DateTime endDate;
        public List<BattlePassTier> tiers = new List<BattlePassTier>();
    }

    private static BattlePassManager instance;
    public static BattlePassManager Instance => instance; // Expose instance
    private Season currentSeason;
    private int playerXP; // cumulative XP this season
    private bool isPremium; // player owns premium pass
    private const string BP_SAVE_KEY = "BattlePass";
    private const string XP_SAVE_KEY = "BattlePassXP";
    private const string PREMIUM_KEY = "BattlePassPremium";
    private const int XP_PER_SCORE_POINT = 5; // 1 score point = 5 XP

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
        LoadBattlePass();
        CheckSeasonReset();
        GenerateCurrentSeason();
    }

    public static void AddXP(int amount)
    {
        if (instance == null) return;
        
        instance.playerXP += amount;
        instance.UpdateTierUnlocks();
        instance.SaveBattlePass();
        
        Debug.Log($"Battle Pass: +{amount} XP (Total: {instance.playerXP})");
    }

    void UpdateTierUnlocks()
    {
        foreach (var tier in currentSeason.tiers)
        {
            if (!tier.unlocked && playerXP >= tier.xpRequired)
            {
                tier.unlocked = true;
                AnalyticsEvents.LogBattlePassTierUnlocked(tier.tierNumber);
            }
        }
    }

    public static void ClaimReward(int tierNumber)
    {
        if (instance == null) return;
        var tier = instance.currentSeason.tiers.Find(t => t.tierNumber == tierNumber);
        if (tier == null || tier.rewardClaimed) return;

        // Award free reward (all players)
        instance.AwardReward(tier.freeReward);

        // Award premium reward (if player owns pass)
        if (instance.isPremium)
            instance.AwardReward(tier.premiumReward);

        tier.rewardClaimed = true;
        AnalyticsEvents.LogBattlePassRewardClaimed(tierNumber, tier.freeReward?.type.ToString() ?? "none");
        instance.SaveBattlePass();
    }

    void AwardReward(BattlePassTier.Reward reward)
    {
        if (reward == null) return;

        switch (reward.type)
        {
            case BattlePassTier.Reward.RewardType.Coins:
                SkinManager.AddCoins(reward.amount);
                break;

            case BattlePassTier.Reward.RewardType.Gems:
                AddGems(reward.amount);
                break;

            case BattlePassTier.Reward.RewardType.Skin:
                UnlockSkin(reward.cosmetic);
                break;

            case BattlePassTier.Reward.RewardType.Trail:
                UnlockTrail(reward.cosmetic);
                break;

            case BattlePassTier.Reward.RewardType.Background:
                UnlockBackground(reward.cosmetic);
                break;
        }

        Debug.Log($"Reward claimed: {reward.type} - {reward.amount}/{reward.cosmetic}");
    }

    public static void PurchasePremiumPass(int cost = 299) // $2.99
    {
        // Integrate with Firebase billing here
        // For now, simplified:
        instance.isPremium = true;
        instance.SaveBattlePass();
        AnalyticsEvents.LogBattlePassPurchased();
    }

    public static int GetCurrentTier()
    {
        int tier = 1;
        foreach (var t in instance.currentSeason.tiers)
        {
            if (t.unlocked) tier = t.tierNumber;
            else break;
        }
        return tier;
    }

    public static bool IsPremium() => instance.isPremium;

    void GenerateCurrentSeason()
    {
        currentSeason = new Season
        {
            seasonNumber = 1,
            name = "Coral Reef",
            startDate = DateTime.Now,
            endDate = DateTime.Now.AddDays(42) // 6 weeks
        };

        int cumulativeXP = 0;

        for (int i = 1; i <= 30; i++)
        {
            int xpForTier = 100 + (i * 50); // scales: tier 1 = 150 XP, tier 30 = 1650 XP
            cumulativeXP += xpForTier;

            var tier = new BattlePassTier
            {
                tierNumber = i,
                name = $"Tier {i}",
                xpRequired = cumulativeXP,
                freeReward = GenerateFreeReward(i),
                premiumReward = GeneratePremiumReward(i),
                unlocked = false,
                rewardClaimed = false
            };

            currentSeason.tiers.Add(tier);
        }

        Debug.Log($"Season {currentSeason.seasonNumber} ({currentSeason.name}) generated. Total XP for Tier 30: {cumulativeXP}");
    }

    BattlePassTier.Reward GenerateFreeReward(int tierNumber)
    {
        if (tierNumber % 5 == 0) // every 5 tiers
            return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Coins, amount = 200 };
        else
            return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Coins, amount = 100 };
    }

    BattlePassTier.Reward GeneratePremiumReward(int tierNumber)
    {
        if (tierNumber == 10) return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Skin, cosmetic = "Skin_Shark" };
        if (tierNumber == 20) return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Skin, cosmetic = "Skin_Jellyfish" };
        if (tierNumber == 30) return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Gems, amount = 1000 };
        if (tierNumber % 3 == 0) return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Gems, amount = 50 };
        return new BattlePassTier.Reward { type = BattlePassTier.Reward.RewardType.Coins, amount = 150 };
    }

    void AddGems(int amount) => PlayerPrefs.SetInt("Gems", PlayerPrefs.GetInt("Gems", 0) + amount);
    void UnlockSkin(string skinId) => PlayerPrefs.SetInt($"Skin_{skinId}", 1);
    void UnlockTrail(string trailId) => PlayerPrefs.SetInt($"Trail_{trailId}", 1);
    void UnlockBackground(string bgId) => PlayerPrefs.SetInt($"BG_{bgId}", 1);

    void CheckSeasonReset()
    {
        // Every 42 days, generate new season
        if (!PlayerPrefs.HasKey("LastSeasonReset"))
        {
            PlayerPrefs.SetString("LastSeasonReset", DateTime.Now.ToString());
            return;
        }

        string lastReset = PlayerPrefs.GetString("LastSeasonReset");
        DateTime last = DateTime.Parse(lastReset);
        if ((DateTime.Now - last).TotalDays >= 42)
        {
            // Reset for new season
            playerXP = 0;
            isPremium = false;
            GenerateCurrentSeason();
            PlayerPrefs.SetString("LastSeasonReset", DateTime.Now.ToString());
        }
    }

    void SaveBattlePass()
    {
        PlayerPrefs.SetInt(XP_SAVE_KEY, playerXP);
        PlayerPrefs.SetInt(PREMIUM_KEY, isPremium ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadBattlePass()
    {
        playerXP = PlayerPrefs.GetInt(XP_SAVE_KEY, 0);
        isPremium = PlayerPrefs.GetInt(PREMIUM_KEY, 0) == 1;
    }

    public static Season GetCurrentSeason() => instance.currentSeason;
    public static int GetPlayerXP() => instance.playerXP;
}