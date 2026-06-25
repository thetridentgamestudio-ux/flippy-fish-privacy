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
    // ── Start-screen layout roots ─────────────────────────────────────────
    private TextMeshProUGUI _topBarUsernameLabel;
    private GameObject _topBarRoot;
    private GameObject _mainButtonsRoot;
    private GameObject _bottomNavBar;
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
            // Returning player — panel stays hidden (new top bar shows username)
            if (usernamePanel != null) usernamePanel.SetActive(false);
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

                // Multiplayer — initialize with same DB reference
                EnsureMultiplayerManager();
                MultiplayerManager.Instance.Initialize(dbReference);
            }
            else
            {
                isFirebaseReady = false;
                Debug.LogError("Firebase not ready: " + task.Result);
            }
        });
    }

    static void EnsureMultiplayerManager()
    {
        if (MultiplayerManager.Instance != null) return;
        var go = new GameObject("MultiplayerManager");
        go.AddComponent<MultiplayerManager>();
        DontDestroyOnLoad(go);
    }

    static void EnsureGhostRaceManager()
    {
        if (GhostRaceManager.Instance != null) return;
        var go = new GameObject("GhostRaceManager");
        go.AddComponent<GhostRaceManager>();
        DontDestroyOnLoad(go);
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

        // Build country standings from same snapshot and cache it
        BuildCountryStandings(scores);
    });

    // Ensure tab buttons exist on the panel
    EnsureLeaderboardTabs();
}

// ── Country leaderboard ───────────────────────────────────────────────

private struct CountryEntry { public string code; public int topScore; public int players; }
private List<CountryEntry> _countryStandings = new List<CountryEntry>();
private bool _showingCountries = false;
private GameObject _tabPlayers, _tabCountries;

static string CountryFlagEmoji(string code)
{
    if (string.IsNullOrEmpty(code) || code.Length != 2) return "🌐";
    // Regional indicator symbols: 🇦 = U+1F1E6, offset from 'A'
    int a = code.ToUpper()[0] - 'A' + 0x1F1E6;
    int b = code.ToUpper()[1] - 'A' + 0x1F1E6;
    return char.ConvertFromUtf32(a) + char.ConvertFromUtf32(b);
}

void BuildCountryStandings(List<(string username, int score)> scores)
{
    var byCountry = new Dictionary<string, (int top, int count)>();
    foreach (var (username, score) in scores)
    {
        // Username format: "IN_PlayerName_1234" — country is the prefix before first '_'
        string[] parts = username.Split('_');
        string code = parts.Length >= 2 && parts[0].Length == 2
            ? parts[0].ToUpper() : "XX";

        if (!byCountry.ContainsKey(code))
            byCountry[code] = (score, 1);
        else
        {
            var cur = byCountry[code];
            byCountry[code] = (Mathf.Max(cur.top, score), cur.count + 1);
        }
    }

    _countryStandings = new List<CountryEntry>();
    foreach (var kv in byCountry)
        _countryStandings.Add(new CountryEntry { code = kv.Key, topScore = kv.Value.top, players = kv.Value.count });

    _countryStandings.Sort((a, b) => b.topScore.CompareTo(a.topScore));
}

void EnsureLeaderboardTabs()
{
    if (_tabPlayers != null) return; // already created
    if (leaderboardPanel == null) return;

    float panelW  = Screen.width  * 0.88f;
    float panelH  = Screen.height * 0.60f;
    float tabW    = panelW * 0.42f;
    float tabH    = Screen.height * 0.052f;
    float titleH  = Screen.height * 0.055f;
    // Title bottom from panel centre = panelH/2 - 12 - titleH
    float titleBottom = panelH * 0.5f - 12f - titleH;
    // Tab centre is half a tabH below the title bottom, with 8px gap
    float tabY    = titleBottom - tabH * 0.5f - 8f;

    // PLAYERS tab
    _tabPlayers = new GameObject("Tab_Players");
    _tabPlayers.transform.SetParent(leaderboardPanel.transform, false);
    _tabPlayers.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.6f, 1f);
    var tabPlayersBtn = _tabPlayers.AddComponent<Button>();
    tabPlayersBtn.onClick.AddListener(() => SwitchTab(false));
    var tpRT = _tabPlayers.GetComponent<RectTransform>();
    tpRT.anchorMin = tpRT.anchorMax = tpRT.pivot = new Vector2(0.5f, 0.5f);
    tpRT.sizeDelta = new Vector2(tabW, tabH);
    tpRT.anchoredPosition = new Vector2(-tabW * 0.5f - 4f, tabY);
    AddTabLabel(_tabPlayers, "🏆 PLAYERS");

    // COUNTRIES tab
    _tabCountries = new GameObject("Tab_Countries");
    _tabCountries.transform.SetParent(leaderboardPanel.transform, false);
    _tabCountries.AddComponent<Image>().color = new Color(0.1f, 0.38f, 0.18f, 1f);
    var tabCountriesBtn = _tabCountries.AddComponent<Button>();
    tabCountriesBtn.onClick.AddListener(() => SwitchTab(true));
    var tcRT = _tabCountries.GetComponent<RectTransform>();
    tcRT.anchorMin = tcRT.anchorMax = tcRT.pivot = new Vector2(0.5f, 0.5f);
    tcRT.sizeDelta = new Vector2(tabW, tabH);
    tcRT.anchoredPosition = new Vector2(tabW * 0.5f + 4f, tabY);
    AddTabLabel(_tabCountries, "🌍 COUNTRIES");
}

void AddTabLabel(GameObject tab, string text)
{
    var go = new GameObject("TabLabel");
    go.transform.SetParent(tab.transform, false);
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text = text; tmp.fontStyle = FontStyles.Bold;
    tmp.fontSize = Screen.height * 0.019f;
    tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    var rt = tmp.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = rt.offsetMax = Vector2.zero;
}

void SwitchTab(bool showCountries)
{
    _showingCountries = showCountries;

    // Highlight active tab
    if (_tabPlayers   != null) _tabPlayers.GetComponent<Image>().color   = showCountries
        ? new Color(0.08f, 0.20f, 0.40f, 1f) : new Color(0.25f, 0.50f, 0.85f, 1f);
    if (_tabCountries != null) _tabCountries.GetComponent<Image>().color = showCountries
        ? new Color(0.15f, 0.55f, 0.25f, 1f) : new Color(0.08f, 0.25f, 0.12f, 1f);

    if (showCountries)
        ShowCountryTab();
    else
        StartCoroutine(RestorePlayerTab());
}

void ShowCountryTab()
{
    if (_countryStandings.Count == 0)
    {
        leaderboardText.text = "\n<color=#aaa>No country data yet.\nPlay more games!</color>";
        return;
    }

    string myCountry = PlayerPrefs.GetString("COUNTRY", "XX").ToUpper();
    int colFlag  = 0;
    int colCode  = Mathf.RoundToInt(Screen.width * 0.16f);
    int colScore = Mathf.RoundToInt(Screen.width * 0.56f);
    int colCount = Mathf.RoundToInt(Screen.width * 0.72f);

    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < _countryStandings.Count && i < 20; i++)
    {
        var c    = _countryStandings[i];
        string flag  = CountryFlagEmoji(c.code);
        string rank  = i < 3 ? "  " : $"{i+1}.";
        string line  = $"<pos={colFlag}>{rank}<pos={colCode}>{flag} {c.code}<pos={colScore}>{c.topScore}<pos={colCount}><color=#aaa>×{c.players}</color>";

        bool isMe = c.code == myCountry;
        sb.Append(isMe
            ? $"<color=#FFE234><b>{line}</b></color>\n"
            : $"{line}\n");
    }

    int myRank = _countryStandings.FindIndex(c => c.code == myCountry) + 1;
    if (myRank > 0)
        sb.Append($"\n<color=#FFE234><b>Your Country Rank: #{myRank}</b></color>");

    leaderboardText.text = sb.ToString();

    // Hide player medals — not relevant in country view
    foreach (var medal in medalObjects)
        if (medal != null) medal.SetActive(false);
}

IEnumerator RestorePlayerTab()
{
    // Re-fetch player leaderboard to restore text
    yield return null;
    ShowLeaderboardUI(lastScore);
}

    // Full-canvas invisible layer — children use it as their anchor base
    GameObject MakeLayerRoot(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    void CreateStartMenuUI()
    {
        // ── Canvas ───────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas");
        mainCanvas = canvasGO.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080, 1920);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Sprite pillSpr   = Resources.Load<Sprite>("pill_dark");
        Sprite coinSpr   = Resources.Load<Sprite>("Coin");
        Sprite fishIcon  = Resources.Load<Sprite>("icon_fish");

        // ── TOP BAR LAYER ────────────────────────────────────────────────────
        _topBarRoot = MakeLayerRoot(canvasGO.transform, "TopBarRoot");

        // Help button — top-left blue bubble
        var helpGO  = new GameObject("HelpButton");
        helpGO.transform.SetParent(_topBarRoot.transform, false);
        var helpImg = helpGO.AddComponent<Image>();
        Sprite helpSpr = Resources.Load<Sprite>("btn_help");
        if (helpSpr != null) helpImg.sprite = helpSpr;
        else { helpImg.color = new Color(0.18f, 0.48f, 0.92f); }
        var helpBtn = helpGO.AddComponent<Button>();
        helpBtn.targetGraphic = helpImg;
        helpBtn.onClick.AddListener(() => OnboardingOverlay.ShowAlways(GameBootstrap.Instance.GetMainCanvas()));
        var helpRT = helpGO.GetComponent<RectTransform>();
        helpRT.anchorMin = new Vector2(0, 1); helpRT.anchorMax = new Vector2(0, 1);
        helpRT.pivot     = new Vector2(0, 1);
        helpRT.sizeDelta        = new Vector2(112, 112);
        helpRT.anchoredPosition = new Vector2(60, -52);

        // Username pill — top center
        var userPillGO  = new GameObject("UsernamePill");
        userPillGO.transform.SetParent(_topBarRoot.transform, false);
        var userPillImg = userPillGO.AddComponent<Image>();
        if (pillSpr != null) { userPillImg.sprite = pillSpr; userPillImg.type = Image.Type.Simple; }
        else userPillImg.color = new Color(0.08f, 0.14f, 0.26f, 0.92f);
        var userPillRT = userPillGO.GetComponent<RectTransform>();
        userPillRT.anchorMin = new Vector2(0.5f, 1); userPillRT.anchorMax = new Vector2(0.5f, 1);
        userPillRT.pivot     = new Vector2(0.5f, 1);
        userPillRT.sizeDelta        = new Vector2(370, 90);
        userPillRT.anchoredPosition = new Vector2(0, -52);

        // Fish icon inside username pill
        var ufishGO  = new GameObject("FishIcon");
        ufishGO.transform.SetParent(userPillGO.transform, false);
        var ufishImg = ufishGO.AddComponent<Image>();
        if (fishIcon != null) ufishImg.sprite = fishIcon;
        else ufishImg.color = new Color(1f, 0.5f, 0.1f);
        var ufishRT  = ufishGO.GetComponent<RectTransform>();
        ufishRT.anchorMin = new Vector2(0, 0.5f); ufishRT.anchorMax = new Vector2(0, 0.5f);
        ufishRT.pivot     = new Vector2(0, 0.5f);
        ufishRT.sizeDelta        = new Vector2(62, 62);
        ufishRT.anchoredPosition = new Vector2(14, 0);

        // Username text inside pill
        var uTxtGO   = new GameObject("UsernameLabel");
        uTxtGO.transform.SetParent(userPillGO.transform, false);
        _topBarUsernameLabel = uTxtGO.AddComponent<TextMeshProUGUI>();
        _topBarUsernameLabel.text      = PlayerPrefs.GetString("USERNAME", "Player");
        _topBarUsernameLabel.fontSize  = 30;
        _topBarUsernameLabel.fontStyle = FontStyles.Bold;
        _topBarUsernameLabel.color     = Color.white;
        _topBarUsernameLabel.alignment = TextAlignmentOptions.Left;
        _topBarUsernameLabel.overflowMode      = TextOverflowModes.Ellipsis;
        _topBarUsernameLabel.textWrappingMode  = TextWrappingModes.NoWrap;
        var uTxtRT   = _topBarUsernameLabel.rectTransform;
        uTxtRT.anchorMin = Vector2.zero; uTxtRT.anchorMax = Vector2.one;
        uTxtRT.offsetMin = new Vector2(84, 4); uTxtRT.offsetMax = new Vector2(-10, -4);

        // Coin pill — top right
        var coinPillGO  = new GameObject("CoinPill");
        coinPillGO.transform.SetParent(_topBarRoot.transform, false);
        var coinPillImg = coinPillGO.AddComponent<Image>();
        if (pillSpr != null) { coinPillImg.sprite = pillSpr; coinPillImg.type = Image.Type.Simple; }
        else coinPillImg.color = new Color(0.08f, 0.14f, 0.26f, 0.92f);
        var coinPillRT  = coinPillGO.GetComponent<RectTransform>();
        coinPillRT.anchorMin = new Vector2(1, 1); coinPillRT.anchorMax = new Vector2(1, 1);
        coinPillRT.pivot     = new Vector2(1, 1);
        coinPillRT.sizeDelta        = new Vector2(225, 90);
        coinPillRT.anchoredPosition = new Vector2(-60, -52);

        // Coin icon inside coin pill
        var cpCoinIconGO  = new GameObject("CoinIcon");
        cpCoinIconGO.transform.SetParent(coinPillGO.transform, false);
        var cpCoinIconImg = cpCoinIconGO.AddComponent<Image>();
        if (coinSpr != null) cpCoinIconImg.sprite = coinSpr;
        else cpCoinIconImg.color = new Color(1f, 0.85f, 0.1f);
        var cpCoinIconRT  = cpCoinIconGO.GetComponent<RectTransform>();
        cpCoinIconRT.anchorMin = new Vector2(1, 0.5f); cpCoinIconRT.anchorMax = new Vector2(1, 0.5f);
        cpCoinIconRT.pivot     = new Vector2(1, 0.5f);
        cpCoinIconRT.sizeDelta        = new Vector2(60, 60);
        cpCoinIconRT.anchoredPosition = new Vector2(-12, 0);

        // Coin count text inside coin pill
        var cpTxtGO   = new GameObject("CoinLabel");
        cpTxtGO.transform.SetParent(coinPillGO.transform, false);
        var cpTxt = cpTxtGO.AddComponent<TextMeshProUGUI>();
        cpTxt.fontSize  = 32;
        cpTxt.fontStyle = FontStyles.Bold;
        cpTxt.color     = new Color(1f, 0.92f, 0.2f);
        cpTxt.alignment = TextAlignmentOptions.Right;
        cpTxt.text      = SkinManager.GetCoins().ToString();
        coinDisplayText = cpTxt;
        var cpTxtRT   = cpTxt.rectTransform;
        cpTxtRT.anchorMin = Vector2.zero; cpTxtRT.anchorMax = Vector2.one;
        cpTxtRT.offsetMin = new Vector2(10, 4); cpTxtRT.offsetMax = new Vector2(-78, -4);

        // ── USERNAME INPUT PANEL (shown only when no username set) ────────────
        usernamePanel = new GameObject("UsernamePanel");
        usernamePanel.transform.SetParent(canvasGO.transform, false);
        var panelRect = usernamePanel.AddComponent<RectTransform>();
        panelRect.sizeDelta        = new Vector2(580, 290);
        panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRect.pivot            = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, 120);
        var panelImage = usernamePanel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.04f, 0.10f, 0.22f, 0.96f);

        // Input field
        var inputGO   = new GameObject("UsernameInput");
        inputGO.transform.SetParent(usernamePanel.transform, false);
        var inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.sizeDelta        = new Vector2(460, 80);
        inputRect.anchoredPosition = new Vector2(0, 70);
        inputGO.AddComponent<UnityEngine.UI.Image>().color = Color.white;
        usernameInput = inputGO.AddComponent<TMP_InputField>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        var inputTxt = textGO.AddComponent<TextMeshProUGUI>();
        inputTxt.fontSize = 34; inputTxt.alignment = TextAlignmentOptions.Center; inputTxt.color = Color.black;
        var tRect = inputTxt.rectTransform;
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = tRect.offsetMax = Vector2.zero;
        usernameInput.textComponent = inputTxt;

        var phGO  = new GameObject("Placeholder");
        phGO.transform.SetParent(inputGO.transform, false);
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = "Enter Username"; ph.fontSize = 34; ph.color = Color.gray;
        ph.alignment = TextAlignmentOptions.Center;
        var phRect = ph.rectTransform;
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = phRect.offsetMax = Vector2.zero;
        usernameInput.placeholder = ph;
        usernameInput.characterLimit = 12;
        usernameInput.contentType = TMP_InputField.ContentType.Alphanumeric;

        // Submit button inside panel
        var submitGO  = new GameObject("SubmitButton");
        submitGO.transform.SetParent(usernamePanel.transform, false);
        var submitImg = submitGO.AddComponent<Image>();
        submitImg.color = new Color(0.05f, 0.55f, 0.85f);
        var submitBtn = submitGO.AddComponent<Button>();
        submitBtn.targetGraphic = submitImg;
        submitBtn.onClick.AddListener(OnSubmitUsername);
        var submitLblGO = new GameObject("Label");
        submitLblGO.transform.SetParent(submitGO.transform, false);
        var submitLbl = submitLblGO.AddComponent<TextMeshProUGUI>();
        submitLbl.text = "PLAY"; submitLbl.fontSize = 36; submitLbl.fontStyle = FontStyles.Bold;
        submitLbl.alignment = TextAlignmentOptions.Center; submitLbl.color = Color.white;
        var slRT = submitLbl.rectTransform;
        slRT.anchorMin = Vector2.zero; slRT.anchorMax = Vector2.one;
        slRT.offsetMin = slRT.offsetMax = Vector2.zero;
        var submitRT = submitGO.GetComponent<RectTransform>();
        submitRT.sizeDelta = new Vector2(460, 100); submitRT.anchoredPosition = new Vector2(0, -60);
        usernamePanel.SetActive(!PlayerPrefs.HasKey("USERNAME"));

        // ── MAIN BUTTONS LAYER ────────────────────────────────────────────────
        _mainButtonsRoot = MakeLayerRoot(canvasGO.transform, "MainButtonsRoot");

        // ── Helper: sprite-only button — PNG already contains icon + text ─────
        void MakeSpriteButton(Transform btnParent, string goName, string spriteName,
                              float yPos, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(btnParent, false);
            var img = go.AddComponent<Image>();
            Sprite spr = Resources.Load<Sprite>(spriteName);
            if (spr != null) { img.sprite = spr; img.color = Color.white; img.preserveAspect = true; }
            else img.color = new Color(0.1f, 0.7f, 0.8f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(860, height);
            rt.anchoredPosition = new Vector2(0, yPos);
        }

        MakeSpriteButton(_mainButtonsRoot.transform, "StartButton",       "start_button", -120f, 155f, OnSubmitUsername);

        // Keep sound button but hide it from start menu; it shows during gameplay
        CreateSoundButton(usernamePanel);
        if (soundButtonObj != null) soundButtonObj.SetActive(false);

        Create2PlayerButton(_mainButtonsRoot);

        // ── BOTTOM NAV BAR ────────────────────────────────────────────────────
        _bottomNavBar = new GameObject("BottomNavBar");
        _bottomNavBar.transform.SetParent(canvasGO.transform, false);
        var navImg = _bottomNavBar.AddComponent<Image>();
        navImg.color = new Color(0f, 0f, 0f, 0f); // transparent — no dark bar
        var navRT = _bottomNavBar.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0, 0); navRT.anchorMax = new Vector2(1, 0);
        navRT.pivot     = new Vector2(0.5f, 0);
        navRT.sizeDelta        = new Vector2(0, 168);
        navRT.anchoredPosition = Vector2.zero;

        // Helper: nav item (icon + label) at anchorX within nav bar
        void MakeNavItem(string goName, string iconSpriteName, string labelText,
                         float anchorX, UnityEngine.Events.UnityAction onClickAction,
                         bool showDot = false)
        {
            var container = new GameObject(goName);
            container.transform.SetParent(_bottomNavBar.transform, false);
            var containerImg = container.AddComponent<Image>();
            containerImg.color = new Color(0,0,0,0);
            var cBtn = container.AddComponent<Button>();
            cBtn.targetGraphic = containerImg;
            if (onClickAction != null) cBtn.onClick.AddListener(onClickAction);
            var cRT = container.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(anchorX, 0f); cRT.anchorMax = new Vector2(anchorX, 1f);
            cRT.pivot     = new Vector2(0.5f, 0.5f);
            cRT.sizeDelta        = new Vector2(200, 0);
            cRT.anchoredPosition = Vector2.zero;

            // Icon
            var iconGO  = new GameObject("Icon");
            iconGO.transform.SetParent(container.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            Sprite spr = Resources.Load<Sprite>(iconSpriteName);
            if (spr != null) { iconImg.sprite = spr; iconImg.preserveAspect = true; }
            else iconImg.color = new Color(0.7f, 0.9f, 1f);
            var iconRT  = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1f); iconRT.anchorMax = new Vector2(0.5f, 1f);
            iconRT.pivot     = new Vector2(0.5f, 1f);
            iconRT.sizeDelta        = new Vector2(105, 105);
            iconRT.anchoredPosition = new Vector2(0, -14);

            // Label (skipped when labelText is empty — icon already has text baked in)
            if (!string.IsNullOrEmpty(labelText))
            {
                var lblGO  = new GameObject("Label");
                lblGO.transform.SetParent(container.transform, false);
                var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
                lbl.text      = labelText;
                lbl.fontSize  = 30;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color     = new Color(0.72f, 0.90f, 1f);
                lbl.alignment = TextAlignmentOptions.Center;
                var lblRT  = lbl.rectTransform;
                lblRT.anchorMin = new Vector2(0f, 0f); lblRT.anchorMax = new Vector2(1f, 0f);
                lblRT.pivot     = new Vector2(0.5f, 0f);
                lblRT.sizeDelta        = new Vector2(0, 36);
                lblRT.anchoredPosition = new Vector2(0, 8);
            }

            // Notification dot
            if (showDot)
            {
                var dotGO  = new GameObject("Dot");
                dotGO.transform.SetParent(iconGO.transform, false);
                var dotImg = dotGO.AddComponent<Image>();
                dotImg.color = new Color(0.20f, 0.60f, 1f);
                var dotRT  = dotGO.GetComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(1f, 1f); dotRT.anchorMax = new Vector2(1f, 1f);
                dotRT.pivot     = new Vector2(0.5f, 0.5f);
                dotRT.sizeDelta        = new Vector2(28, 28);
                dotRT.anchoredPosition = new Vector2(-4, -4);
            }
        }

        // Skins button — store reference for SetSkinsButtonVisible
        MakeNavItem("SkinsButton",  "icon_skins",  "", 0.16f, () => SkinSelectUI.Show());
        skinsButtonObj = _bottomNavBar.transform.Find("SkinsButton")?.gameObject;

        MakeNavItem("QuestButton",  "icon_quest",  "Quest", 0.50f,
                    () => GameBootstrap.Instance?.OpenDailyQuests(), showDot: true);

        // Coins display — right third (no button action)
        var coinDispGO = new GameObject("CoinDisplay");
        coinDispGO.transform.SetParent(_bottomNavBar.transform, false);
        coinDispGO.AddComponent<Image>().color = new Color(0,0,0,0);
        var coinDispRT = coinDispGO.GetComponent<RectTransform>();
        coinDispRT.anchorMin = new Vector2(0.84f, 0f); coinDispRT.anchorMax = new Vector2(1f, 1f);
        coinDispRT.offsetMin = coinDispRT.offsetMax = Vector2.zero;

        var coinIconNavGO  = new GameObject("CoinIcon");
        coinIconNavGO.transform.SetParent(coinDispGO.transform, false);
        var coinIconNavImg = coinIconNavGO.AddComponent<Image>();
        if (coinSpr != null) { coinIconNavImg.sprite = coinSpr; coinIconNavImg.preserveAspect = true; }
        else coinIconNavImg.color = new Color(1f, 0.85f, 0.1f);
        var coinIconNavRT  = coinIconNavGO.GetComponent<RectTransform>();
        coinIconNavRT.anchorMin = new Vector2(1f, 0.5f); coinIconNavRT.anchorMax = new Vector2(1f, 0.5f);
        coinIconNavRT.pivot     = new Vector2(1f, 0.5f);
        coinIconNavRT.sizeDelta        = new Vector2(68, 68);
        coinIconNavRT.anchoredPosition = new Vector2(-14, 8);

        var coinTxtNavGO = new GameObject("CoinLabel");
        coinTxtNavGO.transform.SetParent(coinDispGO.transform, false);
        var coinTxtNav   = coinTxtNavGO.AddComponent<TextMeshProUGUI>();
        coinTxtNav.fontSize  = 36; coinTxtNav.fontStyle = FontStyles.Bold;
        coinTxtNav.color     = new Color(1f, 0.92f, 0.2f);
        coinTxtNav.alignment = TextAlignmentOptions.Right;
        coinTxtNav.text      = SkinManager.GetCoins().ToString();
        coinDisplayText      = coinTxtNav;
        var coinTxtNavRT     = coinTxtNav.rectTransform;
        coinTxtNavRT.anchorMin = Vector2.zero; coinTxtNavRT.anchorMax = Vector2.one;
        coinTxtNavRT.offsetMin = new Vector2(8, 0); coinTxtNavRT.offsetMax = new Vector2(-88, 0);
    }

void Create2PlayerButton(GameObject parent)
{
    // MULTIPLAYER — full sprite PNG
    var mpGO  = new GameObject("TwoPlayerButton");
    mpGO.transform.SetParent(parent.transform, false);
    var mpImg = mpGO.AddComponent<Image>();
    Sprite mpSpr = Resources.Load<Sprite>("multiplayer");
    if (mpSpr != null) { mpImg.sprite = mpSpr; mpImg.color = Color.white; mpImg.preserveAspect = true; }
    else mpImg.color = new Color(0.10f, 0.18f, 0.30f);
    var mpBtn = mpGO.AddComponent<Button>();
    mpBtn.targetGraphic = mpImg;
    mpBtn.onClick.AddListener(OnTwoPlayerPressed);
    var mpRT = mpGO.GetComponent<RectTransform>();
    mpRT.anchorMin = new Vector2(0.5f, 0.5f); mpRT.anchorMax = new Vector2(0.5f, 0.5f);
    mpRT.pivot     = new Vector2(0.5f, 0.5f);
    mpRT.sizeDelta        = new Vector2(860, 145);
    mpRT.anchoredPosition = new Vector2(0, -305);

    // GO PREMIUM — full sprite PNG
    var gpGO  = new GameObject("BattlePassButton");
    gpGO.transform.SetParent(parent.transform, false);
    var gpImg = gpGO.AddComponent<Image>();
    Sprite gpSpr = Resources.Load<Sprite>("go_premium");
    if (gpSpr != null) { gpImg.sprite = gpSpr; gpImg.color = Color.white; gpImg.preserveAspect = true; }
    else gpImg.color = new Color(0.48f, 0.14f, 0.72f);
    var gpBtn = gpGO.AddComponent<Button>();
    gpBtn.targetGraphic = gpImg;
    gpBtn.onClick.AddListener(() => BattlePassUI.Show());
    var gpRT = gpGO.GetComponent<RectTransform>();
    gpRT.anchorMin = new Vector2(0.5f, 0.5f); gpRT.anchorMax = new Vector2(0.5f, 0.5f);
    gpRT.pivot     = new Vector2(0.5f, 0.5f);
    gpRT.sizeDelta        = new Vector2(860, 145);
    gpRT.anchoredPosition = new Vector2(0, -478);
}

// ════════════════════════════════════════════════════════════════════
// MULTIPLAYER UI — v2: production-quality Battle Mode experience
// 4 screen states. Pure anchor layout. Live timers. Identity context.
// Design tokens → MP_ prefix.
// ════════════════════════════════════════════════════════════════════

// ── Design tokens ────────────────────────────────────────────────────
static readonly Color MP_BG_DEEP  = new Color(0.04f, 0.09f, 0.16f, 0.97f);
static readonly Color MP_BG_CARD  = new Color(0.08f, 0.18f, 0.32f, 1.00f);
static readonly Color MP_RED      = new Color(0.91f, 0.27f, 0.27f, 1.00f);
static readonly Color MP_BLUE     = new Color(0.11f, 0.39f, 0.72f, 1.00f);
static readonly Color MP_GREEN    = new Color(0.10f, 0.48f, 0.26f, 1.00f);
static readonly Color MP_CANCEL   = new Color(0.25f, 0.10f, 0.10f, 1.00f);
static readonly Color MP_GOLD     = new Color(1.00f, 0.84f, 0.00f, 1.00f);
static readonly Color MP_TEXT     = new Color(1.00f, 1.00f, 1.00f, 1.00f);
static readonly Color MP_TEXT_DIM = new Color(0.55f, 0.66f, 0.78f, 1.00f);
static readonly Color MP_DIVIDER  = new Color(0.12f, 0.23f, 0.36f, 1.00f);

// ── Screen state refs ────────────────────────────────────────────────
private GameObject      _mpPanel;
private GameObject      _mpScreenMain;
private GameObject      _mpScreenSearch;
private GameObject      _mpScreenHost;
private GameObject      _mpScreenJoin;
private TextMeshProUGUI _mpHostCodeLabel;
private TextMeshProUGUI _mpSearchStatus;
private TextMeshProUGUI _mpSearchCountdown;
private TextMeshProUGUI _mpSearchError;
private TextMeshProUGUI _mpHostCountdown;
private TMP_InputField  _mpJoinInput;
private TextMeshProUGUI _mpJoinError;
private Coroutine       _mpDotsCoroutine;
private Coroutine       _mpSearchTimerCoroutine;
private Coroutine       _mpHostTimerCoroutine;

void OnTwoPlayerPressed()
{
    string username = PlayerPrefs.GetString("USERNAME", "").Trim();
    if (string.IsNullOrEmpty(username)) { OnSubmitUsername(); return; }
    BuildMPPanelIfNeeded();
    ShowMPScreen(_mpScreenMain);
    _mpPanel.SetActive(true);
}

void BuildMPPanelIfNeeded()
{
    if (_mpPanel != null) return;

    _mpPanel = MPMakeFullscreen(mainCanvas.gameObject, "MPPanel", MP_BG_DEEP);
    _mpPanel.SetActive(false);

    _mpScreenMain   = BuildScreenMain();
    _mpScreenSearch = BuildScreenSearch();
    _mpScreenHost   = BuildScreenHost();
    _mpScreenJoin   = BuildScreenJoin();

    foreach (var s in new[]{ _mpScreenMain, _mpScreenSearch, _mpScreenHost, _mpScreenJoin })
        s.SetActive(false);

    MultiplayerManager.OnMatchFound      += OnMPMatchFound;
    MultiplayerManager.OnNoPlayersFound  += OnMPNoPlayersFound;
}

void OnDisable()
{
    MultiplayerManager.OnMatchFound      -= OnMPMatchFound;
    MultiplayerManager.OnNoPlayersFound  -= OnMPNoPlayersFound;
}

void ShowMPScreen(GameObject target)
{
    foreach (var s in new[]{ _mpScreenMain, _mpScreenSearch, _mpScreenHost, _mpScreenJoin })
        if (s != null) s.SetActive(s == target);

    if (_mpDotsCoroutine != null) { StopCoroutine(_mpDotsCoroutine); _mpDotsCoroutine = null; }

    if (target != _mpScreenSearch && _mpSearchTimerCoroutine != null)
    { StopCoroutine(_mpSearchTimerCoroutine); _mpSearchTimerCoroutine = null; }
    if (target != _mpScreenHost && _mpHostTimerCoroutine != null)
    { StopCoroutine(_mpHostTimerCoroutine); _mpHostTimerCoroutine = null; }
}

// Extract display tag "[flag]  Name" from stored username (format: CC_Name_1234)
string MPPlayerTag()
{
    string raw = PlayerPrefs.GetString("USERNAME", "Player");
    var parts = raw.Split('_');
    if (parts.Length >= 3 && parts[0].Length == 2)
    {
        string flag = CountryFlagEmoji(parts[0]);
        string name = string.Join("_", parts, 1, parts.Length - 2);
        return flag + "  " + name;
    }
    return raw;
}

// Fires when MultiplayerManager confirms an opponent. Shows a brief overlay then closes panel.
void OnMPMatchFound(string oppName)
{
    if (_mpPanel == null || !_mpPanel.activeSelf) return;
    StartCoroutine(MatchFoundSequence(oppName));
}

IEnumerator MatchFoundSequence(string oppName)
{
    // Overlay sits on top of whichever search/host screen is active.
    // For bot matches oppName is the bot name — we soften the language there.
    bool isBot = MultiplayerManager.Instance != null && MultiplayerManager.Instance.State == MultiplayerManager.MPState.Countdown
                 && oppName.EndsWith("Bot");

    var overlay = new GameObject("MatchFoundOverlay");
    overlay.transform.SetParent(_mpPanel.transform, false);
    var ovRT = overlay.AddComponent<RectTransform>();
    ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
    ovRT.offsetMin = ovRT.offsetMax = Vector2.zero;

    if (isBot)
    {
        overlay.AddComponent<Image>().color = new Color(0.05f, 0.10f, 0.22f, 0.96f);
        MPTextBand(overlay, "Ghost Fish joining!", 0.56f, 0.72f, 0.05f, 54f, new Color(0.55f, 0.88f, 1f), FontStyles.Bold);
        MPTextBand(overlay, oppName + " is ready to challenge you", 0.42f, 0.56f, 0.05f, 30f, MP_TEXT_DIM, FontStyles.Normal);
        MPTextBand(overlay, "Real players will be matched when available", 0.30f, 0.41f, 0.08f, 24f, MP_TEXT_DIM, FontStyles.Normal);
    }
    else
    {
        overlay.AddComponent<Image>().color = new Color(0.03f, 0.18f, 0.07f, 0.94f);
        MPTextBand(overlay, "MATCH FOUND!", 0.56f, 0.72f, 0.05f, 64f, new Color(0.3f, 1f, 0.45f), FontStyles.Bold);
        MPTextBand(overlay, "vs  " + oppName, 0.42f, 0.56f, 0.05f, 44f, MP_TEXT, FontStyles.Normal);
        MPTextBand(overlay, "Get ready…", 0.32f, 0.42f, 0.10f, 30f, MP_TEXT_DIM, FontStyles.Normal);
    }

    HapticFeedback.PlayPowerUpHaptic();

    yield return new WaitForSecondsRealtime(1.5f);

    if (overlay != null) Destroy(overlay);
    if (_mpPanel != null) _mpPanel.SetActive(false);
}

// Called when 60s expires with no real opponent — update search screen before bot starts.
void OnMPNoPlayersFound()
{
    if (_mpDotsCoroutine != null) { StopCoroutine(_mpDotsCoroutine); _mpDotsCoroutine = null; }
    if (_mpSearchTimerCoroutine != null) { StopCoroutine(_mpSearchTimerCoroutine); _mpSearchTimerCoroutine = null; }

    if (_mpSearchStatus   != null)
    {
        _mpSearchStatus.text = "No players available right now.";
        _mpSearchStatus.color = MP_TEXT_DIM;
    }
    if (_mpSearchCountdown != null) _mpSearchCountdown.text = "";
    if (_mpSearchError     != null)
        _mpSearchError.text = "You'll be matched against a Ghost Fish while we keep looking.";
}

// ── SCREEN: Main ─────────────────────────────────────────────────────
//
//  ┌─────────────────────────────────────┐
//  │          ⚔  BATTLE MODE            │  header  84-100%
//  │  🇮🇳  Vivek                        │  identity 78-84%
//  ├─────────────────────────────────────┤
//  │  ╔══════════════════════════════╗  │
//  │  ║  🌍  QUICK MATCH            ║  │  card 44-76%
//  │  ║  Match with a random player  ║  │
//  │  ║  ┌─────── PLAY NOW ───────┐ ║  │
//  │  ║  └───────────────────────┘ ║  │
//  │  ╚══════════════════════════════╝  │
//  │  ──── OR PLAY WITH A FRIEND ────   │  divider 40%
//  │  [ ⚔  CREATE PRIVATE ROOM ]       │  24-37%
//  │  [ 🔑  JOIN WITH CODE     ]       │  07-20%
//  └─────────────────────────────────────┘

GameObject BuildScreenMain()
{
    var screen = MPMakeContainer(_mpPanel, "Screen_Main");

    MPHeader(screen, "⚔  BATTLE MODE", () => CloseMPPanel());

    // Player identity strip — confirms who they're playing as
    MPTextBand(screen, MPPlayerTag(), 0.78f, 0.84f, 0.05f, 26f, MP_TEXT_DIM, FontStyles.Normal);

    // Quick Match card — dominant visual
    var card = MPCard(screen, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.76f));
    MPTextBand(card, "🌍  QUICK MATCH",   0.70f, 0.92f, 0.04f, 48f, MP_TEXT,     FontStyles.Bold);
    MPTextBand(card, "Match with a random player online", 0.48f, 0.68f, 0.06f, 26f, MP_TEXT_DIM, FontStyles.Normal);
    MPButton(card, "PLAY NOW  ▶",
        anchorMin: new Vector2(0.06f, 0.08f),
        anchorMax: new Vector2(0.94f, 0.44f),
        color: MP_RED, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            ShowMPScreen(_mpScreenSearch);
            if (_mpSearchError != null) _mpSearchError.text = "";
            _mpDotsCoroutine = StartCoroutine(AnimateDots(_mpSearchStatus, "Searching for opponent"));
            _mpSearchTimerCoroutine = StartCoroutine(MPTimerCoroutine(60, _mpSearchCountdown));
            string user = PlayerPrefs.GetString("USERNAME", "Player");
            MultiplayerManager.Instance?.QuickPlay(user,
                status => { if (_mpSearchStatus != null) _mpSearchStatus.text = status; },
                err =>
                {
                    // Show error on the search screen — never flash back to main.
                    if (_mpDotsCoroutine != null) { StopCoroutine(_mpDotsCoroutine); _mpDotsCoroutine = null; }
                    if (_mpSearchTimerCoroutine != null) { StopCoroutine(_mpSearchTimerCoroutine); _mpSearchTimerCoroutine = null; }
                    if (_mpSearchStatus  != null) _mpSearchStatus.text  = "Could not connect";
                    if (_mpSearchCountdown != null) _mpSearchCountdown.text = "";
                    if (_mpSearchError   != null)
                        _mpSearchError.text = err == "Firebase not ready"
                            ? "No connection — check your internet and try again"
                            : err == "Matchmaking error"
                            ? "Server error — check Firebase matchmaking rules, then retry"
                            : err;
                    Debug.LogWarning("[MP] QuickPlay error: " + err);
                });
        });

    MPDivider(screen, 0.40f, "─────  OR PLAY WITH A FRIEND  ─────");

    MPButton(screen, "⚔   CREATE PRIVATE ROOM",
        anchorMin: new Vector2(0.05f, 0.24f),
        anchorMax: new Vector2(0.95f, 0.37f),
        color: MP_BLUE, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            string user = PlayerPrefs.GetString("USERNAME", "Player");
            ShowMPScreen(_mpScreenHost);
            _mpHostCodeLabel.text = "- - - -";
            if (MultiplayerManager.Instance == null)
            {
                ShowMPScreen(_mpScreenMain); return;
            }
            MultiplayerManager.Instance.CreateRoom(user,
                code =>
                {
                    if (_mpHostCodeLabel != null) _mpHostCodeLabel.text = code;
                    _mpHostTimerCoroutine = StartCoroutine(MPTimerCoroutine(300, _mpHostCountdown));
                },
                err => ShowMPScreen(_mpScreenMain));
        });

    MPButton(screen, "🔑   JOIN WITH ROOM CODE",
        anchorMin: new Vector2(0.05f, 0.07f),
        anchorMax: new Vector2(0.95f, 0.20f),
        color: MP_GREEN, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            _mpJoinInput.text = "";
            _mpJoinError.text = "";
            ShowMPScreen(_mpScreenJoin);
        });

    return screen;
}

// ── SCREEN: Searching ─────────────────────────────────────────────────
//
//  ┌─────────────────────────────────────┐
//  │            FINDING MATCH           │  header  84-100%
//  ├─────────────────────────────────────┤
//  │              🎣                     │  icon    68-76%
//  │  Searching for opponent ●●●         │  status  56-67%
//  │           0:58                      │  timer   42-56% (gold, large)
//  │  A bot steps in when time runs out  │  caption 32-42%
//  │         [ ✕  CANCEL ]              │  btn     07-18%
//  └─────────────────────────────────────┘

GameObject BuildScreenSearch()
{
    var screen = MPMakeContainer(_mpPanel, "Screen_Search");

    MPHeader(screen, "FINDING MATCH", () =>
    {
        HapticFeedback.PlayCoinHaptic();
        MultiplayerManager.Instance?.AbortMultiplayer();
        ShowMPScreen(_mpScreenMain);
    });

    MPTextBand(screen, "🎣", 0.68f, 0.76f, 0.30f, 52f, MP_TEXT, FontStyles.Normal);

    _mpSearchStatus = MPTextBand(screen, "Searching for opponent ●",
        0.56f, 0.67f, 0.05f, 30f, MP_TEXT, FontStyles.Normal);

    _mpSearchCountdown = MPTextBand(screen, "1:00",
        0.42f, 0.56f, 0.15f, 72f, MP_GOLD, FontStyles.Bold);

    MPTextBand(screen, "A bot steps in when time runs out",
        0.32f, 0.42f, 0.05f, 24f, MP_TEXT_DIM, FontStyles.Normal);

    // Error label — shown in-place when Firebase or matchmaking fails.
    // Hidden by default; set via MPSearchShowError().
    _mpSearchError = MPTextBand(screen, "",
        0.22f, 0.30f, 0.05f, 24f, new Color(1f, 0.45f, 0.45f), FontStyles.Normal);

    MPButton(screen, "✕   CANCEL",
        anchorMin: new Vector2(0.20f, 0.07f),
        anchorMax: new Vector2(0.80f, 0.18f),
        color: MP_CANCEL, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            MultiplayerManager.Instance?.AbortMultiplayer();
            ShowMPScreen(_mpScreenMain);
        });

    return screen;
}

// ── SCREEN: Host (waiting with code) ─────────────────────────────────
//
//  ┌─────────────────────────────────────┐
//  │           PRIVATE ROOM             │  header  84-100%
//  │  🇮🇳  You: Vivek                  │  identity 78-84%
//  ├─────────────────────────────────────┤
//  │  Share this code with your friend   │  68-76%
//  │  ╔══════════════════════════════╗  │
//  │  ║        K 7 Q M              ║  │  code card 46-66%
//  │  ╚══════════════════════════════╝  │
//  │  Waiting for your friend to join…  │  36-44%
//  │           4:52                      │  timer    26-36% (gold)
//  │         [ ✕  CANCEL ]              │  btn      07-18%
//  └─────────────────────────────────────┘

GameObject BuildScreenHost()
{
    var screen = MPMakeContainer(_mpPanel, "Screen_Host");

    MPHeader(screen, "PRIVATE ROOM", () =>
    {
        HapticFeedback.PlayCoinHaptic();
        MultiplayerManager.Instance?.AbortMultiplayer();
        ShowMPScreen(_mpScreenMain);
    });

    MPTextBand(screen, "You:  " + MPPlayerTag(), 0.78f, 0.84f, 0.05f, 26f, MP_TEXT_DIM, FontStyles.Normal);
    MPTextBand(screen, "Share this code with your friend", 0.68f, 0.76f, 0.05f, 28f, MP_TEXT, FontStyles.Normal);

    var codeCard = MPCard(screen, new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.66f));
    _mpHostCodeLabel = MPTextBand(codeCard, "- - - -", 0.15f, 0.85f, 0.04f, 96f, MP_GOLD, FontStyles.Bold);
    _mpHostCodeLabel.characterSpacing = 20f;

    MPTextBand(screen, "Waiting for your friend to join…", 0.36f, 0.44f, 0.05f, 26f, MP_TEXT_DIM, FontStyles.Normal);

    _mpHostCountdown = MPTextBand(screen, "5:00", 0.26f, 0.36f, 0.20f, 60f, MP_GOLD, FontStyles.Bold);

    MPButton(screen, "✕   CANCEL",
        anchorMin: new Vector2(0.20f, 0.07f),
        anchorMax: new Vector2(0.80f, 0.18f),
        color: MP_CANCEL, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            MultiplayerManager.Instance?.AbortMultiplayer();
            ShowMPScreen(_mpScreenMain);
        });

    return screen;
}

// ── SCREEN: Join (enter code) ─────────────────────────────────────────
//
//  ┌─────────────────────────────────────┐
//  │              JOIN ROOM             │  header  84-100%
//  ├─────────────────────────────────────┤
//  │  Enter your friend's room code      │  72-80%
//  │  ╔══════════════════════════════╗  │
//  │  ║  _ _ _ _                    ║  │  input card 50-70%
//  │  ╚══════════════════════════════╝  │
//  │  [error text]                       │  43-50%
//  │  [ JOIN ROOM  ▶ ]                  │  28-41%
//  └─────────────────────────────────────┘

GameObject BuildScreenJoin()
{
    var screen = MPMakeContainer(_mpPanel, "Screen_Join");

    MPHeader(screen, "JOIN ROOM", () =>
    {
        HapticFeedback.PlayCoinHaptic();
        ShowMPScreen(_mpScreenMain);
    });

    MPTextBand(screen, "Enter your friend's room code", 0.72f, 0.80f, 0.05f, 28f, MP_TEXT, FontStyles.Normal);

    var inputCard = MPCard(screen, new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.70f));
    _mpJoinInput = MPCodeInput(inputCard);

    _mpJoinError = MPTextBand(screen, "", 0.43f, 0.50f, 0.05f, 24f, new Color(1f, 0.4f, 0.4f), FontStyles.Normal);

    MPButton(screen, "JOIN ROOM  ▶",
        anchorMin: new Vector2(0.05f, 0.28f),
        anchorMax: new Vector2(0.95f, 0.41f),
        color: MP_GREEN, onClick: () =>
        {
            HapticFeedback.PlayCoinHaptic();
            string code = _mpJoinInput.text.Trim().ToUpper();
            if (code.Length != 4)
            {
                _mpJoinError.text = "Room codes are 4 letters — try again";
                return;
            }
            _mpJoinError.text = "";
            string user = PlayerPrefs.GetString("USERNAME", "Player");
            MultiplayerManager.Instance?.JoinRoom(code, user,
                () => _mpPanel.SetActive(false),
                err =>
                {
                    _mpJoinError.text = err == "Room not found"
                        ? "That room doesn't exist — check the code"
                        : err == "Room already started"
                        ? "That match has already started"
                        : err;
                });
        });

    return screen;
}

// ────────────────────────────────────────────────────────────────────
// PRIMITIVE BUILDERS — all layout is pure anchors
// ────────────────────────────────────────────────────────────────────

void CloseMPPanel()
{
    MultiplayerManager.Instance?.AbortMultiplayer();
    if (_mpPanel != null) _mpPanel.SetActive(false);
}

GameObject MPMakeFullscreen(GameObject parent, string name, Color bg)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent.transform, false);
    go.AddComponent<Image>().color = bg;
    var rt = go.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = rt.offsetMax = Vector2.zero;
    return go;
}

GameObject MPMakeContainer(GameObject parent, string name)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent.transform, false);
    var rt = go.AddComponent<RectTransform>();
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = rt.offsetMax = Vector2.zero;
    return go;
}

void MPHeader(GameObject screen, string title, System.Action onBack)
{
    var header = new GameObject("Header");
    header.transform.SetParent(screen.transform, false);
    header.AddComponent<Image>().color = new Color(0.06f, 0.14f, 0.25f, 1f);
    var hRT = header.GetComponent<RectTransform>();
    hRT.anchorMin = new Vector2(0f, 0.84f); hRT.anchorMax = new Vector2(1f, 1f);
    hRT.offsetMin = hRT.offsetMax = Vector2.zero;

    var backBtn = new GameObject("BackBtn");
    backBtn.transform.SetParent(header.transform, false);
    backBtn.AddComponent<Image>().color = new Color(1f,1f,1f,0f);
    backBtn.AddComponent<Button>().onClick.AddListener(() => onBack());
    var bRT = backBtn.GetComponent<RectTransform>();
    bRT.anchorMin = new Vector2(0f, 0f); bRT.anchorMax = new Vector2(0f, 1f);
    bRT.pivot = new Vector2(0f, 0.5f);
    bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
    bRT.sizeDelta = new Vector2(140f, 0f);

    var backLbl = new GameObject("BackLbl");
    backLbl.transform.SetParent(backBtn.transform, false);
    var bTxt = backLbl.AddComponent<TextMeshProUGUI>();
    bTxt.text = "‹  Back"; bTxt.fontSize = 32f; bTxt.color = MP_TEXT_DIM;
    bTxt.alignment = TextAlignmentOptions.MidlineLeft;
    var bLRT = bTxt.GetComponent<RectTransform>();
    bLRT.anchorMin = Vector2.zero; bLRT.anchorMax = Vector2.one;
    bLRT.offsetMin = new Vector2(24f, 0f); bLRT.offsetMax = Vector2.zero;

    var titleGO = new GameObject("HeaderTitle");
    titleGO.transform.SetParent(header.transform, false);
    var tTxt = titleGO.AddComponent<TextMeshProUGUI>();
    tTxt.text = title; tTxt.fontSize = 40f; tTxt.color = MP_TEXT;
    tTxt.fontStyle = FontStyles.Bold; tTxt.alignment = TextAlignmentOptions.Center;
    var tRT = tTxt.GetComponent<RectTransform>();
    tRT.anchorMin = new Vector2(0.15f, 0f); tRT.anchorMax = new Vector2(0.85f, 1f);
    tRT.offsetMin = tRT.offsetMax = Vector2.zero;

    var sep = new GameObject("Separator");
    sep.transform.SetParent(screen.transform, false);
    sep.AddComponent<Image>().color = MP_DIVIDER;
    var sRT = sep.GetComponent<RectTransform>();
    sRT.anchorMin = new Vector2(0f, 0.84f); sRT.anchorMax = new Vector2(1f, 0.84f);
    sRT.offsetMin = Vector2.zero; sRT.offsetMax = Vector2.zero;
    sRT.sizeDelta = new Vector2(0f, 2f);
}

GameObject MPCard(GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
{
    var go = new GameObject("Card");
    go.transform.SetParent(parent.transform, false);
    go.AddComponent<Image>().color = MP_BG_CARD;
    var rt = go.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
    rt.offsetMin = rt.offsetMax = Vector2.zero;
    return go;
}

void MPButton(GameObject parent, string label, Vector2 anchorMin, Vector2 anchorMax,
              Color color, System.Action onClick)
{
    var go = new GameObject("Btn_" + label.Substring(0, Mathf.Min(10, label.Length)));
    go.transform.SetParent(parent.transform, false);
    go.AddComponent<Image>().color = color;
    go.AddComponent<Button>().onClick.AddListener(() => onClick());
    var rt = go.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
    rt.offsetMin = rt.offsetMax = Vector2.zero;

    var lGO = new GameObject("Label");
    lGO.transform.SetParent(go.transform, false);
    var tmp = lGO.AddComponent<TextMeshProUGUI>();
    tmp.text = label; tmp.fontSize = 40f;
    tmp.fontStyle = FontStyles.Bold; tmp.color = MP_TEXT;
    tmp.alignment = TextAlignmentOptions.Center;
    var lRT = tmp.GetComponent<RectTransform>();
    lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
    lRT.offsetMin = lRT.offsetMax = Vector2.zero;
}

// Responsive stretch-anchor text. yMin/yMax are 0-1 fractions of parent.
// xPad is horizontal padding fraction applied to both sides.
TextMeshProUGUI MPTextBand(GameObject parent, string text,
    float yMin, float yMax, float xPad,
    float fontSize, Color color, FontStyles style)
{
    var go = new GameObject("Txt_" + text.Substring(0, Mathf.Min(12, text.Length)));
    go.transform.SetParent(parent.transform, false);
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
    tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
    tmp.textWrappingMode = TextWrappingModes.Normal;
    var rt = tmp.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(xPad, yMin);
    rt.anchorMax = new Vector2(1f - xPad, yMax);
    rt.offsetMin = rt.offsetMax = Vector2.zero;
    return tmp;
}

// Legacy point-anchor text — kept for compatibility with leaderboard callers
TextMeshProUGUI MPText(GameObject parent, string text,
    Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta,
    float fontSize, Color color, FontStyles style)
{
    var go = new GameObject("Txt_" + text.Substring(0, Mathf.Min(12, text.Length)));
    go.transform.SetParent(parent.transform, false);
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
    tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
    tmp.textWrappingMode = TextWrappingModes.Normal;
    var rt = tmp.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.sizeDelta = sizeDelta;
    return tmp;
}

void MPDivider(GameObject parent, float anchorY, string label)
{
    var line = new GameObject("Divider");
    line.transform.SetParent(parent.transform, false);
    line.AddComponent<Image>().color = MP_DIVIDER;
    var lRT = line.GetComponent<RectTransform>();
    lRT.anchorMin = new Vector2(0.05f, anchorY); lRT.anchorMax = new Vector2(0.95f, anchorY);
    lRT.sizeDelta = new Vector2(0f, 2f);

    var lbl = new GameObject("DividerLabel");
    lbl.transform.SetParent(parent.transform, false);
    var tmp = lbl.AddComponent<TextMeshProUGUI>();
    tmp.text = label; tmp.fontSize = 24f; tmp.color = MP_TEXT_DIM;
    tmp.alignment = TextAlignmentOptions.Center;
    var rt = tmp.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.1f, anchorY - 0.025f);
    rt.anchorMax = new Vector2(0.9f, anchorY + 0.025f);
    rt.offsetMin = rt.offsetMax = Vector2.zero;

    var bg = new GameObject("DividerBG");
    bg.transform.SetParent(parent.transform, false);
    bg.AddComponent<Image>().color = MP_BG_DEEP;
    var bRT = bg.GetComponent<RectTransform>();
    bRT.anchorMin = new Vector2(0.22f, anchorY - 0.02f);
    bRT.anchorMax = new Vector2(0.78f, anchorY + 0.02f);
    bRT.offsetMin = bRT.offsetMax = Vector2.zero;
    lbl.transform.SetAsLastSibling();
}

TMP_InputField MPCodeInput(GameObject parent)
{
    var go = new GameObject("CodeInput");
    go.transform.SetParent(parent.transform, false);
    go.AddComponent<Image>().color = new Color(0f,0f,0f,0f);
    var input = go.AddComponent<TMP_InputField>();
    input.characterLimit = 4;
    input.contentType = TMP_InputField.ContentType.Alphanumeric;
    var rt = go.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = new Vector2(20f, 10f); rt.offsetMax = new Vector2(-20f, -10f);

    var txtGO = new GameObject("Text");
    txtGO.transform.SetParent(go.transform, false);
    var txt = txtGO.AddComponent<TextMeshProUGUI>();
    txt.fontSize = 88f; txt.color = MP_GOLD; txt.fontStyle = FontStyles.Bold;
    txt.alignment = TextAlignmentOptions.Center; txt.characterSpacing = 18f;
    var tRT = txt.GetComponent<RectTransform>();
    tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
    tRT.offsetMin = tRT.offsetMax = Vector2.zero;
    input.textComponent = txt;

    var phGO = new GameObject("Placeholder");
    phGO.transform.SetParent(go.transform, false);
    var ph = phGO.AddComponent<TextMeshProUGUI>();
    ph.text = "_ _ _ _"; ph.fontSize = 88f; ph.characterSpacing = 18f;
    ph.color = new Color(1f, 0.84f, 0f, 0.3f); ph.alignment = TextAlignmentOptions.Center;
    var pRT = ph.GetComponent<RectTransform>();
    pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
    pRT.offsetMin = pRT.offsetMax = Vector2.zero;
    input.placeholder = ph;

    return input;
}

// Cycles ● ●● ●●● on baseText. Runs indefinitely until coroutine is stopped.
IEnumerator AnimateDots(TextMeshProUGUI label, string baseText, bool dots = true)
{
    string[] frames = dots
        ? new[]{ baseText + " ●", baseText + " ●●", baseText + " ●●●", baseText + " ●●" }
        : new[]{ baseText };
    int i = 0;
    while (true)
    {
        if (label != null) label.text = frames[i % frames.Length];
        i++;
        yield return new WaitForSecondsRealtime(0.5f);
    }
}

// Counts down from `seconds` to 0, updating label as "M:SS". Stops naturally at 0.
IEnumerator MPTimerCoroutine(int seconds, TextMeshProUGUI label)
{
    int remaining = seconds;
    while (remaining >= 0)
    {
        if (label != null)
            label.text = $"{remaining / 60}:{remaining % 60:D2}";
        yield return new WaitForSecondsRealtime(1f);
        remaining--;
    }
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
        if (_topBarUsernameLabel != null) _topBarUsernameLabel.text = username;
    }
    else
    {
        // Username already exists
        playerUsername = GameBootstrap.Instance.GetUsername();
        if (_topBarUsernameLabel != null) _topBarUsernameLabel.text = playerUsername;
    }

    if (usernamePanel != null)
        usernamePanel.SetActive(false);

    if (GameBootstrap.Instance != null)
        GameBootstrap.Instance.StartGame();
}

public void HideStartMenuElements()
{
    if (_topBarRoot     != null) _topBarRoot.SetActive(false);
    if (_mainButtonsRoot != null) _mainButtonsRoot.SetActive(false);
    if (_bottomNavBar   != null) _bottomNavBar.SetActive(false);
    if (usernamePanel   != null) usernamePanel.SetActive(false);
}

public void ShowStartMenuElements()
{
    if (_topBarRoot     != null) _topBarRoot.SetActive(true);
    if (_mainButtonsRoot != null) _mainButtonsRoot.SetActive(true);
    if (_bottomNavBar   != null) _bottomNavBar.SetActive(true);
    // usernamePanel only shown when no username set
    if (usernamePanel != null && !PlayerPrefs.HasKey("USERNAME"))
        usernamePanel.SetActive(true);
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
titleRect.anchoredPosition = new Vector2(0, -12);
titleRect.sizeDelta = new Vector2(Screen.width * 0.80f, Screen.height * 0.055f);

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
    // Text top must be below tab bottom.
    // titleH = Screen.height*0.055, tabH = Screen.height*0.052
    // titleBottom from panel top = 12 + titleH
    // tabBottom from panel top   = 12 + titleH + tabH + 8 + tabH = 12 + titleH + 2*tabH + 8
    // text starts 10px below tab bottom → offsetMax.y = -(12 + titleH + 2*tabH + 8 + 10)
    float _titleH = Screen.height * 0.055f;
    float _tabH   = Screen.height * 0.052f;
    float _topInset = 12f + _titleH + _tabH + 8f + _tabH * 0.5f + 10f;
    // offsetMin.y = 100: bottom padding above restart button
    textRect.offsetMin = new Vector2(70, 100);
    textRect.offsetMax = new Vector2(-70, -_topInset);

    // Top of text area in panel local coords (panel centre = 0)
    float textAreaTop = panelH * 0.5f - _topInset;
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

    // Parent to leaderboard panel so it sits inside it, not floating on canvas
    if (leaderboardPanel != null)
        buttonGO.transform.SetParent(leaderboardPanel.transform, false);
    else
        buttonGO.transform.SetParent(canvas.transform, false);

    Image buttonImage = buttonGO.AddComponent<Image>();
    Sprite restartSprite = Resources.Load<Sprite>("btn_restart");
    if (restartSprite != null) { buttonImage.sprite = restartSprite; buttonImage.preserveAspect = true; }
    else buttonImage.color = new Color(0.10f, 0.75f, 0.20f);

    RectTransform rect = buttonGO.GetComponent<RectTransform>();
    // Anchor to bottom-centre of the leaderboard panel
    rect.anchorMin = new Vector2(0.5f, 0f);
    rect.anchorMax = new Vector2(0.5f, 0f);
    rect.pivot     = new Vector2(0.5f, 0f);
    rect.sizeDelta        = new Vector2(340f, 90f);
    rect.anchoredPosition = new Vector2(0f, 18f);

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

// Called by GameBootstrap.StartMultiplayerRound() to guarantee the MP panel is
// fully hidden before gameplay begins — regardless of match-found overlay timing.
public void HideMP()
{
    if (_mpPanel != null) _mpPanel.SetActive(false);
}

// Called from the MP game-over "Play Again" button to re-open the lobby
public void OpenMultiplayerLobby()
{
    // Find and hide the game-over panel first
    GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
    if (bootstrap != null)
    {
        bootstrap.HideGameOverUI();
        if (bootstrap.gameOverPanel != null) bootstrap.gameOverPanel.SetActive(false);
        if (bootstrap.restartButton  != null) bootstrap.restartButton.SetActive(false);
        if (bootstrap.reviveButton   != null) bootstrap.reviveButton.SetActive(false);
    }
    BuildMPPanelIfNeeded();
    ShowMPScreen(_mpScreenMain);
    _mpPanel.SetActive(true);
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