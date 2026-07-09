using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns ambient bubbles around the player using an object pool to avoid
/// repeated Instantiate/Destroy allocation spikes.
/// </summary>
public class BubbleSpawner : MonoBehaviour
{
    [Header("Bubble Settings")]
    public Sprite bubbleSprite;
    public float spawnInterval    = 0.3f;
    public float bubbleLifetime   = 4f;
    public float bubbleScale      = 0.1f;
    public int   minBubblesPerSpawn = 1;
    public int   maxBubblesPerSpawn = 2;
    [Tooltip("Initial pool size; expands automatically if needed.")]
    public int   initialPoolSize  = 12;

    // Pool
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        if (bubbleSprite == null)
            bubbleSprite = Resources.Load<Sprite>("bubble");
    }

    private void Start()
    {
        PreWarmPool(initialPoolSize);
        _spawnCoroutine = StartCoroutine(SpawnAmbientBubbles());
    }

    private void OnDestroy()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
    }

    // ── Pool helpers ──────────────────────────────────────────────────────────

    void PreWarmPool(int count)
    {
        for (int i = 0; i < count; i++)
            _pool.Enqueue(CreatePooledBubble());
    }

    GameObject CreatePooledBubble()
    {
        var go = new GameObject("Bubble");
        go.SetActive(false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bubbleSprite;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.25f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        go.AddComponent<BubbleMovement>();

        return go;
    }

    GameObject GetFromPool()
    {
        if (_pool.Count > 0)
        {
            var go = _pool.Dequeue();
            if (go != null) return go;
        }
        // Pool exhausted — create an extra bubble rather than dropping it
        return CreatePooledBubble();
    }

    public void ReturnToPool(GameObject bubble)
    {
        bubble.SetActive(false);
        _pool.Enqueue(bubble);
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private IEnumerator SpawnAmbientBubbles()
    {
        while (true)
        {
            if (GameBootstrap.Instance != null && !GameBootstrap.Instance.IsGameOver)
            {
                Vector3 playerPos = GameBootstrap.Instance.PlayerPosition;
                int count = Random.Range(minBubblesPerSpawn, maxBubblesPerSpawn + 1);
                for (int i = 0; i < count; i++)
                    SpawnBubble(playerPos);
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

        GameObject bubbleGO = GetFromPool();
        bubbleGO.transform.position = spawnPos;

        float scaleFactor = Random.Range(0.6f, 0.9f);
        bubbleGO.transform.localScale = Vector3.one * bubbleScale * scaleFactor;

        var bm = bubbleGO.GetComponent<BubbleMovement>();
        bm.speedY         = Mathf.Lerp(1.2f, 0.4f, scaleFactor / 0.9f);
        bm.driftX         = Random.Range(-0.1f, 0.1f);
        bm.enableWave     = true;
        bm.waveAmplitude  = Random.Range(0.03f, 0.08f);
        bm.waveFrequency  = Random.Range(1f, 2f);
        bm.lifetime       = bubbleLifetime;
        bm.SetSpawner(this);

        bubbleGO.SetActive(true);
    }
}
