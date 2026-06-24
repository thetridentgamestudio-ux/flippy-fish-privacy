using UnityEngine;

/// <summary>
/// Subtle animated water caustic shimmer — large soft blobs that drift and pulse alpha.
/// Uses transform.localScale for sizing so sprite PPU doesn't matter.
/// Completely self-contained.
/// </summary>
public class OceanAmbience : MonoBehaviour
{
    struct CausticLayer
    {
        public GameObject     go;
        public SpriteRenderer sr;
        public float          driftSpeed;
        public float          alphaBase;
        public float          alphaPulse;
        public float          pulseSpeed;
        public float          pulseOffset;
        public Vector2        driftDir;
    }

    CausticLayer[] _layers;
    static Sprite  _sharedSprite;

    void Start()
    {
        Camera cam   = Camera.main;
        float  halfH = cam.orthographicSize;
        float  halfW = halfH * cam.aspect;

        // Build a shared soft-circle sprite once (16×16 radial gradient texture)
        if (_sharedSprite == null)
            _sharedSprite = BuildSoftSprite();

        // (driftSpeed, alphaBase, alphaPulse, pulseSpeed, driftAngleDeg, sizeMultiplier)
        var cfgs = new (float spd, float ab, float ap, float ps, float ang, float sz)[]
        {
            (0.08f, 0.18f, 0.10f, 0.40f,  15f, 1.8f),
            (0.05f, 0.14f, 0.08f, 0.28f,  -9f, 2.2f),
            (0.10f, 0.12f, 0.08f, 0.45f,  22f, 1.5f),
            (0.06f, 0.16f, 0.09f, 0.22f, -18f, 2.0f),
        };

        _layers = new CausticLayer[cfgs.Length];

        for (int i = 0; i < cfgs.Length; i++)
        {
            var cfg = cfgs[i];
            var go  = new GameObject($"Caustic_{i}");
            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite       = _sharedSprite;
            sr.sortingOrder = -6;           // behind fish (-7 to -9) but in front of bg
            sr.color        = new Color(0.5f, 0.78f, 1f, cfg.ab);

            // Scale transform to world size — this works regardless of PPU
            float sz = halfH * cfg.sz;
            go.transform.localScale = new Vector3(sz, sz, 1f);
            go.transform.position   = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH), 0f);

            float rad = cfg.ang * Mathf.Deg2Rad;
            _layers[i] = new CausticLayer
            {
                go          = go,
                sr          = sr,
                driftSpeed  = cfg.spd,
                alphaBase   = cfg.ab,
                alphaPulse  = cfg.ap,
                pulseSpeed  = cfg.ps,
                pulseOffset = Random.Range(0f, Mathf.PI * 2f),
                driftDir    = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)),
            };
        }
    }

    void Update()
    {
        if (_layers == null) return;

        Camera cam   = Camera.main;
        float  halfW = cam.orthographicSize * cam.aspect;
        float  halfH = cam.orthographicSize;
        float  t     = Time.time;
        float  wrapW = halfW * 3f;
        float  wrapH = halfH * 3f;

        for (int i = 0; i < _layers.Length; i++)
        {
            CausticLayer c = _layers[i];
            if (c.go == null) continue;

            Vector3 pos = c.go.transform.position;
            pos.x += c.driftDir.x * c.driftSpeed * Time.deltaTime;
            pos.y += c.driftDir.y * c.driftSpeed * Time.deltaTime;
            if (pos.x >  wrapW) pos.x -= wrapW * 2f;
            if (pos.x < -wrapW) pos.x += wrapW * 2f;
            if (pos.y >  wrapH) pos.y -= wrapH * 2f;
            if (pos.y < -wrapH) pos.y += wrapH * 2f;
            c.go.transform.position = pos;

            float alpha = c.alphaBase + Mathf.Sin(t * c.pulseSpeed + c.pulseOffset) * c.alphaPulse;
            Color col   = c.sr.color;
            col.a       = Mathf.Clamp(alpha, 0.02f, 1f);
            c.sr.color  = col;

            _layers[i] = c;
        }
    }

    static Sprite BuildSoftSprite()
    {
        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float fx = (x / (S - 1f)) * 2f - 1f;
            float fy = (y / (S - 1f)) * 2f - 1f;
            float d  = Mathf.Clamp01(1f - Mathf.Sqrt(fx * fx + fy * fy));
            byte  a  = (byte)(d * d * 255f);          // smooth falloff
            px[y * S + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        // PPU=1 → sprite is 32×32 world units; transform.localScale will resize it
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 1f);
    }
}
