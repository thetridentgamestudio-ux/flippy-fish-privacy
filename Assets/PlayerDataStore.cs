using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Versioned JSON save system for all critical player progression data.
///
/// File: Application.persistentDataPath/playerdata.json
/// Version history:
///   1 — initial: coins, skins, bestScore, totalGamesPlayed, milestones, identity, sound
///
/// Strategy: primary reads/writes go to the in-memory PlayerData object and are
/// flushed to disk via Save(). PlayerPrefs are written as a secondary backup on
/// every Save() so that if the JSON file is deleted the player can still recover.
/// On first launch after an update that adds this system, MigrateFromPlayerPrefs()
/// imports whatever PlayerPrefs data already exists so no player loses progress.
/// </summary>
public static class PlayerDataStore
{
    public const int CURRENT_VERSION = 1;

    static string FilePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    static PlayerData _cache;

    /// <summary>Returns the in-memory data, loading from disk if not yet loaded.</summary>
    public static PlayerData Data
    {
        get
        {
            if (_cache == null) Load();
            return _cache;
        }
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    public static PlayerData Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                PlayerData loaded = JsonUtility.FromJson<PlayerData>(json);
                if (loaded != null)
                {
                    _cache = loaded;
                    RunMigrations(_cache);
                    return _cache;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlayerDataStore] JSON load failed ({e.Message}), falling back to PlayerPrefs migration.");
            }
        }

        // No file or corrupt — create fresh and import existing PlayerPrefs data
        _cache = new PlayerData();
        MigrateFromPlayerPrefs(_cache);
        Save(); // persist immediately so next launch reads from JSON
        return _cache;
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    public static void Save()
    {
        if (_cache == null) return;
        try
        {
            string json = JsonUtility.ToJson(_cache, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Failed to write JSON: {e.Message}");
        }

        // Dual-write to PlayerPrefs as backup
        BackupToPlayerPrefs(_cache);
        PlayerPrefs.Save();
    }

    // ── Version migrations ───────────────────────────────────────────────────

    static void RunMigrations(PlayerData d)
    {
        // Future versions: add "if (d.version < 2) { ... d.version = 2; }" blocks here.
        // Each block only runs once because version is persisted after migration.
        if (d.version < CURRENT_VERSION)
        {
            d.version = CURRENT_VERSION;
            Save();
        }
    }

    // ── Import from PlayerPrefs (v0 → v1, one-time on first launch) ──────────

    static void MigrateFromPlayerPrefs(PlayerData d)
    {
        d.version = CURRENT_VERSION;

        // Coins
        d.coins = PlayerPrefs.GetInt("Coins", 0);

        // Skins — SkinManager uses "v2_SkinUnlocked_N" keys
        d.skinsUnlocked = new bool[PlayerData.SKIN_COUNT];
        d.skinsUnlocked[0] = true; // Coral is always free
        for (int i = 1; i < PlayerData.SKIN_COUNT; i++)
            d.skinsUnlocked[i] = PlayerPrefs.GetInt("v2_SkinUnlocked_" + i, 0) == 1;
        d.selectedSkin = PlayerPrefs.GetInt("v2_SelectedSkin", 0);

        // Score / games
        d.bestScore       = Mathf.Max(PlayerPrefs.GetInt("BestScore", 0), PlayerPrefs.GetInt("HIGH_SCORE", 0));
        d.totalGamesPlayed = PlayerPrefs.GetInt("TotalGamesPlayed", 0);

        // Milestones
        d.milestone25 = PlayerPrefs.GetInt("Milestone25", 0) == 1;
        d.milestone50 = PlayerPrefs.GetInt("Milestone50", 0) == 1;
        d.milestone75 = PlayerPrefs.GetInt("Milestone75", 0) == 1;

        // Identity
        d.playerId = PlayerPrefs.GetString("PLAYER_ID", "");
        d.username = PlayerPrefs.GetString("USERNAME", "");
        d.country  = PlayerPrefs.GetString("COUNTRY", "");

        // Settings
        d.soundOn = PlayerPrefs.GetInt("SOUND", 1) == 1;

        Debug.Log($"[PlayerDataStore] Migrated from PlayerPrefs: coins={d.coins}, bestScore={d.bestScore}, username={d.username}");
    }

    // ── Backup to PlayerPrefs (dual-write so the old keys stay in sync) ──────

    static void BackupToPlayerPrefs(PlayerData d)
    {
        PlayerPrefs.SetInt("Coins", d.coins);

        for (int i = 0; i < PlayerData.SKIN_COUNT; i++)
            PlayerPrefs.SetInt("v2_SkinUnlocked_" + i, d.skinsUnlocked != null && i < d.skinsUnlocked.Length && d.skinsUnlocked[i] ? 1 : 0);
        PlayerPrefs.SetInt("v2_SelectedSkin", d.selectedSkin);

        PlayerPrefs.SetInt("BestScore", d.bestScore);
        PlayerPrefs.SetInt("HIGH_SCORE", d.bestScore);
        PlayerPrefs.SetInt("TotalGamesPlayed", d.totalGamesPlayed);

        PlayerPrefs.SetInt("Milestone25", d.milestone25 ? 1 : 0);
        PlayerPrefs.SetInt("Milestone50", d.milestone50 ? 1 : 0);
        PlayerPrefs.SetInt("Milestone75", d.milestone75 ? 1 : 0);

        if (!string.IsNullOrEmpty(d.playerId)) PlayerPrefs.SetString("PLAYER_ID", d.playerId);
        if (!string.IsNullOrEmpty(d.username)) PlayerPrefs.SetString("USERNAME", d.username);
        if (!string.IsNullOrEmpty(d.country))  PlayerPrefs.SetString("COUNTRY", d.country);

        PlayerPrefs.SetInt("SOUND", d.soundOn ? 1 : 0);
    }
}

// ── Data model ───────────────────────────────────────────────────────────────

[Serializable]
public class PlayerData
{
    public const int SKIN_COUNT = 6;

    public int version = PlayerDataStore.CURRENT_VERSION;

    // Wallet
    public int coins;

    // Skins
    public bool[] skinsUnlocked = new bool[SKIN_COUNT];
    public int selectedSkin;

    // Score / progression
    public int bestScore;
    public int totalGamesPlayed;

    // One-time coin milestones
    public bool milestone25;
    public bool milestone50;
    public bool milestone75;

    // Player identity
    public string playerId = "";
    public string username = "";
    public string country  = "";

    // Settings
    public bool soundOn = true;
}
