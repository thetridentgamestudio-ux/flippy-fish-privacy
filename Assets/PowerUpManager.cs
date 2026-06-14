using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Power-Up System — 4 active in-run power-ups
/// Shield, SlowTime, CoinMagnet, DoubleJump
/// 5% spawn rate, collected like coins
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    [System.Serializable]
    public class PowerUp
    {
        public enum PowerUpType { Shield, SlowTime, CoinMagnet, DoubleJump }
        public PowerUpType type;
        public string name;
        public float duration; // how long effect lasts
        public string description;
        public Color color;
        public int spawnWeight; // 1-10, higher = more common

        public PowerUp(PowerUpType t, string n, float d, string desc, Color c, int w)
        {
            type = t;
            name = n;
            duration = d;
            description = desc;
            color = c;
            spawnWeight = w;
        }
    }

    private static PowerUpManager instance;
    private Dictionary<PowerUp.PowerUpType, PowerUp> powerUpDefs = new Dictionary<PowerUp.PowerUpType, PowerUp>();
    private Dictionary<PowerUp.PowerUpType, float> activePowerUps = new Dictionary<PowerUp.PowerUpType, float>();
    private const float SPAWN_CHANCE = 0.05f; // 5% chance per coin
    private const string SHIELD_KEY = "Shield";
    private const string DOUBLE_JUMP_KEY = "DoubleJump";

    public static event Action<PowerUp.PowerUpType> OnPowerUpActivated;
    public static event Action<PowerUp.PowerUpType> OnPowerUpExpired;

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
        InitializePowerUps();
    }

    void InitializePowerUps()
    {
        powerUpDefs.Clear();

        // Shield: negate 1 collision
        powerUpDefs[PowerUp.PowerUpType.Shield] = new PowerUp(
            PowerUp.PowerUpType.Shield,
            "Shield",
            15f, // stays active for 15 seconds
            "Negate 1 collision",
            new Color(0.2f, 1f, 0.8f), // cyan
            8 // common
        );

        // SlowTime: pipes move 50% slower
        powerUpDefs[PowerUp.PowerUpType.SlowTime] = new PowerUp(
            PowerUp.PowerUpType.SlowTime,
            "Slow Time",
            8f,
            "Slow pipes by 50%",
            new Color(1f, 0.5f, 0.2f), // orange
            5 // medium
        );

        // CoinMagnet: attract coins
        powerUpDefs[PowerUp.PowerUpType.CoinMagnet] = new PowerUp(
            PowerUp.PowerUpType.CoinMagnet,
            "Coin Magnet",
            10f,
            "Attract all coins",
            new Color(1f, 0.85f, 0.2f), // gold
            7 // common
        );

        // DoubleJump: extra mid-air jump
        powerUpDefs[PowerUp.PowerUpType.DoubleJump] = new PowerUp(
            PowerUp.PowerUpType.DoubleJump,
            "Double Jump",
            12f,
            "Extra jump in air",
            new Color(1f, 0.2f, 0.6f), // magenta
            4 // rare
        );
    }

    public static PowerUp.PowerUpType TrySpawnPowerUp()
    {
        if (instance == null) return PowerUp.PowerUpType.Shield; // default

        if (UnityEngine.Random.value > SPAWN_CHANCE)
            return PowerUp.PowerUpType.Shield; // no spawn

        // Weighted random selection
        float totalWeight = 0;
        foreach (var def in instance.powerUpDefs.Values)
            totalWeight += def.spawnWeight;

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0;

        foreach (var kvp in instance.powerUpDefs)
        {
            cumulative += kvp.Value.spawnWeight;
            if (roll <= cumulative)
                return kvp.Key;
        }

        return PowerUp.PowerUpType.Shield;
    }

    public static void ActivatePowerUp(PowerUp.PowerUpType type)
    {
        if (instance == null) return;

        if (!instance.powerUpDefs.ContainsKey(type)) return;

        PowerUp def = instance.powerUpDefs[type];
        instance.activePowerUps[type] = def.duration;

        AnalyticsEvents.LogPowerUpCollected(def.name);
        AnalyticsEvents.LogPowerUpUsed(def.name, def.duration);
        OnPowerUpActivated?.Invoke(type);

        // Type-specific logic
        switch (type)
        {
            case PowerUp.PowerUpType.Shield:
                PlayerPrefs.SetInt(SHIELD_KEY, 1);
                break;

            case PowerUp.PowerUpType.SlowTime:
                Time.timeScale = 0.5f; // slow game
                instance.StartCoroutine(instance.ResetTimeScaleCoroutine(def.duration));
                break;

            case PowerUp.PowerUpType.DoubleJump:
                PlayerPrefs.SetInt(DOUBLE_JUMP_KEY, 1);
                break;

            case PowerUp.PowerUpType.CoinMagnet:
                // Passive during duration — handled in coin collection
                break;
        }

        HapticFeedback.PlayPowerUpHaptic();
        CameraShake.ShakePowerUp();
    }

    System.Collections.IEnumerator ResetTimeScaleCoroutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public static bool HasShield()
    {
        return PlayerPrefs.GetInt(SHIELD_KEY, 0) == 1;
    }

    public static bool HasDoubleJump()
    {
        return PlayerPrefs.GetInt(DOUBLE_JUMP_KEY, 0) == 1;
    }

    public static bool HasCoinMagnet()
    {
        return instance.activePowerUps.ContainsKey(PowerUp.PowerUpType.CoinMagnet) &&
               instance.activePowerUps[PowerUp.PowerUpType.CoinMagnet] > 0;
    }

    public static bool HasSlowTime()
    {
        return Time.timeScale < 0.9f;
    }

    public static void ConsumeShield()
    {
        PlayerPrefs.SetInt(SHIELD_KEY, 0);
        OnPowerUpExpired?.Invoke(PowerUp.PowerUpType.Shield);
        Debug.Log("Shield consumed");
    }

    public static void ConsumeDoubleJump()
    {
        PlayerPrefs.SetInt(DOUBLE_JUMP_KEY, 0);
        OnPowerUpExpired?.Invoke(PowerUp.PowerUpType.DoubleJump);
    }

    public static PowerUp GetPowerUpDef(PowerUp.PowerUpType type)
    {
        if (instance.powerUpDefs.ContainsKey(type))
            return instance.powerUpDefs[type];
        return null;
    }

    void Update()
    {
        // Track active power-up durations
        var keys = new List<PowerUp.PowerUpType>(activePowerUps.Keys);
        foreach (var type in keys)
        {
            activePowerUps[type] -= Time.deltaTime;
            if (activePowerUps[type] <= 0)
            {
                activePowerUps.Remove(type);
                OnPowerUpExpired?.Invoke(type);
                Debug.Log($"Power-Up Expired: {type}");

                if (type == PowerUp.PowerUpType.SlowTime)
                    Time.timeScale = 1f;
            }
        }
    }

    public static void ResetForNewGame()
    {
        PlayerPrefs.SetInt(SHIELD_KEY, 0);
        PlayerPrefs.SetInt(DOUBLE_JUMP_KEY, 0);
        Time.timeScale = 1f;
        instance.activePowerUps.Clear();
    }
}