using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD display for active power-ups during gameplay
/// Shows icon + remaining duration
/// </summary>
public class PowerUpHUD : MonoBehaviour
{
    private static GameObject hudContainer;
    private static Canvas gameplayCanvas;

    public static void InitializeHUD(Canvas canvas)
    {
        gameplayCanvas = canvas;
        
        if (hudContainer != null) return;

        hudContainer = new GameObject("PowerUpHUD");
        hudContainer.transform.SetParent(canvas.transform, false);

        RectTransform rt = hudContainer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(200, 100);

        // Subscribe to power-up events
        PowerUpManager.OnPowerUpActivated += ShowPowerUp;
        PowerUpManager.OnPowerUpExpired += HidePowerUp;
    }

    static void ShowPowerUp(PowerUpManager.PowerUp.PowerUpType type)
    {
        if (hudContainer == null) return;

        PowerUpManager.PowerUp def = PowerUpManager.GetPowerUpDef(type);
        if (def == null) return;

        // Create power-up indicator
        GameObject indicator = new GameObject($"Indicator_{type}");
        indicator.transform.SetParent(hudContainer.transform, false);

        // Background circle
        Image bg = indicator.AddComponent<Image>();
        bg.color = def.color;

        RectTransform rt = indicator.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);
        rt.anchoredPosition = new Vector2(-20 - (60 * (int)type), -20);

        // Add duration text
        GameObject timerGO = new GameObject("Timer");
        timerGO.transform.SetParent(indicator.transform, false);

        TextMeshProUGUI timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.text = $"{def.duration:F1}s";
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontSize = 14;
        timerText.color = Color.white;

        RectTransform timerRT = timerGO.GetComponent<RectTransform>();
        timerRT.anchorMin = Vector2.zero;
        timerRT.anchorMax = Vector2.one;
        timerRT.offsetMin = Vector2.zero;
        timerRT.offsetMax = Vector2.zero;

        Debug.Log($"Power-up HUD shown: {def.name}");
    }

    static void HidePowerUp(PowerUpManager.PowerUp.PowerUpType type)
    {
        if (hudContainer == null) return;

        Transform indicator = hudContainer.transform.Find($"Indicator_{type}");
        if (indicator != null)
            Destroy(indicator.gameObject);

        Debug.Log($"Power-up HUD hidden: {type}");
    }

    public static void DestroyHUD()
    {
        if (hudContainer != null)
        {
            PowerUpManager.OnPowerUpActivated -= ShowPowerUp;
            PowerUpManager.OnPowerUpExpired -= HidePowerUp;
            Destroy(hudContainer);
        }
    }
}