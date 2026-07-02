using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Coin in the pipe gap.
/// Collection uses a static list + distance check in GameBootstrap.Update()
/// — no physics dependency, works at any framerate on any device.
public class PipeCoin : MonoBehaviour
{
    public int value = 1;

    // All live coins — GameBootstrap polls this every frame
    public static readonly List<PipeCoin> Active = new List<PipeCoin>();

    SpriteRenderer _sr;
    public bool Collected { get; private set; }

    float _originY;
    const float BOB_AMP  = 0.18f;
    const float BOB_FREQ = 1.2f;
    float _phase;

    void Awake()
    {
        _originY = transform.position.y;
        _phase   = Random.Range(0f, Mathf.PI * 2f);

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite       = MakeCoinSprite();
        _sr.sortingOrder = 5;

        Active.Add(this);
    }

    void OnDestroy() => Active.Remove(this);

    void Update()
    {
        if (Collected) return;

        // Bob up/down
        float y = _originY + Mathf.Sin(Time.time * BOB_FREQ * Mathf.PI * 2f + _phase) * BOB_AMP;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Spin illusion
        float scaleX = Mathf.Abs(Mathf.Cos(Time.time * 2f));
        _sr.transform.localScale = new Vector3(Mathf.Max(scaleX, 0.05f), 1f, 1f);

        // Auto-destroy off screen
        if (transform.position.x < -15f) Destroy(gameObject);
    }

    public void Collect()
    {
        if (Collected) return;
        Collected = true;
        GameBootstrap.Instance?.CollectGoldCoin();
        try { CoinParticles.PlayCoinSparkles(transform.position); } catch { }
        StartCoroutine(CollectAnim());
    }

    IEnumerator CollectAnim()
    {
        float t = 0f;
        Vector3 startScale = new Vector3(1f, 1f, 1f);
        _sr.transform.localScale = startScale;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float p = t / 0.3f;
            _sr.transform.localScale = startScale * (1f + p * 0.8f);
            Color c = _sr.color;
            c.a = 1f - p;
            _sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    static Sprite MakeCoinSprite()
    {
        const int   size  = 96;
        const float half  = size * 0.5f;
        const float outer = 44f;
        const float rim   = 38f;
        const float inner = 30f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[size * size];

        Color32 clear     = new Color32(  0,   0,   0,   0);
        Color32 rimCol    = new Color32(255, 200,  20, 255);
        Color32 faceCol   = new Color32(255, 175,  10, 255);
        Color32 shadowCol = new Color32(180, 120,   5, 255);
        Color32 highlight = new Color32(255, 245, 180, 255);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - half, dy = y - half;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);

            if (d > outer) { px[y*size+x] = clear; continue; }
            if (d > rim)   { px[y*size+x] = rimCol; continue; }

            float dir = (-dx + dy) / (2f * outer);
            Color32 face = d < inner
                ? Color32.Lerp(faceCol, shadowCol, Mathf.Clamp01(dir + 0.5f))
                : faceCol;

            if (dx < -6f && dy > 6f && d < rim * 0.65f)
                face = Color32.Lerp(face, highlight, 0.55f);

            px[y*size+x] = face;
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 0.7f);
    }
}
