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

        // HUD root: bottom-centre, above nav bar
        GameObject go = new GameObject("PowerUpHUD");
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 360f);
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
        cardRT.sizeDelta = new Vector2(120f, 100f);

        // Explicit layout size so HorizontalLayoutGroup measures cards correctly
        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = 120f;
        le.preferredHeight = 100f;
        le.minWidth        = 120f;
        le.minHeight       = 100f;

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

        // ── Timer text (below ring) ──────────────────────────────────────────
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
}
