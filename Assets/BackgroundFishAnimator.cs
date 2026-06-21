using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth background fish/jellyfish animation.
///
/// At startup, scans each sprite sheet's pixel data to auto-detect the exact pixel
/// boundaries of every frame — handles padding, uneven gaps, and vertical offsets
/// without any Unity Editor sprite slicing.
///
/// Requires: Assets/Resources/FishSheets/*.png
///   Texture Type   = Default
///   Read/Write     = Enabled
///   Wrap Mode      = Clamp   (set via code below, no editor step needed)
/// </summary>
public class BackgroundFishAnimator : MonoBehaviour
{
    // ── Creature definitions ──────────────────────────────────────────────────
    struct CreatureDef
    {
        public string resourceName;
        public int    frameCount;
        public float  scale;      // world-space height in units
        public int    sortOrder;
    }

    static readonly CreatureDef[] Defs = new CreatureDef[]
    {
        new CreatureDef { resourceName = "fish_blue",   frameCount = 8,  scale = 1.1f, sortOrder = -8 },
        new CreatureDef { resourceName = "fish_yellow", frameCount = 8,  scale = 1.2f, sortOrder = -7 },
        new CreatureDef { resourceName = "fish_purple", frameCount = 8,  scale = 1.2f, sortOrder = -7 },
        new CreatureDef { resourceName = "fish_green",  frameCount = 8,  scale = 1.1f, sortOrder = -8 },
        new CreatureDef { resourceName = "fish_silver", frameCount = 12, scale = 0.8f, sortOrder = -9 },
        new CreatureDef { resourceName = "jellyfish_1", frameCount = 8,  scale = 2.6f, sortOrder = -8 },
        new CreatureDef { resourceName = "jellyfish_2", frameCount = 8,  scale = 2.8f, sortOrder = -7 },
        new CreatureDef { resourceName = "jellyfish_3", frameCount = 8,  scale = 2.4f, sortOrder = -9 },
        new CreatureDef { resourceName = "jellyfish_4", frameCount = 8,  scale = 2.6f, sortOrder = -8 },
        new CreatureDef { resourceName = "jellyfish_5", frameCount = 8,  scale = 3.0f, sortOrder = -7 },
    };

    const float AnimFPS          = 12f;
    const float FishSpawnInterval = 2.5f;  // seconds between fish spawns
    const float JellyDelay        = 20f;   // seconds before first jellyfish appears
    const float JellySpawnInterval = 18f;  // one jellyfish every 18 seconds
    const int   MaxJellyOnScreen   = 1;    // never more than 1 jellyfish visible at once
    const float MinSpeed           = 0.8f;
    const float MaxSpeed           = 1.6f;
    const byte  AlphaThresh        = 12;

    // ── Per-creature runtime ──────────────────────────────────────────────────
    struct LiveCreature
    {
        public GameObject    go;
        public SpriteRenderer sr;
        public Sprite[]      frames;
        public float         animTimer;
        public int           frame;
        public float         speed;
        public bool          movingRight;
        public float         baseY;
        public float         bobPhase;
        public float         bobSpeed;
        public float         bobAmp;
        public bool          isJellyfish;
    }

    Sprite[][]         _sheets;
    List<LiveCreature> _creatures    = new List<LiveCreature>();
    float              _fishTimer    = 0f;
    float              _jellyTimer   = JellyDelay;  // starts counting down after JellyDelay
    float              _elapsedTime  = 0f;
    int                _jellyCount   = 0;           // jellyfish currently on screen

    // ── Start: load and auto-slice all sheets ─────────────────────────────────
    void Start()
    {
        _sheets = new Sprite[Defs.Length][];

        for (int i = 0; i < Defs.Length; i++)
        {
            CreatureDef def = Defs[i];
            Texture2D tex = Resources.Load<Texture2D>("FishSheets/" + def.resourceName);
            if (tex == null)
            {
                Debug.LogError($"[FishAnim] Cannot load FishSheets/{def.resourceName}. " +
                               "Check: file in Assets/Resources/FishSheets/, " +
                               "Texture Type = Default, Read/Write = Enabled.");
                continue;
            }

            tex.wrapMode   = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Sprite[] frames = AutoSlice(tex, def.frameCount, def.resourceName);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[FishAnim] AutoSlice failed for {def.resourceName} — no content found.");
                continue;
            }

            _sheets[i] = frames;
            Debug.Log($"[FishAnim] {def.resourceName}: sliced {frames.Length} frames " +
                      $"(rect0 = {frames[0].rect})");
        }
    }

    // ── Auto-slice: finds exact pixel rect of each frame by reading alpha data ─
    Sprite[] AutoSlice(Texture2D tex, int frameCount, string name)
    {
        int w = tex.width, h = tex.height;
        Color32[] pixels = tex.GetPixels32();

        // 1. Find vertical content extent (rows that have any opaque pixel)
        int yMin = h, yMax = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > AlphaThresh)
                {
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                    break;
                }
            }
        }

        if (yMax < yMin)
        {
            Debug.LogError($"[FishAnim] {name}: no opaque pixels found at all");
            return null;
        }

        // GetPixels32() already uses Unity coords (y=0 = bottom).
        // yMin = bottom of content, yMax = top of content — use directly, no flip needed.
        // Add 2px padding on all sides so antialiased edges are never clipped.
        int pad      = 2;
        int unityY   = Mathf.Max(0, yMin - pad);
        int contentH = Mathf.Min(h, yMax + pad + 1) - unityY;

        // 2. Find horizontal content columns (columns that have any opaque pixel in content rows)
        bool[] hasContent = new bool[w];
        for (int x = 0; x < w; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                if (pixels[y * w + x].a > AlphaThresh)
                {
                    hasContent[x] = true;
                    break;
                }
            }
        }

        // 3. Group content columns into frame regions (separated by transparent gaps)
        var regions = new List<(int start, int end)>();
        bool inRegion = false;
        int rStart = 0;
        for (int x = 0; x < w; x++)
        {
            if (hasContent[x] && !inRegion)  { inRegion = true; rStart = x; }
            if (!hasContent[x] && inRegion)  { inRegion = false; regions.Add((rStart, x - 1)); }
        }
        if (inRegion) regions.Add((rStart, w - 1));

        Debug.Log($"[FishAnim] {name}: detected {regions.Count} content regions " +
                  $"(expected {frameCount}), yMin={yMin} yMax={yMax} unityY={unityY} contentH={contentH}");

        // 4. If detected frame count doesn't match expectation, warn but use what we found
        if (regions.Count != frameCount)
            Debug.LogWarning($"[FishAnim] {name}: expected {frameCount} frames but detected {regions.Count}. " +
                             "Using detected count. Update frameCount in Defs[] to match.");

        if (regions.Count == 0) return null;

        // 5. Build sprites — one per detected region, cropped exactly to content
        int maxFrameW = 0;
        foreach (var r in regions) maxFrameW = Mathf.Max(maxFrameW, r.end - r.start + 1);

        var sprites = new Sprite[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            int fx = regions[i].start;
            int fw = regions[i].end - regions[i].start + 1;
            // Rect in Unity coords (Y from bottom)
            Rect rect = new Rect(fx, unityY, fw, contentH);
            sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
        }

        return sprites;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.CurrentState == GameBootstrap.GameState.GameOver) return;

        float dt2 = Time.deltaTime;
        _elapsedTime += dt2;

        // Fish spawn immediately and continuously
        _fishTimer -= dt2;
        if (_fishTimer <= 0f)
        {
            _fishTimer = FishSpawnInterval + Random.Range(-0.5f, 0.5f);
            SpawnFish();
        }

        // Jellyfish: wait JellyDelay seconds, then max 1 on screen at a time
        if (_elapsedTime >= JellyDelay)
        {
            _jellyTimer -= dt2;
            if (_jellyTimer <= 0f && _jellyCount < MaxJellyOnScreen)
            {
                _jellyTimer = JellySpawnInterval + Random.Range(-2f, 2f);
                SpawnJellyfish();
            }
        }

        float  dt   = Time.deltaTime;
        Camera cam  = Camera.main;
        float  kill = cam.orthographicSize * cam.aspect + 5f;

        for (int i = _creatures.Count - 1; i >= 0; i--)
        {
            LiveCreature c = _creatures[i];
            if (c.go == null) { _creatures.RemoveAt(i); continue; }

            // Smooth horizontal movement
            float newX = c.go.transform.position.x + (c.movingRight ? 1f : -1f) * c.speed * dt;

            // Absolute-Y bob — never accumulates, always oscillates around baseY
            c.bobPhase += c.bobSpeed * dt;
            float newY = c.baseY + Mathf.Sin(c.bobPhase) * c.bobAmp;

            c.go.transform.position = new Vector3(newX, newY, 0f);

            // Frame advance
            c.animTimer += dt;
            if (c.animTimer >= 1f / AnimFPS)
            {
                c.animTimer -= 1f / AnimFPS;
                c.frame = (c.frame + 1) % c.frames.Length;
                c.sr.sprite = c.frames[c.frame];
            }

            _creatures[i] = c;

            if ((!c.movingRight && newX < -kill) || (c.movingRight && newX > kill))
            {
                if (c.isJellyfish) _jellyCount--;
                Destroy(c.go);
                _creatures.RemoveAt(i);
            }
        }
    }

    // ── Spawn helpers ─────────────────────────────────────────────────────────
    void SpawnFish()
    {
        // Pick randomly from fish types only (indices 0-4)
        int defIdx = -1;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            int t = Random.Range(0, 5); // fish are first 5 entries
            if (_sheets != null && _sheets[t] != null && _sheets[t].Length > 0)
            { defIdx = t; break; }
        }
        if (defIdx < 0) return;
        SpawnCreature(defIdx, false);
    }

    void SpawnJellyfish()
    {
        // Pick randomly from jellyfish types only (indices 5-9)
        int defIdx = -1;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            int t = Random.Range(5, Defs.Length);
            if (_sheets != null && _sheets[t] != null && _sheets[t].Length > 0)
            { defIdx = t; break; }
        }
        if (defIdx < 0) return;
        _jellyCount++;
        SpawnCreature(defIdx, true);
    }

    void SpawnCreature(int defIdx, bool isJellyfish)
    {
        int defIdx2 = defIdx; // keep original name for clarity

        CreatureDef def    = Defs[defIdx2];
        Sprite[]    frames = _sheets[defIdx2];

        Camera cam  = Camera.main;
        float  halfW = cam.orthographicSize * cam.aspect;
        float  halfH = cam.orthographicSize;

        bool  right  = Random.Range(0f, 1f) < 0.15f;
        float spawnX = right ? -(halfW + 2f) : (halfW + 2f);
        float groundTop = GameBootstrap.Instance != null
            ? GameBootstrap.Instance.groundTop : -halfH + 2.5f;
        float spawnY = Random.Range(groundTop + 0.5f, halfH * 0.75f);

        int   band      = Random.Range(0, 3);
        float scaleMul  = 0.55f + band * 0.25f;
        float alpha     = 0.22f + band * 0.16f;

        GameObject go = new GameObject($"BgCreature_{def.resourceName}");
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite         = frames[Random.Range(0, frames.Length)];
        sr.sortingOrder   = def.sortOrder + band;
        Color col         = Color.white; col.a = alpha;
        sr.color          = col;

        float s     = def.scale * scaleMul;
        float flipX = right ? -1f : 1f;
        go.transform.localScale = new Vector3(s * flipX, s, 1f);

        _creatures.Add(new LiveCreature
        {
            go          = go,
            sr          = sr,
            frames      = frames,
            animTimer   = Random.Range(0f, 1f / AnimFPS),
            frame       = Random.Range(0, frames.Length),
            speed       = Random.Range(MinSpeed, MaxSpeed) * (1f + band * 0.35f),
            movingRight = right,
            baseY       = spawnY,
            bobPhase    = Random.Range(0f, Mathf.PI * 2f),
            bobSpeed    = Random.Range(0.8f, 1.8f),
            bobAmp      = Random.Range(0.05f, 0.18f),
            isJellyfish = isJellyfish,
        });
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    public void ClearAll()
    {
        foreach (var c in _creatures)
            if (c.go != null) Destroy(c.go);
        _creatures.Clear();
        _fishTimer   = 0f;
        _jellyTimer  = JellyDelay;
        _elapsedTime = 0f;
        _jellyCount  = 0;
    }
}
