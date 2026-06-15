using UnityEngine;
using System;
using System.Collections.Generic;

public class BattlePassManager : MonoBehaviour
{
    [System.Serializable]
    public class Reward
    {
        public enum RewardType { Coins, Gems, Skin }
        public RewardType type;
        public int    amount;
        public string cosmetic; // skin ID if type == Skin
    }

    [System.Serializable]
    public class Tier
    {
        public int    number;
        public int    xpRequired;    // cumulative XP to unlock this tier
        public Reward freeReward;
        public Reward premiumReward;
        public bool   unlocked;
        public bool   claimed;
    }

    [System.Serializable]
    public class Season
    {
        public int    number;
        public string name;
        public List<Tier> tiers = new List<Tier>();
    }

    // ── Singleton ────────────────────────────────────────────────────────
    static BattlePassManager _instance;
    public static BattlePassManager Instance
    {
        get
        {
            if (_instance == null) Create();
            return _instance;
        }
    }

    public static void Create()
    {
        if (_instance != null) return;
        var go = new GameObject("BattlePassManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<BattlePassManager>();
        _instance.Init();
    }

    // ── Data ─────────────────────────────────────────────────────────────
    Season _season;
    int    _xp;
    bool   _isPremium;

    const string KEY_XP      = "BP_XP";
    const string KEY_PREMIUM = "BP_Premium";
    const string KEY_CLAIMED = "BP_Claimed_";

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    void Init()
    {
        _xp        = PlayerPrefs.GetInt(KEY_XP, 0);
        _isPremium = PlayerPrefs.GetInt(KEY_PREMIUM, 0) == 1;
        BuildSeason();
    }

    void BuildSeason()
    {
        _season = new Season { number = 1, name = "Coral Reef" };
        int cumXP = 0;
        for (int i = 1; i <= 20; i++)
        {
            cumXP += 150 + i * 50;
            bool unlocked = _xp >= cumXP;
            bool claimed  = PlayerPrefs.GetInt(KEY_CLAIMED + i, 0) == 1;
            _season.tiers.Add(new Tier
            {
                number   = i,
                xpRequired = cumXP,
                freeReward    = new Reward { type = Reward.RewardType.Coins, amount = (i % 5 == 0) ? 200 : 75 },
                premiumReward = (i % 5 == 0)
                    ? new Reward { type = Reward.RewardType.Gems, amount = 50 }
                    : new Reward { type = Reward.RewardType.Coins, amount = 150 },
                unlocked = unlocked,
                claimed  = claimed
            });
        }
    }

    // ── Public API ───────────────────────────────────────────────────────
    public static Season GetSeason()    => Instance._season;
    public static int    GetXP()        => Instance._xp;
    public static int    GetCurrentTier()
    {
        int t = 0;
        foreach (var tier in Instance._season.tiers)
            if (tier.unlocked) t = tier.number;
        return t;
    }
    public static bool IsPremium()      => Instance._isPremium;

    public static void AddXP(int amount)
    {
        var mgr = Instance;
        mgr._xp += amount;
        PlayerPrefs.SetInt(KEY_XP, mgr._xp);
        // refresh unlock flags without rebuilding whole season
        foreach (var t in mgr._season.tiers)
            if (!t.unlocked && mgr._xp >= t.xpRequired) t.unlocked = true;
        PlayerPrefs.Save();
    }

    public static void ClaimReward(int tierNumber)
    {
        var mgr  = Instance;
        var tier = mgr._season.tiers.Find(t => t.number == tierNumber);
        if (tier == null || tier.claimed || !tier.unlocked) return;

        Award(tier.freeReward);
        if (mgr._isPremium) Award(tier.premiumReward);
        tier.claimed = true;
        PlayerPrefs.SetInt(KEY_CLAIMED + tierNumber, 1);
        PlayerPrefs.Save();
    }

    static void Award(Reward r)
    {
        if (r == null) return;
        if (r.type == Reward.RewardType.Coins) SkinManager.AddCoins(r.amount);
        else if (r.type == Reward.RewardType.Gems)
            PlayerPrefs.SetInt("Gems", PlayerPrefs.GetInt("Gems", 0) + r.amount);
    }

    public static void PurchasePremium()
    {
        Instance._isPremium = true;
        PlayerPrefs.SetInt(KEY_PREMIUM, 1);
        PlayerPrefs.Save();
    }
}
