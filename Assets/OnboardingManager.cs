using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple onboarding flow to explain progression
/// Shows once on first launch only
/// </summary>
public class OnboardingManager : MonoBehaviour
{
    private static bool hasShownOnboarding = false;
    private const string ONBOARDING_KEY = "OnboardingShown";

    public static void CheckAndShowOnboarding()
    {
        if (hasShownOnboarding) return;
        if (PlayerPrefs.GetInt(ONBOARDING_KEY, 0) == 1) return;

        ShowOnboarding();
        PlayerPrefs.SetInt(ONBOARDING_KEY, 1);
        PlayerPrefs.Save();
    }

    static void ShowOnboarding()
    {
        // Create simple overlay with tips
        GameObject onboardingGO = new GameObject("OnboardingPanel");
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        onboardingGO.transform.SetParent(canvas.transform, false);
        Image bg = onboardingGO.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        RectTransform rt = onboardingGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Add tips text
        GameObject tipsGO = new GameObject("Tips");
        tipsGO.transform.SetParent(onboardingGO.transform, false);
        TextMeshProUGUI tips = tipsGO.AddComponent<TextMeshProUGUI>();
        tips.text = "TAP to jump!\nAvoid pipes!\nCollect coins!\nUnlock skins!\nDaily quests = bonuses!";
        tips.alignment = TextAlignmentOptions.Center;
        tips.fontSize = 20;
        tips.color = Color.white;

        RectTransform tipsRT = tipsGO.GetComponent<RectTransform>();
        tipsRT.anchoredPosition = Vector2.zero;
        tipsRT.sizeDelta = new Vector2(400, 300);

        // Add close button
        GameObject closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(onboardingGO.transform, false);
        Image closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.1f, 0.75f, 0.2f);
        Button closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => Destroy(onboardingGO));

        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchoredPosition = new Vector2(0, -150);
        closeRT.sizeDelta = new Vector2(200, 50);

        // Add button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI btnText = textGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "GOT IT!";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 16;
        btnText.color = Color.white;

        Debug.Log("Onboarding shown");
    }
}