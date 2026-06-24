using UnityEngine;

/// <summary>
/// Adds subtle animated water caustic layers behind everything.
/// Each layer is a large white sprite that slowly drifts and pulses alpha,
/// giving the impression of light rippling through ocean water.
/// Completely self-contained — no dependencies on other scripts.
/// </summary>
public class OceanAmbience : MonoBehaviour
{
    struct CausticLayer
    {
        public GameObject   go;
        public SpriteRenderer sr;
        public float        driftSpeed;
        public float        alphaBase;
        public float        alphaPulse;
        public float        pulseSpeed;
        public float        pulseOffset;
        public Vector2      driftDir;
    }

    CausticLayer[] _layers;

    void Start()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Each entry: (driftSpeed, alphaBase, alphaPulse, pulseSpeed, driftAngleDeg, sortOrder)
        var configs = new (float ds, float ab, float ap, float ps, float ang, int so)[]
        {
            (0.08f, 0.10f, 0.06f, 0.35f, 12f,  -6),
            (0.05f, 0.08f, 0.05f, 0.28f, -8f,  -5),
            (0.11f, 0.07f, 0.05f, 0.42f, 20f,  -6),
            (0.06f, 0.09f, 0.06f, 0.22f, -15f, -5),
        };

        _layers = new CausticLayer[configs.Length];

        for (int i = 0; i < configs.Length; i++)
        {
            var cfg = configs[i];
            var go = new GameObject($"Caustic_{i}");
            var sr = go.AddComponent<SpriteRenderer>();

            // Create a large soft-edged white sprite
            sr.sprite           = CreateSoftSprite(halfW * 2.4f, halfH * 2.4f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = cfg.so;
            // Light blue tint — subtle underwater colour
            sr.color = new Color(0.55f, 0.82f, 1f, cfg.ab);

            float rad = cfg.ang * Mathf.Deg2Rad;
            float startX = Random.Range(-halfW, halfW);
            float startY = Random.Range(-halfH, halfH);
            go.transform.position = new Vector3(startX, startY, 0f);

            _layers[i] = new CausticLayer
            {
                go          = go,
                sr          = sr,
                driftSpeed  = cfg.ds,
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

        Camera cam  = Camera.main;
        float  halfW = cam.orthographicSize * cam.aspect;
        float  halfH = cam.orthographicSize;
        float  t     = Time.time;

        for (int i = 0; i < _layers.Length; i++)
        {
            CausticLayer c = _layers[i];
            if (c.go == null) continue;

            // Slow drift
            Vector3 pos = c.go.transform.position;
            pos.x += c.driftDir.x * c.driftSpeed * Time.deltaTime;
            pos.y += c.driftDir.y * c.driftSpeed * Time.deltaTime;

            // Wrap when fully off screen
            float wrapW = halfW * 2.8f;
            float wrapH = halfH * 2.8f;
            if (pos.x >  wrapW) pos.x -= wrapW * 2f;
            if (pos.x < -wrapW) pos.x += wrapW * 2f;
            if (pos.y >  wrapH) pos.y -= wrapH * 2f;
            if (pos.y < -wrapH) pos.y += wrapH * 2f;

            c.go.transform.position = pos;

            // Pulse alpha
            float alpha = c.alphaBase + Mathf.Sin(t * c.pulseSpeed + c.pulseOffset) * c.alphaPulse;
            Color col = c.sr.color;
            col.a     = Mathf.Clamp(alpha, 0f, 1f);
            c.sr.color = col;

            _layers[i] = c;
        }
    }

    // Creates a sprite that fades from white at centre to transparent at edges
    static Sprite CreateSoftSprite(float worldW, float worldH)
    {
        int texW = 64, texH = 64;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[texW * texH];
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float fx = (x / (float)(texW - 1)) * 2f - 1f;
                float fy = (y / (float)(texH - 1)) * 2f - 1f;
                float d  = Mathf.Clamp01(1f - Mathf.Sqrt(fx * fx + fy * fy));
                float a  = d * d; // squared = softer falloff
                byte  b  = (byte)(a * 255f);
                pixels[y * texW + x] = new Color32(255, 255, 255, b);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        float ppu = texW / worldW;
        return Sprite.Create(tex, new Rect(0, 0, texW, texH),
                             new Vector2(0.5f, 0.5f), ppu);
    }
}
