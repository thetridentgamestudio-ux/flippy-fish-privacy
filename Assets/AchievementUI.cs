using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Achievements panel — viewable from main menu
/// Shows locked/unlocked status, progress bars, reward preview
/// </summary>
public class AchievementUI : MonoBehaviour
{
    private static GameObject panel;

    public static void ShowAchievementsPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        panel = new GameObject("AchievementsPanel");
        panel.transform.SetParent(canvas.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.15f, 0.3f, 0.95f);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Header
        CreateHeader(panel);

        // Achievement list (scrollable)
        CreateAchievementList(panel);

        // Close button
        CreateCloseButton(panel);
    }

    static void CreateHeader(GameObject parent)
    {
        int unlocked = AchievementManager.GetUnlockedCount();
        int total = AchievementManager.GetAchievements().Count;

        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(parent.transform, false);

        TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
        headerText.text = $"ACHIEVEMENTS\n{unlocked} / {total}";
        headerText.alignment = TextAlignmentOptions.TopLeft;
        headerText.fontSize = 24;
        headerText.color = Color.yellow;

        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchoredPosition = new Vector2(0, -40);
        headerRT.sizeDelta = new Vector2(400, 80);
    }

    static void CreateAchievementList(GameObject parent)
    {
        GameObject listGO = new GameObject("AchievementList");
        listGO.transform.SetParent(parent.transform, false);

        ScrollRect scrollRect = listGO.AddComponent<ScrollRect>();
        Image scrollBG = listGO.AddComponent<Image>();
        scrollBG.color = new Color(0, 0, 0, 0);

        RectTransform listRT = listGO.GetComponent<RectTransform>();
        listRT.anchoredPosition = new Vector2(0, -200);
        listRT.sizeDelta = new Vector2(400, 500);

        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(listGO.transform, false);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childForceExpandHeight = false;

        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        var achievements = AchievementManager.GetAchievements();
        contentRT.sizeDelta = new Vector2(400, achievements.Count * 70);

        scrollRect.content = contentRT;

        // Create achievement items
        foreach (var ach in achievements)
        {
            CreateAchievementItem(contentGO, ach);
        }
    }

    static void CreateAchievementItem(GameObject parent, AchievementManager.Achievement ach)
    {
        GameObject itemGO = new GameObject($"Ach_{ach.id}");
        itemGO.transform.SetParent(parent.transform, false);

        Image itemBG = itemGO.AddComponent<Image>();
        itemBG.color = ach.unlocked ? new Color(0.1f, 0.4f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);

        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(360, 65);

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = $"{ach.title} {(ach.unlocked ? "✓" : "")}";
        titleText.fontSize = 14;
        titleText.color = ach.unlocked ? Color.green : Color.white;

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(-170, 15);
        titleRT.sizeDelta = new Vector2(300, 25);

        // Description
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = ach.description;
        descText.fontSize = 10;
        descText.color = new Color(0.8f, 0.8f, 0.8f);

        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchoredPosition = new Vector2(-170, -5);
        descRT.sizeDelta = new Vector2(300, 20);

        // Progress bar
        GameObject progressGO = new GameObject("Progress");
        progressGO.transform.SetParent(itemGO.transform, false);

        Image progressBG = progressGO.AddComponent<Image>();
        progressBG.color = new Color(0.1f, 0.1f, 0.1f);

        RectTransform progressRT = progressGO.GetComponent<RectTransform>();
        progressRT.anchoredPosition = new Vector2(-170, -25);
        progressRT.sizeDelta = new Vector2(280, 10);

        // Progress fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(progressGO.transform, false);
        Image fill = fillGO.AddComponent<Image>();
        fill.color = ach.unlocked ? Color.green : new Color(0.2f, 0.6f, 1f);

        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        float fillPercent = Mathf.Min((float)ach.progress / ach.goal, 1f);
        fillRT.offsetMax = new Vector2(280 * fillPercent, 0);

        // Reward
        GameObject rewardGO = new GameObject("Reward");
        rewardGO.transform.SetParent(itemGO.transform, false);
        TextMeshProUGUI rewardText = rewardGO.AddComponent<TextMeshProUGUI>();
        rewardText.text = $"+{ach.rewardCoins}";
        rewardText.fontSize = 12;
        rewardText.color = Color.yellow;

        RectTransform rewardRT = rewardGO.GetComponent<RectTransform>();
        rewardRT.anchoredPosition = new Vector2(150, 0);
        rewardRT.sizeDelta = new Vector2(70, 30);
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
}