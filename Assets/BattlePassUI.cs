using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Battle Pass UI — fullscreen overlay, scrollable tier list, real data.
/// Call BattlePassUI.Show() from anywhere. Safe to call before BattlePassManager
/// exists on scene — it self-initialises.
/// </summary>
public static class BattlePassUI
{
    static GameObject _root;
    static TMP_FontAsset _font;

    // ── Colours ──────────────────────────────────────────────────────────
    static readonly Color BG_DARK    = new Color(0.05f, 0.10f, 0.18f, 0.98f);
    static readonly Color BG_CARD    = new Color(0.09f, 0.17f, 0.28f, 1.00f);
    static readonly Color BG_LOCKED  = new Color(0.12f, 0.12f, 0.14f, 1.00f);
    static readonly Color COL_GOLD   = new Color(1.00f, 0.82f, 0.10f, 1.00f);
    static readonly Color COL_GREEN  = new Color(0.18f, 0.85f, 0.38f, 1.00f);
    static readonly Color COL_BLUE   = new Color(0.28f, 0.62f, 1.00f, 1.00f);
    static readonly Color COL_DIM    = new Color(0.55f, 0.62f, 0.70f, 1.00f);
    static readonly Color COL_WHITE  = Color.white;

    // ── Entry point ──────────────────────────────────────────────────────
    public static void Show()
    {
        // Guarantee manager exists and data is ready before we touch it
        BattlePassManager.Create();

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return;

        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (_root != null)
        {
            // Rebuild content in case XP / claim state changed
            Object.Destroy(_root);
            _root = null;
        }

        Build(canvas);
    }

    // ── Build ────────────────────────────────────────────────────────────
    static void Build(Canvas canvas)
    {
        // Root — fullscreen
        _root = NewGO("BattlePassRoot", canvas.transform);
        Stretch(_root);
        _root.AddComponent<Image>().color = BG_DARK;

        // Must be on top of all other canvas children so close button receives events
        _root.transform.SetAsLastSibling();

        // ── Header zone: starts at 5% from top (safe area) to 26% ───────
        // Keeps content away from phone notch/status bar
        BuildHeader(_root);

        // ── Scrollable tier list (26% – 90%) ────────────────────────────
        BuildTierScroll(_root);

        // ── Footer / Go Premium button (bottom 10%) ──────────────────────
        if (!BattlePassManager.IsPremium())
            BuildPremiumFooter(_root);

        // ── Close button ─────────────────────────────────────────────────
        BuildCloseButton(_root);
    }

    // ── Header ───────────────────────────────────────────────────────────
    static void BuildHeader(GameObject parent)
    {
        var season    = BattlePassManager.GetSeason();
        int tier      = BattlePassManager.GetCurrentTier();
        int xp        = BattlePassManager.GetXP();

        // Header background strip — starts at 5% from top to leave safe-area room
        var hdr = NewGO("Header", parent.transform);
        var hdrRT = hdr.AddComponent<RectTransform>();
        hdrRT.anchorMin = new Vector2(0f, 0.76f);
        hdrRT.anchorMax = new Vector2(1f, 0.95f);
        hdrRT.offsetMin = hdrRT.offsetMax = Vector2.zero;
        hdr.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 1f);

        // Season title — within header, leave room for close button (80px) on right
        var title = MakeTMP("SeasonTitle", hdr.transform,
            $"Season {season.number}  ·  {season.name}",
            32, FontStyles.Bold, COL_GOLD);
        var tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.52f); tRT.anchorMax = new Vector2(1f, 1.00f);
        tRT.offsetMin = new Vector2(24, 0); tRT.offsetMax = new Vector2(-90, -10);

        // Tier label
        var tierLbl = MakeTMP("TierLabel", hdr.transform,
            tier == 0 ? "Tier 0  ·  No XP yet" : $"Tier {tier}  ·  {xp} XP earned",
            22, FontStyles.Normal, COL_DIM);
        var tlRT = tierLbl.GetComponent<RectTransform>();
        tlRT.anchorMin = new Vector2(0f, 0.28f); tlRT.anchorMax = new Vector2(1f, 0.54f);
        tlRT.offsetMin = new Vector2(24, 0); tlRT.offsetMax = new Vector2(-24, 0);

        // XP bar background
        var barBG = NewGO("XPBarBG", hdr.transform);
        var barBGRT = barBG.AddComponent<RectTransform>();
        barBGRT.anchorMin = new Vector2(0.04f, 0.07f); barBGRT.anchorMax = new Vector2(0.96f, 0.24f);
        barBGRT.offsetMin = barBGRT.offsetMax = Vector2.zero;
        barBG.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.20f, 1f);

        // XP bar fill
        // Calculate fill: find next tier's XP requirement
        var tiers    = season.tiers;
        int prevXP   = tier == 0 ? 0 : (tier <= tiers.Count ? tiers[tier - 1].xpRequired : 0);
        int nextXP   = tier < tiers.Count ? tiers[tier].xpRequired : prevXP;
        float fill   = (nextXP - prevXP) > 0 ? Mathf.Clamp01((float)(xp - prevXP) / (nextXP - prevXP)) : 1f;

        var barFill = NewGO("XPFill", barBG.transform);
        var barFillRT = barFill.AddComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = new Vector2(fill, 1f);
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;
        barFill.AddComponent<Image>().color = COL_GREEN;
    }

    // ── Tier scroll ───────────────────────────────────────────────────────
    static void BuildTierScroll(GameObject parent)
    {
        float footerHeight = BattlePassManager.IsPremium() ? 0.00f : 0.10f;

        // ScrollRect viewport — sits between footer and header
        var view = NewGO("TierScrollView", parent.transform);
        var viewRT = view.AddComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(0f, footerHeight);
        viewRT.anchorMax = new Vector2(1f, 0.76f);
        viewRT.offsetMin = viewRT.offsetMax = Vector2.zero;

        Image viewMask = view.AddComponent<Image>();
        viewMask.color = Color.clear;
        view.AddComponent<Mask>().showMaskGraphic = false;

        ScrollRect scroll = view.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 30f;

        // Content container
        var content = NewGO("Content", view.transform);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding           = new RectOffset(16, 16, 12, 12);
        vlg.spacing           = 10;
        vlg.childAlignment    = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRT;

        // Column header
        BuildColumnHeader(content);

        // Tier rows
        var season = BattlePassManager.GetSeason();
        foreach (var t in season.tiers)
            BuildTierRow(content, t);
    }

    static void BuildColumnHeader(GameObject parent)
    {
        var row = NewGO("ColHeader", parent.transform);
        row.AddComponent<LayoutElement>().preferredHeight = 36;
        row.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 0.8f);

        var hGrid = row.AddComponent<HorizontalLayoutGroup>();
        hGrid.childAlignment    = TextAnchor.MiddleLeft;
        hGrid.childControlHeight = true;
        hGrid.childControlWidth  = false;
        hGrid.childForceExpandHeight = true;

        MakeHeaderCell(row.transform, "TIER",    80,  TextAlignmentOptions.Center);
        MakeHeaderCell(row.transform, "FREE",    0,   TextAlignmentOptions.Center, true);
        MakeHeaderCell(row.transform, "PREMIUM", 110, TextAlignmentOptions.Center);
        MakeHeaderCell(row.transform, "",        80,  TextAlignmentOptions.Center);
    }

    static void MakeHeaderCell(Transform parent, string text, float width, TextAlignmentOptions align, bool flex = false)
    {
        var cell = NewGO("H_" + text, parent);
        var le   = cell.AddComponent<LayoutElement>();
        if (flex) le.flexibleWidth = 1; else le.preferredWidth = width;
        var tmp = MakeTMP(text, cell.transform, text, 18, FontStyles.Bold, COL_DIM);
        Stretch(tmp.gameObject);
        tmp.alignment = align;
    }

    static void BuildTierRow(GameObject parent, BattlePassManager.Tier tier)
    {
        var row = NewGO($"Tier{tier.number}", parent.transform);
        var le  = row.AddComponent<LayoutElement>();
        le.preferredHeight = 64;

        row.AddComponent<Image>().color = tier.unlocked ? BG_CARD : BG_LOCKED;

        var hGrid = row.AddComponent<HorizontalLayoutGroup>();
        hGrid.childAlignment     = TextAnchor.MiddleLeft;
        hGrid.childControlHeight = true;
        hGrid.childControlWidth  = false;
        hGrid.childForceExpandHeight = true;
        hGrid.padding = new RectOffset(0, 4, 0, 0);

        // Tier number column
        var numCell = NewGO("Num", row.transform);
        numCell.AddComponent<LayoutElement>().preferredWidth = 80;
        var numTmp = MakeTMP("T", numCell.transform, tier.number.ToString(), 28, FontStyles.Bold,
            tier.unlocked ? COL_GOLD : COL_DIM);
        Stretch(numTmp.gameObject);
        numTmp.alignment = TextAlignmentOptions.Center;

        // Free reward column
        var freeCell = NewGO("Free", row.transform);
        freeCell.AddComponent<LayoutElement>().flexibleWidth = 1;
        string freeStr = RewardStr(tier.freeReward);
        var freeTmp = MakeTMP("FR", freeCell.transform, freeStr, 20, FontStyles.Normal,
            tier.unlocked ? COL_WHITE : COL_DIM);
        Stretch(freeTmp.gameObject);
        freeTmp.alignment = TextAlignmentOptions.Center;

        // Premium reward column
        var premCell = NewGO("Prem", row.transform);
        premCell.AddComponent<LayoutElement>().preferredWidth = 110;
        string premStr = BattlePassManager.IsPremium() ? RewardStr(tier.premiumReward) : "🔒  Premium";
        var premTmp = MakeTMP("PR", premCell.transform, premStr, 18, FontStyles.Normal,
            BattlePassManager.IsPremium() ? COL_GOLD : COL_DIM);
        Stretch(premTmp.gameObject);
        premTmp.alignment = TextAlignmentOptions.Center;

        // Action column
        var actCell = NewGO("Act", row.transform);
        actCell.AddComponent<LayoutElement>().preferredWidth = 80;

        if (tier.unlocked && !tier.claimed)
        {
            var claimBtn = NewGO("ClaimBtn", actCell.transform);
            Stretch(claimBtn, 8, 10);
            var img      = claimBtn.AddComponent<Image>();
            Sprite claimSpr = Resources.Load<Sprite>("btn_claim");
            if (claimSpr != null) { img.sprite = claimSpr; img.preserveAspect = true; }
            else img.color = COL_GREEN;
            var btn = claimBtn.AddComponent<Button>();
            btn.targetGraphic = img;
            int capturedNum = tier.number;
            btn.onClick.AddListener(() =>
            {
                BattlePassManager.ClaimReward(capturedNum);
                img.color = COL_DIM;
                btn.interactable = false;
                var lbl = claimBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl) lbl.text = "✓";
            });
        }
        else if (tier.claimed)
        {
            var doneTmp = MakeTMP("Done", actCell.transform, "✓", 26, FontStyles.Bold, COL_GREEN);
            Stretch(doneTmp.gameObject);
            doneTmp.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            var lockTmp = MakeTMP("Lock", actCell.transform, "🔒", 22, FontStyles.Normal, COL_DIM);
            Stretch(lockTmp.gameObject);
            lockTmp.alignment = TextAlignmentOptions.Center;
        }
    }

    static string RewardStr(BattlePassManager.Reward r)
    {
        if (r == null) return "—";
        return r.type switch
        {
            BattlePassManager.Reward.RewardType.Coins => $"🪙 {r.amount}",
            BattlePassManager.Reward.RewardType.Gems  => $"💎 {r.amount}",
            BattlePassManager.Reward.RewardType.Skin  => $"🐠 {r.cosmetic?.Replace("Skin_", "")}",
            _ => "—"
        };
    }

    // ── Premium footer ────────────────────────────────────────────────────
    static void BuildPremiumFooter(GameObject parent)
    {
        var footer = NewGO("PremiumFooter", parent.transform);
        var fRT    = footer.AddComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0f, 0f);
        fRT.anchorMax = new Vector2(1f, 0.10f);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;
        footer.AddComponent<Image>().color = new Color(0.10f, 0.06f, 0.02f, 1f);

        var premBtn = NewGO("GoPremium", footer.transform);
        Stretch(premBtn, 20, 8);
        var img     = premBtn.AddComponent<Image>();
        Sprite spr  = Resources.Load<Sprite>("btn_premium");
        if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
        else img.color = COL_GOLD;
        var btn = premBtn.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            BattlePassManager.PurchasePremium();
            // Rebuild panel to show premium rewards
            Show();
        });

        var lbl = MakeTMP("PremLbl", premBtn.transform,
            "⭐  GO PREMIUM  —  Unlock all rewards", 22, FontStyles.Bold, new Color(0.08f, 0.04f, 0f));
        Stretch(lbl.gameObject);
        lbl.alignment = TextAlignmentOptions.Center;
    }

    // ── Close button ─────────────────────────────────────────────────────
    static void BuildCloseButton(GameObject parent)
    {
        var closeBtn = NewGO("CloseBtn", parent.transform);
        var cRT      = closeBtn.AddComponent<RectTransform>();
        // Anchor to header area top-right: header goes from 0.95 to 0.76 of screen.
        // Close button anchored at (1, 0.95) — top-right of header zone — pushed in by safe margin.
        cRT.anchorMin        = new Vector2(1f, 0.95f);
        cRT.anchorMax        = new Vector2(1f, 0.95f);
        cRT.pivot            = new Vector2(1f, 1f);
        cRT.anchoredPosition = new Vector2(-14f, 0f);  // 14px from right edge, at header top
        cRT.sizeDelta        = new Vector2(70f, 70f);

        var img    = closeBtn.AddComponent<Image>();
        Sprite spr = Resources.Load<Sprite>("btn_close");
        if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
        else { img.color = new Color(0.7f, 0.15f, 0.15f); }

        var btn = closeBtn.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    // Stretch to fill parent with optional pixel inset on each side
    static void Stretch(GameObject go, float insetH = 0, float insetV = 0)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(insetH, insetV);
        rt.offsetMax = new Vector2(-insetH, -insetV);
    }

    static TextMeshProUGUI MakeTMP(string goName, Transform parent,
        string text, float size, FontStyles style, Color color)
    {
        var go  = NewGO(goName, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        if (_font != null) tmp.font = _font;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
