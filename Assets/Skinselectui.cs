using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Skin shop — large featured card + 2 peek cards + bottom grid selector.
/// Layout: Header | FeaturedArea (main card + peek cards) | Grid (2×3 thumbnails)
public class SkinSelectUI : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    static SkinSelectUI _instance;

    // ── Public API ────────────────────────────────────────────────────────────
    public static void Show()
    {
        if (_instance == null) return;
        _instance.BuildIfNeeded();
        _instance._panel.SetActive(true);
        _instance._panel.transform.SetAsLastSibling();
        _instance._selectedIdx = SkinManager.GetSelectedSkin();
        _instance.RefreshAll();
    }

    public static void Hide()
    {
        if (_instance != null && _instance._panel != null)
            _instance._panel.SetActive(false);
    }

    // ── Rarity colours ────────────────────────────────────────────────────────
    static readonly Color[] RarityCol =
    {
        new Color(0.00f, 0.82f, 1.00f), // Common    – cyan
        new Color(0.00f, 0.82f, 1.00f), // Common    – cyan
        new Color(0.60f, 0.28f, 1.00f), // Rare      – purple
        new Color(0.60f, 0.28f, 1.00f), // Rare      – purple
        new Color(1.00f, 0.28f, 0.63f), // Epic      – pink
        new Color(1.00f, 0.84f, 0.00f), // Legendary – gold
    };

    const int N = 6; // skin count

    // ── State ─────────────────────────────────────────────────────────────────
    Canvas          _canvas;
    GameObject      _panel;
    bool            _built;
    int             _selectedIdx;

    // Header
    TextMeshProUGUI _coinTxt;

    // Main featured card
    Image           _mainBorder;
    Image           _mainFishImg;
    TextMeshProUGUI _mainRarityTxt;
    Image           _mainRarityBg;
    TextMeshProUGUI _mainNameTxt;
    Image           _actionBtnImg;
    TextMeshProUGUI _actionBtnTxt;

    // Peek cards (index 0 = nearer, index 1 = further)
    Image[]           _peekBorder   = new Image[2];
    Image[]           _peekFishImg  = new Image[2];
    TextMeshProUGUI[] _peekNameTxt  = new TextMeshProUGUI[2];
    TextMeshProUGUI[] _peekRarTxt   = new TextMeshProUGUI[2];
    Image[]           _peekLockDim  = new Image[2];
    int[]             _peekSkinIdx  = new int[2];

    // Grid
    Image[]           _cellBorder   = new Image[N];
    Image[]           _cellFishImg  = new Image[N];
    Image[]           _cellLockDim  = new Image[N];
    Image[]           _cellSelRing  = new Image[N];

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
    }

    // ── Build ─────────────────────────────────────────────────────────────────
    void BuildIfNeeded()
    {
        if (_built) return;

        // Resolve canvas here rather than in Awake — guarantees it exists at build time
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[SkinSelectUI] No Canvas found in scene — cannot build skin UI");
            return;
        }

        _built = true;
        _panel = new GameObject("SkinSelectPanel");
        _panel.transform.SetParent(_canvas.transform, false);

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.01f, 0.05f, 0.16f, 1.00f); // fully opaque — no game UI bleed
        Stretch(_panel.GetComponent<RectTransform>());

        BuildHeader(_panel.transform);
        BuildFeaturedArea(_panel.transform);
        BuildGrid(_panel.transform);
    }

    // ── Header ────────────────────────────────────────────────────────────────
    void BuildHeader(Transform parent)
    {
        var hdr   = MakeGO("Header", parent);
        var hdrRT = hdr.AddComponent<RectTransform>(); // container — no Image, needs explicit RT
        hdrRT.anchorMin = new Vector2(0f, 1f); hdrRT.anchorMax = new Vector2(1f, 1f);
        hdrRT.pivot = new Vector2(0.5f, 1f);
        hdrRT.sizeDelta = new Vector2(0f, 145f);
        hdrRT.anchoredPosition = Vector2.zero;

        // Back button
        var backGO = MakeGO("Back", hdr.transform);
        var backImg = backGO.AddComponent<Image>();
        backImg.sprite = RoundedRect(120, 90, 28);
        backImg.type   = Image.Type.Simple;
        backImg.color  = new Color(0.08f, 0.18f, 0.42f, 0.90f);
        var backBtn = backGO.AddComponent<Button>();
        backBtn.onClick.AddListener(() => Hide());
        var backRT = backGO.GetComponent<RectTransform>();
        backRT.anchorMin = backRT.anchorMax = new Vector2(0f, 0.5f);
        backRT.pivot = new Vector2(0f, 0.5f);
        backRT.sizeDelta = new Vector2(120f, 90f);
        backRT.anchoredPosition = new Vector2(30f, 0f);
        Lbl(backGO.transform, "<b><</b>", 52f, Color.white);

        // Title
        var titleGO = MakeGO("Title", hdr.transform);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.15f, 0f); titleRT.anchorMax = new Vector2(0.85f, 1f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "FISH SKINS"; titleTxt.fontSize = 54f;
        titleTxt.fontStyle = FontStyles.Bold; titleTxt.color = Color.white;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.overflowMode = TextOverflowModes.Overflow;

        // Coin pill
        var coinGO = MakeGO("Coins", hdr.transform);
        var coinImg = coinGO.AddComponent<Image>();
        coinImg.sprite = RoundedRect(240, 72, 36);
        coinImg.type   = Image.Type.Simple;
        coinImg.color  = new Color(0.12f, 0.09f, 0.02f, 0.95f);
        var coinRT = coinGO.GetComponent<RectTransform>();
        coinRT.anchorMin = coinRT.anchorMax = new Vector2(1f, 0.5f);
        coinRT.pivot = new Vector2(1f, 0.5f);
        coinRT.sizeDelta = new Vector2(240f, 72f);
        coinRT.anchoredPosition = new Vector2(-25f, 0f);
        _coinTxt = Lbl(coinGO.transform, "0", 36f, new Color(1f, 0.92f, 0.18f));
        _coinTxt.alignment = TextAlignmentOptions.Center;
    }

    // ── Featured area ─────────────────────────────────────────────────────────
    void BuildFeaturedArea(Transform parent)
    {
        var area = MakeGO("Featured", parent);
        var aRT  = area.AddComponent<RectTransform>(); // first and only RT add
        // Occupies roughly middle 45% of screen height, below header
        aRT.anchorMin = new Vector2(0f, 0.38f); aRT.anchorMax = new Vector2(1f, 0.93f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;

        const float MW = 540f, MH = 730f;
        const float P1W = 440f, P1H = 587f;
        const float P2W = 350f, P2H = 467f;

        // Add peek cards FIRST so they render behind the main card
        BuildPeekCard(area.transform, 1, P2W, P2H, new Vector2(270f, -60f));
        BuildPeekCard(area.transform, 0, P1W, P1H, new Vector2(110f, -25f));
        BuildMainCard(area.transform, MW, MH, new Vector2(-140f, 0f));
    }

    void BuildMainCard(Transform parent, float w, float h, Vector2 offset)
    {
        var card   = MakeGO("MainCard", parent);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(w, h);
        cardRT.anchoredPosition = offset;

        // Glow border (rarity coloured — rendered behind body)
        var glowGO  = MakeGO("Glow", card.transform);
        _mainBorder = glowGO.AddComponent<Image>();
        _mainBorder.sprite = RoundedRect((int)(w+20), (int)(h+20), 46);
        _mainBorder.type   = Image.Type.Simple;
        var glowRT = glowGO.GetComponent<RectTransform>();
        glowRT.anchorMin = glowRT.anchorMax = glowRT.pivot = new Vector2(0.5f, 0.5f);
        glowRT.sizeDelta = new Vector2(w+20, h+20); glowRT.anchoredPosition = Vector2.zero;

        // Card body
        var bodyGO  = MakeGO("Body", card.transform);
        var bodyImg = bodyGO.AddComponent<Image>();
        bodyImg.sprite = RoundedRect((int)w, (int)h, 42);
        bodyImg.type   = Image.Type.Simple;
        bodyImg.color  = new Color(0.04f, 0.09f, 0.24f, 1f);
        var bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = bodyRT.anchorMax = bodyRT.pivot = new Vector2(0.5f, 0.5f);
        bodyRT.sizeDelta = new Vector2(w, h); bodyRT.anchoredPosition = Vector2.zero;

        // Rarity badge (top-left)
        var badgeGO  = MakeGO("RarityBadge", card.transform);
        _mainRarityBg = badgeGO.AddComponent<Image>();
        _mainRarityBg.sprite = RoundedRect(210, 58, 29);
        _mainRarityBg.type   = Image.Type.Simple;
        var badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(0f, 1f);
        badgeRT.pivot = new Vector2(0f, 1f);
        badgeRT.sizeDelta = new Vector2(210f, 58f);
        badgeRT.anchoredPosition = new Vector2(22f, -22f);
        _mainRarityTxt = Lbl(badgeGO.transform, "Common", 30f, Color.white);
        _mainRarityTxt.fontStyle = FontStyles.Bold;
        _mainRarityTxt.alignment = TextAlignmentOptions.Center;

        // Fish image (upper portion of card)
        var fishGO   = MakeGO("Fish", card.transform);
        _mainFishImg = fishGO.AddComponent<Image>();
        _mainFishImg.preserveAspect = true; _mainFishImg.raycastTarget = false;
        var fishRT = fishGO.GetComponent<RectTransform>();
        fishRT.anchorMin = new Vector2(0.04f, 0.28f); fishRT.anchorMax = new Vector2(0.96f, 0.94f);
        fishRT.offsetMin = fishRT.offsetMax = Vector2.zero;

        // Skin name
        var nameGO   = MakeGO("Name", card.transform);
        _mainNameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        _mainNameTxt.fontSize  = 50f; _mainNameTxt.fontStyle = FontStyles.Bold;
        _mainNameTxt.color     = Color.white;
        _mainNameTxt.alignment = TextAlignmentOptions.Center;
        _mainNameTxt.overflowMode = TextOverflowModes.Overflow;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.14f); nameRT.anchorMax = new Vector2(1f, 0.28f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        // Action button
        var btnGO    = MakeGO("ActionBtn", card.transform);
        _actionBtnImg = btnGO.AddComponent<Image>();
        _actionBtnImg.sprite = RoundedRect(400, 105, 44);
        _actionBtnImg.type   = Image.Type.Simple;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnActionTapped);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.sizeDelta = new Vector2(400f, 105f);
        btnRT.anchoredPosition = new Vector2(0f, 22f);
        _actionBtnTxt = Lbl(btnGO.transform, "EQUIP", 44f, Color.white);
        _actionBtnTxt.fontStyle = FontStyles.Bold;
        _actionBtnTxt.alignment = TextAlignmentOptions.Center;
    }

    void BuildPeekCard(Transform parent, int pi, float w, float h, Vector2 offset)
    {
        var card = MakeGO("Peek" + pi, parent);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(w, h);
        cardRT.anchoredPosition = offset;

        // Border
        var borGO      = MakeGO("Border", card.transform);
        _peekBorder[pi] = borGO.AddComponent<Image>();
        _peekBorder[pi].sprite = RoundedRect((int)(w+16), (int)(h+16), 38);
        _peekBorder[pi].type   = Image.Type.Simple;
        var borRT = borGO.GetComponent<RectTransform>();
        borRT.anchorMin = borRT.anchorMax = borRT.pivot = new Vector2(0.5f, 0.5f);
        borRT.sizeDelta = new Vector2(w+16, h+16); borRT.anchoredPosition = Vector2.zero;

        // Body
        var bodGO  = MakeGO("Body", card.transform);
        var bodImg = bodGO.AddComponent<Image>();
        bodImg.sprite = RoundedRect((int)w, (int)h, 34);
        bodImg.type   = Image.Type.Simple;
        bodImg.color  = new Color(0.04f, 0.08f, 0.22f, 0.92f);
        var bodRT = bodGO.GetComponent<RectTransform>();
        bodRT.anchorMin = bodRT.anchorMax = bodRT.pivot = new Vector2(0.5f, 0.5f);
        bodRT.sizeDelta = new Vector2(w, h); bodRT.anchoredPosition = Vector2.zero;

        // Fish image
        var fishGO      = MakeGO("Fish", card.transform);
        _peekFishImg[pi] = fishGO.AddComponent<Image>();
        _peekFishImg[pi].preserveAspect = true; _peekFishImg[pi].raycastTarget = false;
        var fishRT = fishGO.GetComponent<RectTransform>();
        fishRT.anchorMin = new Vector2(0.06f, 0.24f); fishRT.anchorMax = new Vector2(0.94f, 0.88f);
        fishRT.offsetMin = fishRT.offsetMax = Vector2.zero;

        // Skin name (below fish)
        var nameGO      = MakeGO("Name", card.transform);
        _peekNameTxt[pi] = nameGO.AddComponent<TextMeshProUGUI>();
        _peekNameTxt[pi].fontSize  = 30f; _peekNameTxt[pi].fontStyle = FontStyles.Bold;
        _peekNameTxt[pi].color     = new Color(0.85f, 0.85f, 0.90f);
        _peekNameTxt[pi].alignment = TextAlignmentOptions.Center;
        _peekNameTxt[pi].overflowMode = TextOverflowModes.Overflow;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.06f); nameRT.anchorMax = new Vector2(1f, 0.22f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        // Rarity label (top)
        var rarGO      = MakeGO("Rarity", card.transform);
        _peekRarTxt[pi] = rarGO.AddComponent<TextMeshProUGUI>();
        _peekRarTxt[pi].fontSize  = 22f; _peekRarTxt[pi].fontStyle = FontStyles.Bold;
        _peekRarTxt[pi].color     = Color.white;
        _peekRarTxt[pi].alignment = TextAlignmentOptions.Center;
        _peekRarTxt[pi].overflowMode = TextOverflowModes.Overflow;
        var rarRT = rarGO.GetComponent<RectTransform>();
        rarRT.anchorMin = new Vector2(0f, 0.88f); rarRT.anchorMax = new Vector2(1f, 1f);
        rarRT.offsetMin = rarRT.offsetMax = Vector2.zero;

        // Lock dim overlay
        var lockGO      = MakeGO("LockDim", card.transform);
        _peekLockDim[pi] = lockGO.AddComponent<Image>();
        _peekLockDim[pi].color = new Color(0f, 0f, 0f, 0.58f);
        _peekLockDim[pi].sprite = RoundedRect((int)w, (int)h, 34);
        _peekLockDim[pi].type   = Image.Type.Simple;
        _peekLockDim[pi].raycastTarget = false;
        var lockRT = lockGO.GetComponent<RectTransform>();
        lockRT.anchorMin = lockRT.anchorMax = lockRT.pivot = new Vector2(0.5f, 0.5f);
        lockRT.sizeDelta = new Vector2(w, h); lockRT.anchoredPosition = Vector2.zero;

        // Lock label inside dim
        var lockLblTxt = Lbl(lockGO.transform, "LOCKED", 28f, new Color(1f, 1f, 1f, 0.80f));
        lockLblTxt.fontStyle = FontStyles.Bold;
        lockLblTxt.alignment = TextAlignmentOptions.Center;

        // Tap to preview
        int capturedPi = pi;
        var hitImg = card.AddComponent<Image>(); hitImg.color = Color.clear;
        var cardBtn = card.AddComponent<Button>();
        cardBtn.onClick.AddListener(() => SelectFromPeek(capturedPi));
    }

    // ── Grid ──────────────────────────────────────────────────────────────────
    void BuildGrid(Transform parent)
    {
        // Grid panel — bottom 37% of screen
        var gridBgGO  = MakeGO("GridPanel", parent);
        var gridBgImg = gridBgGO.AddComponent<Image>();
        gridBgImg.sprite = RoundedRect(1080, 680, 36);
        gridBgImg.type   = Image.Type.Simple;
        gridBgImg.color  = new Color(0.03f, 0.07f, 0.20f, 0.88f);
        var gridBgRT = gridBgGO.GetComponent<RectTransform>();
        gridBgRT.anchorMin = new Vector2(0f, 0f); gridBgRT.anchorMax = new Vector2(1f, 0.38f);
        gridBgRT.offsetMin = gridBgRT.offsetMax = Vector2.zero;

        // "SELECT SKIN" label
        var hdrGO = MakeGO("GridHdr", gridBgGO.transform);
        var hdrRT = hdrGO.AddComponent<RectTransform>(); // container, needs explicit RT
        hdrRT.anchorMin = new Vector2(0f, 1f); hdrRT.anchorMax = new Vector2(1f, 1f);
        hdrRT.pivot = new Vector2(0.5f, 1f);
        hdrRT.sizeDelta = new Vector2(0f, 60f);
        hdrRT.anchoredPosition = new Vector2(0f, -6f);
        var hdrTxt = Lbl(hdrGO.transform, "SELECT SKIN", 30f, new Color(0.65f, 0.70f, 0.85f));
        hdrTxt.fontStyle = FontStyles.Bold; hdrTxt.alignment = TextAlignmentOptions.Center;

        // Grid container — use GridLayoutGroup for clean auto-layout
        var gridGO = MakeGO("Grid", gridBgGO.transform);
        var gridRT = gridGO.AddComponent<RectTransform>(); // container, needs explicit RT
        gridRT.anchorMin = new Vector2(0f, 0f); gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.offsetMin = new Vector2(24f, 12f); gridRT.offsetMax = new Vector2(-24f, -68f);

        var glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize       = new Vector2(310f, 255f);
        glg.spacing        = new Vector2(16f, 14f);
        glg.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;
        glg.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < N; i++)
            BuildGridCell(gridGO.transform, i);
    }

    void BuildGridCell(Transform parent, int idx)
    {
        var cell   = MakeGO("Cell_" + idx, parent);
        var cellRT = cell.AddComponent<RectTransform>(); // GridLayoutGroup sizes this — first RT add

        // Selection ring — added FIRST so it renders behind the rarity border.
        // Shows as a white outer halo around the colored border when selected.
        var selGO        = MakeGO("SelRing", cell.transform);
        _cellSelRing[idx] = selGO.AddComponent<Image>();
        _cellSelRing[idx].sprite = RoundedRect(320, 250, 32);
        _cellSelRing[idx].type   = Image.Type.Simple;
        _cellSelRing[idx].color  = Color.clear; // hidden until selected
        _cellSelRing[idx].raycastTarget = false;
        var selRT = selGO.GetComponent<RectTransform>();
        selRT.anchorMin = selRT.anchorMax = selRT.pivot = new Vector2(0.5f, 0.5f);
        selRT.sizeDelta = new Vector2(320f, 250f); selRT.anchoredPosition = Vector2.zero;

        // Rarity border
        var borGO       = MakeGO("Border", cell.transform);
        _cellBorder[idx] = borGO.AddComponent<Image>();
        _cellBorder[idx].sprite = RoundedRect(308, 242, 28);
        _cellBorder[idx].type   = Image.Type.Simple;
        _cellBorder[idx].color  = RarityCol[idx];
        _cellBorder[idx].raycastTarget = false;
        var borRT = borGO.GetComponent<RectTransform>();
        borRT.anchorMin = borRT.anchorMax = borRT.pivot = new Vector2(0.5f, 0.5f);
        borRT.sizeDelta = new Vector2(308f, 242f); borRT.anchoredPosition = Vector2.zero;

        // Cell body
        var bodGO  = MakeGO("Body", cell.transform);
        var bodImg = bodGO.AddComponent<Image>();
        bodImg.sprite = RoundedRect(296, 230, 24);
        bodImg.type   = Image.Type.Simple;
        bodImg.color  = new Color(0.05f, 0.11f, 0.28f, 1f);
        var bodRT = bodGO.GetComponent<RectTransform>();
        bodRT.anchorMin = bodRT.anchorMax = bodRT.pivot = new Vector2(0.5f, 0.5f);
        bodRT.sizeDelta = new Vector2(296f, 230f); bodRT.anchoredPosition = Vector2.zero;

        // Rarity label bar (top of cell)
        var rarBgGO  = MakeGO("RarBg", cell.transform);
        var rarBgImg = rarBgGO.AddComponent<Image>();
        rarBgImg.sprite = RoundedRect(296, 42, 14);
        rarBgImg.type   = Image.Type.Simple;
        rarBgImg.color  = RarityCol[idx];
        rarBgImg.raycastTarget = false;
        var rarBgRT = rarBgGO.GetComponent<RectTransform>();
        rarBgRT.anchorMin = new Vector2(0f, 1f); rarBgRT.anchorMax = new Vector2(1f, 1f);
        rarBgRT.pivot = new Vector2(0.5f, 1f);
        rarBgRT.sizeDelta = new Vector2(0f, 42f); rarBgRT.anchoredPosition = Vector2.zero;
        var rarTxt = Lbl(rarBgGO.transform, SkinManager.Rarities[idx].ToUpper(), 22f, Color.white);
        rarTxt.fontStyle = FontStyles.Bold; rarTxt.alignment = TextAlignmentOptions.Center;

        // Fish thumbnail
        var fishGO       = MakeGO("Fish", cell.transform);
        _cellFishImg[idx] = fishGO.AddComponent<Image>();
        _cellFishImg[idx].preserveAspect = true; _cellFishImg[idx].raycastTarget = false;
        Sprite sp = Resources.Load<Sprite>(SkinManager.SkinSprites[idx]);
        if (sp != null) _cellFishImg[idx].sprite = sp;
        var fishRT = fishGO.GetComponent<RectTransform>();
        fishRT.anchorMin = new Vector2(0.08f, 0.22f); fishRT.anchorMax = new Vector2(0.92f, 0.88f);
        fishRT.offsetMin = fishRT.offsetMax = Vector2.zero;

        // Price / free label (bottom)
        var priceGO = MakeGO("Price", cell.transform);
        var priceRT = priceGO.AddComponent<RectTransform>();
        priceRT.anchorMin = new Vector2(0f, 0f); priceRT.anchorMax = new Vector2(1f, 0f);
        priceRT.pivot = new Vector2(0.5f, 0f);
        priceRT.sizeDelta = new Vector2(0f, 38f); priceRT.anchoredPosition = new Vector2(0f, 6f);
        string priceStr = idx == 0 ? "FREE" : SkinManager.SkinCosts[idx].ToString() + " coins";
        var priceTxt = Lbl(priceGO.transform, priceStr, 24f, new Color(1f, 0.90f, 0.20f));
        priceTxt.fontStyle = FontStyles.Bold; priceTxt.alignment = TextAlignmentOptions.Center;

        // Lock dim (shown when locked)
        var lockGO       = MakeGO("LockDim", cell.transform);
        _cellLockDim[idx] = lockGO.AddComponent<Image>();
        _cellLockDim[idx].sprite = RoundedRect(296, 230, 24);
        _cellLockDim[idx].type   = Image.Type.Simple;
        _cellLockDim[idx].color  = new Color(0f, 0f, 0f, 0.52f);
        _cellLockDim[idx].raycastTarget = false;
        var lockRT = lockGO.GetComponent<RectTransform>();
        lockRT.anchorMin = lockRT.anchorMax = lockRT.pivot = new Vector2(0.5f, 0.5f);
        lockRT.sizeDelta = new Vector2(296f, 230f); lockRT.anchoredPosition = Vector2.zero;
        // "LOCKED" text on dim
        var lockTxt = Lbl(lockGO.transform, "LOCKED", 26f, new Color(1f,1f,1f,0.80f));
        lockTxt.fontStyle = FontStyles.Bold; lockTxt.alignment = TextAlignmentOptions.Center;

        // Tap to select
        int captured = idx;
        var hitImg = cell.AddComponent<Image>(); hitImg.color = Color.clear;
        var cellBtn = cell.AddComponent<Button>();
        cellBtn.onClick.AddListener(() => SelectSkin(captured));
    }

    // ── Refresh ───────────────────────────────────────────────────────────────
    void RefreshAll()
    {
        RefreshCoins();
        RefreshMainCard();
        RefreshPeekCards();
        RefreshGrid();
    }

    void RefreshCoins()
    {
        if (_coinTxt != null) _coinTxt.text = SkinManager.GetCoins().ToString("N0");
    }

    void RefreshMainCard()
    {
        int id = _selectedIdx;
        Color rc = RarityCol[id];

        _mainBorder.color    = rc;
        _mainRarityBg.color  = rc;
        _mainRarityTxt.text  = SkinManager.Rarities[id].ToUpper();
        _mainNameTxt.text    = SkinManager.SkinNames[id].ToUpper();

        Sprite sp = Resources.Load<Sprite>(SkinManager.SkinSprites[id]);
        _mainFishImg.sprite = sp;
        _mainFishImg.color  = sp != null ? Color.white : new Color(0.3f, 0.3f, 0.4f, 0.5f);

        bool unlocked  = SkinManager.IsUnlocked(id);
        bool equipped  = SkinManager.GetSelectedSkin() == id && unlocked;
        int  coins     = SkinManager.GetCoins();
        int  cost      = SkinManager.SkinCosts[id];
        bool canAfford = coins >= cost;

        if (equipped)
        {
            _actionBtnImg.color = new Color(0.00f, 0.65f, 0.55f);
            _actionBtnTxt.text  = "EQUIPPED  v";
        }
        else if (unlocked)
        {
            _actionBtnImg.color = new Color(0.10f, 0.45f, 0.92f);
            _actionBtnTxt.text  = "EQUIP";
        }
        else if (canAfford)
        {
            _actionBtnImg.color = new Color(0.08f, 0.62f, 0.18f);
            _actionBtnTxt.text  = "BUY  " + cost;
        }
        else
        {
            _actionBtnImg.color = new Color(0.30f, 0.30f, 0.35f);
            _actionBtnTxt.text  = cost + " coins";
        }
    }

    void RefreshPeekCards()
    {
        for (int pi = 0; pi < 2; pi++)
        {
            int sid = (_selectedIdx + pi + 1) % N;
            _peekSkinIdx[pi] = sid;

            Color rc = RarityCol[sid];
            _peekBorder[pi].color = rc;
            _peekRarTxt[pi].text  = SkinManager.Rarities[sid];
            _peekNameTxt[pi].text = SkinManager.SkinNames[sid].ToUpper();

            Sprite sp = Resources.Load<Sprite>(SkinManager.SkinSprites[sid]);
            _peekFishImg[pi].sprite = sp;
            _peekFishImg[pi].color  = sp != null ? Color.white : new Color(0.3f, 0.3f, 0.4f);

            bool locked = !SkinManager.IsUnlocked(sid);
            _peekLockDim[pi].gameObject.SetActive(locked);
        }
    }

    void RefreshGrid()
    {
        for (int i = 0; i < N; i++)
        {
            bool locked   = !SkinManager.IsUnlocked(i);
            bool selected = i == _selectedIdx;

            _cellLockDim[i].gameObject.SetActive(locked);

            // White outer halo visible only on selected cell — rendered behind border so content stays clear
            _cellSelRing[i].color = selected ? Color.white : Color.clear;

            // Bright border on selected, dimmed on others
            _cellBorder[i].color = selected
                ? RarityCol[i]
                : new Color(RarityCol[i].r * 0.55f, RarityCol[i].g * 0.55f, RarityCol[i].b * 0.55f);
        }
    }

    // ── Interaction ───────────────────────────────────────────────────────────
    void SelectSkin(int idx)
    {
        _selectedIdx = idx;
        RefreshAll();
    }

    void SelectFromPeek(int peekIdx)
    {
        _selectedIdx = _peekSkinIdx[peekIdx];
        RefreshAll();
    }

    void OnActionTapped()
    {
        int id = _selectedIdx;
        if (SkinManager.IsUnlocked(id))
        {
            SkinManager.SelectSkin(id);
            FindFirstObjectByType<GameBootstrap>()?.ApplySelectedSkin();
            Hide();
            return;
        }
        if (SkinManager.TryPurchase(id))
        {
            SkinManager.SelectSkin(id);
            FindFirstObjectByType<GameBootstrap>()?.ApplySelectedSkin();
            Hide();
            return;
        }
        // Not enough coins — flash the coin display
        RefreshCoins();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go; // caller is responsible for adding RectTransform or Image (which adds it)
    }

    static TextMeshProUGUI Lbl(Transform parent, string text, float size, Color col)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    static Sprite RoundedRect(int w, int h, int r)
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
