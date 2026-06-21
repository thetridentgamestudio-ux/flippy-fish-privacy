using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth background fish/jellyfish animation.
/// Uses Material.mainTextureOffset (UV scroll) instead of SpriteRenderer.sprite swapping —
/// no mesh rebuild per frame, zero flicker, perfectly smooth at any game FPS.
///
/// Place PNGs in Assets/Resources/FishSheets/ with:
///   Texture Type = Default, Read/Write Enabled (required for Sprite.Create slicing)
///   Wrap Mode    = Clamp
/// </summary>
public class BackgroundFishAnimator : MonoBehaviour
{
    // ── Creature definitions ──────────────────────────────────────────────────
    struct CreatureDef
    {
        public string resourceName;
        public int    frameCount;   // frames in horizontal strip
        public float  scale;        // world-space height target (units)
        public int    sortOrder;    // base sorting order
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

    // ── Tuning ────────────────────────────────────────────────────────────────
    const float AnimFPS       = 12f;   // animation frames per second
    const float SpawnInterval = 1.8f;
    const float MinSpeed      = 1.2f;
    const float MaxSpeed      = 2.8f;

    // ── Per-creature runtime data ─────────────────────────────────────────────
    struct LiveCreature
    {
        public GameObject go;
        public Material   mat;
        public int        frameCount;
        public float      frameDuration;  // 1 / AnimFPS
        public float      animTimer;
        public int        frame;
        public float      speed;
        public bool       movingRight;
        public float      baseY;          // spawn Y — bob oscillates around this
        public float      bobPhase;       // current sine phase
        public float      bobSpeed;       // radians/sec
        public float      bobAmp;         // world units amplitude
    }

    // ── Loaded textures indexed by Defs[] ─────────────────────────────────────
    Texture2D[]        _textures;
    List<LiveCreature> _creatures = new List<LiveCreature>();
    float              _spawnTimer;

    static Mesh _quadMesh;  // shared unit quad — created once

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        _textures = new Texture2D[Defs.Length];
        for (int i = 0; i < Defs.Length; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>("FishSheets/" + Defs[i].resourceName);
            if (tex == null)
            {
                Debug.LogError($"[FishAnim] Cannot load FishSheets/{Defs[i].resourceName}. " +
                               "Check: file exists in Assets/Resources/FishSheets/, " +
                               "Texture Type = Default, Read/Write = Enabled.");
                continue;
            }
            tex.wrapMode   = TextureWrapMode.Clamp;   // prevents edge bleed between frames
            tex.filterMode = FilterMode.Bilinear;
            _textures[i]   = tex;
            Debug.Log($"[FishAnim] Loaded {Defs[i].resourceName} ({Defs[i].frameCount} frames)");
        }
    }

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.CurrentState == GameBootstrap.GameState.GameOver) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = SpawnInterval + Random.Range(-0.4f, 0.4f);
            Spawn();
        }

        float dt  = Time.deltaTime;
        Camera cam = Camera.main;
        float killX = cam.orthographicSize * cam.aspect + 5f;

        for (int i = _creatures.Count - 1; i >= 0; i--)
        {
            LiveCreature c = _creatures[i];
            if (c.go == null) { _creatures.RemoveAt(i); continue; }

            // ── Smooth horizontal movement ────────────────────────────────────
            float dir = c.movingRight ? 1f : -1f;
            float newX = c.go.transform.position.x + dir * c.speed * dt;

            // ── Smooth vertical bob — absolute Y, never accumulates ───────────
            c.bobPhase += c.bobSpeed * dt;
            float newY = c.baseY + Mathf.Sin(c.bobPhase) * c.bobAmp;

            c.go.transform.position = new Vector3(newX, newY, 0f);

            // ── UV-offset animation — no mesh rebuild, no flicker ─────────────
            c.animTimer += dt;
            if (c.animTimer >= c.frameDuration)
            {
                c.animTimer -= c.frameDuration;
                c.frame = (c.frame + 1) % c.frameCount;
                // Slide the UV window to the current frame column
                c.mat.mainTextureOffset = new Vector2((float)c.frame / c.frameCount, 0f);
            }

            _creatures[i] = c;

            // ── Cull off-screen ───────────────────────────────────────────────
            if ((!c.movingRight && newX < -killX) || (c.movingRight && newX > killX))
            {
                Destroy(c.mat);   // material is per-instance — must destroy manually
                Destroy(c.go);
                _creatures.RemoveAt(i);
            }
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    void Spawn()
    {
        // Pick a random loaded creature type
        int defIdx = -1;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            int t = Random.Range(0, Defs.Length);
            if (_textures[t] != null) { defIdx = t; break; }
        }
        if (defIdx < 0) return;

        CreatureDef def = Defs[defIdx];
        Texture2D   tex = _textures[defIdx];

        Camera cam  = Camera.main;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        bool  movingRight = Random.Range(0f, 1f) < 0.15f;
        float spawnX      = movingRight ? -(halfW + 2f) : (halfW + 2f);

        float groundTop = GameBootstrap.Instance != null
            ? GameBootstrap.Instance.groundTop
            : -halfH + 2.5f;
        float spawnY = Random.Range(groundTop + 0.5f, halfH * 0.75f);

        // Depth band: 0=far (slow, small, dim), 1=mid, 2=near
        int   band       = Random.Range(0, 3);
        float speedMul   = 1f + band * 0.35f;
        float scaleMul   = 0.55f + band * 0.25f;
        float alpha      = 0.22f + band * 0.16f;

        // Build a quad sized to match one animation frame
        int   frameW  = tex.width / def.frameCount;
        int   frameH  = tex.height;
        float aspect  = (float)frameW / frameH;
        float worldH  = def.scale * scaleMul;
        float worldW  = worldH * aspect;

        GameObject go = new GameObject($"BgCreature_{def.resourceName}_{band}");
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = GetQuadMesh();

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows       = false;
        mr.sortingLayerName     = "Default";
        mr.sortingOrder         = def.sortOrder + band;

        // Per-instance material so each creature has its own UV offset
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture      = tex;
        // Show only one frame column at a time
        mat.mainTextureScale  = new Vector2(1f / def.frameCount, 1f);
        mat.mainTextureOffset = new Vector2(0f, 0f);
        Color col = Color.white;
        col.a     = alpha;
        mat.color = col;
        mr.material = mat;

        // Scale: X flipped when moving right so fish faces travel direction
        float flipX = movingRight ? -1f : 1f;
        go.transform.localScale = new Vector3(worldW * flipX, worldH, 1f);

        _creatures.Add(new LiveCreature
        {
            go            = go,
            mat           = mat,
            frameCount    = def.frameCount,
            frameDuration = 1f / AnimFPS,
            animTimer     = Random.Range(0f, 1f / AnimFPS), // stagger so not all flip at once
            frame         = Random.Range(0, def.frameCount),
            speed         = Random.Range(MinSpeed, MaxSpeed) * speedMul,
            movingRight   = movingRight,
            baseY         = spawnY,
            bobPhase      = Random.Range(0f, Mathf.PI * 2f),
            bobSpeed      = Random.Range(0.8f, 1.8f),
            bobAmp        = Random.Range(0.05f, 0.18f),
        });
    }

    // ── Shared unit quad mesh ─────────────────────────────────────────────────
    static Mesh GetQuadMesh()
    {
        if (_quadMesh != null) return _quadMesh;
        _quadMesh = new Mesh { name = "BgFishQuad" };
        _quadMesh.vertices  = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
        };
        _quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        _quadMesh.uv        = new Vector2[] {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1),
        };
        _quadMesh.RecalculateNormals();
        return _quadMesh;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    public void ClearAll()
    {
        foreach (var c in _creatures)
        {
            if (c.mat != null) Destroy(c.mat);
            if (c.go  != null) Destroy(c.go);
        }
        _creatures.Clear();
        _spawnTimer = 0f;
    }

    void OnDestroy()
    {
        ClearAll();
        _quadMesh = null;
    }
}
