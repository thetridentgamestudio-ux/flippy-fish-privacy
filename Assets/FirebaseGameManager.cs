using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FirebaseGameManager : MonoBehaviour
{
    [SerializeField] TMP_FontAsset monoFont;
    bool isGameOverActive = false;
    GameObject restartButtonObj;
    TextMeshProUGUI scoreTextUI;
    TextMeshProUGUI coinDisplayText;
    GameObject skinsButtonObj; // hidden during gameplay
    private DatabaseReference dbReference;
    public Sprite startButtonSprite;
    bool isRestarting = false;
    private bool isFirebaseReady = false;
    int bestScore;
    int lastScore;
    TextMeshProUGUI usernameText;
    private string playerUsername = "";
    //bool hasRevivedThisRun = false;
    public bool hasRevivedThisRun = false;
    GameObject[] medalObjects = new GameObject[3];
    private float lastInterstitialTime = -999f; // initialized low so first ad always shows
    private const float INTERSTITIAL_COOLDOWN = 90f; // 90s between ads (industry standard for hyper-casual)

    [Header("UI Panels")]
    public GameObject usernamePanel;      // Assign in Inspector
    public TMP_InputField usernameInput;  // Assign in Inspector
    public GameObject leaderboardPanel;   // Assign in Inspector
    public TextMeshProUGUI leaderboardText; // Assign in Inspector
    bool isSoundOn = true;
    Image soundIconImage;
    GameObject soundButtonObj;
TextMeshProUGUI soundIconText;
Canvas mainCanvas;
    [Header("Firebase Settings")]
    [Tooltip("Enter your Firebase Realtime Database URL here")]
    public string databaseURL = "https://flappymobilegame-default-rtdb.firebaseio.com/";

    void Start()
    {
        CreateStartMenuUI();
        CreateLeaderboardUI();
        CreateRestartButton();
        InitializeFirebase();

        int soundPref = PlayerPrefs.GetInt("SOUND", 1);
        isSoundOn = soundPref == 1;
        AudioListener.volume = isSoundOn ? 1f : 0f;

        if (PlayerPrefs.HasKey("USERNAME"))
        {
            playerUsername = PlayerPrefs.GetString("USERNAME");
            // Returning player: hide input field, make panel transparent, show only Play button
            if (usernameInput != null)
                usernameInput.gameObject.SetActive(false);
            UnityEngine.UI.Image panelImg = usernamePanel != null
                ? usernamePanel.GetComponent<UnityEngine.UI.Image>() : null;
            if (panelImg != null) panelImg.color = new Color(0, 0, 0, 0f);
            if (usernamePanel != null) usernamePanel.SetActive(true);
        }
        else
        {
            ShowUsernamePanel(); // new player: show input + Play button
        }
    }
 void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbReference = FirebaseDatabase.GetInstance(app, databaseURL).RootReference;
                isFirebaseReady = true;
            }
            else
            {
                isFirebaseReady = false;
                Debug.LogError("Firebase not ready: " + task.Result);
            }
        });
    }

    #region Username Panel
    void ShowUsernamePanel()
    {
      // s
        if (usernamePanel != null) usernamePanel.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    
    #endregion

    #region Score & Leaderboard
   public void GameOver(int score)
{
    // Legacy path — delegates to OnGameOver to keep single source of truth
    OnGameOver(score);
}
    System.Collections.IEnumerator WaitAndShowLeaderboard(int score)
    {
        while (!isFirebaseReady)
            yield return null;

       //
        GameOver(score);
    }

    IEnumerator WaitThenShowLeaderboard(int score, int topCount)
    {
        float elapsed = 0f;
        while (!isFirebaseReady && elapsed < 10f)
        {
            elapsed += 0.5f;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (isFirebaseReady)
            ShowLeaderboardUI(score, topCount);
        else
        {
            Debug.LogWarning("Firebase timeout — restarting without leaderboard");
            GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
            if (bootstrap != null) bootstrap.RestartGame();
        }
    }

    public void SaveScore(string username, int score)
    {
        if (!isFirebaseReady)
        {
            // Firebase not ready yet — retry once it's ready
            StartCoroutine(SaveScoreWhenReady(username, score));
            return;
        }

        string key = dbReference.Child("leaderboard").Push().Key;
        var data = new Dictionary<string, object>()
        {
            { "username", username },
            { "score", score },
            { "timestamp", System.DateTime.UtcNow.ToString() }
        };

        dbReference.Child("leaderboard").Child(key).SetValueAsync(data).ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
                Debug.LogError("❌ Failed to save score: " + task.Exception);
        });
    }

    IEnumerator SaveScoreWhenReady(string username, int score)
    {
        float elapsed = 0f;
        while (!isFirebaseReady && elapsed < 15f)
        {
            elapsed += 0.5f;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (isFirebaseReady)
            SaveScore(username, score);
        else
            Debug.LogWarning("Firebase never ready — score not saved");
    }

//    
public void ShowLeaderboardUI(int score, int topCount = 10)
{
   GameObject canvas = mainCanvas.gameObject;
    if (!isFirebaseReady)
    {
        Debug.LogWarning("Firebase not ready — waiting then showing leaderboard");
        StartCoroutine(WaitThenShowLeaderboard(score, topCount));
        return;
    }

if(scoreTextUI == null)
{
    GameObject scoreGO = new GameObject("LeaderboardBestScore");
    scoreGO.transform.SetParent(canvas.transform, false);

    scoreTextUI = scoreGO.AddComponent<TextMeshProUGUI>();
    scoreTextUI.fontSize  = 38;
    scoreTextUI.fontStyle = FontStyles.Bold;
    scoreTextUI.alignment = TextAlignmentOptions.Center;
    scoreTextUI.color     = new Color(1f, 0.92f, 0.3f); // warm gold — visible on panel
    scoreTextUI.outlineColor = new Color(0.3f, 0.15f, 0f, 1f);
    scoreTextUI.outlineWidth = 0.2f;

    RectTransform rt = scoreGO.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(0.5f, 1f);
    rt.anchorMax        = new Vector2(0.5f, 1f);
    rt.pivot            = new Vector2(0.5f, 1f);
    rt.sizeDelta        = new Vector2(Screen.width * 0.80f, Screen.height * 0.06f);
    rt.anchoredPosition = new Vector2(0f, -Screen.height * 0.28f);
}

scoreTextUI.text = "Score: " + score + "    Best: " + bestScore;
scoreTextUI.gameObject.SetActive(true);
   //

    dbReference.Child("leaderboard")
        .OrderByChild("score")
        .LimitToLast(topCount)
        .GetValueAsync()
        .ContinueWithOnMainThread(task =>
    {
        if (task.IsFaulted)
        {
            Debug.LogError("Failed to fetch leaderboard");
            return;
        }

        DataSnapshot snapshot = task.Result;

        List<(string username, int score)> scores = new List<(string, int)>();

        foreach (var child in snapshot.Children)
        {
            string user = child.Child("username").Value?.ToString() ?? "Player";

            int s = 0;
            int.TryParse(child.Child("score").Value?.ToString(), out s);

            scores.Add((user, s));
        }

        scores = scores.OrderByDescending(x => x.score).ToList();

        System.Text.StringBuilder display = new System.Text.StringBuilder();

        // Column positions scale with panel width
        // Panel width = Screen.width * 0.88, text has 70px left margin
        // So usable text width ≈ Screen.width * 0.88 - 140
        int colName  = Mathf.RoundToInt(Screen.width * 0.12f); // name column
        int colScore = Mathf.RoundToInt(Screen.width * 0.62f); // score column
        int maxChars = 11; // max username chars before truncation

        string[] topLabels = { "  ", "  ", "  " }; // blank — medal image sits here

        for (int i = 0; i < scores.Count; i++)
        {
            string rank = i < 3 ? topLabels[i] : $"{i + 1}.";
            string name = scores[i].username.Length > maxChars
                          ? scores[i].username.Substring(0, maxChars)
                          : scores[i].username;
            string sc   = scores[i].score.ToString();

            bool isMe = scores[i].username == playerUsername;
            string line = $"<pos=0>{rank}<pos={colName}>{name}<pos={colScore}>{sc}";

            if (isMe)
                display.Append($"<color=#FFE234><b>{line}</b></color>\n");
            else
                display.Append($"{line}\n");
        }

        int playerRank = scores.FindIndex(x => x.username == playerUsername) + 1;
        if (playerRank > 0)
            display.Append($"\n<color=#FFE234><b><pos={colName}>Your Rank: #{playerRank}</b></color>");























        // APPLY TEXT
     StartCoroutine(ShowLeaderboardNextFrame(display.ToString(), scores.Count));
    });
    
}
    void CreateStartMenuUI()
{
    // Canvas
    GameObject canvasGO = new GameObject("Canvas");
   mainCanvas = canvasGO.AddComponent<Canvas>();
   mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

    // MOBILE SCALING FIX
    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1080, 1920); // standard mobile resolution
    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    scaler.matchWidthOrHeight = 0.5f;

    canvasGO.AddComponent<GraphicRaycaster>();

    // Panel
    usernamePanel = new GameObject("UsernamePanel");
    usernamePanel.transform.SetParent(canvasGO.transform);

    RectTransform panelRect = usernamePanel.AddComponent<RectTransform>();
    panelRect.sizeDelta = new Vector2(600, 400);
    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
    panelRect.pivot = new Vector2(0.5f, 0.5f);
    panelRect.anchoredPosition = Vector2.zero;

    UnityEngine.UI.Image panelImage = usernamePanel.AddComponent<UnityEngine.UI.Image>();
    panelImage.color = new Color(0,0,0,0.7f);

    // Input Field
    GameObject inputGO = new GameObject("UsernameInput");
    inputGO.transform.SetParent(usernamePanel.transform);

    RectTransform inputRect = inputGO.AddComponent<RectTransform>();
    inputRect.sizeDelta = new Vector2(400, 80);
    inputRect.anchoredPosition = new Vector2(0, 50);

    inputGO.AddComponent<UnityEngine.UI.Image>().color = Color.white;

    usernameInput = inputGO.AddComponent<TMP_InputField>();

    // Text inside input
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(inputGO.transform);

    TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
    text.text = "";
    text.fontSize = 36;
    text.alignment = TextAlignmentOptions.Center;
    text.color = Color.black;   // ← ADD THIS

    RectTransform textRect = textGO.GetComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.offsetMin = Vector2.zero;
    textRect.offsetMax = Vector2.zero;

    usernameInput.textComponent = text;

    // Placeholder
    GameObject placeholderGO = new GameObject("Placeholder");
    placeholderGO.transform.SetParent(inputGO.transform);

    TextMeshProUGUI placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
    placeholder.text = "Enter Username";
    placeholder.fontSize = 36;
    placeholder.color = Color.gray;
    placeholder.alignment = TextAlignmentOptions.Center;

    RectTransform phRect = placeholderGO.GetComponent<RectTransform>();
    phRect.anchorMin = Vector2.zero;
    phRect.anchorMax = Vector2.one;
    phRect.offsetMin = Vector2.zero;
    phRect.offsetMax = Vector2.zero;

    usernameInput.placeholder = placeholder;

    // Enforce max 12 chars at input level — keyboard won't allow more
    usernameInput.characterLimit = 12;
    // Allow alphanumeric only on supported keyboards
    usernameInput.contentType = TMP_InputField.ContentType.Alphanumeric;

    // Start Button
  GameObject buttonGO = new GameObject("StartButton");
buttonGO.transform.SetParent(usernamePanel.transform);

RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
buttonRect.sizeDelta = new Vector2(420, 150);
buttonRect.anchoredPosition = new Vector2(0, -140);

// Image
Image buttonImage = buttonGO.AddComponent<Image>();

Sprite startSprite = Resources.Load<Sprite>("StartButton");

if(startSprite != null)
{
    buttonImage.sprite = startSprite;
    buttonImage.preserveAspect = true;
}
else
{
    Debug.LogError("StartButton sprite not found in Resources folder!");
}

buttonImage.type = Image.Type.Sliced;

// Button component
Button startButton = buttonGO.AddComponent<Button>();
startButton.onClick.AddListener(OnSubmitUsername);

    // Button Text
    GameObject buttonTextGO = new GameObject("Text");
    buttonTextGO.transform.SetParent(buttonGO.transform);

    // ── SKINS BUTTON (bottom-left, screen-relative sizing) ───────────
    float btnW = Screen.width  * 0.28f;
    float btnH = Screen.height * 0.07f;
    float btnMargin = Screen.width * 0.03f;

    GameObject skinsBtn = new GameObject("SkinsButton");
    skinsButtonObj = skinsBtn; // store so we can hide during gameplay
    skinsBtn.transform.SetParent(mainCanvas.transform, false);
    Image skinsBtnImg = skinsBtn.AddComponent<Image>();
    skinsBtnImg.color = new Color(0.08f, 0.22f, 0.42f, 0.92f);
    Button skinsBtnComp = skinsBtn.AddComponent<Button>();
    skinsBtnComp.onClick.AddListener(() => SkinSelectUI.Show());
    RectTransform skinsRT = skinsBtn.GetComponent<RectTransform>();
    skinsRT.anchorMin        = new Vector2(0f, 0f);
    skinsRT.anchorMax        = new Vector2(0f, 0f);
    skinsRT.pivot            = new Vector2(0f, 0f);
    skinsRT.sizeDelta        = new Vector2(btnW, btnH);
    skinsRT.anchoredPosition = new Vector2(btnMargin, btnMargin);

    GameObject skinsTxtGO = new GameObject("SkinsLabel");
    skinsTxtGO.transform.SetParent(skinsBtn.transform, false);
    TextMeshProUGUI skinsTxt = skinsTxtGO.AddComponent<TextMeshProUGUI>();
    skinsTxt.text      = "SKINS";
    skinsTxt.fontSize  = Screen.height * 0.022f;
    skinsTxt.fontStyle = FontStyles.Bold;
    skinsTxt.color     = Color.white;
    skinsTxt.alignment = TextAlignmentOptions.Center;
    RectTransform skinsTxtRT = skinsTxt.GetComponent<RectTransform>();
    skinsTxtRT.anchorMin = Vector2.zero;
    skinsTxtRT.anchorMax = Vector2.one;
    skinsTxtRT.offsetMin = skinsTxtRT.offsetMax = Vector2.zero;

    // ── COIN DISPLAY (bottom-right, screen-relative sizing) ──────────
    GameObject coinDisp = new GameObject("CoinDisplay");
    coinDisp.transform.SetParent(mainCanvas.transform, false);
    Image coinDispImg = coinDisp.AddComponent<Image>();
    coinDispImg.color = new Color(0.12f, 0.10f, 0.02f, 0.88f);
    RectTransform coinRT = coinDisp.GetComponent<RectTransform>();
    coinRT.anchorMin        = new Vector2(1f, 0f);
    coinRT.anchorMax        = new Vector2(1f, 0f);
    coinRT.pivot            = new Vector2(1f, 0f);
    coinRT.sizeDelta        = new Vector2(btnW, btnH);
    coinRT.anchoredPosition = new Vector2(-btnMargin, btnMargin);

    // Coin icon
    float iconSize = btnH * 0.6f;
    GameObject coinIconGO = new GameObject("CoinIcon");
    coinIconGO.transform.SetParent(coinDisp.transform, false);
    Image coinIconImg = coinIconGO.AddComponent<Image>();
    Sprite coinSpr = Resources.Load<Sprite>("Coin");
    if (coinSpr != null) coinIconImg.sprite = coinSpr;
    else coinIconImg.color = new Color(1f, 0.85f, 0.1f);
    RectTransform coinIconRT = coinIconGO.GetComponent<RectTransform>();
    coinIconRT.anchorMin        = new Vector2(0f, 0.5f);
    coinIconRT.anchorMax        = new Vector2(0f, 0.5f);
    coinIconRT.pivot            = new Vector2(0f, 0.5f);
    coinIconRT.sizeDelta        = new Vector2(iconSize, iconSize);
    coinIconRT.anchoredPosition = new Vector2(btnH * 0.15f, 0f);

    // Coin count text
    GameObject coinTxtGO = new GameObject("CoinLabel");
    coinTxtGO.transform.SetParent(coinDisp.transform, false);
    TextMeshProUGUI coinTxt = coinTxtGO.AddComponent<TextMeshProUGUI>();
    coinTxt.fontSize  = Screen.height * 0.022f;
    coinTxt.fontStyle = FontStyles.Bold;
    coinTxt.color     = new Color(1f, 0.88f, 0.1f);
    coinTxt.alignment = TextAlignmentOptions.Left;
    RectTransform coinTxtRT = coinTxt.GetComponent<RectTransform>();
    coinTxtRT.anchorMin = new Vector2(0f, 0f);
    coinTxtRT.anchorMax = new Vector2(1f, 1f);
    coinTxtRT.offsetMin = new Vector2(iconSize + btnH * 0.2f, 0f);
    coinTxtRT.offsetMax = Vector2.zero;

    coinTxt.text = SkinManager.GetCoins().ToString();
    coinDisplayText = coinTxt; // store so RefreshCoinDisplay() can update it

    CreateSoundButton(usernamePanel);
    // Sound button stays visible throughout menu AND gameplay
    if (soundButtonObj != null)
        soundButtonObj.SetActive(true);
}

public void OnSubmitUsername()
{
    string username = "";

    // If input field is visible → read from it
    if (usernameInput != null && usernameInput.gameObject.activeSelf)
    {
        username = usernameInput.text.Trim();

        TextMeshProUGUI placeholder = usernameInput.placeholder.GetComponent<TextMeshProUGUI>();

        // ── Validation ────────────────────────────────────────────
        if (string.IsNullOrEmpty(username))
        {
            // Guest play — assign random name, don't block
            username = "Fish" + UnityEngine.Random.Range(1000, 9999);
        }
        else if (username.Length < 3)
        {
            placeholder.text = "Min 3 chars (or leave blank)";
            usernameInput.text = "";
            return;
        }

        if (username.Length > 12)
        {
            // Auto-trim rather than reject — better UX
            username = username.Substring(0, 12);
        }

        // Only allow letters, numbers and underscore
        // Reject spaces and special characters that break Firebase paths
        System.Text.RegularExpressions.Regex validChars =
            new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_]+$");
        if (!validChars.IsMatch(username))
        {
            placeholder.text = "Letters, numbers, _ only!";
            usernameInput.text = "";
            return;
        }
        // ── End validation ────────────────────────────────────────

        playerUsername = username;
        GameBootstrap.Instance.SaveUsername(username);
        GameBootstrap.Instance.RefreshUsernameText();
    }
    else
    {
        // Username already exists
        playerUsername = GameBootstrap.Instance.GetUsername();
    }

    if (usernamePanel != null)
        usernamePanel.SetActive(false);

    if (GameBootstrap.Instance != null)
        GameBootstrap.Instance.StartGame();
}
public void SetUsername(string username)
{
    playerUsername = username;
}
    #endregion
   void CreateLeaderboardUI()
{
    GameObject canvas = mainCanvas.gameObject;

    // PANEL
    leaderboardPanel = new GameObject("LeaderboardPanel");
    leaderboardPanel.transform.SetParent(canvas.transform);
    GameObject titleGO = new GameObject("LeaderboardTitle");
titleGO.transform.SetParent(leaderboardPanel.transform);

TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();

titleText.text = "*** LEADERBOARD ***";
titleText.fontSize = Screen.height * 0.028f;
titleText.fontStyle = FontStyles.Bold;
titleText.alignment = TextAlignmentOptions.Center;
titleText.color = new Color(1f, 0.9f, 0.2f); // gold

RectTransform titleRect = titleGO.GetComponent<RectTransform>();
titleRect.anchorMin = new Vector2(0.5f, 1f);
titleRect.anchorMax = new Vector2(0.5f, 1f);
titleRect.pivot = new Vector2(0.5f, 1f);
titleRect.anchoredPosition = new Vector2(0, 15);
titleRect.sizeDelta = new Vector2(Screen.width * 0.80f, Screen.height * 0.07f);

    RectTransform panelRect = leaderboardPanel.AddComponent<RectTransform>();
    panelRect.sizeDelta = new Vector2(Screen.width * 0.88f, Screen.height * 0.60f);
    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
    panelRect.pivot = new Vector2(0.5f, 0.5f);
panelRect.anchoredPosition = new Vector2(0, Screen.height * 0.05f);

    Image panelImage = leaderboardPanel.AddComponent<Image>();
    //panelImage.color = new Color(0,0,0,0.85f);
    Sprite panelSprite = Resources.Load<Sprite>("LeaderboardPanel");

if(panelSprite != null)
{
    panelImage.sprite = panelSprite;
    panelImage.type = Image.Type.Sliced;
   panelImage.color = new Color(1f, 1f, 1f, 0.82f);
}
else
{
    Debug.LogError("LeaderboardPanel not found in Resources!");
}

    // TEXT OBJECT
    GameObject textGO = new GameObject("LeaderboardText");
    textGO.transform.SetParent(leaderboardPanel.transform);

    leaderboardText = textGO.AddComponent<TextMeshProUGUI>();

    // LOAD MONO FONT
    TMP_FontAsset monoFont = Resources.Load<TMP_FontAsset>("Fonts/JetBrainsMono-Regular SDF");

    if(monoFont != null)
        leaderboardText.font = monoFont;
    else
        Debug.LogError("Mono font not found in Resources!");

    leaderboardText.fontSize = Screen.height * 0.022f; // scales to screen — ~51px on 2340
    leaderboardText.color = Color.white;
    leaderboardText.alignment = TextAlignmentOptions.TopLeft;

    leaderboardText.textWrappingMode = TextWrappingModes.NoWrap;
    leaderboardText.overflowMode = TextOverflowModes.Overflow;

    float panelH   = Screen.height * 0.60f;
    float fontSize = Screen.height * 0.022f;
    float lineH    = fontSize * 1.32f; // TMP default line spacing ~1.32x font size

    RectTransform textRect = leaderboardText.GetComponent<RectTransform>();
    textRect.anchorMin = new Vector2(0,0);
    textRect.anchorMax = new Vector2(1,1);
    textRect.offsetMin = new Vector2(70, 120);
    textRect.offsetMax = new Vector2(-70, -140);

    // Top of text area in panel local coords (panel centre = 0)
    // offsetMax.y = -140 means text starts 140px below top of panel
    float textAreaTop = panelH * 0.5f - 140f;
    // First row centre = textAreaTop minus half a line
    float firstRowY = textAreaTop - lineH * 0.5f;

    // =====================================================
    // MEDAL IMAGES for top 3 rows — aligned to text rows
    // =====================================================
    string[] medalNames = { "Medal_1", "Medal_2", "Medal_3" };
    float medalSize = lineH * 0.85f;
    // Medal X: panel left edge + 70px text margin + half medal width
    // This puts medal centre aligned with the rank column (pos=0)
    float panelW    = Screen.width * 0.88f;
    float textLeft  = -panelW * 0.5f + 70f; // left edge of text area
    float medalX    = textLeft + medalSize * 0.5f;

    for (int m = 0; m < 3; m++)
    {
        Sprite medalSprite = Resources.Load<Sprite>(medalNames[m]);
        if (medalSprite == null)
        {
            Debug.LogWarning($"Medal sprite not found: {medalNames[m]}");
            continue;
        }

        GameObject medalGO = new GameObject($"Medal_{m + 1}");
        medalGO.transform.SetParent(leaderboardPanel.transform, false);

        Image medalImg = medalGO.AddComponent<Image>();
        medalImg.sprite  = medalSprite;
        medalImg.preserveAspect = true;

        RectTransform medalRect = medalGO.GetComponent<RectTransform>();
        medalRect.anchorMin        = new Vector2(0.5f, 0.5f);
        medalRect.anchorMax        = new Vector2(0.5f, 0.5f);
        medalRect.pivot            = new Vector2(0.5f, 0.5f);
        medalRect.sizeDelta        = new Vector2(medalSize, medalSize);
        medalRect.anchoredPosition = new Vector2(medalX, firstRowY - m * lineH);

        medalObjects[m] = medalGO; // store reference
    }

    leaderboardPanel.SetActive(false);
}
void CreateSoundButton(GameObject parent)
{
    soundButtonObj = new GameObject("SoundButton");
    soundButtonObj.transform.SetParent(mainCanvas.transform, false);

    // Rounded ocean-dark background matching username badge
    Image img = soundButtonObj.AddComponent<Image>();
    Sprite rounded = GetRoundedSprite();
    if (rounded != null) { img.sprite = rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.35f; }
    img.color = new Color(0.04f, 0.18f, 0.32f, 0.88f); // same navy as username badge

    RectTransform rt = soundButtonObj.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(1f, 1f);
    rt.anchorMax        = new Vector2(1f, 1f);
    rt.pivot            = new Vector2(1f, 1f);
    rt.sizeDelta        = new Vector2(72, 72);
    rt.anchoredPosition = new Vector2(-16, -100); // just below username badge

    Button btn = soundButtonObj.AddComponent<Button>();
    btn.onClick.AddListener(ToggleSound);
    ColorBlock cb = btn.colors;
    cb.highlightedColor = new Color(0.1f, 0.35f, 0.55f, 1f);
    cb.pressedColor     = new Color(0.02f, 0.1f, 0.2f, 1f);
    btn.colors = cb;

    // Icon
    GameObject iconGO = new GameObject("Icon");
    iconGO.transform.SetParent(soundButtonObj.transform, false);

    RectTransform iconRT = iconGO.AddComponent<RectTransform>();
    iconRT.anchorMin = new Vector2(0.15f, 0.15f);
    iconRT.anchorMax = new Vector2(0.85f, 0.85f);
    iconRT.offsetMin = Vector2.zero;
    iconRT.offsetMax = Vector2.zero;

    Image icon = iconGO.AddComponent<Image>();
    icon.preserveAspect = true;
    soundIconImage = icon;

    Sprite soundOnSprite  = Resources.Load<Sprite>("SoundOn");
    Sprite soundOffSprite = Resources.Load<Sprite>("SoundOff");
    icon.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
}
void ToggleSound()
{
    isSoundOn = !isSoundOn;

    AudioListener.volume = isSoundOn ? 1f : 0f;

    PlayerPrefs.SetInt("SOUND", isSoundOn ? 1 : 0);

    if (soundIconImage != null)
    {
        soundIconImage.sprite = isSoundOn
            ? Resources.Load<Sprite>("SoundOn")
            : Resources.Load<Sprite>("SoundOff");
    }
}

void UpdateSoundIcon()
{
    if (soundIconText == null) return;

    soundIconText.text = isSoundOn ? "🔊" : "🔇";
}
// void CreateRestartButton()
// {
    
//       if(isRestarting) return;
//

//     GameObject canvas = GameObject.Find("Canvas");

//     GameObject buttonGO = new GameObject("RestartButton");
//     buttonGO.transform.SetParent(canvas.transform);

//     RectTransform rect = buttonGO.AddComponent<RectTransform>();

//     rect.sizeDelta = new Vector2(320,120);
//     rect.anchorMin = new Vector2(0.5f,0.5f);
//     rect.anchorMax = new Vector2(0.5f,0.5f);
//     rect.pivot = new Vector2(0.5f,0.5f);
//     rect.anchoredPosition = new Vector2(0,-500);

//     Image buttonImage = buttonGO.AddComponent<Image>();

//     Sprite restartSprite = Resources.Load<Sprite>("Restart");

//     if(restartSprite != null)
//     {
//         buttonImage.sprite = restartSprite;
//         buttonImage.preserveAspect = true;
//     }

//     Button restartButton = buttonGO.AddComponent<Button>();
//     restartButton.onClick.AddListener(RestartGame);
// }
void CreateRestartButton()
{
    if (restartButtonObj != null)
    {
        return;
    }

    GameObject canvas = mainCanvas.gameObject;

    restartButtonObj = new GameObject("RestartButton");
    GameObject buttonGO = restartButtonObj;

    buttonGO.transform.SetParent(canvas.transform);

    RectTransform rect = buttonGO.AddComponent<RectTransform>();
    rect.sizeDelta = new Vector2(320,120);
    rect.anchorMin = new Vector2(0.5f,0.5f);
    rect.anchorMax = new Vector2(0.5f,0.5f);
    rect.pivot = new Vector2(0.5f,0.5f);
    rect.anchoredPosition = new Vector2(0,-720);

    Image buttonImage = buttonGO.AddComponent<Image>();

    Sprite restartSprite = Resources.Load<Sprite>("Restart");

    if(restartSprite != null)
    {
        buttonImage.sprite = restartSprite;
        buttonImage.preserveAspect = true;
    }

    Button restartButton = buttonGO.AddComponent<Button>();
    restartButton.onClick.AddListener(() =>
    {
        // Hide leaderboard UI
        if (restartButtonObj  != null) restartButtonObj.SetActive(false);
        if (leaderboardPanel  != null) leaderboardPanel.SetActive(false);
        if (scoreTextUI       != null) scoreTextUI.gameObject.SetActive(false);

        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        if (bootstrap == null) return;

        // If came from revive path — no ad was shown before leaderboard, show one now
        // If came from normal restart path — ad already shown before leaderboard, go straight to game
        bool cooldownExpired = (Time.realtimeSinceStartup - lastInterstitialTime) >= INTERSTITIAL_COOLDOWN;

        if (hasRevivedThisRun && AdManager.Instance != null && cooldownExpired)
        {
            lastInterstitialTime = Time.realtimeSinceStartup;
            AdManager.Instance.ShowInterstitial(() => bootstrap.RestartGame());
        }
        else
        {
            bootstrap.RestartGame();
        }
    });
    restartButtonObj.SetActive(false);
}
// Called by GameBootstrap panel restart button — ensures interstitial always plays
// Called by GameBootstrap after every pipe passed to update coin counter
// Called by GameBootstrap to hide skins button during gameplay
public void UpdateLastAdTime()
{
    lastInterstitialTime = Time.realtimeSinceStartup;
}

public void SetSkinsButtonVisible(bool visible)
{
    if (skinsButtonObj != null)
        skinsButtonObj.SetActive(visible);
}

public void RefreshCoinDisplay()
{
    if (coinDisplayText != null)
        coinDisplayText.text = SkinManager.GetCoins().ToString();
}

public void TriggerRestartWithAd()
{
    Time.timeScale = 1f;
    hasRevivedThisRun = false;

    bool cooldownExpired = (Time.realtimeSinceStartup - lastInterstitialTime) >= INTERSTITIAL_COOLDOWN;

    if (AdManager.Instance != null && cooldownExpired)
    {
        lastInterstitialTime = Time.realtimeSinceStartup;
        AdManager.Instance.ShowInterstitial(ContinueRestart);
    }
    else
        ContinueRestart();
}

// NOTE: This private method is only called by TriggerRestartWithAd.
// The leaderboard restart button calls bootstrap.RestartGame() directly.
void RestartGameWithAd()
{
    hasRevivedThisRun = false;

    if (AdManager.Instance != null)
        AdManager.Instance.ShowInterstitial(ContinueRestart);
    else
        ContinueRestart();
}
IEnumerator RestartDelay(GameBootstrap bootstrap)
{
    yield return new WaitForSecondsRealtime(1f);

    if(bootstrap != null)
    {
        bootstrap.StartGame();
    }
}
IEnumerator ShowLeaderboardNextFrame(string text, int scoreCount = 10)
{
    yield return null;

    leaderboardText.text = text;

    // Show/hide medals based on actual number of scores
    for (int m = 0; m < 3; m++)
    {
        if (medalObjects[m] != null)
            medalObjects[m].SetActive(m < scoreCount);
    }

    if (leaderboardPanel != null)
    {
        leaderboardPanel.SetActive(true);
        leaderboardPanel.transform.SetAsLastSibling();
    }

    if (scoreTextUI != null) scoreTextUI.gameObject.SetActive(false);

    // Hide skins button during leaderboard
    if (skinsButtonObj != null) skinsButtonObj.SetActive(false);

    GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
    if (bootstrap != null)
    {
        if (bootstrap.gameOverPanel != null)
        {
            bootstrap.gameOverPanel.SetActive(false);
            CanvasGroup cg = bootstrap.gameOverPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
        }
        if (bootstrap.scoreText != null) bootstrap.scoreText.gameObject.SetActive(false);
        bootstrap.HideGameOverUI();
        if (bootstrap.restartButton != null) bootstrap.restartButton.SetActive(false);
        if (bootstrap.reviveButton  != null) bootstrap.reviveButton.SetActive(false);

        // Hide player fish during leaderboard
        if (bootstrap.player != null) bootstrap.player.SetActive(false);

        // Hide pipes during leaderboard
        foreach (var moving in UnityEngine.Object.FindObjectsOfType<Moving>())
            moving.gameObject.SetActive(false);
    }

    if (restartButtonObj != null)
    {
        restartButtonObj.SetActive(true);
        restartButtonObj.transform.SetAsLastSibling();
    }
}

// void HandleReviveResult()
// {
//     if (GameBootstrap.Instance == null) return;

//     if (GameBootstrap.Instance.wasRevived)
//     {
//

//         hasRevivedThisRun = true;

//         GameBootstrap.Instance.RevivePlayer();
//     }
//     else
//     {
//

//         ShowLeaderboardUI(lastScore);
//     }
// }
public void ShowFinalLeaderboard()
{
    // Uses lastScore set by OnGameOver — no double save needed
    ShowLeaderboardUI(lastScore);
}
public string GetUsername()
{
    return playerUsername;
}

public int GetLastScore()
{
    return lastScore;
}
public void OnGameOver(int score)
{
    lastScore = score;

    bestScore = PlayerPrefs.GetInt("BestScore", 0);

    if (score > bestScore)
    {
        bestScore = score;
        PlayerPrefs.SetInt("BestScore", bestScore);
    }

    // Lazy username collection — prompt only once, only when they have a real score
    if (score > 0 && !PlayerPrefs.HasKey("USERNAME"))
    {
        ShowLazyUsernamePrompt(score);
        return; // SaveScore called after they submit/skip
    }

    if (score > 0) SaveScore(playerUsername, score);

    // ✅ DO NOT show leaderboard here anymore.
    // Game over panel shows first. Then:
    // - If REVIVE: player revives and plays again
    // - If RESTART: goes through ContinueRestart which shows leaderboard
    // This prevents flash/overlap screens.
}
void ContinueRestart()
{
    // Always hide all Firebase-owned UI
    if (restartButtonObj != null) restartButtonObj.SetActive(false);
    if (leaderboardPanel  != null) leaderboardPanel.SetActive(false);
    if (scoreTextUI       != null) scoreTextUI.gameObject.SetActive(false);

    GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
    if (bootstrap == null) { Debug.LogError("GameBootstrap NOT FOUND"); return; }

    if (bootstrap.restartButton != null) bootstrap.restartButton.SetActive(false);
    if (bootstrap.reviveButton  != null) bootstrap.reviveButton.SetActive(false);
    bootstrap.HideGameOverUI();

    int scoreToShow = lastScore;
    lastScore = 0; // reset so second call skips leaderboard

    if (scoreToShow > 0)
        ShowLeaderboardUI(scoreToShow);  // leaderboard shows, its restart button starts the game
    else
        bootstrap.RestartGame();         // no score → go straight to game
}

// Android-safe sprite helpers (GetBuiltinResource fails on device)
Sprite GetRoundedSprite()
{
    return null; // flat color on device — no crash
}
Sprite GetCircleSprite()
{
    return null;
}

// Lazy username prompt shown after first real game-over
// Framed as "Save your score!" not a forced gate
void ShowLazyUsernamePrompt(int score)
{
    GameObject canvas = mainCanvas.gameObject;

    GameObject overlay = new GameObject("LazyUsernameOverlay");
    overlay.transform.SetParent(canvas.transform, false);

    Image bg = overlay.AddComponent<Image>();
    bg.color = new Color(0f, 0f, 0f, 0.78f);
    RectTransform bgRT = overlay.GetComponent<RectTransform>();
    bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
    bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

    // Card
    GameObject card = new GameObject("Card");
    card.transform.SetParent(overlay.transform, false);
    Image cardImg = card.AddComponent<Image>();
    cardImg.color = new Color(0.04f, 0.08f, 0.18f, 0.97f);
    RectTransform cardRT = card.GetComponent<RectTransform>();
    cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
    cardRT.sizeDelta = new Vector2(500, 380);

    // Title
    GameObject titleGO = new GameObject("Title");
    titleGO.transform.SetParent(card.transform, false);
    TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
    title.text = "Save your score!";
    title.fontSize = 44; title.fontStyle = FontStyles.Bold;
    title.alignment = TextAlignmentOptions.Center;
    title.color = new Color(1f, 0.88f, 0.2f);
    RectTransform titleRT = title.GetComponent<RectTransform>();
    titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
    titleRT.pivot = new Vector2(0.5f, 1f);
    titleRT.anchoredPosition = new Vector2(0, -24); titleRT.sizeDelta = new Vector2(0, 60);

    // Score display
    GameObject scoreGO = new GameObject("ScoreDisplay");
    scoreGO.transform.SetParent(card.transform, false);
    TextMeshProUGUI scoreTxt = scoreGO.AddComponent<TextMeshProUGUI>();
    scoreTxt.text = "You scored " + score + "!";
    scoreTxt.fontSize = 34; scoreTxt.alignment = TextAlignmentOptions.Center;
    scoreTxt.color = Color.white;
    RectTransform scoreRT = scoreTxt.GetComponent<RectTransform>();
    scoreRT.anchorMin = new Vector2(0f, 1f); scoreRT.anchorMax = new Vector2(1f, 1f);
    scoreRT.pivot = new Vector2(0.5f, 1f);
    scoreRT.anchoredPosition = new Vector2(0, -90); scoreRT.sizeDelta = new Vector2(0, 48);

    // Input
    GameObject inputGO = new GameObject("LazyInput");
    inputGO.transform.SetParent(card.transform, false);
    inputGO.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);
    TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
    inputField.characterLimit = 12;
    inputField.contentType = TMP_InputField.ContentType.Alphanumeric;
    RectTransform inputRT = inputGO.GetComponent<RectTransform>();
    inputRT.anchorMin = inputRT.anchorMax = inputRT.pivot = new Vector2(0.5f, 0.5f);
    inputRT.sizeDelta = new Vector2(380, 70); inputRT.anchoredPosition = new Vector2(0, 20);

    GameObject inputText = new GameObject("Text");
    inputText.transform.SetParent(inputGO.transform, false);
    TextMeshProUGUI inputTMP = inputText.AddComponent<TextMeshProUGUI>();
    inputTMP.fontSize = 32; inputTMP.color = Color.black;
    inputTMP.alignment = TextAlignmentOptions.Center;
    RectTransform itRT = inputText.GetComponent<RectTransform>();
    itRT.anchorMin = Vector2.zero; itRT.anchorMax = Vector2.one;
    itRT.offsetMin = itRT.offsetMax = Vector2.zero;
    inputField.textComponent = inputTMP;

    GameObject phGO = new GameObject("Placeholder");
    phGO.transform.SetParent(inputGO.transform, false);
    TextMeshProUGUI ph = phGO.AddComponent<TextMeshProUGUI>();
    ph.text = "Enter username"; ph.fontSize = 32;
    ph.color = Color.gray; ph.alignment = TextAlignmentOptions.Center;
    RectTransform phRT = ph.GetComponent<RectTransform>();
    phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
    phRT.offsetMin = phRT.offsetMax = Vector2.zero;
    inputField.placeholder = ph;

    TextMeshProUGUI errorTxt = null;
    GameObject errorGO = new GameObject("Error");
    errorGO.transform.SetParent(card.transform, false);
    errorTxt = errorGO.AddComponent<TextMeshProUGUI>();
    errorTxt.fontSize = 22; errorTxt.color = new Color(1f, 0.4f, 0.3f);
    errorTxt.alignment = TextAlignmentOptions.Center; errorTxt.text = "";
    RectTransform errRT = errorTxt.GetComponent<RectTransform>();
    errRT.anchorMin = new Vector2(0f, 0.5f); errRT.anchorMax = new Vector2(1f, 0.5f);
    errRT.pivot = new Vector2(0.5f, 0.5f);
    errRT.anchoredPosition = new Vector2(0, -30); errRT.sizeDelta = new Vector2(0, 36);

    // Save button
    GameObject saveBtn = new GameObject("SaveBtn");
    saveBtn.transform.SetParent(card.transform, false);
    Image saveBtnImg = saveBtn.AddComponent<Image>();
    saveBtnImg.color = new Color(0.1f, 0.75f, 0.2f);
    Button saveBtnComp = saveBtn.AddComponent<Button>();
    RectTransform saveBtnRT = saveBtn.GetComponent<RectTransform>();
    saveBtnRT.anchorMin = saveBtnRT.anchorMax = saveBtnRT.pivot = new Vector2(0.5f, 0f);
    saveBtnRT.sizeDelta = new Vector2(360, 80); saveBtnRT.anchoredPosition = new Vector2(0, 48);
    GameObject saveTxtGO = new GameObject("Label"); saveTxtGO.transform.SetParent(saveBtn.transform, false);
    TextMeshProUGUI saveTxt = saveTxtGO.AddComponent<TextMeshProUGUI>();
    saveTxt.text = "SAVE SCORE"; saveTxt.fontSize = 28; saveTxt.fontStyle = FontStyles.Bold;
    saveTxt.color = Color.white; saveTxt.alignment = TextAlignmentOptions.Center;
    RectTransform saveTxtRT = saveTxt.GetComponent<RectTransform>();
    saveTxtRT.anchorMin = Vector2.zero; saveTxtRT.anchorMax = Vector2.one;
    saveTxtRT.offsetMin = saveTxtRT.offsetMax = Vector2.zero;

    // Skip button
    GameObject skipBtn = new GameObject("SkipBtn");
    skipBtn.transform.SetParent(card.transform, false);
    Image skipBtnImg = skipBtn.AddComponent<Image>();
    skipBtnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    Button skipBtnComp = skipBtn.AddComponent<Button>();
    RectTransform skipBtnRT = skipBtn.GetComponent<RectTransform>();
    skipBtnRT.anchorMin = skipBtnRT.anchorMax = skipBtnRT.pivot = new Vector2(0.5f, 0f);
    skipBtnRT.sizeDelta = new Vector2(360, 56); skipBtnRT.anchoredPosition = new Vector2(0, -16);
    GameObject skipTxtGO = new GameObject("Label"); skipTxtGO.transform.SetParent(skipBtn.transform, false);
    TextMeshProUGUI skipTxt = skipTxtGO.AddComponent<TextMeshProUGUI>();
    skipTxt.text = "Skip for now"; skipTxt.fontSize = 22;
    skipTxt.color = new Color(0.8f, 0.8f, 0.8f); skipTxt.alignment = TextAlignmentOptions.Center;
    RectTransform skipTxtRT = skipTxt.GetComponent<RectTransform>();
    skipTxtRT.anchorMin = Vector2.zero; skipTxtRT.anchorMax = Vector2.one;
    skipTxtRT.offsetMin = skipTxtRT.offsetMax = Vector2.zero;

    System.Text.RegularExpressions.Regex validChars =
        new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_]+$");

    saveBtnComp.onClick.AddListener(() =>
    {
        string name = inputField.text.Trim();
        if (string.IsNullOrEmpty(name) || name.Length < 3)
        {
            if (errorTxt != null) errorTxt.text = "Min 3 characters!";
            return;
        }
        if (!validChars.IsMatch(name))
        {
            if (errorTxt != null) errorTxt.text = "Letters, numbers, _ only!";
            return;
        }
        playerUsername = name.Length > 12 ? name.Substring(0, 12) : name;
        GameBootstrap.Instance?.SaveUsername(playerUsername);
        GameBootstrap.Instance?.RefreshUsernameText();
        Destroy(overlay);
        SaveScore(playerUsername, lastScore);
    });

    skipBtnComp.onClick.AddListener(() =>
    {
        playerUsername = "Player" + UnityEngine.Random.Range(100, 9999);
        Destroy(overlay);
    });

    overlay.transform.SetAsLastSibling();
}

}