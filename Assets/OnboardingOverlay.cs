using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Tutorial overlay — glassmorphic card, fish mascot bursting above, bubble decorations.
/// ShowIfNeeded: auto on first launch. ShowAlways: triggered by help button.
public static class OnboardingOverlay
{
    const string PREF_KEY = "Onboarding_v2_Done";

    struct Step { public string title, body, btnLabel; }

    static readonly Step[] Steps =
    {
        new Step { title    = "Tap to Swim!",
                   body     = "Tap the screen to keep your fish swimming.\nHit a pipe once and it's game over.",
                   btnLabel = "GOT IT" },
        new Step { title    = "Daily Quests",
                   body     = "Three fresh goals every day.\nComplete them to earn bonus coins.",
                   btnLabel = "NEXT" },
        new Step { title    = "Collect Skins",
                   body     = "Earn coins every pipe you pass.\nUnlock new fish in the Skins shop.",
                   btnLabel = "NEXT" },
        new Step { title    = "Power-Ups!",
                   body     = "<line-height=180%>SHIELD  -  survive one pipe hit\nSLOW TIME  -  pipes move at half speed\nCOIN MAGNET  -  all coins come to you\nDOUBLE JUMP  -  extra jump mid-air\nSPEED BOOST  -  turbo jump force\n\n<size=85%>Fly into glowing icons to collect!</size>",
                   btnLabel = "NEXT" },
        new Step { title    = "Ghost Race",
                   body     = "Race against a ghost of another player's best run.\nBeat their score to top the leaderboard!",
                   btnLabel = "LET'S PLAY!" },
    };


    static GameObject _root;
    static int        _step;
    static Canvas     _canvas;

    // ── Entry points ──────────────────────────────────────────────────────────
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

    // ── Root dim layer ────────────────────────────────────────────────────────
    static void Build()
    {
        _root = new GameObject("OnboardingOverlay");
        _root.transform.SetParent(_canvas.transform, false);
        _root.transform.SetAsLastSibling();

        // Dim masks bottom nav while still letting game world breathe
        var dim = _root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        var dimRT = _root.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;

        _root.AddComponent<GraphicRaycaster>(); // absorb touches beneath

        ShowStep(_step);
    }

    // ── Per-step card ─────────────────────────────────────────────────────────
    static void ShowStep(int index)
    {
        var old = _root.transform.Find("Card");
        if (old != null) Object.Destroy(old.gameObject);

        if (index >= Steps.Length) { Finish(); return; }

        Step step = Steps[index];
        bool last = index == Steps.Length - 1;

        // ── Card ─────────────────────────────────────────────────────────────
        // Card dimensions: 800 wide, 690 tall
        const float CW = 800f, CH = 690f;

        var card = new GameObject("Card");
        card.transform.SetParent(_root.transform, false);

        // Use the custom panel sprite — has its own border, curves and bubble decorations built in
        var cardImg = card.AddComponent<Image>();
        Sprite panelSp = Resources.Load<Sprite>("Overlboard_panel");
        if (panelSp != null)
        {
            cardImg.sprite = panelSp;
            cardImg.type   = Image.Type.Simple;
            cardImg.color  = Color.white; // no tint — show sprite as-is
        }
        else
        {
            // Fallback if sprite not found
            cardImg.sprite = MakeRoundedRect((int)CW, (int)CH, 56);
            cardImg.type   = Image.Type.Simple;
            cardImg.color  = new Color(0.72f, 0.88f, 0.97f, 0.70f);
        }

        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta        = new Vector2(CW, CH);
        cardRT.anchoredPosition = new Vector2(0f, 130f); // raised to clear bottom nav buttons

        // Bubble decorations are built into the Overlboard_panel sprite — no code bubbles needed

        bool isPowerUpStep = step.title == "Power-Ups!";

        // ── Fish mascot — bursts 70% above card top ───────────────────────────
        // Card top = CH/2 = 310 from card centre.
        // Fish: 300×300. Anchor = card top-centre. Pivot = fish top-centre.
        // anchoredPosition.y = +210 → fish top is 210 above card top.
        // Fish bottom = 210 - 300 = -90 → 90px inside card. (~70% above, 30% inside)
        Sprite fishSp = Resources.Load<Sprite>("FishDefault");
        if (fishSp != null)
        {
            var fishGO = MakeChild("FishMascot", card.transform);
            var fishImg = fishGO.AddComponent<Image>();
            fishImg.sprite = fishSp;
            fishImg.preserveAspect = true;
            fishImg.raycastTarget  = false;
            var fishRT = fishGO.GetComponent<RectTransform>();
            fishRT.anchorMin = fishRT.anchorMax = new Vector2(0.5f, 1f);
            fishRT.pivot     = new Vector2(0.5f, 1f);
            fishRT.sizeDelta        = new Vector2(300f, 300f);
            fishRT.anchoredPosition = new Vector2(0f, 210f); // 210px above card top
        }

        // ── Step counter (subtle, top-right) ──────────────────────────────────
        var cntGO = MakeChild("StepCount", card.transform);
        var cntT  = cntGO.AddComponent<TextMeshProUGUI>();
        cntT.text      = $"{index + 1} / {Steps.Length}";
        cntT.fontSize  = 28f;
        cntT.color     = new Color(0.05f, 0.10f, 0.30f, 1f);
        cntT.alignment = TextAlignmentOptions.TopRight;
        cntT.overflowMode = TextOverflowModes.Overflow;
        var cntRT = cntGO.GetComponent<RectTransform>();
        cntRT.anchorMin = cntRT.anchorMax = new Vector2(1f, 1f);
        cntRT.pivot     = new Vector2(1f, 1f);
        cntRT.sizeDelta        = new Vector2(120f, 52f);
        cntRT.anchoredPosition = new Vector2(-22f, -22f);

        // ── Title ─────────────────────────────────────────────────────────────
        // Starts 215px below card top (below fish overlap zone)
        var titleGO = MakeChild("Title", card.transform);
        var titleT  = titleGO.AddComponent<TextMeshProUGUI>();
        titleT.text      = step.title;
        titleT.fontSize  = 56f;
        titleT.fontStyle = FontStyles.Bold;
        titleT.color     = new Color(0f, 0f, 0f, 1f); // pure black — maximum contrast on frosted card
        titleT.alignment = TextAlignmentOptions.Center;
        titleT.overflowMode = TextOverflowModes.Overflow;
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 1f);
        titleRT.anchorMax = new Vector2(0.95f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.sizeDelta        = new Vector2(0f, 76f);
        titleRT.anchoredPosition = new Vector2(0f, isPowerUpStep ? -60f : -215f);

        // ── Body ──────────────────────────────────────────────────────────────
        var bodyGO = MakeChild("Body", card.transform);
        var bodyT  = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyT.text      = step.body;
        bodyT.fontSize  = isPowerUpStep ? 30f : 34f;
        bodyT.color     = new Color(0.05f, 0.10f, 0.30f, 1f);
        bodyT.alignment = isPowerUpStep ? TextAlignmentOptions.Top : TextAlignmentOptions.Center;
        bodyT.enableWordWrapping = true;
        bodyT.overflowMode = TextOverflowModes.Overflow;
        var bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.06f, 1f);
        bodyRT.anchorMax = new Vector2(0.94f, 1f);
        bodyRT.pivot     = new Vector2(0.5f, 1f);
        bodyRT.sizeDelta        = new Vector2(0f, isPowerUpStep ? 380f : 140f);
        bodyRT.anchoredPosition = new Vector2(0f, isPowerUpStep ? -140f : -308f);

        // ── Action button (TAP / NEXT / LET'S PLAY!) ─────────────────────────
        Color btnColor = last
            ? new Color(0.08f, 0.62f, 0.20f, 1f)   // green on last step
            : new Color(0.10f, 0.45f, 0.92f, 1f);   // blue for all others

        var btnGO  = MakeChild("ActionBtn", card.transform);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = MakeRoundedRect(500, 120, 52);
        btnImg.type   = Image.Type.Simple;
        btnImg.color  = btnColor;
        var btn = btnGO.AddComponent<Button>();

        // Colour block so button doesn't look grey when highlighted
        var cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
        cb.pressedColor     = new Color(0f, 0f, 0f, 0.15f);
        btn.colors = cb;

        btn.onClick.AddListener(() => { _step++; ShowStep(_step); });
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot     = new Vector2(0.5f, 0f);
        btnRT.sizeDelta        = new Vector2(480f, 108f);
        btnRT.anchoredPosition = new Vector2(0f, isPowerUpStep ? 60f : 110f);

        var btnLbl = MakeLabel(btnGO.transform, step.btnLabel, 46f, Color.white);
        StretchFull(btnLbl);

        // ── Skip (plain text, bottom-left, hidden on last step) ───────────────
        if (!last)
        {
            var skipGO  = MakeChild("Skip", card.transform);
            var skipHit = skipGO.AddComponent<Image>(); // invisible hit area
            skipHit.color = Color.clear;
            var skipBtn = skipGO.AddComponent<Button>();
            skipBtn.onClick.AddListener(Finish);
            var skipRT = skipGO.GetComponent<RectTransform>();
            skipRT.anchorMin = skipRT.anchorMax = new Vector2(0f, 0f);
            skipRT.pivot     = new Vector2(0f, 0f);
            skipRT.sizeDelta        = new Vector2(180f, 70f);
            skipRT.anchoredPosition = new Vector2(100f, 35f); // pushed right so full word is inside panel

            var skipLbl = MakeLabel(skipGO.transform, "Skip", 34f,
                new Color(0.05f, 0.10f, 0.30f, 1f)); // full opacity dark navy
            skipLbl.alignment = TextAlignmentOptions.BottomLeft;
            StretchFull(skipLbl);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Finish()
    {
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
        if (_root != null) Object.Destroy(_root);
        _root = null;
    }

    static GameObject MakeChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // Sized, centre-anchored child
    static void SizeRT(GameObject go, float w, float h, Vector2 pos = default)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string text, float size, Color col)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.fontStyle = FontStyles.Bold;
        t.color     = col;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    static void StretchFull(TextMeshProUGUI t)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite MakeRoundedRect(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int   cx = Mathf.Clamp(x, r, w - r);
            int   cy = Mathf.Clamp(y, r, h - r);
            float d  = Mathf.Sqrt((x-cx)*(x-cx) + (y-cy)*(y-cy));
            px[y*w+x] = d <= r
                ? new Color32(255,255,255,255)
                : new Color32(0,0,0,0);
        }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f));
    }
}
