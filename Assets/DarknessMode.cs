using UnityEngine;
using UnityEngine.UI;

/// Phase 2: Darkness Mode.
/// At TRIGGER_SCORE the screen fades to near-black with a spotlight
/// centred on the fish. Spotlight follows the fish every frame via
/// a custom shader — no texture memory overhead.
public class DarknessMode : MonoBehaviour
{
    public static DarknessMode Instance { get; private set; }

    // ── Tweak these in one place ──────────────────────────────────────────────
    const int   TRIGGER_SCORE  = 20;   // kicks in after oscillating pipes (score 15)
    const float FADE_DURATION  = 2.5f; // seconds to reach full darkness
    const float MAX_DARKNESS   = 0.97f;
    const float INNER_RADIUS   = 0.13f; // spotlight clear zone (UV-space, aspect-corrected)
    const float OUTER_RADIUS   = 0.22f; // falloff end

    // ── Runtime state ─────────────────────────────────────────────────────────
    bool    _active;
    float   _alpha;   // current darkness alpha, 0→MAX_DARKNESS
    Canvas  _canvas;

    // Shader-driven fullscreen quad
    RawImage _overlay;
    Material _mat;

    static readonly int PropCenter      = Shader.PropertyToID("_Center");
    static readonly int PropInnerRadius = Shader.PropertyToID("_InnerRadius");
    static readonly int PropOuterRadius = Shader.PropertyToID("_OuterRadius");
    static readonly int PropDarkness    = Shader.PropertyToID("_Darkness");

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null) { Debug.LogError("[DarknessMode] No Canvas found"); return; }
        BuildOverlay();
    }

    void Update()
    {
        if (!_active) return;
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.IsGameOver) return;

        // Fade in darkness
        _alpha = Mathf.MoveTowards(_alpha, MAX_DARKNESS, (MAX_DARKNESS / FADE_DURATION) * Time.deltaTime);
        _mat.SetFloat(PropDarkness, _alpha);

        // Track fish in viewport UV (0-1 range)
        Vector3 worldPos  = GameBootstrap.Instance.PlayerPosition;
        Vector3 viewport  = Camera.main.WorldToViewportPoint(worldPos);
        _mat.SetVector(PropCenter, new Vector4(viewport.x, viewport.y, 0, 0));
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void Activate()
    {
        if (_active) return;
        _active = true;
        if (_overlay != null) _overlay.gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        _active = false;
        _alpha  = 0f;
        if (_mat != null) _mat.SetFloat(PropDarkness, 0f);
        if (_overlay != null) _overlay.gameObject.SetActive(false);
    }

    // ── Build fullscreen shader quad ──────────────────────────────────────────
    void BuildOverlay()
    {
        var shader = Shader.Find("Custom/DarknessOverlay");
        if (shader == null)
        {
            // Fallback: plain dark Image if shader not found
            Debug.LogWarning("[DarknessMode] DarknessOverlay shader not found — using plain fallback");
            var fallback = new GameObject("DarknessFallback");
            fallback.transform.SetParent(_canvas.transform, false);
            fallback.transform.SetAsLastSibling();
            var img = fallback.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.88f);
            img.raycastTarget = false;
            var fRT = fallback.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = fRT.offsetMax = Vector2.zero;
            _overlay = null;
            return;
        }

        _mat = new Material(shader);
        _mat.SetFloat(PropInnerRadius, INNER_RADIUS);
        _mat.SetFloat(PropOuterRadius, OUTER_RADIUS);
        _mat.SetFloat(PropDarkness, 0f);
        _mat.SetVector(PropCenter, new Vector4(0.5f, 0.5f, 0, 0));

        var go = new GameObject("DarknessOverlay");
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _overlay = go.AddComponent<RawImage>();
        _overlay.material     = _mat;
        _overlay.raycastTarget = false;
        _overlay.color        = Color.white;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.SetActive(false); // hidden until Activate() is called
    }
}
