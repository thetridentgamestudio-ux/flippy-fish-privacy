using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI panel showing daily quests on start screen
/// </summary>
public class DailyQuestUI : MonoBehaviour
{
    public static void ShowQuestsPanel(Canvas canvas)
    {
        // Create quest panel - MUCH LARGER NOW
        GameObject questPanelGO = new GameObject("DailyQuestsPanel");
        questPanelGO.transform.SetParent(canvas.transform, false);

        Image bg = questPanelGO.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.3f, 0.5f, 0.95f);

        RectTransform rt = questPanelGO.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;  // Center on screen
        rt.sizeDelta = new Vector2(700, 500);  // Much bigger: 700×500

        // ===== BACKGROUND PANEL (Dark overlay) =====
        GameObject bgPanelGO = new GameObject("BackgroundOverlay");
        bgPanelGO.transform.SetParent(canvas.transform, false);
        bgPanelGO.transform.SetSiblingIndex(questPanelGO.transform.GetSiblingIndex() - 1);  // Behind main panel
        
        Image bgOverlay = bgPanelGO.AddComponent<Image>();
        bgOverlay.color = new Color(0, 0, 0, 0.6f);  // Semi-transparent black
        
        RectTransform bgRT = bgPanelGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Title - BIGGER
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(questPanelGO.transform, false);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "📋 DAILY QUESTS";
        title.alignment = TextAlignmentOptions.Top;
        title.fontSize = 36;  // Much bigger
        title.fontStyle = FontStyles.Bold;
        title.color = Color.yellow;

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0, -40);
        titleRT.sizeDelta = new Vector2(650, 60);

        // Quest items - BIGGER AND CLEARER
        var quests = DailyQuestManager.GetDailyQuests();
        float yOffset = -120;
        int questCount = 0;
        foreach (var quest in quests)
        {
            questCount++;
            
            // Quest title
            GameObject questTitleGO = new GameObject($"Quest{questCount}_Title");
            questTitleGO.transform.SetParent(questPanelGO.transform, false);
            TextMeshProUGUI questTitle = questTitleGO.AddComponent<TextMeshProUGUI>();
            questTitle.text = quest.title;
            questTitle.alignment = TextAlignmentOptions.Left;
            questTitle.fontSize = 22;
            questTitle.fontStyle = FontStyles.Bold;
            questTitle.color = quest.completed ? Color.green : Color.white;

            RectTransform questTitleRT = questTitleGO.GetComponent<RectTransform>();
            questTitleRT.anchoredPosition = new Vector2(-300, yOffset);
            questTitleRT.sizeDelta = new Vector2(600, 40);

            yOffset -= 50;

            // Quest progress
            GameObject questProgressGO = new GameObject($"Quest{questCount}_Progress");
            questProgressGO.transform.SetParent(questPanelGO.transform, false);
            TextMeshProUGUI questProgress = questProgressGO.AddComponent<TextMeshProUGUI>();
            questProgress.text = $"Progress: {quest.currentProgress}/{quest.targetValue}  |  Reward: {quest.rewardCoins} 🪙";
            questProgress.alignment = TextAlignmentOptions.Left;
            questProgress.fontSize = 20;
            questProgress.color = quest.completed ? new Color(0, 1, 0, 0.9f) : new Color(1, 1, 1, 0.8f);

            RectTransform questProgressRT = questProgressGO.GetComponent<RectTransform>();
            questProgressRT.anchoredPosition = new Vector2(-300, yOffset);
            questProgressRT.sizeDelta = new Vector2(600, 35);

            yOffset -= 55;

            // Progress bar background
            GameObject progressBarBGGO = new GameObject($"Quest{questCount}_ProgressBarBG");
            progressBarBGGO.transform.SetParent(questPanelGO.transform, false);
            Image progressBarBG = progressBarBGGO.AddComponent<Image>();
            progressBarBG.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            RectTransform progressBarBGRT = progressBarBGGO.GetComponent<RectTransform>();
            progressBarBGRT.anchoredPosition = new Vector2(0, yOffset);
            progressBarBGRT.sizeDelta = new Vector2(600, 25);

            // Progress bar fill
            float progress = quest.targetValue > 0 ? (float)quest.currentProgress / quest.targetValue : 0;
            GameObject progressBarGO = new GameObject($"Quest{questCount}_ProgressBar");
            progressBarGO.transform.SetParent(progressBarBGGO.transform, false);
            Image progressBar = progressBarGO.AddComponent<Image>();
            progressBar.color = quest.completed ? new Color(0, 1, 0, 0.8f) : new Color(0.2f, 0.55f, 0.9f, 0.8f);

            RectTransform progressBarRT = progressBarGO.GetComponent<RectTransform>();
            progressBarRT.anchoredPosition = new Vector2(-300 + (progress * 300), 0);
            progressBarRT.sizeDelta = new Vector2(progress * 600, 25);

            yOffset -= 40;
        }

        // Close button hint
        GameObject closeGO = new GameObject("CloseHint");
        closeGO.transform.SetParent(questPanelGO.transform, false);
        TextMeshProUGUI closeText = closeGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "Tap QUESTS button again to close";
        closeText.alignment = TextAlignmentOptions.Bottom;
        closeText.fontSize = 16;
        closeText.color = new Color(1, 1, 1, 0.6f);

        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchoredPosition = new Vector2(0, 20);
        closeRT.sizeDelta = new Vector2(650, 40);

        // Add button to close panel
        Button closeBtn = questPanelGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => {
            Destroy(questPanelGO);
            Destroy(bgPanelGO);
        });

        Debug.Log("[🎯 DailyQuests] Large quest panel displayed!");
    }
}