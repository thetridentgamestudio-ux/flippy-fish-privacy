using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// Manages temporary speed spikes at score milestones (25 / 50 / 75).
/// Shows a screen flash and "SPEED UP!" banner for the duration.
/// GameBootstrap reads IsSpiking to multiply pipe speeds.
public class SpeedSpikeManager : MonoBehaviour
{
    public static SpeedSpikeManager Instance { get; private set; }

    public const  float SPIKE_MULT     = 1.35f; // 35% speed boost — readable by GameBootstrap
    const         float SPIKE_DURATION = 6f;     // seconds
    const         float FLASH_ALPHA    = 0.22f;  // subtle warm tint at peak

    static readonly int[] MILESTONES = { 25, 50, 75 };

    public bool IsSpiking { get; private set; }

    Coroutine  _spikeRoutine;
    Canvas     _canvas;
    Image      _flashOverlay;
    TextMeshProUGUI _label;
    Image      _labelBg;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas != null) BuildUI();
    }

    // ── Called from GameBootstrap.AddScore ────────────────────────────────────
    public void CheckMilestone(int score)
    {
        foreach (int ms in MILESTONES)
        {
            if (score == ms && !IsSpiking)
            {
                if (_spikeRoutine != null) StopCoroutine(_spikeRoutine);
                _spikeRoutine = StartCoroutine(SpikeRoutine());
                return;
            }
        }
    }

    // ── Called from GameBootstrap.TriggerGameOver ─────────────────────────────
    public void StopSpike()
    {
        if (_spikeRoutine != null) StopCoroutine(_spikeRoutine);
        IsSpiking = false;
        SetFlash(0f);
        SetLabel(false);
    }

    // ── Spike coroutine ───────────────────────────────────────────────────────
    IEnumerator SpikeRoutine()
    {
        IsSpiking = true;
        SetLabel(true);

        float elapsed = 0f;

        while (elapsed < SPIKE_DURATION)
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.IsGameOver) break;

            // Flash curve: ramp up 0.3s, hold, ramp down last 1s
            float t = elapsed / SPIKE_DURATION;
            float alpha = t < 0.05f ? Mathf.Lerp(0f, FLASH_ALPHA, t / 0.05f)
                        : t > 0.83f ? Mathf.Lerp(FLASH_ALPHA, 0f, (t - 0.83f) / 0.17f)
                        : FLASH_ALPHA;
            SetFlash(alpha);

            // Countdown on label
            float remaining = SPIKE_DURATION - elapsed;
            if (_label != null)
                _label.text = $"SPEED UP!  {remaining:F0}s";

            elapsed += Time.deltaTime;
            yield return null;
        }

        IsSpiking = false;
        SetFlash(0f);
        SetLabel(false);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Full-screen warm flash overlay
        var flashGO = new GameObject("SpeedFlash");
        flashGO.transform.SetParent(_canvas.transform, false);
        flashGO.transform.SetAsLastSibling();
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = new Color(1f, 0.75f, 0.10f, 0f); // orange-yellow tint
        _flashOverlay.raycastTarget = false;
        var fRT = flashGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;

        // "SPEED UP!" banner — dark pill + bold text, top-centre of screen
        var bgGO = new GameObject("SpeedBg");
        bgGO.transform.SetParent(_canvas.transform, false);
        bgGO.transform.SetAsLastSibling();
        _labelBg = bgGO.AddComponent<Image>();
        _labelBg.color  = new Color(0.90f, 0.55f, 0.00f, 0.92f);
        _labelBg.sprite = MakeRoundedRect(560, 100, 50);
        _labelBg.type   = Image.Type.Simple;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 1f);
        bgRT.pivot     = new Vector2(0.5f, 1f);
        bgRT.sizeDelta = new Vector2(560f, 100f);
        bgRT.anchoredPosition = new Vector2(0f, -160f); // below top HUD

        var lblGO = new GameObject("SpeedLbl");
        lblGO.transform.SetParent(bgGO.transform, false);
        _label = lblGO.AddComponent<TextMeshProUGUI>();
        _label.text      = "SPEED UP!";
        _label.fontSize  = 46f;
        _label.fontStyle = FontStyles.Bold;
        _label.color     = Color.white;
        _label.alignment = TextAlignmentOptions.Center;
        _label.overflowMode = TextOverflowModes.Overflow;
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        SetLabel(false);
    }

    void SetFlash(float alpha)
    {
        if (_flashOverlay == null) return;
        var c = _flashOverlay.color;
        c.a = alpha;
        _flashOverlay.color = c;
    }

    void SetLabel(bool visible)
    {
        if (_labelBg  != null) _labelBg.gameObject.SetActive(visible);
    }

    static Sprite MakeRoundedRect(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px  = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int   cx = Mathf.Clamp(x, r, w - r);
            int   cy = Mathf.Clamp(y, r, h - r);
            float d  = Mathf.Sqrt((x-cx)*(x-cx) + (y-cy)*(y-cy));
            px[y*w+x] = d <= r ? new Color32(255,255,255,255) : new Color32(0,0,0,0);
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f));
    }
}
