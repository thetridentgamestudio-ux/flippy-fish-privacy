using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PowerUpHUD : MonoBehaviour
{
    private static PowerUpHUD instance;

    // Map of active cards: type → card root GO
    private Dictionary<PowerUpManager.PowerUp.PowerUpType, GameObject> _indicators
        = new Dictionary<PowerUpManager.PowerUp.PowerUpType, GameObject>();

    // Map type → fill image so CountDown can drain it
    private Dictionary<PowerUpManager.PowerUp.PowerUpType, Image> _fillImages
        = new Dictionary<PowerUpManager.PowerUp.PowerUpType, Image>();

    private RectTransform _container; // horizontal layout container

    // Cached white circle sprite for the radial ring
    private static Sprite _whiteCircleSprite;

    public static void InitializeHUD(Canvas canvas)
    {
        if (instance != null) return;

        // Dedicated overlay canvas — always renders on top of everything
        GameObject canvasGO = new GameObject("PowerUpHUDCanvas");
        var overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 99;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode        = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode    = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        Object.DontDestroyOnLoad(canvasGO);

        // HUD root: top-centre, below score area — eyes follow the fish at top of screen
        GameObject go = new GameObject("PowerUpHUD");
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -220f);
        rt.sizeDelta        = new Vector2(400f, 120f);

        // Horizontal layout group so cards auto-arrange with gap
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // Also add a ContentSizeFitter so the container shrinks/grows with cards
        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        instance = go.AddComponent<PowerUpHUD>();
        instance._container = rt;

        PowerUpManager.OnPowerUpActivated += instance.OnActivated;
        PowerUpManager.OnPowerUpExpired   += instance.OnExpired;
    }

    // ── Solid white circle sprite for the radial fill ring ──────────────────
    static Sprite GetWhiteCircleSprite()
    {
        if (_whiteCircleSprite != null) return _whiteCircleSprite;
        _whiteCircleSprite = PowerUpSpawner.CreateCircleSprite(Color.white);
        return _whiteCircleSprite;
    }

    // ── Build one card ───────────────────────────────────────────────────────
    void OnActivated(PowerUpManager.PowerUp.PowerUpType type)
    {
        PowerUpManager.PowerUp def = PowerUpManager.GetPowerUpDef(type);
        if (def == null) return;

        // Remove old card if power-up re-collected
        if (_indicators.ContainsKey(type))
        {
            Destroy(_indicators[type]);
            _indicators.Remove(type);
            _fillImages.Remove(type);
        }

        // ── Card root (120 × 100) ────────────────────────────────────────────
        GameObject card = new GameObject($"Card_{type}");
        card.transform.SetParent(transform, false);

        var cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(120f, 120f);

        // Explicit layout size so HorizontalLayoutGroup measures cards correctly
        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = 120f;
        le.preferredHeight = 120f;
        le.minWidth        = 120f;
        le.minHeight       = 120f;

        // Semi-transparent dark background
        var bgImg = card.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        bgImg.type  = Image.Type.Simple;

        // ── Circular ring (behind icon, 70 × 70, radial fill) ───────────────
        GameObject ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(card.transform, false);
        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin        = new Vector2(0.5f, 1f);
        ringRT.anchorMax        = new Vector2(0.5f, 1f);
        ringRT.pivot            = new Vector2(0.5f, 1f);
        ringRT.sizeDelta        = new Vector2(70f, 70f);
        ringRT.anchoredPosition = new Vector2(0f, -8f);

        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite      = GetWhiteCircleSprite();
        ringImg.color       = def.color;
        ringImg.type        = Image.Type.Filled;
        ringImg.fillMethod  = Image.FillMethod.Radial360;
        ringImg.fillOrigin  = (int)Image.Origin360.Top;
        ringImg.fillClockwise = true;
        ringImg.fillAmount  = 1f;

        // ── Icon (60 × 60, centred inside ring) ─────────────────────────────
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(card.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin        = new Vector2(0.5f, 1f);
        iconRT.anchorMax        = new Vector2(0.5f, 1f);
        iconRT.pivot            = new Vector2(0.5f, 1f);
        iconRT.sizeDelta        = new Vector2(60f, 60f);
        iconRT.anchoredPosition = new Vector2(0f, -13f); // centred within ring

        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;

        // Try to load the power-up icon sprite
        string iconName = GetIconName(type);
        if (iconName != null)
        {
            Sprite s = Resources.Load<Sprite>(iconName);
            if (s != null) iconImg.sprite = s;
        }
        // If no sprite loaded, leave the Image with no sprite (invisible) — ring shows def colour anyway

        // ── Name label (above timer, shows power-up name) ───────────────────
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(card.transform, false);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text      = def.name.ToUpper();
        nameTxt.fontSize  = 16;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color     = def.color;
        nameTxt.alignment = TextAlignmentOptions.Center;

        var nameRT = nameTxt.rectTransform;
        nameRT.anchorMin        = new Vector2(0f, 0f);
        nameRT.anchorMax        = new Vector2(1f, 0f);
        nameRT.pivot            = new Vector2(0.5f, 0f);
        nameRT.sizeDelta        = new Vector2(0f, 22f);
        nameRT.anchoredPosition = new Vector2(0f, 30f);

        // ── Timer text (below name) ──────────────────────────────────────────
        GameObject timerGO = new GameObject("Timer");
        timerGO.transform.SetParent(card.transform, false);
        var timerTxt = timerGO.AddComponent<TextMeshProUGUI>();
        timerTxt.text      = $"{Mathf.CeilToInt(def.duration)}s";
        timerTxt.fontSize  = 22;
        timerTxt.fontStyle = FontStyles.Bold;
        timerTxt.color     = Color.white;
        timerTxt.alignment = TextAlignmentOptions.Center;

        var timerRT = timerTxt.rectTransform;
        timerRT.anchorMin        = new Vector2(0f, 0f);
        timerRT.anchorMax        = new Vector2(1f, 0f);
        timerRT.pivot            = new Vector2(0.5f, 0f);
        timerRT.sizeDelta        = new Vector2(0f, 28f);
        timerRT.anchoredPosition = new Vector2(0f, 6f);

        _indicators[type]  = card;
        _fillImages[type]  = ringImg;

        StartCoroutine(CountDown(type, def.duration, timerTxt, bgImg, ringImg));
    }

    IEnumerator CountDown(PowerUpManager.PowerUp.PowerUpType type, float duration,
                          TextMeshProUGUI timerTxt, Image bgImg, Image ringImg)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (remaining < 0f) remaining = 0f;

            if (timerTxt != null) timerTxt.text = $"{Mathf.CeilToInt(remaining)}s";
            if (ringImg  != null) ringImg.fillAmount = remaining / duration;

            // Flash card background red in last 3 seconds
            if (remaining <= 3f && bgImg != null)
            {
                float t = Mathf.PingPong(Time.time * 2f, 1f);
                bgImg.color = Color.Lerp(new Color(0f, 0f, 0f, 0.7f), new Color(0.8f, 0.1f, 0.1f, 0.85f), t);
            }

            yield return null;
        }
    }

    void OnExpired(PowerUpManager.PowerUp.PowerUpType type)
    {
        if (_indicators.TryGetValue(type, out GameObject card))
        {
            Destroy(card);
            _indicators.Remove(type);
        }
        _fillImages.Remove(type);
    }

    public static void ClearAll()
    {
        if (instance == null) return;
        foreach (var card in instance._indicators.Values)
            if (card != null) Destroy(card);
        instance._indicators.Clear();
        instance._fillImages.Clear();
    }

    public static void DestroyHUD()
    {
        if (instance != null)
        {
            PowerUpManager.OnPowerUpActivated -= instance.OnActivated;
            PowerUpManager.OnPowerUpExpired   -= instance.OnExpired;
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    // Sprite name helper (mirrors PowerUpSpawner)
    static string GetIconName(PowerUpManager.PowerUp.PowerUpType type)
    {
        switch (type)
        {
            case PowerUpManager.PowerUp.PowerUpType.Shield:     return "powerup_shield";
            case PowerUpManager.PowerUp.PowerUpType.SlowTime:   return "powerup_slowtime";
            case PowerUpManager.PowerUp.PowerUpType.CoinMagnet: return "powerup_magnet";
            case PowerUpManager.PowerUp.PowerUpType.DoubleJump: return "powerup_doublejump";
            case PowerUpManager.PowerUp.PowerUpType.SpeedBoost: return "powerup_speedboost";
            default: return null;
        }
    }

    // ── Collection banner ────────────────────────────────────────────────────
    // Shown immediately on pickup — large, centred, screen-space, fades in 1.5s.
    // Lives on the same overlay canvas (sortingOrder 99) so it's always visible.
    // Safe to call even if HUD isn't initialised yet (guard at top).
    public static void ShowCollectionBanner(string powerUpName, Color accentColor)
    {
        if (instance == null) return;

        // Find the overlay canvas this HUD lives in
        Canvas overlayCanvas = instance.GetComponentInParent<Canvas>();
        if (overlayCanvas == null) return;

        // Kill any existing banner so rapid re-collects don't stack
        Transform old = overlayCanvas.transform.Find("CollectionBanner");
        if (old != null) Destroy(old.gameObject);

        // Banner root — full-width, vertically centred at 55% up the screen
        GameObject bannerGO = new GameObject("CollectionBanner");
        bannerGO.transform.SetParent(overlayCanvas.transform, false);

        var bannerRT = bannerGO.AddComponent<RectTransform>();
        bannerRT.anchorMin        = new Vector2(0f, 0.5f);
        bannerRT.anchorMax        = new Vector2(1f, 0.5f);
        bannerRT.pivot            = new Vector2(0.5f, 0.5f);
        bannerRT.sizeDelta        = new Vector2(0f, 140f);
        bannerRT.anchoredPosition = new Vector2(0f, 160f); // slightly above centre so fish stays visible

        // Semi-transparent dark pill behind text
        var bg = bannerGO.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        // Power-up name — large, bold, accent colour
        GameObject textGO = new GameObject("BannerText");
        textGO.transform.SetParent(bannerGO.transform, false);
        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text      = $"{powerUpName}  ACTIVATED!";
        txt.fontSize  = 72;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = accentColor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.outlineColor = Color.black;
        txt.outlineWidth = 0.25f;
        var txtRT = txt.rectTransform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        // Fader component destroys banner after 1.5s
        var fader = bannerGO.AddComponent<CollectionBannerFader>();
        fader.textComponent = txt;
        fader.bgComponent   = bg;
    }
}

/// <summary>Fades and removes the collection banner over 1.5 seconds.</summary>
public class CollectionBannerFader : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public UnityEngine.UI.Image bgComponent;

    const float kHoldTime = 0.6f;  // stay opaque
    const float kFadeTime = 0.9f;  // then fade out
    float _elapsed;
    Color _startText;
    Color _startBg;

    void Start()
    {
        _startText = textComponent != null ? textComponent.color : Color.white;
        _startBg   = bgComponent   != null ? bgComponent.color   : Color.clear;
    }

    void Update()
    {
        _elapsed += Time.unscaledDeltaTime; // unscaled so SlowTime power-up doesn't delay the banner
        if (_elapsed < kHoldTime) return;

        float t = (_elapsed - kHoldTime) / kFadeTime;
        float alpha = Mathf.Clamp01(1f - t);

        if (textComponent != null)
            textComponent.color = new Color(_startText.r, _startText.g, _startText.b, alpha);
        if (bgComponent != null)
            bgComponent.color = new Color(_startBg.r, _startBg.g, _startBg.b, _startBg.a * alpha);

        if (t >= 1f) Destroy(gameObject);
    }
}
