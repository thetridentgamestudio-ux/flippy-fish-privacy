using UnityEngine;

/// <summary>
/// Manages coins, skin unlocks and selected skin.
/// All state stored in PlayerPrefs — no server needed.
///
/// Skin IDs:
///   0 = Default  (free)
///   1 = Golden   (100 coins)
///   2 = Ghost    (250 coins)
///   3 = Devil    (500 coins)
///
/// PlayerPrefs keys:
///   "Coins"          int   — total coins earned lifetime
///   "SkinUnlocked_1" int   — 1 if unlocked, 0 if not
///   "SkinUnlocked_2" int
///   "SkinUnlocked_3" int
///   "SelectedSkin"   int   — currently equipped skin ID
///   "Milestone25"    int   — 1 if bonus coins already awarded
///   "Milestone50"    int
///   "Milestone75"    int
/// </summary>
public static class SkinManager
{
    public static readonly string[] SkinNames   = { "Default", "Whale",   "Shark",    "Dragon",   "Puffer Fish", "Anglerfish"   };
    public static readonly int[]    SkinCosts   = { 0,         300,        750,        1500,       3500,          7000           };
    public static readonly string[] SkinSprites     = { "FishDefault", "FishWhale", "FishShark", "FishDragon", "FishPuffer", "FishAnglerfish" };
    public static readonly string[] SkinSadSprites  = { "FishDefaultSad", "FishWhaleSad", "FishSharkSad", "FishDragonSad", "FishPufferSad", "FishAnglerSad" };

    // ── Coins ──────────────────────────────────────────────

    public static int GetCoins() => PlayerPrefs.GetInt("Coins", 0);

    public static void AddCoins(int amount)
    {
        int current = GetCoins();
        PlayerPrefs.SetInt("Coins", current + amount);
        PlayerPrefs.Save();
    }

    // Called every time player passes a pipe
    // 1 coin per pipe — earns immediately, keeps players motivated to continue
    public static void OnPipePassed(int currentScore)
    {
        AddCoins(1);

        // Milestone bonuses — awarded once each
        if (currentScore >= 25 && PlayerPrefs.GetInt("Milestone25", 0) == 0)
        {
            PlayerPrefs.SetInt("Milestone25", 1);
            AddCoins(5);
        }
        if (currentScore >= 50 && PlayerPrefs.GetInt("Milestone50", 0) == 0)
        {
            PlayerPrefs.SetInt("Milestone50", 1);
            AddCoins(5);
        }
        if (currentScore >= 75 && PlayerPrefs.GetInt("Milestone75", 0) == 0)
        {
            PlayerPrefs.SetInt("Milestone75", 1);
            AddCoins(5);
        }
    }

    // Called when player beats their personal best
    public static void OnNewBest()
    {
        AddCoins(3);
    }

    // ── Skins ──────────────────────────────────────────────

    public static int GetSelectedSkin() => PlayerPrefs.GetInt("SelectedSkin", 0);

    public static bool IsUnlocked(int skinId)
    {
        if (skinId == 0) return true; // default always free
        return PlayerPrefs.GetInt("SkinUnlocked_" + skinId, 0) == 1;
    }

    // Returns true if purchase succeeded, false if not enough coins
    public static bool TryPurchase(int skinId)
    {
        if (IsUnlocked(skinId)) return true;
        int cost = SkinCosts[skinId];
        if (GetCoins() < cost) return false;
        AddCoins(-cost);
        PlayerPrefs.SetInt("SkinUnlocked_" + skinId, 1);
        PlayerPrefs.Save();
        AnalyticsEvents.LogCosmeticPurchased("skin", SkinNames[skinId], cost);
        return true;
    }

    public static void SelectSkin(int skinId)
    {
        if (!IsUnlocked(skinId)) return;
        PlayerPrefs.SetInt("SelectedSkin", skinId);
        PlayerPrefs.Save();
    }

    // Returns the playing sprite for the currently selected skin
    public static Sprite GetSelectedSprite()
    {
        string name = SkinSprites[GetSelectedSkin()];
        Sprite sp = Resources.Load<Sprite>(name);
        if (sp == null) sp = Resources.Load<Sprite>(SkinSprites[0]);
        return sp;
    }

    // Returns the death/sad sprite for the currently selected skin
    public static Sprite GetSelectedSadSprite()
    {
        string name = SkinSadSprites[GetSelectedSkin()];
        Sprite sp = Resources.Load<Sprite>(name);
        if (sp == null) sp = Resources.Load<Sprite>(SkinSadSprites[0]);
        return sp;
    }
}