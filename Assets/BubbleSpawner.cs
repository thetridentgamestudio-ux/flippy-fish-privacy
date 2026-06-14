using System.Collections;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Bubble Settings")]
    public Sprite bubbleSprite;
    public float spawnInterval = 0.3f; // seconds
    public float bubbleLifetime = 4f;
    public float bubbleScale = 0.1f;
    public int minBubblesPerSpawn = 1;
    public int maxBubblesPerSpawn = 2;

    private void Awake()
    {
        if (bubbleSprite == null)
        {
            bubbleSprite = Resources.Load<Sprite>("bubble");
            if (bubbleSprite == null)
                Debug.LogError("Bubble sprite not found in Resources!");
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnAmbientBubbles());
    }

    private IEnumerator SpawnAmbientBubbles()
    {
        while (true)
        {
            if (GameBootstrap.Instance != null && !GameBootstrap.Instance.IsGameOver)
            {
                Vector3 playerPos = GameBootstrap.Instance.PlayerPosition;
                int bubbleCount = Random.Range(minBubblesPerSpawn, maxBubblesPerSpawn + 1);

                for (int i = 0; i < bubbleCount; i++)
                {
                    SpawnBubble(playerPos);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnBubble(Vector3 playerPos)
    {
        if (bubbleSprite == null || GameBootstrap.Instance == null || GameBootstrap.Instance.IsGameOver)
            return;

        Vector3 spawnPos = playerPos;
        spawnPos.y += Random.Range(0.3f, 1.0f);
        spawnPos.x += Random.Range(-0.5f, 0.5f);

        GameObject bubbleGO = new GameObject("Bubble");
        bubbleGO.transform.position = spawnPos;

        // Sprite
        var sr = bubbleGO.AddComponent<SpriteRenderer>();
        sr.sprite = bubbleSprite;

        // Collider
        var col = bubbleGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.25f;

        // Rigidbody
        var rb = bubbleGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Random scale
        float scaleFactor = Random.Range(0.6f, 0.9f);
        bubbleGO.transform.localScale = Vector3.one * bubbleScale * scaleFactor;

        // Movement
        var bm = bubbleGO.AddComponent<BubbleMovement>();

        // Smaller bubbles rise faster
        float maxSpeedY = 1.2f;
        float minSpeedY = 0.4f;
        bm.speedY = Mathf.Lerp(maxSpeedY, minSpeedY, scaleFactor / 0.9f);

        bm.driftX = Random.Range(-0.1f, 0.1f); // gentle horizontal drift

        // Wave motion for natural effect
        bm.enableWave = true;
        bm.waveAmplitude = Random.Range(0.03f, 0.08f);
        bm.waveFrequency = Random.Range(1f, 2f);

        bm.lifetime = bubbleLifetime;

        // Auto destroy (backup)
        Destroy(bubbleGO, bubbleLifetime + 0.1f);
    }
}