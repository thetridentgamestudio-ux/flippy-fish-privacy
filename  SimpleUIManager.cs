// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections.Generic;
// using UnityEngine.EventSystems;


// public class SimpleUIManager : MonoBehaviour
// {
//     public static SimpleUIManager Instance { get; private set; }
// private TextMeshProUGUI scoreText;
//     private GameObject canvas;
// private GameBootstrap bootstrap;

//     // Username UI
//     private GameObject usernamePanel;
//     private TMP_InputField usernameInput;

//     // Game Over UI
//     private GameObject gameOverPanel;
//     private TextMeshProUGUI gameOverText;

//     // Leaderboard UI
//     private GameObject dimBackground;
//     private GameObject leaderboardPanel;
//     private TMP_FontAsset emojiFont;

//     private string username = "Player";

//     void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }
//     }

//     void Start()
//     {
//         bootstrap = FindObjectOfType<GameBootstrap>();

//         emojiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoColorEmoji SDF");

//         CreateCanvas();
//         CreateDimBackground();
//         CreateUsernamePanel();
//         CreateGameOverPanel();
//         CreateLeaderboardPanel();
//        CreateScoreText(); 
//         ShowUsernamePanel();
//         leaderboardPanel.SetActive(false);
        




//     }

//     // ---------------------------------------------------------
//     // CANVAS
//     // ---------------------------------------------------------
//     void CreateCanvas()
//     {
//         canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
//         Canvas c = canvas.GetComponent<Canvas>();
//         c.renderMode = RenderMode.ScreenSpaceOverlay;

//         CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
//         cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//         cs.referenceResolution = new Vector2(1080, 1920);
//         if (FindObjectOfType<EventSystem>() == null) { var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); DontDestroyOnLoad(es); }
//     }

//     // ---------------------------------------------------------
//     // DIM BACKGROUND (for modal popup)
//     // ---------------------------------------------------------
//     void CreateDimBackground()
//     {
//         dimBackground = new GameObject("DimBackground", typeof(RectTransform), typeof(Image));
//         dimBackground.transform.SetParent(canvas.transform, false);

//         Image img = dimBackground.GetComponent<Image>();
//         img.color = new Color(0, 0, 0, 0.6f);

//         RectTransform rt = dimBackground.GetComponent<RectTransform>();
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = Vector2.zero;
//         rt.offsetMax = Vector2.zero;

//         dimBackground.SetActive(false);
//     }

//     // ---------------------------------------------------------
//     // USERNAME PANEL
//     // ---------------------------------------------------------
//     void CreateUsernamePanel()
//     {
//         usernamePanel = CreateFullScreenPanel("UsernamePanel");

//         usernameInput = CreateInputField(usernamePanel, "Enter Username", new Vector2(0, 80));

//         CreateButton(usernamePanel, "START", new Vector2(0, -80), () =>
//         {
//             username = string.IsNullOrEmpty(usernameInput.text) ? "Player" : usernameInput.text;
//             HideUsernamePanel();
//             StartGame();
//         });
//     }

//     // ---------------------------------------------------------
//     // GAME OVER PANEL
//     // ---------------------------------------------------------
//     void CreateGameOverPanel()
//     {
//         gameOverPanel = CreateCenteredPanel("GameOverPanel", new Vector2(700, 500));
//         gameOverPanel.SetActive(false);

//         gameOverText = CreateText(gameOverPanel, "GAME OVER", new Vector2(0, 80), 48);

//         CreateButton(gameOverPanel, "RESTART", new Vector2(0, -120), () =>
//         {
//             gameOverPanel.SetActive(false);
//             leaderboardPanel.SetActive(false);
//             dimBackground.SetActive(false);
//             StartGame();
//         });
//         CreateButton(gameOverPanel, "LEADERBOARD", new Vector2(0, -20), () =>
// {
//     ShowLeaderboardButtonPressed();
// });

//     }

//     // ---------------------------------------------------------
//     // LEADERBOARD PANEL (CENTERED POPUP)
//     // ---------------------------------------------------------
//     void CreateLeaderboardPanel()
//     {
//         leaderboardPanel = CreateCenteredPanel("LeaderboardPanel", new Vector2(700, 900));
//         var layout = leaderboardPanel.AddComponent<VerticalLayoutGroup>();
//          layout.childAlignment = TextAnchor.UpperCenter; 
//          layout.spacing = 20; layout.childForceExpandHeight = false;
//           layout.childForceExpandWidth = true;

//         leaderboardPanel.SetActive(false);
//     }

//     // ---------------------------------------------------------
//     // HELPERS
//     // ---------------------------------------------------------
//     GameObject CreateFullScreenPanel(string name)
//     {
//         GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
//         panel.transform.SetParent(canvas.transform, false);

//         Image bg = panel.GetComponent<Image>();
//         bg.color = new Color(0, 0, 0, 0.8f);

//         RectTransform rt = panel.GetComponent<RectTransform>();
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = Vector2.zero;
//         rt.offsetMax = Vector2.zero;

//         return panel;
//     }

//     GameObject CreateCenteredPanel(string name, Vector2 size)
//     {
//         GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
//         panel.transform.SetParent(canvas.transform, false);

//         Image bg = panel.GetComponent<Image>();
//         bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

//         RectTransform rt = panel.GetComponent<RectTransform>();
//         rt.anchorMin = new Vector2(0.5f, 0.5f);
//         rt.anchorMax = new Vector2(0.5f, 0.5f);
//         rt.pivot = new Vector2(0.5f, 0.5f);
//         rt.sizeDelta = size;
//         rt.anchoredPosition = Vector2.zero;

//         return panel;
//     }

//     TMP_InputField CreateInputField(GameObject parent, string placeholder, Vector2 pos)
//     {
//         GameObject go = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
//         go.transform.SetParent(parent.transform, false);

//         RectTransform rt = go.GetComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(500, 100);
//         rt.anchoredPosition = pos;

//         Image bg = go.GetComponent<Image>();
//         bg.color = Color.white;

//         // Text
//         var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
//         textGO.transform.SetParent(go.transform, false);

//         TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
//         text.fontSize = 40;
//         text.color = Color.black;
//         text.alignment = TextAlignmentOptions.Center;

//         RectTransform textRT = textGO.GetComponent<RectTransform>();
//         textRT.anchorMin = Vector2.zero;
//         textRT.anchorMax = Vector2.one;
//         textRT.offsetMin = Vector2.zero;
//         textRT.offsetMax = Vector2.zero;

//         // Placeholder
//         var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
//         phGO.transform.SetParent(go.transform, false);

//         TextMeshProUGUI ph = phGO.GetComponent<TextMeshProUGUI>();
//         ph.text = placeholder;
//         ph.fontSize = 40;
//         ph.color = Color.gray;
//         ph.alignment = TextAlignmentOptions.Center;

//         RectTransform phRT = phGO.GetComponent<RectTransform>();
//         phRT.anchorMin = Vector2.zero;
//         phRT.anchorMax = Vector2.one;
//         phRT.offsetMin = Vector2.zero;
//         phRT.offsetMax = Vector2.zero;

//         TMP_InputField input = go.GetComponent<TMP_InputField>();
//         input.textComponent = text;
//         input.placeholder = ph;

//         return input;
//     }

//     TextMeshProUGUI CreateText(GameObject parent, string txt, Vector2 pos, int size)
//     {
//         GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
//         go.transform.SetParent(parent.transform, false);

//         TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
//         text.text = txt;
//         text.fontSize = size;
//         text.alignment = TextAlignmentOptions.Center;
//         text.color = Color.white;

//         RectTransform rt = go.GetComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(600, 100);
//         rt.anchoredPosition = Vector2.zero; // layout group will position it


//         return text;
//     }

//     void CreateButton(GameObject parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
//     {
//         GameObject btnGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
//         btnGO.transform.SetParent(parent.transform, false);

//         RectTransform rt = btnGO.GetComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(300, 100);
//         rt.anchoredPosition = pos;

//         Image img = btnGO.GetComponent<Image>();
//         img.color = new Color(0.2f, 0.6f, 0.2f);

//         Button btn = btnGO.GetComponent<Button>();
//         btn.onClick.AddListener(action);

//         CreateText(btnGO, label, Vector2.zero, 36);
//     }

//     // ---------------------------------------------------------
//     // FLOW
//     // ---------------------------------------------------------
//     public void ShowUsernamePanel()
//     {
//         usernamePanel.SetActive(true);
//     }

//     public void HideUsernamePanel()
//     {
//         usernamePanel.SetActive(false);
//     }

   
//       void StartGame()
// {
//     GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
//     bootstrap?.StartGameFromMenu();
//     if (scoreText != null) 
//     scoreText.gameObject.SetActive(true);
// }

    

//     // ---------------------------------------------------------
//     // GAME OVER + LEADERBOARD
//     // ---------------------------------------------------------
//     public void ShowGameOver(int score)
//     {
//         leaderboardPanel.SetActive(false); 
//         dimBackground.SetActive(false);
//         gameOverText.text = $"GAME OVER\nScore: {score}";
//         scoreText.gameObject.SetActive(false);

//         gameOverPanel.SetActive(true);
//     }

//    public void ShowGameOverWithLeaderboard(List<FirebaseGameManager.ScoreEntry> scores)
// {
//     // Hide Game Over panel
//     scoreText.gameObject.SetActive(false);

//     gameOverPanel.SetActive(false);

//     // Show dim background + leaderboard
//     dimBackground.SetActive(true);
//     leaderboardPanel.SetActive(true);

//     // Clear old entries
//     foreach (Transform child in leaderboardPanel.transform)
//         Destroy(child.gameObject);

//     // Layout fixes
//     var layout = leaderboardPanel.GetComponent<VerticalLayoutGroup>();
//     layout.spacing = 20;
//     layout.childAlignment = TextAnchor.UpperCenter;
//     layout.childForceExpandHeight = false;
//     layout.childForceExpandWidth = true;

//     var fitter = leaderboardPanel.GetComponent<ContentSizeFitter>();
//     fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
//     fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

//     // Title
// var title = CreateText(leaderboardPanel, "🏆 LEADERBOARD 🏆", Vector2.zero, 48);
//     title.font = emojiFont;

//     // Entries
//     for (int i = 0; i < scores.Count; i++)
//         CreateLeaderboardEntry(i + 1, scores[i].username, scores[i].score);
// }



//     void CreateLeaderboardEntry(int rank, string name, int score)
//     {
//         GameObject entry = new GameObject("Entry", typeof(RectTransform), typeof(Image));
//         entry.transform.SetParent(leaderboardPanel.transform, false);

//         Image bg = entry.GetComponent<Image>();
//         bg.sprite = Resources.Load<Sprite>("rounded_box_9slice");
//         bg.type = Image.Type.Sliced;
//         bg.color = new Color(0.1f, 0.2f, 0.35f, 0.85f);

//         RectTransform rt = entry.GetComponent<RectTransform>();
// rt.sizeDelta = new Vector2(0, 120); // width controlled by layout group

//         GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
//         textGO.transform.SetParent(entry.transform, false);

//         TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
//         text.text = $"{rank}. {name} - {score}";
//         text.fontSize = 40;
//         text.color = Color.white;
//         text.alignment = TextAlignmentOptions.Center;
//         text.font = emojiFont;

//         RectTransform textRT = textGO.GetComponent<RectTransform>();
//         textRT.anchorMin = Vector2.zero;
//         textRT.anchorMax = Vector2.one;
//         textRT.offsetMin = new Vector2(20, 0);
//         textRT.offsetMax = new Vector2(-20, 0);
//     }

//     public string GetUsername()
//     {
//         return username;
//     }
//     public void ShowLeaderboardButtonPressed()
// {
//     // Hide Game Over panel
//     gameOverPanel.SetActive(false);

//     // Show dim background
//     dimBackground.SetActive(true);

//     // Ask Firebase to fetch scores
//     FirebaseGameManager.Instance.FetchLeaderboardManually();
// }

// public void ShowStandaloneLeaderboard(List<FirebaseGameManager.ScoreEntry> scores)
// {
//     // Hide Game Over panel
//     scoreText.gameObject.SetActive(false);

//     gameOverPanel.SetActive(false);

//     // Show dim background + leaderboard
//     dimBackground.SetActive(true);
//     leaderboardPanel.SetActive(true);

//     // Clear old entries
//     foreach (Transform child in leaderboardPanel.transform)
//         Destroy(child.gameObject);

//     // Layout settings
//     var layout = leaderboardPanel.GetComponent<VerticalLayoutGroup>();
//     layout.spacing = 20;
//     layout.childAlignment = TextAnchor.UpperCenter;
//     layout.childForceExpandHeight = false;
//     layout.childForceExpandWidth = true;

//     // Title
//     var title = CreateText(leaderboardPanel, "🏆 LEADERBOARD 🏆", new Vector2(0, 0), 48);
//     title.font = emojiFont;

//     // Entries
//     for (int i = 0; i < scores.Count; i++)
//         CreateLeaderboardEntry(i + 1, scores[i].username, scores[i].score);
// }
// void CreateScoreText()
// {
//     GameObject go = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
//     go.transform.SetParent(canvas.transform, false);

//     scoreText = go.GetComponent<TextMeshProUGUI>();
//     scoreText.text = "Score: 0";
//     scoreText.fontSize = 60;
//     scoreText.color = Color.white;
//     scoreText.alignment = TextAlignmentOptions.TopLeft;

//     RectTransform rt = go.GetComponent<RectTransform>();
//     rt.anchorMin = new Vector2(0f, 1f);
//     rt.anchorMax = new Vector2(0f, 1f);
//     rt.pivot = new Vector2(0f, 1f);
//     rt.anchoredPosition = new Vector2(20f, -20f);
//     rt.sizeDelta = new Vector2(600f, 120f);

//     go.SetActive(false); // hidden until gameplay starts
// }
// public void UpdateScore(int score)
// {
//     if (scoreText != null)
//         scoreText.text = "Score: " + score;
// }


// }
