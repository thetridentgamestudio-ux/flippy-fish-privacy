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
        public int    gridRows;
        public float  scale;
        public int    sortOrder;
        public float  speedMul;   // 0 = use default (1.0). Lower = slower.
    }

    static readonly CreatureDef[] Defs = new CreatureDef[]
    {
        // gridRows defaults to 0 in C# struct — treated as 1 in AutoSlice
        new CreatureDef { resourceName = "fish_blue",   frameCount = 8,  scale = 1.1f, sortOrder = -8 },
        new CreatureDef { resourceName = "fish_yellow", frameCount = 8,  scale = 1.2f, sortOrder = -7 },
        new CreatureDef { resourceName = "fish_purple", frameCount = 8,  scale = 1.2f, sortOrder = -7 },
        new CreatureDef { resourceName = "fish_green",  frameCount = 8,  scale = 1.1f, sortOrder = -8 },
        new CreatureDef { resourceName = "fish_silver", frameCount = 12, scale = 0.8f, sortOrder = -9 },
        new CreatureDef { resourceName = "fish_school", frameCount = 8,  gridRows = 2, scale = 3.5f, sortOrder = -8, speedMul = 0.35f },
        new CreatureDef { resourceName = "jellyfish_1", frameCount = 8,  scale = 2.6f, sortOrder = -8 },
        new CreatureDef { resourceName = "jellyfish_2", frameCount = 8,  scale = 2.8f, sortOrder = -7 },
        new CreatureDef { resourceName = "jellyfish_3", frameCount = 8,  scale = 2.4f, sortOrder = -9 },
        new CreatureDef { resourceName = "jellyfish_4", frameCount = 8,  scale = 2.6f, sortOrder = -8 },
        new CreatureDef { resourceName = "jellyfish_5", frameCount = 8,  scale = 3.0f, sortOrder = -7 },
    };

    const float AnimFPS          = 12f;
    const float FishSpawnInterval = 2.5f;  // seconds between fish spawns
    const float JellyDelay        = 10f;   // seconds before first jellyfish appears
    const float JellySpawnInterval = 10f;  // one jellyfish every 10 seconds
    const int   MaxJellyOnScreen   = 1;    // never more than 1 jellyfish visible at once
    const float MinSpeed           = 1.5f;
    const float MaxSpeed           = 3.5f;
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

            int rows = def.gridRows <= 1 ? 1 : def.gridRows;
            Sprite[] frames = AutoSlice(tex, def.frameCount, rows, def.resourceName);
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
    // gridRows=1: single horizontal strip. gridRows=2: 2 rows of (frameCount/2) columns each.
    Sprite[] AutoSlice(Texture2D tex, int frameCount, int gridRows, string name)
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

        if (regions.Count == 0) return null;

        // ── Grid-row support ──────────────────────────────────────────────────
        // For gridRows=1: use the single detected content band (current behaviour).
        // For gridRows=2+: find horizontal row bands (separated by transparent row gaps),
        //   slice each band into columns independently, read top→bottom, left→right.
        if (gridRows > 1)
            return AutoSliceGrid(tex, frameCount, gridRows, name, w, h, pixels);

        // ── Single-row: build sprites from detected column regions ─────────────
        int expectedCols = frameCount;
        if (regions.Count != expectedCols)
            Debug.LogWarning($"[FishAnim] {name}: expected {expectedCols} frames, detected {regions.Count}.");

        var sprites = new Sprite[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            int fx = regions[i].start;
            int fw = regions[i].end - regions[i].start + 1;
            Rect rect = new Rect(fx, unityY, fw, contentH);
            sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
        }
        return sprites;
    }

    // ── Grid slicer: for sheets with multiple rows of frames ──────────────────
    // Handles two cases:
    //   A) Sheet has transparent gaps between frames → gap-detect each cell
    //   B) Sheet fills edge-to-edge (no gaps) → uniform equal-size grid division
    Sprite[] AutoSliceGrid(Texture2D tex, int totalFrames, int gridRows,
                           string name, int w, int h, Color32[] pixels)
    {
        int colsPerRow = totalFrames / gridRows;
        int cellW      = w / colsPerRow;
        int cellH      = h / gridRows;

        // Try gap-detection for rows first
        bool[] rowHasContent = new bool[h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pixels[y * w + x].a > AlphaThresh) { rowHasContent[y] = true; break; }

        var rowBands = new List<(int yMin, int yMax)>();
        bool inBand = false; int bStart = 0;
        for (int y = 0; y < h; y++)
        {
            if (rowHasContent[y]  && !inBand) { inBand = true;  bStart = y; }
            if (!rowHasContent[y] && inBand)  { inBand = false; rowBands.Add((bStart, y - 1)); }
        }
        if (inBand) rowBands.Add((bStart, h - 1));

        bool useUniformGrid = rowBands.Count != gridRows;
        Debug.Log($"[FishAnim] {name}: {rowBands.Count} row bands detected " +
                  $"(need {gridRows}) → {(useUniformGrid ? "UNIFORM grid" : "gap-detect")} mode, cell={cellW}x{cellH}");

        var sprites = new Sprite[totalFrames];

        if (useUniformGrid)
        {
            // Case B: no transparent gaps — divide sheet into equal-size cells.
            // Unity GetPixels32 y=0=bottom; image row 0 = top visually.
            // We read top-row first (left→right), then bottom row, etc.
            // Top row in image = highest Unity y values.
            for (int r = 0; r < gridRows; r++)          // r=0 = top visual row
            {
                // Convert image row r to Unity y (image top row → Unity high y)
                int unityRowY = h - (r + 1) * cellH;    // bottom-left y of this row in Unity coords
                unityRowY     = Mathf.Max(0, unityRowY);
                int rowH      = Mathf.Min(cellH, h - unityRowY);

                for (int c = 0; c < colsPerRow; c++)    // c=0 = leftmost column
                {
                    int fx   = c * cellW;
                    int fw   = (c == colsPerRow - 1) ? w - fx : cellW; // last col gets remainder
                    Rect rect = new Rect(fx, unityRowY, fw, rowH);
                    sprites[r * colsPerRow + c] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        else
        {
            // Case A: transparent gaps exist — use detected bands, gap-detect columns per band
            // rowBands[0] = bottom visual row in Unity coords (lowest y)
            for (int row = gridRows - 1; row >= 0; row--) // row=gridRows-1 → top visual row
            {
                int bandIdx  = gridRows - 1 - row;          // maps visual-top → rowBands index
                var band     = rowBands[Mathf.Min(bandIdx, rowBands.Count - 1)];
                int unityY   = Mathf.Max(0, band.yMin - 2);
                int bandH    = Mathf.Min(h, band.yMax + 3) - unityY;

                bool[] colHas = new bool[w];
                for (int x = 0; x < w; x++)
                    for (int y = band.yMin; y <= band.yMax; y++)
                        if (pixels[y * w + x].a > AlphaThresh) { colHas[x] = true; break; }

                var colRegions = new List<(int s, int e)>();
                bool inReg = false; int rs = 0;
                for (int x = 0; x < w; x++)
                {
                    if (colHas[x]  && !inReg) { inReg = true;  rs = x; }
                    if (!colHas[x] && inReg)  { inReg = false; colRegions.Add((rs, x - 1)); }
                }
                if (inReg) colRegions.Add((rs, w - 1));

                int visualRow = gridRows - 1 - row; // 0=top
                for (int c = 0; c < colRegions.Count && c < colsPerRow; c++)
                {
                    int fx = colRegions[c].s, fw = colRegions[c].e - colRegions[c].s + 1;
                    Rect rect = new Rect(fx, unityY, fw, bandH);
                    sprites[visualRow * colsPerRow + c] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }
            }
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
        // Fish are indices 0-5 (fish_blue..fish_school); jellyfish start at 6
        int defIdx = -1;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            int t = Random.Range(0, 6); // fish are first 6 entries
            if (_sheets != null && _sheets[t] != null && _sheets[t].Length > 0)
            { defIdx = t; break; }
        }
        if (defIdx < 0) return;
        SpawnCreature(defIdx, false);
    }

    void SpawnJellyfish()
    {
        // Jellyfish start at index 6 now (fish_school is index 5)
        int defIdx = -1;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            int t = Random.Range(6, Defs.Length);
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
            speed       = Random.Range(MinSpeed, MaxSpeed) * (1f + band * 0.35f) * (def.speedMul > 0f ? def.speedMul : 1f),
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
