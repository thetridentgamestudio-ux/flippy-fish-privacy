using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// First-launch coach-mark overlay. Shows once, never again.
/// Call OnboardingOverlay.ShowIfNeeded(canvas) from GameBootstrap.Start().
/// </summary>
public static class OnboardingOverlay
{
    const string PREF_KEY = "Onboarding_v1_Done";

    struct Step
    {
        public string title, body, emoji;
    }

    static readonly Step[] Steps =
    {
        new Step { emoji = "🐟", title = "Tap to Flap!",    body = "Tap the screen to keep your fish flying.\nHit a pipe once and it's game over." },
        new Step { emoji = "📋", title = "Daily Quests",    body = "Three goals refresh every day.\nComplete them to earn coins." },
        new Step { emoji = "🏆", title = "Battle Pass",     body = "Earn XP every run and climb tiers.\nUnlock coins, gems and exclusive skins." },
        new Step { emoji = "⚔️", title = "Battle Mode",    body = "Challenge a real opponent.\nOutlast them to win the match." },
    };

    static GameObject _root;
    static int        _step;
    static Canvas     _canvas;

    // ── Entry points ─────────────────────────────────────────────────────────
    public static void ShowIfNeeded(Canvas canvas)
    {
        if (PlayerPrefs.GetInt(PREF_KEY, 0) == 1) return;
        Show(canvas);
    }

    public static void ShowAlways(Canvas canvas) => Show(canvas);

    static void Show(Canvas canvas)
    {
        if (_root != null) Object.Destroy(_root);
        _canvas = canvas;
        _step   = 0;
        Build();
    }

    // ── Build ─────────────────────────────────────────────────────────────────
    static void Build()
    {
        // Root — full-screen dim layer
        _root = new GameObject("OnboardingOverlay");
        _root.transform.SetParent(_canvas.transform, false);
        _root.transform.SetAsLastSibling();

        Image dimImg = _root.AddComponent<Image>();   // Image auto-adds RectTransform
        dimImg.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform dimRT = _root.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;

        // Block touches so taps don't pass through to the game
        _root.AddComponent<GraphicRaycaster>();

        ShowStep(_step);
    }

    // ── Step card ─────────────────────────────────────────────────────────────
    static void ShowStep(int index)
    {
        // Remove previous card
        Transform old = _root.transform.Find("Card");
        if (old != null) Object.Destroy(old.gameObject);

        if (index >= Steps.Length) { Finish(); return; }

        Step step  = Steps[index];
        bool last  = index == Steps.Length - 1;

        // ── Card background ───────────────────────────────────────────────────
        GameObject card = new GameObject("Card");
        card.transform.SetParent(_root.transform, false);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.12f, 0.26f, 0.98f);

        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = new Vector2(0f, 60f);
        cardRT.sizeDelta        = new Vector2(340f, 250f);

        // Blue accent border (sits behind content)
        GameObject border = new GameObject("Border");
        border.transform.SetParent(card.transform, false);
        border.transform.SetAsFirstSibling();
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.18f, 0.52f, 1f, 0.55f);
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-2f, -2f);
        borderRT.offsetMax = new Vector2( 2f,  2f);

        // ── Emoji ─────────────────────────────────────────────────────────────
        GameObject emojiGO = new GameObject("Emoji");
        emojiGO.transform.SetParent(card.transform, false);
        TextMeshProUGUI emojiTxt = emojiGO.AddComponent<TextMeshProUGUI>();
        emojiTxt.text      = step.emoji;
        emojiTxt.fontSize  = 50;
        emojiTxt.alignment = TextAlignmentOptions.Center;
        RectTransform emojiRT = emojiGO.GetComponent<RectTransform>();
        emojiRT.anchorMin        = new Vector2(0f, 1f);
        emojiRT.anchorMax        = new Vector2(1f, 1f);
        emojiRT.pivot            = new Vector2(0.5f, 1f);
        emojiRT.offsetMin        = Vector2.zero;
        emojiRT.offsetMax        = Vector2.zero;
        emojiRT.anchoredPosition = new Vector2(0f, -14f);
        emojiRT.sizeDelta        = new Vector2(0f, 68f);

        // ── Title ─────────────────────────────────────────────────────────────
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(card.transform, false);
        TextMeshProUGUI titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = step.title;
        titleTxt.fontSize  = 22;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = new Color(1f, 0.85f, 0.2f);
        titleTxt.alignment = TextAlignmentOptions.Center;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.05f, 1f);
        titleRT.anchorMax        = new Vector2(0.95f, 1f);
        titleRT.pivot            = new Vector2(0.5f, 1f);
        titleRT.offsetMin        = Vector2.zero;
        titleRT.offsetMax        = Vector2.zero;
        titleRT.anchoredPosition = new Vector2(0f, -86f);
        titleRT.sizeDelta        = new Vector2(0f, 34f);

        // ── Body ──────────────────────────────────────────────────────────────
        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(card.transform, false);
        TextMeshProUGUI bodyTxt = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTxt.text      = step.body;
        bodyTxt.fontSize  = 15;
        bodyTxt.color     = new Color(0.85f, 0.85f, 0.92f);
        bodyTxt.alignment = TextAlignmentOptions.Center;
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin        = new Vector2(0.06f, 1f);
        bodyRT.anchorMax        = new Vector2(0.94f, 1f);
        bodyRT.pivot            = new Vector2(0.5f, 1f);
        bodyRT.offsetMin        = Vector2.zero;
        bodyRT.offsetMax        = Vector2.zero;
        bodyRT.anchoredPosition = new Vector2(0f, -126f);
        bodyRT.sizeDelta        = new Vector2(0f, 70f);

        // ── Progress dots ─────────────────────────────────────────────────────
        for (int i = 0; i < Steps.Length; i++)
        {
            GameObject dot = new GameObject($"Dot{i}");
            dot.transform.SetParent(card.transform, false);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = (i == index)
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.35f, 0.35f, 0.5f);
            RectTransform dotRT = dot.GetComponent<RectTransform>();
            float dotW  = (i == index) ? 22f : 8f;
            float totalW = Steps.Length * 14f + 8f; // approx centre
            float startX = -totalW * 0.5f + i * 14f;
            dotRT.anchorMin        = new Vector2(0.5f, 0f);
            dotRT.anchorMax        = new Vector2(0.5f, 0f);
            dotRT.pivot            = new Vector2(0.5f, 0f);
            dotRT.anchoredPosition = new Vector2(startX + dotW * 0.5f - 4f, 56f);
            dotRT.sizeDelta        = new Vector2(dotW, 8f);
        }

        // ── Next / Let's Play button ──────────────────────────────────────────
        GameObject nextGO = new GameObject("NextBtn");
        nextGO.transform.SetParent(card.transform, false);
        Image nextImg = nextGO.AddComponent<Image>();
        nextImg.color = new Color(0.15f, 0.52f, 1f);
        Button nextBtn = nextGO.AddComponent<Button>();
        nextBtn.targetGraphic = nextImg;
        nextBtn.onClick.AddListener(() => { _step++; ShowStep(_step); });
        RectTransform nextRT = nextGO.GetComponent<RectTransform>();
        nextRT.anchorMin        = new Vector2(0.5f, 0f);
        nextRT.anchorMax        = new Vector2(0.5f, 0f);
        nextRT.pivot            = new Vector2(last ? 0.5f : 1f, 0f);
        nextRT.anchoredPosition = new Vector2(last ? 0f : -6f, 10f);
        nextRT.sizeDelta        = new Vector2(last ? 200f : 140f, 46f);

        AddLabel(nextGO, last ? "Let's Play! 🎮" : "Next  →", 16, Color.white);

        // ── Skip button (hidden on last step) ─────────────────────────────────
        if (!last)
        {
            GameObject skipGO = new GameObject("SkipBtn");
            skipGO.transform.SetParent(card.transform, false);
            Image skipImg = skipGO.AddComponent<Image>();
            skipImg.color = new Color(0.22f, 0.22f, 0.32f);
            Button skipBtn = skipGO.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.onClick.AddListener(Finish);
            RectTransform skipRT = skipGO.GetComponent<RectTransform>();
            skipRT.anchorMin        = new Vector2(0.5f, 0f);
            skipRT.anchorMax        = new Vector2(0.5f, 0f);
            skipRT.pivot            = new Vector2(0f, 0f);
            skipRT.anchoredPosition = new Vector2(6f, 10f);
            skipRT.sizeDelta        = new Vector2(100f, 46f);

            AddLabel(skipGO, "Skip", 14, new Color(0.65f, 0.65f, 0.75f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void AddLabel(GameObject parent, string text, float size, Color color)
    {
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = lbl.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Finish()
    {
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
        if (_root != null) Object.Destroy(_root);
        _root = null;
    }
}
