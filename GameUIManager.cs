using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
  

public class GameUIManager : MonoBehaviour
{
    private string username;

    private GameObject usernamePanel;
    private TMP_InputField usernameInput;
    private Button submitButton;

    private GameObject leaderboardPanel;
    private TextMeshProUGUI leaderboardText;

    void Start()
    {
        CreateCanvasIfNeeded();
        CreateUsernameUI();
        CreateLeaderboardUI();
    }

    void CreateCanvasIfNeeded()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
    }

    #region USERNAME UI


void CreateUsernameUI()
{
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    // Dark panel
    usernamePanel = new GameObject("UsernamePanel");
    usernamePanel.transform.SetParent(canvas.transform, false);
    Image bg = usernamePanel.AddComponent<Image>();
    bg.color = new Color(0, 0, 0, 0.6f);
    RectTransform rt = usernamePanel.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;

    // Input Field
    GameObject inputGO = new GameObject("UsernameInput");
    inputGO.transform.SetParent(usernamePanel.transform, false);
    Image inputBg = inputGO.AddComponent<Image>();
    inputBg.color = Color.white;

    usernameInput = inputGO.AddComponent<TMP_InputField>();

    RectTransform inputRT = inputGO.GetComponent<RectTransform>();
    inputRT.sizeDelta = new Vector2(400, 60);
    inputRT.anchorMin = new Vector2(0.5f, 0.5f);
    inputRT.anchorMax = new Vector2(0.5f, 0.5f);
    inputRT.anchoredPosition = new Vector2(0, 80);

    // Text component
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(inputGO.transform, false);
    TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
    text.font = Resources.GetBuiltinResource<TMP_FontAsset>("Arial SDF.asset");
    text.fontSize = 36;
    text.color = Color.black;
    text.alignment = TextAlignmentOptions.Center;
    textGO.AddComponent<CanvasRenderer>();
    RectTransform textRT = textGO.GetComponent<RectTransform>();
    textRT.anchorMin = Vector2.zero;
    textRT.anchorMax = Vector2.one;
    textRT.offsetMin = Vector2.zero;
    textRT.offsetMax = Vector2.zero;
    usernameInput.textComponent = text;

    // Placeholder
    GameObject placeholderGO = new GameObject("Placeholder");
    placeholderGO.transform.SetParent(inputGO.transform, false);
    TextMeshProUGUI placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
    placeholder.font = Resources.GetBuiltinResource<TMP_FontAsset>("Arial SDF.asset");
    placeholder.text = "Enter Username";
    placeholder.fontSize = 36;
    placeholder.color = Color.gray;
    placeholder.alignment = TextAlignmentOptions.Center;
    placeholderGO.AddComponent<CanvasRenderer>();
    RectTransform phRT = placeholderGO.GetComponent<RectTransform>();
    phRT.anchorMin = Vector2.zero;
    phRT.anchorMax = Vector2.one;
    phRT.offsetMin = Vector2.zero;
    phRT.offsetMax = Vector2.zero;
    usernameInput.placeholder = placeholder;

    // Button
    GameObject buttonGO = new GameObject("SubmitButton");
    buttonGO.transform.SetParent(usernamePanel.transform, false);
    submitButton = buttonGO.AddComponent<Button>();
    Image btnImage = buttonGO.AddComponent<Image>();
    btnImage.color = Color.green;
    RectTransform btnRT = buttonGO.GetComponent<RectTransform>();
    btnRT.sizeDelta = new Vector2(200, 60);
    btnRT.anchorMin = new Vector2(0.5f, 0.5f);
    btnRT.anchorMax = new Vector2(0.5f, 0.5f);
    btnRT.anchoredPosition = new Vector2(0, -50);

    // Button Text
    GameObject btnTextGO = new GameObject("Text");
    btnTextGO.transform.SetParent(buttonGO.transform, false);
    TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
    btnText.font = Resources.GetBuiltinResource<TMP_FontAsset>("Arial SDF.asset");
    btnText.text = "Start";
    btnText.fontSize = 36;
    btnText.color = Color.white;
    btnText.alignment = TextAlignmentOptions.Center;
    btnTextGO.AddComponent<CanvasRenderer>();
    RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
    btnTextRT.anchorMin = Vector2.zero;
    btnTextRT.anchorMax = Vector2.one;
    btnTextRT.offsetMin = Vector2.zero;
    btnTextRT.offsetMax = Vector2.zero;

    submitButton.onClick.AddListener(OnSubmitUsername);
}
   void OnSubmitUsername()
{
    username = string.IsNullOrEmpty(usernameInput.text) ? "Player" : usernameInput.text;
    usernamePanel.SetActive(false);
    Debug.Log("Game Started with username: " + username);

    // Resume game if paused
    Time.timeScale = 1f;

    // Trigger GameBootstrap to start
//<GameBootstrap>()?.StartGameFromMenu();
}
    #endregion

    #region LEADERBOARD
    void CreateLeaderboardUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();

        leaderboardPanel = new GameObject("LeaderboardPanel");
        leaderboardPanel.transform.SetParent(canvas.transform, false);

        Image bg = leaderboardPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        RectTransform rt = leaderboardPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        leaderboardPanel.SetActive(false);

        GameObject textGO = new GameObject("LeaderboardText");
        textGO.transform.SetParent(leaderboardPanel.transform, false);

        leaderboardText = textGO.AddComponent<TextMeshProUGUI>();
        leaderboardText.fontSize = 36;
        leaderboardText.color = Color.yellow;
        leaderboardText.alignment = TextAlignmentOptions.Center;

        RectTransform textRT = leaderboardText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    public void ShowLeaderboard(List<(string username, int score)> scores)
{
    
    var sorted = scores.OrderByDescending(x => x.score).ToList();

    string display = "🏆 LEADERBOARD 🏆\n\n";
    foreach (var s in sorted)
    {
        display += $"{s.username} : {s.score}\n";
    }

    leaderboardText.text = display;
    leaderboardPanel.SetActive(true);
}
    #endregion

   public void SubmitScore(int score)
{
    if (string.IsNullOrEmpty(username))
        username = "Player";

    FirebaseGameManager fm = FindObjectOfType<FirebaseGameManager>();
    // if (fm != null)
    // {
    //     fm.GameOver(username, score);
    // }
}

   public void ShowUsernamePanel(Action<string> onSubmitted)
{
    usernamePanel.SetActive(true);

    submitButton.onClick.RemoveAllListeners();
    submitButton.onClick.AddListener(() =>
    {
        string name = string.IsNullOrEmpty(usernameInput.text) ? "Player" : usernameInput.text;
        username = name;
        usernamePanel.SetActive(false);
        onSubmitted?.Invoke(name);
    });

    // Bring panel on top
    Canvas canvas = usernamePanel.GetComponent<Canvas>();
    if (canvas == null)
    {
        canvas = usernamePanel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
    }
}
}