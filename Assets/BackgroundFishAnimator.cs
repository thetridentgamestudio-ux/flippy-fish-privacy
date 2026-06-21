using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns animated background fish using sprite sheets loaded entirely via code.
/// No Unity Editor sprite slicing or Animator setup required.
/// Place sprite sheet PNGs in Assets/Resources/FishSheets/ with Read/Write enabled.
/// </summary>
public class BackgroundFishAnimator : MonoBehaviour
{
    // ── Fish sheet definitions ────────────────────────────────────────────────
    struct FishDef
    {
        public string resourceName; // filename inside Resources/FishSheets/ (no extension)
        public int    frameCount;   // how many frames are in the horizontal strip
        public float  scale;        // world-space scale at spawn
        public int    layer;        // sorting order offset (deeper = more negative)
    }

    static readonly FishDef[] FishDefs = new FishDef[]
    {
        // scale is world-space base size; camera orthographicSize=12.5 so screen height=25 units.
        // A 16px frame at 100 PPU = 0.16 world units, so scale ~4-5 gives a visible ~0.7-0.8 unit fish.
        new FishDef { resourceName = "fish_blue",   frameCount = 8,  scale = 4.5f, layer = -8 },
        new FishDef { resourceName = "fish_yellow", frameCount = 8,  scale = 5.0f, layer = -7 },
        new FishDef { resourceName = "fish_purple", frameCount = 8,  scale = 5.0f, layer = -7 },
        new FishDef { resourceName = "fish_green",  frameCount = 8,  scale = 4.5f, layer = -8 },
        new FishDef { resourceName = "fish_silver", frameCount = 12, scale = 3.5f, layer = -9 },
    };

    // ── Tuning ────────────────────────────────────────────────────────────────
    const float AnimFPS      = 10f;   // frame rate for the flip-book animation
    const float SpawnInterval = 1.8f; // seconds between each new fish
    const float MinSpeed      = 1.5f;
    const float MaxSpeed      = 3.5f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    struct LiveFish
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Sprite[]  frames;
        public float     animTimer;
        public int       frame;
        public float     speed;
        public bool      movingRight; // false = left (normal), true = right (rare)
    }

    Sprite[][]         _sheets;   // pre-sliced frame arrays per fish type
    List<LiveFish>     _fish      = new List<LiveFish>();
    float              _spawnTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        _sheets = new Sprite[FishDefs.Length][];

        for (int i = 0; i < FishDefs.Length; i++)
        {
            FishDef def = FishDefs[i];
            Texture2D tex = Resources.Load<Texture2D>("FishSheets/" + def.resourceName);
            if (tex == null)
            {
                Debug.LogError($"[FishAnim] FAILED to load FishSheets/{def.resourceName} — check: 1) file is in Assets/Resources/FishSheets/, 2) Texture Type = Default, 3) Read/Write Enabled in Inspector");
                _sheets[i] = null;
                continue;
            }

            // Slice horizontally into equal frames — pure runtime code, no editor slicing
            int frameW = tex.width / def.frameCount;
            int frameH = tex.height;
            _sheets[i] = new Sprite[def.frameCount];
            for (int f = 0; f < def.frameCount; f++)
            {
                // Unity's Texture2D origin is bottom-left, so y=0 is correct for a single-row sheet
                Rect rect = new Rect(f * frameW, 0, frameW, frameH);
                _sheets[i][f] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
            }

            Debug.Log($"[FishAnim] Loaded {def.resourceName}: {def.frameCount} frames @ {frameW}x{frameH}px, world size ~{frameH/100f*def.scale:F2} units tall");
        }
    }

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        // Fish swim in background during menu AND gameplay — pause only on game-over
        if (GameBootstrap.Instance.CurrentState == GameBootstrap.GameState.GameOver) return;

        // Spawn timer
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = SpawnInterval + Random.Range(-0.3f, 0.3f); // slight jitter
            SpawnFish();
        }

        // Update all live fish
        float dt = Time.deltaTime;
        Camera cam = Camera.main;
        float killX = cam.orthographicSize * cam.aspect + 5f;

        for (int i = _fish.Count - 1; i >= 0; i--)
        {
            LiveFish f = _fish[i];
            if (f.go == null) { _fish.RemoveAt(i); continue; }

            // Move
            float dir = f.movingRight ? 1f : -1f;
            f.go.transform.position += Vector3.right * dir * f.speed * dt;

            // Advance animation frame
            f.animTimer += dt;
            if (f.animTimer >= 1f / AnimFPS)
            {
                f.animTimer = 0f;
                f.frame = (f.frame + 1) % f.frames.Length;
                f.sr.sprite = f.frames[f.frame];
            }

            _fish[i] = f;

            // Kill when off-screen
            float posX = f.go.transform.position.x;
            if ((!f.movingRight && posX < -killX) || (f.movingRight && posX > killX))
            {
                Destroy(f.go);
                _fish.RemoveAt(i);
            }
        }
    }

    void SpawnFish()
    {
        // Pick a random fish type that loaded successfully
        int typeIndex = -1;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int t = Random.Range(0, FishDefs.Length);
            if (_sheets[t] != null && _sheets[t].Length > 0) { typeIndex = t; break; }
        }
        if (typeIndex < 0) return;

        FishDef def       = FishDefs[typeIndex];
        Sprite[] frames   = _sheets[typeIndex];

        Camera cam = Camera.main;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        // 15% chance fish swims right (from left side)
        bool movingRight = Random.Range(0f, 1f) < 0.15f;
        float spawnX = movingRight ? -(halfW + 2f) : (halfW + 2f);

        // Y: spread across the full water column, avoid very top and very bottom
        float groundTop = GameBootstrap.Instance != null
            ? GameBootstrap.Instance.groundTop
            : -halfH + 2.5f;
        float spawnY = Random.Range(groundTop + 0.5f, halfH * 0.75f);

        // Depth layer: 3 parallax bands
        int band = Random.Range(0, 3); // 0 = far, 1 = mid, 2 = near
        float speedMulti  = 1f + band * 0.4f;
        float scaleMulti  = 0.6f + band * 0.3f;
        float alpha       = 0.4f + band * 0.25f; // far = dimmer

        GameObject go = new GameObject($"BgFish_{def.resourceName}_{band}");
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite        = frames[Random.Range(0, frames.Length)];
        sr.sortingOrder  = def.layer + band;
        Color c          = Color.white;
        c.a              = alpha;
        sr.color         = c;

        float s = def.scale * scaleMulti;
        // Flip to face direction of travel; right-moving fish flip on X
        float flipX = movingRight ? -1f : 1f;
        go.transform.localScale = new Vector3(s * flipX, s, 1f);

        LiveFish lf = new LiveFish
        {
            go          = go,
            sr          = sr,
            frames      = frames,
            animTimer   = 0f,
            frame       = Random.Range(0, frames.Length), // random start frame
            speed       = Random.Range(MinSpeed, MaxSpeed) * speedMulti,
            movingRight = movingRight,
        };
        _fish.Add(lf);
    }

    public void ClearAll()
    {
        foreach (var f in _fish)
            if (f.go != null) Destroy(f.go);
        _fish.Clear();
        _spawnTimer = 0f;
    }
}
