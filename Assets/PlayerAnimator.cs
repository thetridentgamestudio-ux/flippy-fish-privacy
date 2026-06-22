using UnityEngine;

/// <summary>
/// Animates the default player skin using Fish_Default.png (4×2 sprite sheet).
/// Silently inactive when any other skin is equipped.
///
/// Setup: drop Fish_Default.png in Assets/Resources/FishSheets/
///        Texture Type = Default, Read/Write = Enabled
///
/// Attach to the Player GameObject (GameBootstrap.CreatePlayer adds it automatically).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────────
    const string SheetPath   = "FishSheets/Fish_Default";
    const int    TotalFrames = 8;
    const int    GridRows    = 2;          // 4 frames top row, 4 bottom row
    const float  IdleFPS     = 10f;        // normal swim cycle
    const float  FlapFPS     = 22f;        // faster during flap burst
    const float  FlapBurst   = 0.25f;      // seconds of fast animation after tap
    const byte   AlphaThresh = 12;

    // Squish on flap
    const float SquishX      = 0.75f;      // compress horizontally on flap
    const float SquishY      = 1.25f;      // stretch vertically on flap
    const float SpringSpeed  = 8f;         // how fast scale springs back

    // ── State ─────────────────────────────────────────────────────────────────
    SpriteRenderer _sr;
    Sprite[]       _frames;
    float          _animTimer;
    int            _frameIdx;
    float          _flapTimer;             // counts down after a flap
    Vector3        _baseScale;
    bool           _active;               // false if non-default skin equipped

    // ── Init ──────────────────────────────────────────────────────────────────
    void Start()
    {
        _sr        = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;

        // Only animate on default skin (ID 0)
        if (SkinManager.GetSelectedSkin() != 0) { _active = false; return; }

        Texture2D tex = Resources.Load<Texture2D>(SheetPath);
        if (tex == null)
        {
            Debug.LogWarning("[PlayerAnim] Fish_Default.png not found in Resources/FishSheets/. " +
                             "Texture Type must be Default with Read/Write Enabled.");
            _active = false;
            return;
        }

        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Calculate PPU so animated frames match the original sprite's world height exactly.
        // Original intrinsic height = sprite.rect.height / sprite.pixelsPerUnit.
        // We want: frameH / targetPPU = origIntrinsicH  →  targetPPU = frameH / origIntrinsicH
        float origIntrinsicH = 1f;
        if (_sr.sprite != null)
            origIntrinsicH = _sr.sprite.rect.height / _sr.sprite.pixelsPerUnit;
        int frameH    = tex.height / GridRows;
        float targetPPU = (origIntrinsicH > 0f) ? frameH / origIntrinsicH : 100f;
        Debug.Log($"[PlayerAnim] origIntrinsicH={origIntrinsicH:F3} frameH={frameH} targetPPU={targetPPU:F1}");

        _frames = SliceGrid(tex, targetPPU);
        if (_frames == null || _frames.Length == 0)
        {
            Debug.LogWarning("[PlayerAnim] Failed to slice Fish_Default.png.");
            _active = false;
            return;
        }

        _active    = true;
        _frameIdx  = 0;
        _animTimer = 0f;
        Debug.Log($"[PlayerAnim] Loaded {_frames.Length} frames from Fish_Default.png");
    }

    // ── Per-frame ──────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_active) return;

        float dt  = Time.deltaTime;
        float fps = (_flapTimer > 0f) ? FlapFPS : IdleFPS;

        // Advance animation frame
        _animTimer += dt;
        if (_animTimer >= 1f / fps)
        {
            _animTimer -= 1f / fps;
            _frameIdx   = (_frameIdx + 1) % _frames.Length;
            _sr.sprite  = _frames[_frameIdx];
        }

        _flapTimer = Mathf.Max(0f, _flapTimer - dt);

        // Spring scale back to base after flap squish
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * SpringSpeed);
    }

    // ── Called externally by PlayerController on every tap ────────────────────
    public void OnFlap()
    {
        if (!_active) return;

        // Squish — brief horizontal compress + vertical stretch
        transform.localScale = new Vector3(
            _baseScale.x * SquishX,
            _baseScale.y * SquishY,
            _baseScale.z);

        // Trigger fast-frame burst
        _flapTimer = FlapBurst;
    }

    // ── Call this when skin changes at runtime (e.g. from skin select screen) ──
    public void RefreshSkin()
    {
        if (SkinManager.GetSelectedSkin() != 0)
        {
            _active = false;
            // Restore base sprite so non-default skin shows correctly
            Sprite skinSprite = SkinManager.GetSelectedSprite();
            if (skinSprite != null) _sr.sprite = skinSprite;
            transform.localScale = _baseScale;
        }
        else
        {
            // Re-enable animation if player switched back to default
            if (_frames != null && _frames.Length > 0) _active = true;
            else Start(); // reload if never loaded
        }
    }

    // ── Grid slicer (same logic as BackgroundFishAnimator) ────────────────────
    Sprite[] SliceGrid(Texture2D tex, float ppu = 100f)
    {
        int w          = tex.width;
        int h          = tex.height;
        int colsPerRow = TotalFrames / GridRows;
        Color32[] pix  = tex.GetPixels32();

        // Check if sheet has transparent row gaps
        bool[] rowHas = new bool[h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pix[y * w + x].a > AlphaThresh) { rowHas[y] = true; break; }

        // Count distinct row bands
        int bands = 0; bool inBand = false;
        for (int y = 0; y < h; y++)
        {
            if (rowHas[y]  && !inBand) { inBand = true;  bands++; }
            if (!rowHas[y] && inBand)  { inBand = false; }
        }

        bool uniform = (bands != GridRows);
        int  cellW   = w / colsPerRow;
        int  cellH   = h / GridRows;

        Debug.Log($"[PlayerAnim] {w}x{h}, {bands} row bands → {(uniform ? "uniform" : "gap-detect")} grid, cell={cellW}x{cellH}");

        var sprites = new Sprite[TotalFrames];

        if (uniform)
        {
            // No transparent gaps — divide evenly
            for (int r = 0; r < GridRows; r++)
            {
                int unityY = Mathf.Max(0, h - (r + 1) * cellH);
                int rowH   = Mathf.Min(cellH, h - unityY);
                for (int c = 0; c < colsPerRow; c++)
                {
                    int fx = c * cellW;
                    int fw = (c == colsPerRow - 1) ? w - fx : cellW;
                    sprites[r * colsPerRow + c] = Sprite.Create(
                        tex, new Rect(fx, unityY, fw, rowH), new Vector2(0.5f, 0.5f), ppu);
                }
            }
        }
        else
        {
            var rowBands = new System.Collections.Generic.List<(int yMin, int yMax)>();
            inBand = false; int bs = 0;
            for (int y = 0; y < h; y++)
            {
                if (rowHas[y]  && !inBand) { inBand = true;  bs = y; }
                if (!rowHas[y] && inBand)  { inBand = false; rowBands.Add((bs, y - 1)); }
            }
            if (inBand) rowBands.Add((bs, h - 1));

            for (int r = 0; r < GridRows && r < rowBands.Count; r++)
            {
                var   band   = rowBands[GridRows - 1 - r];
                int   unityY = Mathf.Max(0, band.yMin - 2);
                int   bandH  = Mathf.Min(h, band.yMax + 3) - unityY;

                bool[] colHas = new bool[w];
                for (int x = 0; x < w; x++)
                    for (int y = band.yMin; y <= band.yMax; y++)
                        if (pix[y * w + x].a > AlphaThresh) { colHas[x] = true; break; }

                var cols = new System.Collections.Generic.List<(int s, int e)>();
                bool inCol = false; int cs = 0;
                for (int x = 0; x < w; x++)
                {
                    if (colHas[x]  && !inCol) { inCol = true;  cs = x; }
                    if (!colHas[x] && inCol)  { inCol = false; cols.Add((cs, x - 1)); }
                }
                if (inCol) cols.Add((cs, w - 1));

                for (int c = 0; c < cols.Count && c < colsPerRow; c++)
                {
                    int fx = cols[c].s, fw = cols[c].e - cols[c].s + 1;
                    sprites[r * colsPerRow + c] = Sprite.Create(
                        tex, new Rect(fx, unityY, fw, bandH), new Vector2(0.5f, 0.5f), ppu);
                }
            }
        }

        return sprites;
    }
}
