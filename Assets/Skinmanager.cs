using UnityEngine;

/// Manages coins, skin unlocks and selected skin.
///
/// Skin IDs:
///   0 = Coral   (free)       – Common
///   1 = Bubbles (300 coins)  – Common
///   2 = Sunny   (750 coins)  – Rare
///   3 = Rosie   (1500 coins) – Rare
///   4 = Scout   (3500 coins) – Epic
///   5 = King    (7000 coins) – Legendary
///
/// PlayerPrefs keys (v2 prefix isolates from old build data):
///   "Coins"              int  – total coins (never reset on update)
///   "v2_SkinUnlocked_N"  int  – 1 if skin N is purchased
///   "v2_SelectedSkin"    int  – currently equipped skin ID
///   "Milestone25/50/75"  int  – bonus coin milestones
public static class SkinManager
{
    public static readonly string[] SkinNames      = { "Coral",     "Bubbles",     "Sunny",     "Rosie",     "Scout",     "King"      };
    public static readonly int[]    SkinCosts      = { 0,            300,           750,         1500,        3500,        7000        };
    public static readonly string[] SkinSprites    = { "FishCoral", "FishBubbles", "FishSunny", "FishRosie", "FishScout", "FishKing"  };
    public static readonly string[] SkinSadSprites = { "FishCoralSad","FishBubblesSad","FishSunnySad","FishRosieSad","FishScoutSad","FishKingSad" };
    public static readonly string[] Rarities       = { "Common",    "Common",      "Rare",      "Rare",      "Epic",      "Legendary" };

    static string UnlockKey(int id) => "v2_SkinUnlocked_" + id;
    const string SELECTED_KEY = "v2_SelectedSkin";

    // ── Coins ──────────────────────────────────────────────────────────────────

    public static int GetCoins() => PlayerDataStore.Data.coins;

    public static void AddCoins(int amount)
    {
        PlayerDataStore.Data.coins += amount;
        // Keep PlayerPrefs in sync as secondary backup
        PlayerPrefs.SetInt("Coins", PlayerDataStore.Data.coins);
    }

    public static void OnPipePassed(int currentScore)
    {
        AddCoins(1);

        if (currentScore >= 25 && !PlayerDataStore.Data.milestone25)
        { PlayerDataStore.Data.milestone25 = true; PlayerPrefs.SetInt("Milestone25", 1); AddCoins(5); }

        if (currentScore >= 50 && !PlayerDataStore.Data.milestone50)
        { PlayerDataStore.Data.milestone50 = true; PlayerPrefs.SetInt("Milestone50", 1); AddCoins(5); }

        if (currentScore >= 75 && !PlayerDataStore.Data.milestone75)
        { PlayerDataStore.Data.milestone75 = true; PlayerPrefs.SetInt("Milestone75", 1); AddCoins(5); }
    }

    public static void OnNewBest() => AddCoins(3);

    // ── Skins ──────────────────────────────────────────────────────────────────

    public static int GetSelectedSkin() => PlayerDataStore.Data.selectedSkin;

    public static bool IsUnlocked(int skinId)
    {
        if (skinId == 0) return true;
        var d = PlayerDataStore.Data;
        if (d.skinsUnlocked == null || skinId >= d.skinsUnlocked.Length) return false;
        return d.skinsUnlocked[skinId];
    }

    public static bool TryPurchase(int skinId)
    {
        if (IsUnlocked(skinId)) return true;
        int cost = SkinCosts[skinId];
        if (GetCoins() < cost) return false;
        AddCoins(-cost);
        PlayerDataStore.Data.skinsUnlocked[skinId] = true;
        PlayerPrefs.SetInt(UnlockKey(skinId), 1); // backup
        PlayerDataStore.Save();
        PlayerPrefs.Save();
        AnalyticsEvents.LogCosmeticPurchased("skin", SkinNames[skinId], cost);
        return true;
    }

    public static void SelectSkin(int skinId)
    {
        if (!IsUnlocked(skinId)) return;
        PlayerDataStore.Data.selectedSkin = skinId;
        PlayerPrefs.SetInt(SELECTED_KEY, skinId); // backup
        PlayerDataStore.Save();
        PlayerPrefs.Save();
    }

    public static Sprite GetSelectedSprite()
    {
        string name = SkinSprites[GetSelectedSkin()];
        Sprite sp = Resources.Load<Sprite>(name);
        if (sp == null) sp = Resources.Load<Sprite>(SkinSprites[0]);
        return sp;
    }

    public static Sprite GetSelectedSadSprite()
    {
        string name = SkinSadSprites[GetSelectedSkin()];
        Sprite sp = Resources.Load<Sprite>(name);
        if (sp == null) sp = Resources.Load<Sprite>("FishSad"); // universal fallback
        return sp;
    }
}
