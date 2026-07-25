using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Spawns visual power-up objects in game world
/// Drifts left like coins, player collects
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite> _iconCache
        = new Dictionary<string, Sprite>();

    // Maps PowerUpType to the resource sprite name
    private static string GetIconName(PowerUpManager.PowerUp.PowerUpType type)
    {
        switch (type)
        {
            case PowerUpManager.PowerUp.PowerUpType.Shield:     return "powerup_shield";
            case PowerUpManager.PowerUp.PowerUpType.SlowTime:   return "powerup_slowtime";
            case PowerUpManager.PowerUp.PowerUpType.CoinMagnet: return "powerup_magnet";
            case PowerUpManager.PowerUp.PowerUpType.DoubleJump: return "powerup_doublejump";
            case PowerUpManager.PowerUp.PowerUpType.SpeedBoost: return "powerup_speedboost";
            default: return null;
        }
    }

    public static GameObject SpawnPowerUp(Vector3 position, PowerUpManager.PowerUp.PowerUpType type)
    {
        PowerUpManager.PowerUp def = PowerUpManager.GetPowerUpDef(type);
        if (def == null) return null;

        // Create game object
        GameObject powerUpGO = new GameObject($"PowerUp_{type}");
        powerUpGO.transform.position = position;

        // --- Glow halo behind icon (child, sortingOrder 9) ---
        GameObject glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(powerUpGO.transform, false);
        SpriteRenderer glowSR = glowGO.AddComponent<SpriteRenderer>();
        // Use a fresh circle sprite with the def colour at 40% alpha for the glow
        Color glowColor = def.color;
        glowColor.a = 0.4f;
        glowSR.sprite = GetWhiteCircleSprite();
        glowSR.color  = glowColor;
        glowSR.sortingLayerName = "Default";
        glowSR.sortingOrder = 9;
        // Glow scale is set after icon scale is computed below.

        // --- Main icon sprite ---
        SpriteRenderer sr = powerUpGO.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;

        // Try to load the icon sprite from Resources
        string iconName = GetIconName(type);
        Sprite iconSprite = null;
        if (iconName != null)
        {
            if (!_iconCache.TryGetValue(iconName, out iconSprite))
            {
                iconSprite = Resources.Load<Sprite>(iconName);
                if (iconSprite == null)
                    Debug.LogWarning($"[PowerUp] Resources.Load failed for '{iconName}' — using fallback circle");
                _iconCache[iconName] = iconSprite; // cache even if null
            }
        }

        if (iconSprite != null)
        {
            sr.sprite = iconSprite;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Simple;
        }
        else
        {
            // Fallback: coloured circle
            sr.sprite = GetWhiteCircleSprite();
            sr.color  = def.color;
        }

        // Normalise icon to 0.7 world units regardless of sprite import settings.
        // sprite.bounds.size.x is in world units at scale=1 (pixels / PPU).
        const float kTargetSize = 1.8f;
        float spriteW = (sr.sprite != null) ? sr.sprite.bounds.size.x : 1f;
        float iconScale = kTargetSize / spriteW;
        powerUpGO.transform.localScale = Vector3.one * iconScale;

        // Glow sprite is 32px @ 100PPU = 0.32 world units at scale 1.
        // We want glow world size = kTargetSize * 1.6.
        // worldGlow = iconScale * glowChildScale * 0.32  →  glowChildScale = (kTargetSize * 1.6) / (iconScale * 0.32)
        float glowChildScale = (kTargetSize * 1.6f) / (iconScale * 0.32f);
        glowGO.transform.localScale = Vector3.one * glowChildScale;

        // Collider radius in LOCAL space. World radius = iconScale * cc.radius.
        // Target world radius = kTargetSize * 0.6 → local radius = (kTargetSize * 0.6) / iconScale
        CircleCollider2D cc = powerUpGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = (kTargetSize * 0.6f) / iconScale;

        // Kinematic RB with continuous detection prevents tunneling at high speed
        var rb = powerUpGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Use Moving component (same as pipes/coins) so it respects game speed and auto-destroys off-screen
        // Match current pipe speed so powerup stays in sync with its pipe and doesn't drift
        var moving = powerUpGO.AddComponent<Moving>();
        moving.speed = GameBootstrap.Instance != null ? GameBootstrap.Instance.CurrentPipeSpeed : 4f;

        // Bobbing animation
        var bobScript = powerUpGO.AddComponent<PowerUpBob>();
        bobScript.amplitude = 0.3f;
        bobScript.frequency = 1.5f;

        // Pulsing scale animation
        powerUpGO.AddComponent<PowerUpPulse>();

        // PowerUpPickup handles collection via component check — no Unity tag needed.
        var pickup = powerUpGO.AddComponent<PowerUpPickup>();
        pickup.powerUpType = type;

        // --- Label above the pickup ---
        // We need a world-space Canvas on the pickup GO so TMP works cleanly
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(powerUpGO.transform, false);
        Canvas labelCanvas = labelGO.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingLayerName = "Default";
        labelCanvas.sortingOrder = 11;
        // Scale the canvas to match world units (default canvas pixel size is huge)
        labelGO.transform.localScale = Vector3.one * 0.01f;

        GameObject textGO = new GameObject("LabelText");
        textGO.transform.SetParent(labelGO.transform, false);
        TextMeshProUGUI labelTxt = textGO.AddComponent<TextMeshProUGUI>();
        labelTxt.text = def.name.ToUpper();
        labelTxt.fontSize = 28;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.color = Color.white;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.outlineColor = Color.black;
        labelTxt.outlineWidth = 0.3f;

        RectTransform labelRT = textGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 1f);
        labelRT.anchorMax = new Vector2(0.5f, 1f);
        labelRT.pivot = new Vector2(0.5f, 0f);
        labelRT.sizeDelta = new Vector2(200f, 40f);
        // canvas localScale is 0.01 so world offset of 1.2 = 120 canvas units
        labelRT.anchoredPosition = new Vector2(0f, 80f);

        // Moving component auto-destroys at x < -15; long backup for safety only.
        // Powerups spawn far off-screen (nextPipeX grows throughout the game), so they
        // need up to 60s+ to travel to the player at typical pipe speeds.
        Destroy(powerUpGO, 90f);

        return powerUpGO;
    }

    // White circle sprite shared across all glow/particle uses — generated once.
    private static Sprite _cachedWhiteCircle;

    public static Sprite GetWhiteCircleSprite()
    {
        if (_cachedWhiteCircle == null)
            _cachedWhiteCircle = CreateCircleSprite(Color.white);
        return _cachedWhiteCircle;
    }

    public static Sprite CreateCircleSprite(Color color)
    {
        // Create 32x32 circle texture
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[32 * 32];

        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                float dx = i - 16;
                float dy = j - 16;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[i + j * 32] = dist < 16 ? color : new Color(0, 0, 0, 0);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
    }
}

/// <summary>
/// Handles player collision with a power-up.
/// Uses GetComponent instead of CompareTag so no Unity tag registration is needed.
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    public PowerUpManager.PowerUp.PowerUpType powerUpType;

    bool _collected = false;

    void Update()
    {
        if (_collected) return;
        if (GameBootstrap.Instance == null ||
            GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing) return;

        // OverlapCircleAll so the power-up's own trigger collider (same position) never
        // blocks detection — OverlapCircle returns only one result and often returns self.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.0f, ~0);
        bool playerFound = false;
        foreach (var hit in hits)
        {
            if (hit.GetComponent<PlayerController>() != null ||
                hit.GetComponentInParent<PlayerController>() != null)
            { playerFound = true; break; }
        }
        if (!playerFound) return;

        _collected = true;
        Collect();
    }

    void Collect()
    {

        PowerUpManager.PowerUp def = PowerUpManager.GetPowerUpDef(powerUpType);

        PowerUpManager.ActivatePowerUp(powerUpType);

        // --- Burst particles ---
        Vector3 collectPos = transform.position;
        for (int i = 0; i < 8; i++)
        {
            GameObject particleGO = new GameObject("PowerUpParticle");
            particleGO.transform.position = collectPos;
            SpriteRenderer psr = particleGO.AddComponent<SpriteRenderer>();
            psr.sprite = PowerUpSpawner.GetWhiteCircleSprite();
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 15;
            particleGO.transform.localScale = Vector3.one * 0.15f;
            var particle = particleGO.AddComponent<PowerUpParticle>();
            if (def != null) particle.particleColor = def.color;
        }

        // --- Screen-space collection banner (replaces unreadably small world-space label) ---
        if (def != null)
            PowerUpHUD.ShowCollectionBanner(def.name.ToUpper(), def.color);

        Destroy(gameObject);
    }
}

/// <summary>
/// Bobbing animation for power-ups
/// </summary>
public class PowerUpBob : MonoBehaviour
{
    public float amplitude = 0.5f;
    public float frequency = 2f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}

/// <summary>
/// Pulsing scale animation for power-up pickups
/// </summary>
public class PowerUpPulse : MonoBehaviour
{
    Vector3 _base;
    void Start()  { _base = transform.localScale; }
    void Update()
    {
        float s = 1f + 0.12f * Mathf.Sin(Time.time * 3f);
        transform.localScale = _base * s;
    }
}

/// <summary>
/// Burst particle that flies outward and fades on collection
/// </summary>
public class PowerUpParticle : MonoBehaviour
{
    public Color particleColor = Color.white;
    Vector2 vel;
    float life = 0.4f;
    SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = particleColor;
        vel = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
    }

    void Update()
    {
        transform.position += (Vector3)(vel * Time.deltaTime);
        life -= Time.deltaTime;
        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, life / 0.4f);
        if (life <= 0) Destroy(gameObject);
    }
}

/// <summary>
/// Moves the label upward and fades it over 1.2 seconds
/// </summary>
public class FloatingLabel : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    float life = 1.2f;
    float elapsed = 0f;
    Color startColor;

    void Start()
    {
        if (textComponent != null) startColor = textComponent.color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * 2f * Time.deltaTime;
        float alpha = 1f - (elapsed / life);
        if (textComponent != null)
            textComponent.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Max(0, alpha));
        if (elapsed >= life) Destroy(gameObject);
    }
}
