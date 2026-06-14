using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinSelectUI : MonoBehaviour
{
    static SkinSelectUI _instance;
    GameObject _panel;
    TextMeshProUGUI _coinLabel;
    GameObject[] _cards = new GameObject[6];
    bool _built;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
    }

    public static void Show()
    {
        if (_instance == null) return;
        _instance.BuildIfNeeded();
        _instance._panel.SetActive(true);
        _instance._panel.transform.SetAsLastSibling();
        _instance.Refresh();
    }

    public static void Hide()
    {
        if (_instance != null && _instance._panel != null)
            _instance._panel.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    RectTransform Rect(GameObject go, Vector2 size, Vector2 anchoredPos,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot; rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return rt;
    }

    // Anchor-stretch helper (fills parent with optional margin)
    void Stretch(GameObject go, float l=0,float r=0,float t=0,float b=0)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
        TMP_FontAsset font, float fontSize, Color color,
        Vector2 size, Vector2 pos,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = align; t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        Rect(go, size, pos,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        return t;
    }

    void BuildIfNeeded()
    {
        if (_built) return;
        _built = true;

        Canvas cv = FindFirstObjectByType<Canvas>();
        if (cv == null) return;
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        float sw = Screen.width;
        float sh = Screen.height;

        // ── Fullscreen backdrop ───────────────────────────────────────
        _panel = new GameObject("SkinSelectPanel");
        _panel.transform.SetParent(cv.transform, false);
        _panel.AddComponent<Image>().color = new Color(0.05f, 0.55f, 0.75f, 0.97f);
        Stretch(_panel);

        // ── Outer card (90% wide, 82% tall, centred) ─────────────────
        float CW = sw * 0.90f;
        float CH = sh * 0.82f;

        GameObject card = new GameObject("Card");
        card.transform.SetParent(_panel.transform, false);
        card.AddComponent<Image>().color = new Color(0.04f, 0.45f, 0.65f, 0.95f);
        Rect(card, new Vector2(CW,CH), Vector2.zero,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // ── Layout constants (top-to-bottom anchors inside card) ──────
        // Title bar  : top 10 %
        // Coin row   : 10–17 %
        // Grid row 1 : 17–55 %
        // Grid row 2 : 55–90 %
        // Close btn  : 90–100 %

        float titleH  = CH * 0.10f;
        float coinH   = CH * 0.09f;
        float gridH   = CH * 0.34f;
        float closeH  = CH * 0.10f;

        float top     = CH * 0.5f;    // top edge of card in local space

        // ── Title bar ────────────────────────────────────────────────
        float titleY = top - titleH * 0.5f;
        GameObject titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(card.transform, false);
        // Rounded title bar
        Image titleImg = titleBar.AddComponent<Image>();
        titleImg.sprite = MakeRoundedRectSprite(512, 128, 40);
        titleImg.type = Image.Type.Simple;
        titleImg.color = new Color(0.02f, 0.25f, 0.45f, 1f);
        Rect(titleBar, new Vector2(CW * 0.92f, titleH), new Vector2(0, titleY),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        MakeLabel(titleBar.transform, "T", "🐟  CHOOSE YOUR FISH", font, sh*0.030f,
            new Color(1f,0.95f,0.35f), new Vector2(CW * 0.92f, titleH), Vector2.zero);

        // ── Coin pill ────────────────────────────────────────────────
        float coinY  = top - titleH - coinH * 0.5f;
        float pillW  = CW * 0.38f;
        float pillH  = coinH * 0.70f;
        float icoSz  = pillH * 0.80f;

        // Shadow behind pill
        GameObject pillShadow = new GameObject("PillShadow");
        pillShadow.transform.SetParent(card.transform, false);
        Image pillShadowImg = pillShadow.AddComponent<Image>();
        pillShadowImg.sprite = MakeRoundedRectSprite(256, 64, 28);
        pillShadowImg.type = Image.Type.Simple;
        pillShadowImg.color = new Color(0f, 0f, 0f, 0.3f);
        Rect(pillShadow, new Vector2(pillW + 6f, pillH + 6f), new Vector2(2f, coinY - 3f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Rounded coin pill
        GameObject pill = new GameObject("Pill");
        pill.transform.SetParent(card.transform, false);
        Image pillImg = pill.AddComponent<Image>();
        pillImg.sprite = MakeRoundedRectSprite(256, 64, 28);
        pillImg.type = Image.Type.Simple;
        pillImg.color = new Color(0.02f, 0.25f, 0.45f, 0.98f);
        Rect(pill, new Vector2(pillW,pillH), new Vector2(0,coinY),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Coin sprite — anchored to left of pill
        GameObject icoGO = new GameObject("Ico");
        icoGO.transform.SetParent(pill.transform, false);
        Image icoImg = icoGO.AddComponent<Image>();
        Sprite coinSpr = Resources.Load<Sprite>("Coin");
        if (coinSpr!=null){ icoImg.sprite=coinSpr; icoImg.color=Color.white; }
        else icoImg.color = new Color(1f,0.82f,0.1f);
        Rect(icoGO, new Vector2(icoSz,icoSz), new Vector2(-pillW*0.5f+icoSz*0.6f, 0),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Coin number — right of icon
        TextMeshProUGUI coinTxt = MakeLabel(pill.transform, "Coins", "0", font,
            sh*0.027f, new Color(1f,0.88f,0.15f),
            new Vector2(pillW*0.55f, pillH),
            new Vector2(icoSz*0.7f, 0),
            TextAlignmentOptions.Left);
        _coinLabel = coinTxt;

        // ── Skin grid — 3 rows × 2 cols for 6 skins ─────────────────
        float col0X = -CW * 0.255f;
        float col1X =  CW * 0.255f;
        float gridH3 = CH * 0.24f; // height per row — 3 rows fit in card
        float row0Y = top - titleH - coinH - gridH3 * 0.5f;
        float row1Y = row0Y - gridH3;
        float row2Y = row1Y - gridH3;

        Vector2[] gridPos = {
            new Vector2(col0X, row0Y), new Vector2(col1X, row0Y),
            new Vector2(col0X, row1Y), new Vector2(col1X, row1Y),
            new Vector2(col0X, row2Y), new Vector2(col1X, row2Y)
        };

        Color[] accents = {
            new Color(1.00f, 0.65f, 0.05f),  // Default     — orange
            new Color(0.30f, 0.75f, 0.95f),  // Whale       — ocean blue
            new Color(0.15f, 0.80f, 0.30f),  // Shark       — ocean grey-green
            new Color(0.95f, 0.75f, 0.10f),  // Dragon Fish — legendary gold
            new Color(0.95f, 0.40f, 0.05f),  // Dragon Fish  — fiery orange-red
            new Color(0.70f, 0.20f, 1.00f)   // Anglerfish  — bright purple glow
        };
        Color[] bgs = {
            new Color(0.28f, 0.16f, 0.02f, 1f),  // Default     — dark orange
            new Color(0.02f, 0.18f, 0.30f, 1f),  // Whale       — deep ocean
            new Color(0.02f, 0.20f, 0.08f, 1f),  // Shark       — dark ocean green
            new Color(0.25f, 0.12f, 0.00f, 1f),  // Dragon Fish — dark crimson
            new Color(0.28f, 0.08f, 0.01f, 1f),  // Dragon Fish  — dark fiery brown
            new Color(0.20f, 0.75f, 0.35f, 1f)   // Anglerfish  — light green
        };

        // Resize cards to fit 3 rows
        float cardW = CW * 0.43f;
        float cardH = gridH3 * 0.88f;

        // Resize _cards array for 6 skins
        _cards = new GameObject[6];

        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            _cards[i] = MakeSkinCard(card.transform, idx,
                gridPos[i], cardW, cardH, accents[i], bgs[i], font);
            Debug.Log($"Skin card {i} created at pos={gridPos[i]} size={cardW}x{cardH}");
        }

        // ── Close button ─────────────────────────────────────────────
        float closeBtnY = -CH*0.5f + closeH*0.55f;
        float closeBtnW = CW * 0.50f;
        float closeBtnH = closeH * 0.75f;

        // Shadow
        GameObject closeShadow = new GameObject("CloseShadow");
        closeShadow.transform.SetParent(card.transform, false);
        Image shadowImg = closeShadow.AddComponent<Image>();
        shadowImg.sprite = MakeRoundedRectSprite(256, 128, 32);
        shadowImg.type = Image.Type.Simple;
        shadowImg.color = new Color(0f, 0f, 0f, 0.4f);
        Rect(closeShadow, new Vector2(closeBtnW + 6f, closeBtnH + 6f),
            new Vector2(2f, closeBtnY - 3f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Rounded close button
        GameObject closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(card.transform, false);
        Image closeImg = closeGO.AddComponent<Image>();
        closeImg.sprite = MakeRoundedRectSprite(256, 128, 32);
        closeImg.type = Image.Type.Simple;
        closeImg.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        closeGO.AddComponent<Button>().onClick.AddListener(Hide);
        Rect(closeGO, new Vector2(closeBtnW, closeBtnH),
            new Vector2(0, closeBtnY),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
        MakeLabel(closeGO.transform, "L", "✕  CLOSE", font, sh*0.025f,
            Color.white,
            new Vector2(closeBtnW, closeBtnH), Vector2.zero);

        _panel.SetActive(false);
    }

    // ── Individual skin card ──────────────────────────────────────────────
    Sprite MakeRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            // Check corners
            int cx = Mathf.Clamp(x, radius, width - radius);
            int cy = Mathf.Clamp(y, radius, height - radius);
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            pixels[y * width + x] = dist <= radius ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    Sprite MakeCircleSprite(int resolution = 128)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), center);
            pixels[y * resolution + x] = dist <= radius ? Color.white : Color.clear;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    GameObject MakeSkinCard(Transform parent, int skinId,
        Vector2 pos, float cw, float ch,
        Color accent, Color bgCol, TMP_FontAsset font)
    {
        float sh = Screen.height;
        bool locked = !SkinManager.IsUnlocked(skinId);

        // Shadow behind card
        GameObject shadow = new GameObject("Shadow_"+skinId);
        shadow.transform.SetParent(parent, false);
        Image shadowImg = shadow.AddComponent<Image>();
        shadowImg.sprite = MakeRoundedRectSprite(256, 320, 28);
        shadowImg.type = Image.Type.Simple;
        shadowImg.color = new Color(0f, 0f, 0f, 0.4f);
        Rect(shadow, new Vector2(cw + 6f, ch + 8f), new Vector2(pos.x + 3f, pos.y - 4f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Card with rounded corners
        GameObject card = new GameObject("SC_"+skinId);
        card.transform.SetParent(parent, false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.sprite = MakeRoundedRectSprite(256, 320, 28);
        cardImg.type = Image.Type.Simple;
        cardImg.color = bgCol;
        Button btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => OnSkinCardTapped(skinId));
        Rect(card, new Vector2(cw, ch), pos,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Top accent bar
        GameObject bar = new GameObject("Bar");
        bar.transform.SetParent(card.transform, false);
        Image barImg = bar.AddComponent<Image>();
        barImg.sprite = MakeRoundedRectSprite(256, 64, 28);
        barImg.type = Image.Type.Simple;
        barImg.color = new Color(accent.r, accent.g, accent.b, 0.90f);
        Rect(bar, new Vector2(cw, ch*0.07f),
            new Vector2(0, ch*0.5f - ch*0.035f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Highlight strip at top
        GameObject hl = new GameObject("Highlight");
        hl.transform.SetParent(card.transform, false);
        Image hlImg = hl.AddComponent<Image>();
        hlImg.sprite = MakeRoundedRectSprite(256, 64, 28);
        hlImg.type = Image.Type.Simple;
        hlImg.color = new Color(1f, 1f, 1f, 0.08f);
        Rect(hl, new Vector2(cw, ch*0.5f),
            new Vector2(0, ch*0.25f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Fish image
        string[] names = { "FishDefault", "FishWhale", "FishShark", "FishDragon", "FishPuffer", "FishAnglerfish" };
        float fishSz = ch * 0.48f;
        GameObject fishGO = new GameObject("Fish");
        fishGO.transform.SetParent(card.transform, false);
        Image fishImg = fishGO.AddComponent<Image>();
        Sprite sp = Resources.Load<Sprite>(names[skinId]);
        if (sp != null){ fishImg.sprite = sp; fishImg.preserveAspect = true; fishImg.color = Color.white; }
        else fishImg.color = accent;
        Rect(fishGO, new Vector2(fishSz, fishSz),
            new Vector2(0, ch*0.13f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));

        // Name
        MakeLabel(card.transform, "Name", SkinManager.SkinNames[skinId], font,
            sh*0.022f, accent,
            new Vector2(cw-8f, ch*0.22f),
            new Vector2(0, -ch*0.29f));

        // Cost
        string costStr = skinId==0 ? "Free" : SkinManager.SkinCosts[skinId]+" coins";
        MakeLabel(card.transform, "Cost", costStr, font,
            sh*0.019f, locked ? new Color(0.7f,0.7f,0.7f) : new Color(0.45f,0.90f,0.45f),
            new Vector2(cw-8f, ch*0.20f),
            new Vector2(0, -ch*0.42f));

        MakeLabel(card.transform, "Badge", "Equipped", font,
            sh*0.019f, new Color(0.2f,0.95f,0.4f),
            new Vector2(cw-8f, ch*0.20f),
            new Vector2(0, -ch*0.42f)).gameObject.SetActive(false);

        // Locked overlay
        if (locked)
        {
            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(card.transform, false);
            Image dimImg = dim.AddComponent<Image>();
            dimImg.sprite = MakeRoundedRectSprite(256, 320, 28);
            dimImg.type = Image.Type.Simple;
            dimImg.color = new Color(0f, 0f, 0f, 0.62f);
            Stretch(dim);

            Sprite lockSpr = Resources.Load<Sprite>("LockIcon");
            if (lockSpr != null)
            {
                GameObject lockGO = new GameObject("LockSprite");
                lockGO.transform.SetParent(dim.transform, false);
                Image lockImg = lockGO.AddComponent<Image>();
                lockImg.sprite = lockSpr;
                lockImg.color = Color.white;
                Rect(lockGO, new Vector2(ch*0.20f, ch*0.20f),
                    new Vector2(0, ch*0.06f),
                    new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f));
            }
            else
            {
                MakeLabel(dim.transform, "LockText", "LOCKED", font,
                    sh*0.024f, Color.white,
                    new Vector2(cw, ch*0.30f), Vector2.zero);
            }
        }

        return card;
    }

    void OnSkinCardTapped(int skinId)
    {
        if (SkinManager.IsUnlocked(skinId))
        {
            SkinManager.SelectSkin(skinId);
            GameBootstrap gb = FindFirstObjectByType<GameBootstrap>();
            if (gb != null) gb.ApplySelectedSkin();
        }
        else if (SkinManager.TryPurchase(skinId))
        {
            SkinManager.SelectSkin(skinId);
            // Remove dim overlay — skin is now unlocked
            if (_cards[skinId] != null)
            {
                Transform dim = _cards[skinId].transform.Find("Dim");
                if (dim != null) Destroy(dim.gameObject);
            }
            GameBootstrap gb = FindFirstObjectByType<GameBootstrap>();
            if (gb != null) gb.ApplySelectedSkin();
        }
        Refresh();
    }

    void Refresh()
    {
        if (_coinLabel != null)
            _coinLabel.text = SkinManager.GetCoins().ToString();

        int sel = SkinManager.GetSelectedSkin();
        for (int i = 0; i < 6; i++)
        {
            if (_cards[i] == null) continue;
            bool isSel = (i == sel);

            // Brighten/dim top bar to show selection
            Transform bar = _cards[i].transform.Find("Bar");
            if (bar != null)
            {
                Image bi = bar.GetComponent<Image>();
                Color bc = bi.color;
                bc.a = isSel ? 1f : 0.40f;
                bi.color = bc;
            }

            // Badge vs Cost
            Transform badge = _cards[i].transform.Find("Badge");
            Transform cost  = _cards[i].transform.Find("Cost");
            if (badge != null) badge.gameObject.SetActive(isSel);
            if (cost  != null)
            {
                cost.gameObject.SetActive(!isSel);
                if (!isSel)
                {
                    TextMeshProUGUI ct = cost.GetComponent<TextMeshProUGUI>();
                    if (SkinManager.IsUnlocked(i))
                        ct.text = "Tap to equip";
                    else
                        ct.text = SkinManager.SkinCosts[i] + " coins";
                }
            }
        }
    }
}