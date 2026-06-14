using UnityEngine;

/// <summary>
/// Spawns visual power-up objects in game world
/// Drifts left like coins, player collects
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    public static GameObject SpawnPowerUp(Vector3 position, PowerUpManager.PowerUp.PowerUpType type)
    {
        PowerUpManager.PowerUp def = PowerUpManager.GetPowerUpDef(type);
        if (def == null) return null;

        // Create game object
        GameObject powerUpGO = new GameObject($"PowerUp_{type}");
        powerUpGO.transform.position = position;

        // Visual: sphere collider + sprite renderer (use filled circle)
        SpriteRenderer sr = powerUpGO.AddComponent<SpriteRenderer>();
        sr.color = def.color;
        sr.sprite = CreateCircleSprite(def.color);
        sr.sortingOrder = 1; // above pipes

        // Collision
        CircleCollider2D cc = powerUpGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;

        // Physics
        Rigidbody2D rb = powerUpGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(-5f, 0); // drift left like coins

        // Bobbing animation
        var bobScript = powerUpGO.AddComponent<PowerUpBob>();
        bobScript.amplitude = 0.5f;
        bobScript.frequency = 2f;

        // Tag for detection
        powerUpGO.tag = "PowerUp";
        powerUpGO.layer = LayerMask.NameToLayer("Default");

        // Destroy after 30 seconds off-screen
        Destroy(powerUpGO, 30f);

        Debug.Log($"Power-Up spawned: {def.name}");
        return powerUpGO;
    }

    static Sprite CreateCircleSprite(Color color)
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