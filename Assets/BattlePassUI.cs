using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Battle Pass UI — shows tiers, XP progress, rewards
/// Accessible from main menu
/// </summary>
public class BattlePassUI : MonoBehaviour
{
    private static GameObject panel;
    private static Canvas mainCanvas;

    public static void ShowBattlePassPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            return;
        }

        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null) return;

        // Create main panel
        panel = new GameObject("BattlePassPanel");
        panel.transform.SetParent(mainCanvas.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.2f, 0.4f, 0.95f);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Header: Season name + XP progress
        CreateHeader(panel);

        // Tier list (scrollable)
        CreateTierList(panel);

        // Close button
        CreateCloseButton(panel);

        // Premium pass button (if not owned)
        if (!BattlePassManager.IsPremium())
            CreatePremiumButton(panel);
    }

    static void CreateHeader(GameObject parent)
    {
        var season = BattlePassManager.GetCurrentSeason();
        int currentTier = BattlePassManager.GetCurrentTier();
        int xp = BattlePassManager.GetPlayerXP();

        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(parent.transform, false);

        TextMeshProUGUI seasonText = headerGO.AddComponent<TextMeshProUGUI>();
        seasonText.text = $"Season {season.seasonNumber}: {season.name}";
        seasonText.alignment = TextAlignmentOptions.TopLeft;
        seasonText.fontSize = 24;
        seasonText.color = Color.yellow;

        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchoredPosition = new Vector2(10, -30);
        headerRT.sizeDelta = new Vector2(300, 50);

        // XP bar
        GameObject xpBarGO = new GameObject("XPBar");
        xpBarGO.transform.SetParent(parent.transform, false);

        Image xpBG = xpBarGO.AddComponent<Image>();
        xpBG.color = new Color(0.1f, 0.1f, 0.1f);

        RectTransform xpRT = xpBarGO.GetComponent<RectTransform>();
        xpRT.anchoredPosition = new Vector2(0, -100);
        xpRT.sizeDelta = new Vector2(400, 30);

        // XP fill
        GameObject xpFillGO = new GameObject("Fill");
        xpFillGO.transform.SetParent(xpBarGO.transform, false);
        Image xpFill = xpFillGO.AddComponent<Image>();
        xpFill.color = new Color(0.2f, 1f, 0.4f);

        RectTransform xpFillRT = xpFillGO.GetComponent<RectTransform>();
        xpFillRT.anchorMin = new Vector2(0, 0);
        xpFillRT.anchorMax = new Vector2(0, 1);
        xpFillRT.offsetMin = Vector2.zero;
        xpFillRT.offsetMax = Vector2.zero;
        float fillPercent = Mathf.Min(xp / 10000f, 1f); // Max 10000 XP = 100%
        xpFillRT.offsetMax = new Vector2(400 * fillPercent, 0);

        // XP text
        GameObject xpTextGO = new GameObject("XPText");
        xpTextGO.transform.SetParent(xpBarGO.transform, false);
        TextMeshProUGUI xpText = xpTextGO.AddComponent<TextMeshProUGUI>();
        xpText.text = $"Tier {currentTier}: {xp} XP";
        xpText.alignment = TextAlignmentOptions.Center;
        xpText.fontSize = 14;
        xpText.color = Color.white;

        RectTransform xpTextRT = xpTextGO.GetComponent<RectTransform>();
        xpTextRT.anchorMin = Vector2.zero;
        xpTextRT.anchorMax = Vector2.one;
        xpTextRT.offsetMin = Vector2.zero;
        xpTextRT.offsetMax = Vector2.zero;
    }

    static void CreateTierList(GameObject parent)
    {
        var season = BattlePassManager.GetCurrentSeason();

        GameObject listGO = new GameObject("TierList");
        listGO.transform.SetParent(parent.transform, false);

        ScrollRect scrollRect = listGO.AddComponent<ScrollRect>();
        Image scrollBG = listGO.AddComponent<Image>();
        scrollBG.color = new Color(0, 0, 0, 0);

        RectTransform listRT = listGO.GetComponent<RectTransform>();
        listRT.anchoredPosition = new Vector2(0, -200);
        listRT.sizeDelta = new Vector2(400, 400);

        // Content area
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(listGO.transform, false);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.childForceExpandHeight = false;

        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        contentRT.sizeDelta = new Vector2(400, season.tiers.Count * 50);

        scrollRect.content = contentRT;

        // Create tier items
        foreach (var tier in season.tiers)
        {
            CreateTierItem(contentGO, tier);
        }
    }

    static void CreateTierItem(GameObject parent, BattlePassManager.BattlePassTier tier)
    {
        GameObject itemGO = new GameObject($"Tier_{tier.tierNumber}");
        itemGO.transform.SetParent(parent.transform, false);

        Image itemBG = itemGO.AddComponent<Image>();
        itemBG.color = tier.unlocked ? new Color(0.1f, 0.5f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);

        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(360, 45);

        // Tier number
        GameObject numGO = new GameObject("Number");
        numGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI numText = numGO.AddComponent<TextMeshProUGUI>();
        numText.text = $"Tier {tier.tierNumber}";
        numText.fontSize = 16;
        numText.color = Color.white;

        RectTransform numRT = numGO.GetComponent<RectTransform>();
        numRT.anchoredPosition = new Vector2(-150, 0);
        numRT.sizeDelta = new Vector2(80, 45);

        // Reward preview
        GameObject rewardGO = new GameObject("Reward");
        rewardGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI rewardText = rewardGO.AddComponent<TextMeshProUGUI>();
        rewardText.text = $"Free: {tier.freeReward.amount} coins";
        rewardText.fontSize = 12;
        rewardText.color = Color.yellow;

        RectTransform rewardRT = rewardGO.GetComponent<RectTransform>();
        rewardRT.anchoredPosition = new Vector2(50, 0);
        rewardRT.sizeDelta = new Vector2(150, 45);

        // Claim button (if unlocked and not claimed)
        if (tier.unlocked && !tier.rewardClaimed)
        {
            GameObject claimGO = new GameObject("ClaimBtn");
            claimGO.transform.SetParent(itemGO.transform, false);

            Image claimImg = claimGO.AddComponent<Image>();
            claimImg.color = new Color(0.2f, 1f, 0.4f);

            Button claimBtn = claimGO.AddComponent<Button>();
            claimBtn.onClick.AddListener(() =>
            {
                BattlePassManager.ClaimReward(tier.tierNumber);
                Destroy(itemGO);
            });

            RectTransform claimRT = claimGO.GetComponent<RectTransform>();
            claimRT.anchoredPosition = new Vector2(150, 0);
            claimRT.sizeDelta = new Vector2(60, 35);

            GameObject claimTextGO = new GameObject("Text");
            claimTextGO.transform.SetParent(claimGO.transform, false);
            TextMeshProUGUI claimText = claimTextGO.AddComponent<TextMeshProUGUI>();
            claimText.text = "CLAIM";
            claimText.fontSize = 10;
            claimText.color = Color.white;

            RectTransform claimTextRT = claimTextGO.GetComponent<RectTransform>();
            claimTextRT.anchorMin = Vector2.zero;
            claimTextRT.anchorMax = Vector2.one;
            claimTextRT.offsetMin = Vector2.zero;
            claimTextRT.offsetMax = Vector2.zero;
        }
    }

    static void CreateCloseButton(GameObject parent)
    {
        GameObject closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(parent.transform, false);

        Image closeBG = closeGO.AddComponent<Image>();
        closeBG.color = new Color(0.8f, 0.2f, 0.2f);

        Button closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => panel.SetActive(false));

        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchoredPosition = new Vector2(0, 30);
        closeRT.sizeDelta = new Vector2(150, 50);

        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "CLOSE";
        closeText.fontSize = 16;
        closeText.color = Color.white;

        RectTransform closeTextRT = closeTextGO.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.offsetMin = Vector2.zero;
        closeTextRT.offsetMax = Vector2.zero;
    }

    static void CreatePremiumButton(GameObject parent)
    {
        GameObject premiumGO = new GameObject("PremiumButton");
        premiumGO.transform.SetParent(parent.transform, false);

        Image premiumBG = premiumGO.AddComponent<Image>();
        premiumBG.color = new Color(1f, 0.85f, 0.2f);

        Button premiumBtn = premiumGO.AddComponent<Button>();
        premiumBtn.onClick.AddListener(() =>
        {
            // Trigger purchase flow (Firebase billing)
            BattlePassManager.PurchasePremiumPass();
            Destroy(premiumGO);
        });

        RectTransform premiumRT = premiumGO.GetComponent<RectTransform>();
        premiumRT.anchoredPosition = new Vector2(0, -420);
        premiumRT.sizeDelta = new Vector2(250, 60);

        GameObject premiumTextGO = new GameObject("Text");
        premiumTextGO.transform.SetParent(premiumGO.transform, false);
        TextMeshProUGUI premiumText = premiumTextGO.AddComponent<TextMeshProUGUI>();
        premiumText.text = "UNLOCK PREMIUM\n$2.99";
        premiumText.alignment = TextAlignmentOptions.Center;
        premiumText.fontSize = 16;
        premiumText.color = Color.black;

        RectTransform premiumTextRT = premiumTextGO.GetComponent<RectTransform>();
        premiumTextRT.anchorMin = Vector2.zero;
        premiumTextRT.anchorMax = Vector2.one;
        premiumTextRT.offsetMin = Vector2.zero;
        premiumTextRT.offsetMax = Vector2.zero;
    }
}