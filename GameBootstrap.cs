using UnityEngine;
using UnityEngine.UI;
using TMPro; // make sure you have this
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Globalization;


public class GameBootstrap : MonoBehaviour
{
    [Header("Game Over Image")]
public Sprite gameOverSprite;        // assign in Inspector
private GameObject gameOverImageObj; // runtime instance
    private TextMeshProUGUI gameOverText;
    private TextMeshProUGUI gameOverScoreText;  // score on game over panel
    public GameObject gameOverPanel; // unified panel containing GO text + buttons
    float groundTop;
    public Sprite gameLogoSprite;
    float lastGapCenter = 1.5f;
    int hardPipeCounter = 0;

public int reliefPipeFrequency = 5;
    bool forceHighNext = false;
    bool isReliefPipe = false;
    public GameObject reviveButton;
    GameObject ground1;
GameObject ground2;
float groundWidth;

public GameObject restartButton;
    public bool wasRevived = false;
    private Coroutine _reviveTimeoutCoroutine = null;
    private AudioSource bgMusicSource = null;
    private TextMeshProUGUI newRunText;
    GameObject ground;
    Image gameLogo;
    TextMeshProUGUI bestScoreText;
    GameObject bestScoreCard;
    private TextMeshProUGUI restartText;
    //private GameObject pauseButtonObj; // gravity multiplier when falling
   // private TextMeshProUGUI pauseText;
    public Sprite playerSadSprite;
    private TextMeshProUGUI highScoreText;
    [Header("Sprites")]
    public Sprite playerSprite;
    //private bool isPaused = false;
    TextMeshProUGUI usernameText;
    
     public int Score = 0;
     Vector3 originalPlayerScale;
     private Canvas mainCanvas;
    public float minGap = 3.0f;       // smallest possible gap
public float maxGap = 3.9f;       // largest possible gap
public float easyStartGap = 2.7f; // guaranteed easier start
//public float gapDecreaseRate = 0.005f; // how much gap shrinks per pipe (difficulty)
    private BubbleSpawner spawner;
    private float tapBounceTimer = 0f;
private float tapBounceSpeed = 2f;
private float tapBounceAmount = 20f; // pixels


    public string playerUsername = "Player";
      public TMPro.TMP_Text scoreText;
      private float lastCenterY = 0f;
     public BubbleSpawner bubbleSpawner;
    private GameObject bg1, bg2;
    //  private Button pauseButton;
   // private TextMeshProUGUI pauseButtonText;
    bool waitingForAd = false;
private float bgWidth = 20f;
public enum GameState
{
    Menu,
    WaitingForRevive,  // after revive ad — waiting for first tap
    Playing,
    GameOver
}

public GameState CurrentState = GameState.Menu;

    public static GameBootstrap Instance { get; private set; }
    public bool IsGameOver { get; private set; } = false;
    public Vector3 PlayerPosition => player != null ? player.transform.position : Vector3.zero;

   // public bool IsGameOver = false;
   public Sprite[] topSprites;
public Sprite[] bottomSprites;   // Rock / bottom
    public Sprite backgroundSprite;
    //public static GameBootstrap Instance;
    // Difficulty & gameplay scaling
private float baseGap = 3.2f;             // starting gap between pipes
private float maxCenterY = 1.5f;          // top limit for pipe Y
private float minCenterY = -1.5f;         // bottom limit for pipe Y
private float baseGravity = -9.8f;        // starting gravity
private float maxGravity = -14f;          // max gravity at highest difficulty
//public float DifficultyMultiplier = 0f;   // grows with score
 public TMP_FontAsset tmpFont; 
void Awake()
{
    Instance = this;

    // Ensure SkinSelectUI is available in the scene
    if (GetComponent<SkinSelectUI>() == null)
        gameObject.AddComponent<SkinSelectUI>();

    // Load TMP font
    tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    if (tmpFont == null)
    {
        ////debug.LogError("TMP Font Asset not found! Make sure TMP Essentials are imported!");
    }

StartCoroutine(GetCountryCode());
    // -----------------------------
    // 1. Ensure UI exists before gameplay
    // -----------------------------
    CreateAllUI();       // Score, High Score, Pause button
    
    CreateUIText();  
    CreateNewRunText();
    
    HideGameOverUI();    // Hide initially
CreateGameOverImage();
LoadUsername();
CreateUsernameText();
    
    CreatePlayer();
    CreateSpawner();


    // -----------------------------
    // 4. Background and bounds
    // -----------------------------
    CreateBackground();
    CreateBounds();
    CreateGameOverButtons();

    // -----------------------------
    // 5. Load high score
    // -----------------------------
    highScore = PlayerPrefs.GetInt("HIGH_SCORE", 0);

    ////debug.Log("[GameBootstrap] Awake complete. GameOver TMP texts initialized.");
}

    [Header("Gameplay")]
    public float pipeSpawnX = 12f;
    public float gapSize = 2.5f;
    public float pipeSpeed = 4f;
    public float backgroundSpeed = 1f;

    public GameObject player;
    private PlayerController playerScript;
   // private int score = 0;
    private int highScore = 0;

    // private Text scoreText;
    // private Text highScoreText;

    private float nextPipeX;
    [Header("Obstacle Settings")]
[Range(0.1f, 2f)]
public float obstacleVerticalScale = 0.7f; // 1 = original height, <1 = shorter, >1 = taller

    // Parallax background
    private GameObject bg;

    [System.Obsolete]
    void Start()
    {
        mainCanvas = Object.FindFirstObjectByType<Canvas>();
        Camera.main.orthographicSize = 9f;
    if(mainCanvas == null)
    {
        GameObject canvasGO = new GameObject("Canvas");
        mainCanvas = canvasGO.AddComponent<Canvas>();
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }
    lastGapCenter = 1.5f;
       CurrentState = GameState.Menu;
Time.timeScale = 0f;
    // Destroy any leftover pipes from previous session
    foreach (var m in FindObjectsByType<Moving>(FindObjectsSortMode.None))
        Destroy(m.gameObject);
////debug.Log("[Game] Starting in MENU state");
        CreateBackground();
        CreateBounds();
   CreateBestScoreText();
   CreateGameLogo();
   CreateGround();
   ////debug.Log("GROUND CREATED");


        highScore = PlayerPrefs.GetInt("HIGH_SCORE", 0);

        // Spawn first safe pipe
        SpawnPipe(5f);
        nextPipeX = pipeSpawnX + 4f;

        // Start spawning pipes repeatedly
        Invoke(nameof(SpawnPipeRepeated), 1f);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

     

        UpdateScoreText();
    }

 void Update()
{
   
  

UpdateGroundScroll();
UpdateTapToStart();
    if (bg != null)
    {
        bg.transform.position += Vector3.left * backgroundSpeed * Time.deltaTime;

        // Loop background
        if (bg.transform.position.x <= -20f)
            bg.transform.position = new Vector3(0, bg.transform.position.y, bg.transform.position.z);
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // ✅ stops play mode
#else
        Application.Quit();
#endif
    }
}


    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Score;
    }
  
// GameBootstrap.cs
void CreateBackground()
{
    GameObject bgParent = new GameObject("Backgrounds");

    Camera cam = Camera.main;

    float camHeight = cam.orthographicSize * 2f;
    float camWidth = camHeight * cam.aspect;
    Vector3 camCenter = cam.transform.position;

    // ---------- FIRST BACKGROUND ----------
    GameObject bg1 = new GameObject("BG1");
    SpriteRenderer sr1 = bg1.AddComponent<SpriteRenderer>();

    sr1.sprite = backgroundSprite != null
        ? backgroundSprite
        : CreateSprite(Color.cyan, camWidth, camHeight);

    sr1.sortingOrder = -10;

    float spriteWidth = sr1.sprite.bounds.size.x;
    float spriteHeight = sr1.sprite.bounds.size.y;

    // scale based on height so image stays proportional
float widthScale = camWidth / sr1.sprite.bounds.size.x;
float heightScale = camHeight / sr1.sprite.bounds.size.y;

float scale = Mathf.Max(widthScale, heightScale);
bg1.transform.localScale = new Vector3(scale, scale, 1);

    float scaledWidth = spriteWidth * scale;
    float scaledHeight = spriteHeight * scale;

    // move background upward so top reaches camera top
    float bgY = camCenter.y + (camHeight / 2f) - (scaledHeight / 2f);

    bg1.transform.position = new Vector3(camCenter.x, bgY, 10);
    bg1.transform.SetParent(bgParent.transform);

    // ---------- SECOND BACKGROUND ----------
    GameObject bg2 = new GameObject("BG2");
    SpriteRenderer sr2 = bg2.AddComponent<SpriteRenderer>();

    sr2.sprite = sr1.sprite;
    sr2.sortingOrder = -10;

    ////debug.Log("BG sprite bounds: " + sr1.sprite.bounds.size);
    ////debug.Log("BG scaled dimensions: " + scaledWidth + " x " + scaledHeight);

    bg2.transform.localScale = bg1.transform.localScale;

    // place exactly next to first background
    bg2.transform.position = new Vector3(camCenter.x + scaledWidth, bgY, 10);

    bg2.transform.SetParent(bgParent.transform);

    // ---------- PARALLAX ----------
    bgParent.AddComponent<ParallaxBackground>().speed = 0.5f;
}
 void CreatePlayer()
{
    // -----------------------------
    // Create Player
    // -----------------------------
    player = new GameObject("Player");
    player.tag = "Player";

    // -----------------------------
    // Add PlayerController
    // -----------------------------
    playerScript = player.AddComponent<PlayerController>();

    // -----------------------------
    // Add SpriteRenderer
    // -----------------------------
    var sr = player.AddComponent<SpriteRenderer>();
    sr.sprite = playerSprite != null ? playerSprite : CreateSprite(Color.yellow, 1f, 1f);

    // Apply selected skin immediately — overrides default inspector sprite
    Sprite selectedSpr = SkinManager.GetSelectedSprite();
    if (selectedSpr != null) sr.sprite = selectedSpr;
    sr.sortingOrder = 10; // <-- ensures player renders above obstacles

    // -----------------------------
    // Position player
    // -----------------------------
    player.transform.position = new Vector3(0f, 3f, 0f);

    // -----------------------------
    // Scale player
    // -----------------------------
    // Scale player to 1 unit, then apply a small visual multiplier
    // The collider stays at 1u — only the sprite appears larger
    float visualScale = 1.15f; // 15% bigger visually — safe for all difficulty tiers
    if (sr.sprite != null)
        player.transform.localScale = new Vector3(
            visualScale / sr.sprite.bounds.size.x,
            visualScale / sr.sprite.bounds.size.y, 1f);
    else
        player.transform.localScale = new Vector3(0.6f * visualScale, 0.6f * visualScale, 1f);
        originalPlayerScale = player.transform.localScale;

    // -----------------------------
    // Add AudioSource
    // -----------------------------
    var audioSource = player.AddComponent<AudioSource>();
    audioSource.playOnAwake = false;

    // ── BACKGROUND MUSIC ─────────────────────────────────
    GameObject musicGO = new GameObject("BackgroundMusic");
    AudioSource musicSource = musicGO.AddComponent<AudioSource>();
    musicSource.loop = true;
    musicSource.playOnAwake = false;
    musicSource.volume = 0.4f; // quieter than sound effects
    AudioClip musicClip = Resources.Load<AudioClip>("Sounds/BackgroundMusic");
    if (musicClip != null)
        musicSource.clip = musicClip;
    else
        Debug.LogWarning("BackgroundMusic not found in Resources/Sounds/");
    bgMusicSource = musicSource;
    DontDestroyOnLoad(musicGO);
    // var rb = player.AddComponent<Rigidbody2D>();
    // rb.gravityScale = 1f;
    // rb.freezeRotation = true;

    // ✅ Fin will still be created automatically inside PlayerController.Awake()
}

void CreateBounds()
    {
       CreateBound("Ground", new Vector3(0, -3.5f, 0), 20f, 1f);
       
        CreateBound("Ceiling", new Vector3(8, 8f, 0), 30f, 1f);
    }
    void CreateBound(string name, Vector3 pos, float width, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = pos;

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
        col.isTrigger = false;
        if (name == "Ground")
            obj.tag = "Ground";
        else if (name == "Ceiling")
            obj.tag = "Obstacle";
        
    }
    void UpdateTapToStart()
{
    if (CurrentState != GameState.Menu || gameOverText == null) return;

    tapBounceTimer += Time.deltaTime * tapBounceSpeed;
    float offset = Mathf.Sin(tapBounceTimer) * tapBounceAmount;

    gameOverText.rectTransform.anchoredPosition = new Vector2(0, 50 + offset);
}

 void SpawnPipeRepeated()
{
    if (IsGameOver) return;

    DifficultyTier tier = GetTier();

    // Smooth current speed toward tier target
    _currentSpeed = Mathf.Lerp(_currentSpeed, tier.pipeSpd, tierLerpSpeed * 8f);

    // CRITICAL: sync all existing pipes to current speed so they don't get
    // caught up by faster new pipes spawning behind them (causes overlap)
    foreach (var m in FindObjectsByType<Moving>(FindObjectsSortMode.None))
    {
        if (m != null) m.speed = _currentSpeed;
    }

    SpawnPipe(nextPipeX);

    // Spacing keeps pipes readable — tighter at high speed but never claustrophobic
    float spacing = Mathf.Clamp(_currentSpeed * 1.25f, 7f, 13f);
    nextPipeX += spacing;

    // Reschedule with the current tier's spawn interval (dynamic rate)
    CancelInvoke(nameof(SpawnPipeRepeated));
    Invoke(nameof(SpawnPipeRepeated), tier.spawnInterval);
}
float lastGap = 3.2f; // store outside method (class level)

void SpawnPipe(float spawnX)
{
    if (IsGameOver) return;

    DifficultyTier tier = GetTier();
    float difficultyMultiplier = GetDifficultyMultiplier(); // legacy compat

    float speedMultiplier = 0f; // kept for pipe mover

    GameObject pipeGroup = new GameObject("PipeGroup");
    pipeGroup.transform.position = new Vector3(spawnX, 0, 0);

    // =====================================================
    // GAP CONTROL — tier-based with smoothing
    // =====================================================

    float targetGap = tier.gap;

    // Smooth gap so it never jumps suddenly between tiers
    float currentGap = Mathf.Lerp(lastGap, targetGap, 0.25f);

    // Force easy gap for very first few seconds
    if (Time.timeSinceLevelLoad < 6f)
        currentGap = Mathf.Max(currentGap, 3.2f);

    // Hard floor — never smaller than master tier minimum
    currentGap = Mathf.Max(currentGap, 2.0f);
    lastGap = currentGap;

    // =====================================================
    // SCREEN + GROUND
    // =====================================================

    float screenTop = Camera.main.orthographicSize;

    float groundTop =
        ground.transform.position.y +
        ground.GetComponent<SpriteRenderer>().bounds.size.y / 2f;

    float topPadding = 0.15f;

    // =====================================================
    // GAP CENTER LIMITS
    // =====================================================
float bottomMargin = 1.2f;

// CUSTOM GAMEPLAY CEILING
float gameplayCeiling = 4.8f;

float minGapCenter = groundTop + currentGap / 2f + bottomMargin;

float maxGapCenter = gameplayCeiling - currentGap / 2f;

    float gapCenter;

    // =====================================================
    // RELIEF PIPE SYSTEM
    // =====================================================

    bool spawnReliefPipe = false;

    int reliefInterval = tier.reliefEvery;

    if (reliefInterval > 0) // 0 means no relief pipes (easy tier)
    {
        hardPipeCounter++;
        if (hardPipeCounter >= reliefInterval)
        {
            spawnReliefPipe = true;
            hardPipeCounter = 0;
        }
    }

    if (spawnReliefPipe)
    {
        isReliefPipe = true;

        gapCenter = Mathf.Lerp(
            minGapCenter,
            maxGapCenter,
            0.5f
        );

        currentGap += 0.4f;
    }
    else
    {
        isReliefPipe = false;

        if (Score < 15)
        {
            // Clamp within valid screen bounds so pipes always fill top/bottom
            gapCenter = Random.Range(minGapCenter, maxGapCenter);
        }
        else if (Score < 30)
        {
            gapCenter = Random.Range(
                minGapCenter + 1f,
                maxGapCenter - 1f
            );
        }
        else
        {
            if (forceHighNext)
            {
                gapCenter = Random.Range(
                    maxGapCenter - 1.5f,
                    maxGapCenter
                );
            }
            else
            {
                gapCenter = Random.Range(
                    minGapCenter,
                    minGapCenter + 1.5f
                );
            }

            forceHighNext = !forceHighNext;
        }
    }

    lastGapCenter = gapCenter;

    // =====================================================
    // SPECIAL PIPES
    // =====================================================

    if (Score > 30)
    {
        float specialChance = 0.25f;

        if (Random.value < specialChance)
        {
            if (Random.value < 0.5f)
            {
                gapCenter = minGapCenter;
            }
            else
            {
                gapCenter = maxGapCenter;
            }
        }
    }

    // =====================================================
    // GAP EDGES
    // =====================================================

    float gapTop =
        gapCenter + currentGap / 2f;

    float gapBottom =
        gapCenter - currentGap / 2f;

    GameObject topPipe = null;
    GameObject bottomPipe = null;

    // =====================================================
    // TOP PIPE
    // =====================================================

    Sprite topSprite = GetRandomTopSprite();

    if (topSprite != null)
    {
        topPipe = CreatePipe(
            "TopObstacle",
            Vector3.zero,
            topSprite,
            true
        );

        topPipe.tag = "Obstacle";

        float originalTopHeight =
            topPipe.GetComponent<SpriteRenderer>()
            .bounds.size.y;

        // Bottom edge of top pipe = gapTop
        float topPipeY = gapTop + (originalTopHeight / 2f);

        // Snap upward so top pipe always reaches/overlaps the screen top (no gap at ceiling)
        float screenTopEdge = Camera.main.orthographicSize;
        float topPipeTopEdge = topPipeY + (originalTopHeight / 2f);
        if (topPipeTopEdge < screenTopEdge)
            topPipeY += (screenTopEdge - topPipeTopEdge) + 0.1f; // 0.1f overshoot to hide seam

        topPipe.transform.position =
            new Vector3(
                spawnX,
                topPipeY,
                0
            );
        // Do NOT override Y scale — CreatePipe already set the correct scale
    }

    // =====================================================
    // BOTTOM PIPE
    // =====================================================

    Sprite bottomSprite = GetRandomBottomSprite();

    if (bottomSprite != null)
    {
        bottomPipe = CreatePipe(
            "BottomObstacle",
            Vector3.zero,
            bottomSprite,
            false
        );

        bottomPipe.tag = "Obstacle";

        float originalBottomHeight =
            bottomPipe.GetComponent<SpriteRenderer>()
            .bounds.size.y;

        // Top edge of bottom pipe = gapBottom
        float bottomPipeY = gapBottom - (originalBottomHeight / 2f);

        // Snap downward so bottom pipe always sits on/below the ground (no floating gap)
        float bottomPipeBottomEdge = bottomPipeY - (originalBottomHeight / 2f);
        if (bottomPipeBottomEdge > groundTop)
            bottomPipeY -= (bottomPipeBottomEdge - groundTop) + 0.1f; // 0.1f overshoot into ground

        bottomPipe.transform.position =
            new Vector3(
                spawnX,
                bottomPipeY,
                0
            );
    }

    // =====================================================
    // RELIEF VISUALS
    // =====================================================

    if (isReliefPipe)
    {
        ApplyReliefPipeVisual(topPipe);
        ApplyReliefPipeVisual(bottomPipe);
    }

    // =====================================================
    // MOVEMENT
    // =====================================================

    Moving moving = pipeGroup.AddComponent<Moving>();

    moving.speed = isReliefPipe
        ? _currentSpeed * 0.9f
        : _currentSpeed;

    // =====================================================
    // PARENTING
    // =====================================================

    if (topPipe != null)
        topPipe.transform.SetParent(
            pipeGroup.transform,
            true
        );

    if (bottomPipe != null)
        bottomPipe.transform.SetParent(
            pipeGroup.transform,
            true
        );

    // =====================================================
    // SCORE TRIGGER
    // =====================================================

    GameObject trigger = new GameObject("ScoreTrigger");

    trigger.transform.position =
        new Vector3(
            spawnX,
            gapCenter,
            0
        );

    trigger.transform.SetParent(
        pipeGroup.transform,
        true
    );

    var col = trigger.AddComponent<BoxCollider2D>();

    col.isTrigger = true;
    col.size = new Vector2(
        0.5f,
        currentGap
    );

    var st = trigger.AddComponent<ScoreTrigger>();
    st.bootstrap = this;

    // =====================================================
    // DYNAMIC GRAVITY
    // =====================================================

    // =====================================================
    // DYNAMIC GRAVITY — tier based, smoothed
    // =====================================================

    if (player != null)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Smoothly lerp toward tier target gravity scale
            _currentGravity = Mathf.Lerp(_currentGravity, tier.gravScale, 0.05f);
            rb.gravityScale  = _currentGravity;
        }
    }

    // =====================================================
    // DEBUG
    // =====================================================

    float topPipeBottomEdge =
        topPipe.transform.position.y -
        (
            topPipe.GetComponent<SpriteRenderer>()
            .bounds.size.y / 2f
        );

    float bottomPipeTopEdge =
        bottomPipe.transform.position.y +
        (
            bottomPipe.GetComponent<SpriteRenderer>()
            .bounds.size.y / 2f
        );
}
GameObject CreatePipe(string name, Vector3 pos, Sprite sprite, bool flipY)

{
    // -----------------------------
    // Create Pipe GameObject
    // -----------------------------
    GameObject pipe = new GameObject(name);
    
    pipe.transform.position = pos;
    pipe.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

    // -----------------------------    
    // Sprite Renderer
    // -----------------------------
    var sr = pipe.AddComponent<SpriteRenderer>();
    sr.sprite = sprite != null ? sprite : CreateSprite(Color.green, 1f, 5f);

// Tall enough to always cover from gap edge to screen edge (camera orthographicSize=9, full height=18)
float targetHeight = 12f;
// Fixed world width — keeps pipes a reasonable size on screen regardless of sprite aspect ratio
float targetWidth = 3.5f;

    Vector2 spriteSize = sr.sprite.bounds.size;

    float scaleY = targetHeight / spriteSize.y;
    float scaleX = targetWidth  / spriteSize.x;

    pipe.transform.localScale = flipY
        ? new Vector3(scaleX, -scaleY, 1f)
        : new Vector3(scaleX,  scaleY, 1f);

    // Main collider in LOCAL space — Unity multiplies by scaleX/scaleY automatically
    var col = pipe.AddComponent<BoxCollider2D>();
    col.size = spriteSize;
    col.offset = Vector2.zero;
    col.isTrigger = false;


    // -----------------------------
    // Rigidbody
    // -----------------------------
    var rb = pipe.AddComponent<Rigidbody2D>();
    rb.bodyType = RigidbodyType2D.Kinematic;
    rb.gravityScale = 0f;
    rb.freezeRotation = true;

    

    pipe.tag = "Obstacle";

    // ////debug.Log($"[Pipe] {name} created at {pos}, speed: {moving.speed}");

    return pipe;
}
// Optional: Draw colliders in editor for ////debugging
void OnDrawGizmos()
{
    var col = GetComponent<BoxCollider2D>();
    if (col != null)
    {
        Gizmos.color = Color.red;
        Vector3 pos = transform.position + (Vector3)col.offset;
        Vector3 size = new Vector3(col.size.x, col.size.y, 1f);
        Gizmos.DrawWireCube(pos, size);
    }

    var fCol = transform.Find("ForgivenessZone")?.GetComponent<BoxCollider2D>();
    if (fCol != null)
    {
        Gizmos.color = Color.yellow;
        Vector3 fPos = transform.position + (Vector3)fCol.offset;
        Vector3 fSize = new Vector3(fCol.size.x, fCol.size.y, 1f);
        Gizmos.DrawWireCube(fPos, fSize);
    }
}
Sprite CreateSprite(Color color, float width, float height)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

// GetBuiltinResource fails on Android builds — use this instead for rounded UI sprites
Sprite GetRoundedSprite()
{
    // Returns null on Android device (Image renders flat color — that's fine)
    return null;
}

// Same for particle circle sprite
Sprite GetCircleSprite()
{
    return null;
}

   
 public void AddScore(int points)
{
    if (IsGameOver) return;

    Score += points;

    // Award coins for passing pipe (includes milestone bonuses)
    SkinManager.OnPipePassed(Score);

    // Refresh coin display immediately after any coin change
    FirebaseGameManager fbCoin = FindObjectOfType<FirebaseGameManager>();
    if (fbCoin != null) fbCoin.RefreshCoinDisplay();

    // Single best score check — save only, coins awarded in TriggerGameOver
    int best = PlayerPrefs.GetInt("BestScore", 0);
    if (Score > best)
    {
        PlayerPrefs.SetInt("BestScore", Score);
        PlayerPrefs.Save();
    }

    // Refresh coin counter on start menu
    FirebaseGameManager fb = FindObjectOfType<FirebaseGameManager>();
    if (fb != null) fb.RefreshCoinDisplay();

    // ✅ Floating score effect (KEEP THIS)
    FloatingScoreManager fsm = Object.FindFirstObjectByType<FloatingScoreManager>();
    if (fsm != null)
        fsm.AddScore(points);

    // ✅ Single UI update source
    UpdateScoreText();

    // ✅ Sound
    if (playerScript != null)
        playerScript.PlayScoreSound();

    // Haptic tick on score
    HapticManager.Score();
}
public void TriggerGameOver()
{
    if (IsGameOver) return;



    IsGameOver = true;
    CurrentState = GameState.GameOver;

    // Stop background music on death
    if (bgMusicSource != null && bgMusicSource.isPlaying)
        bgMusicSource.Stop();

    CancelInvoke(nameof(SpawnPipeRepeated));

    // Hide score text so it doesn't bleed through the game over panel
    if (scoreText != null) scoreText.gameObject.SetActive(false);

    foreach (var moving in Object.FindObjectsOfType<Moving>())
        moving.enabled = false;

    if (player != null)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }

     Time.timeScale = 0.2f; // slow motion instead of freeze

    HideGameOverUI();

    // Show unified panel (contains GAME OVER text + buttons) — always on top
    if (gameOverPanel != null)
    {
        // Update score display before showing
        if (gameOverScoreText != null)
        {
            int best = PlayerPrefs.GetInt("BestScore", 0);
            if (Score >= best && Score > 0)
            {
                gameOverScoreText.text = "Score: " + Score + "\n<size=70%><color=#FFD740>NEW BEST!</color></size>";
                HapticManager.NewBest();     // triple pulse for new record
                SkinManager.OnNewBest();     // +3 coins — awarded ONCE per game
            }
            else
            {
                gameOverScoreText.text = "Score: " + Score + "   Best: " + best;
                HapticManager.Death();       // strong thud on normal death
            }
        }

        FirebaseGameManager firebase = FindObjectOfType<FirebaseGameManager>();
        bool hasRevived = firebase != null && firebase.hasRevivedThisRun;

        if (hasRevived)
        {
            // Player already revived this run — skip game over panel, go straight to leaderboard
            gameOverPanel.SetActive(false);
            if (firebase != null) firebase.OnGameOver(Score);
            firebase.ShowLeaderboardUI(Score);
            return;
        }

        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();

        bool canRevive = (firebase == null || !firebase.hasRevivedThisRun);
        if (reviveButton != null)  reviveButton.SetActive(canRevive);
        if (restartButton != null) restartButton.SetActive(true);
    }
    else
    {
        // Fallback if panel not created
        gameOverImageObj?.SetActive(true);
        gameOverImageObj?.transform.SetSiblingIndex(0);
        FirebaseGameManager firebase = FindObjectOfType<FirebaseGameManager>();
        bool canRevive = (firebase == null || !firebase.hasRevivedThisRun);
        if (reviveButton != null) { reviveButton.SetActive(canRevive); reviveButton.transform.SetAsLastSibling(); }
        if (restartButton != null) { restartButton.SetActive(true); restartButton.transform.SetAsLastSibling(); }
    }

    FirebaseGameManager fb = FindObjectOfType<FirebaseGameManager>();
    if (fb != null) fb.OnGameOver(Score);
}
private IEnumerator ShowGameOverImageDelayed(float delay)
{
    if (gameOverImageObj == null) yield break;
    yield return new WaitForSeconds(delay);
    gameOverImageObj.SetActive(true);
    gameOverImageObj.transform.SetSiblingIndex(0); // always behind buttons
}
IEnumerator FadeInGameOverUI()
{
    // gameOverText is now a child of gameOverPanel — just ensure it's visible
    if (gameOverText != null)
    {
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "GAME  OVER";
        gameOverText.color = new Color(1f, 0.35f, 0.1f, 1f);
    }
    yield return null;
}

public void StartGame()
{
    _currentSpeed   = 4.0f;
    _currentGravity = 1.0f;
    lastGap         = 3.2f;
    hardPipeCounter = 0;
    forceHighNext   = false;
    FirebaseGameManager manager = FindObjectOfType<FirebaseGameManager>();

    if (manager != null)

    {

        manager.hasRevivedThisRun = false;

    }
    wasRevived = false;
    CurrentState = GameState.Playing;

    // Start background music
    if (bgMusicSource != null && !bgMusicSource.isPlaying && bgMusicSource.clip != null)
        bgMusicSource.Play();
    FirebaseGameManager fbm = FindObjectOfType<FirebaseGameManager>();
    if (fbm != null) fbm.SetSkinsButtonVisible(false);

    // Apply chosen fish skin
    ApplySelectedSkin();

    if (bestScoreCard != null)
        bestScoreCard.SetActive(false);
    else if (bestScoreText != null)
        bestScoreText.gameObject.SetActive(false);
    if(scoreText != null)
        scoreText.gameObject.SetActive(true);
    IsGameOver = false;
if(gameLogo != null)
        gameLogo.gameObject.SetActive(false);
    Time.timeScale = 1f; // resume game
 CancelInvoke(nameof(SpawnPipeRepeated));

    Invoke(nameof(SpawnPipeRepeated), 1f);
    // Enable gravity on player
    if (player != null)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0.35f;
            ////debug.Log("[Game] Gravity enabled");
        }
    }
    // if (pauseButtonObj != null)
    // pauseButtonObj.SetActive(true);
}



void CreateUIText()
{
    // Game Over Text
    GameObject goObj = new GameObject("GameOverText");
    goObj.transform.SetParent(mainCanvas.transform, false);

    gameOverText = goObj.AddComponent<TextMeshProUGUI>();
    gameOverText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); // TMP font asset
    gameOverText.fontSize = 60;
    gameOverText.alignment = TextAlignmentOptions.Center;
    gameOverText.color = Color.red;
    gameOverText.text = "";

    RectTransform goRect = gameOverText.GetComponent<RectTransform>();
    goRect.anchorMin = new Vector2(0.5f, 0.5f);
    goRect.anchorMax = new Vector2(0.5f, 0.5f);
    goRect.pivot = new Vector2(0.5f, 0.5f);
    goRect.anchoredPosition = new Vector2(0, 50);
    goRect.sizeDelta = new Vector2(600, 200);

    // Restart Text
    GameObject restartObj = new GameObject("RestartText");
    restartObj.transform.SetParent(mainCanvas.transform, false);

    restartText = restartObj.AddComponent<TextMeshProUGUI>();
    restartText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    restartText.fontSize = 36;
    restartText.alignment = TextAlignmentOptions.Center;
    restartText.color = Color.white;
    restartText.text = "";

    RectTransform rtRect = restartText.GetComponent<RectTransform>();
    rtRect.anchorMin = new Vector2(0.5f, 0.5f);
    rtRect.anchorMax = new Vector2(0.5f, 0.5f);
    rtRect.pivot = new Vector2(0.5f, 0.5f);
    rtRect.anchoredPosition = new Vector2(0, -60);
    rtRect.sizeDelta = new Vector2(600, 100);

    // High Score Text
    GameObject hsObj = new GameObject("HighScoreText");
    hsObj.transform.SetParent(mainCanvas.transform, false);

    highScoreText = hsObj.AddComponent<TextMeshProUGUI>();
    highScoreText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    highScoreText.fontSize = 36;
    highScoreText.alignment = TextAlignmentOptions.Center;
    highScoreText.color = Color.yellow;
    highScoreText.text = "";

    RectTransform hsRect = highScoreText.GetComponent<RectTransform>();
    hsRect.anchorMin = new Vector2(0.5f, 0.5f);
    hsRect.anchorMax = new Vector2(0.5f, 0.5f);
    hsRect.pivot = new Vector2(0.5f, 0.5f);
    hsRect.anchoredPosition = new Vector2(0, -10);
    hsRect.sizeDelta = new Vector2(600, 100);
}
// =========================
// RestartGame – resets everything
// =========================

    // ===== Reset game flags =====
//   public void RestartGame()
// {
//     HideGameOverUI();

//     // 🔴 STOP OLD SPAWNING
//     CancelInvoke(nameof(SpawnPipeRepeated));

//     CurrentState = GameState.Menu;
//     Time.timeScale = 0f;
//     IsGameOver = false;
//     Score = 0;

//     // 🔴 RESET GAP MEMORY (CRITICAL)
//     lastGap = 3.2f;
//     lastGapCenter = 1.5f;

//     GameObject restartButton = GameObject.Find("RestartButton");
//     if (restartButton != null)
//         Destroy(restartButton);

//     if (gameOverImageObj != null)
//         gameOverImageObj.SetActive(false);

//     if (scoreText != null)
//         scoreText.text = "Score: 0";

//     if (playerScript != null)
//         playerScript.ResetPlayer(new Vector3(-2f, 0f, 0f));

//     // Restore sprite
//     if (player != null)
//     {
//         var sr = player.GetComponent<SpriteRenderer>();
//         if (sr != null && playerSprite != null)
//         {
//             sr.sprite = playerSprite;
//             player.transform.localScale = originalPlayerScale;
//         }
//     }

//     // 🔴 DESTROY ALL PIPES CLEANLY
//     foreach (var pipe in Object.FindObjectsOfType<Moving>())
//     {
//         Destroy(pipe.gameObject);
//     }

//     // 🔴 SPAWN FIRST PIPE (VISIBLE IN MENU)
//     nextPipeX = pipeSpawnX + 5f;
//     SpawnPipe(5f);
// }
public void RestartGame()
{
    CancelInvoke(nameof(SpawnPipeRepeated));
    FirebaseGameManager manager = FindObjectOfType<FirebaseGameManager>();

    // Hide skins button during gameplay
    if (manager != null) manager.SetSkinsButtonVisible(false);

if (manager != null)

{

    manager.hasRevivedThisRun = false;

}
if (reviveButton != null) reviveButton.SetActive(false);
if (restartButton != null) restartButton.SetActive(false);
    Time.timeScale = 1f;
    IsGameOver = false;
    CurrentState = GameState.WaitingForRevive; // frozen until first tap — set properly below

    Score = 0;
    UpdateScoreText();
    if (scoreText != null) scoreText.gameObject.SetActive(true);
    StartCoroutine(ShowNewRun());
    lastGap = 3.2f;
    lastGapCenter = 1.5f;
    _currentSpeed   = 4.0f;  // reset smoothed values
    _currentGravity = 1.0f;
    hardPipeCounter = 0;      // reset relief pipe counter
    forceHighNext   = false;  // reset alternating pipe pattern

    // 🔥 IMPORTANT FIX
    nextPipeX = pipeSpawnX + 4f;

    // Destroy old pipes (including ones hidden during leaderboard)
    foreach (var pipe in Object.FindObjectsOfType<Moving>(true))
        Destroy(pipe.gameObject);

    // Reset player — frozen until first tap, same as revive mechanism
    if (playerScript != null)
    {
        player.SetActive(true); // re-enable if hidden during leaderboard
        playerScript.StopAllCoroutines();
        playerScript.ResetPlayer(new Vector3(0f, 3f, 0f)); // same as first game start
        ResetPlayerVisual();
        playerScript.SetInvincible(10f); // long invincibility — cancelled on first tap

        // Freeze player completely until first tap
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;   // no falling at all
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Set WaitingForRevive so pipes freeze and player waits for first tap
    // PlayerController.Update handles the first tap and calls StartPipeSpawning()
    CurrentState = GameState.WaitingForRevive;

    // Start music ready for when player taps
    if (bgMusicSource != null && !bgMusicSource.isPlaying && bgMusicSource.clip != null)
        bgMusicSource.Play();

    // Cancel any pending spawns — pipes start on first tap
    CancelInvoke(nameof(SpawnPipeRepeated));

    HideGameOverUI();

    // Reapply selected skin so start screen shows correct fish
    ApplySelectedSkin();
}
Sprite GetRandomTopSprite()
{
    if (topSprites != null && topSprites.Length > 0)
        return topSprites[Random.Range(0, topSprites.Length)];

    return null;
}
public void SpawnBubbleFromBootstrap()
    {
      
    if (IsGameOver) return; // stop spawning after Game Over
    bubbleSpawner.SpawnBubble(PlayerPosition);
    }

Sprite GetRandomBottomSprite()
{
    if (bottomSprites != null && bottomSprites.Length > 0)
        return bottomSprites[Random.Range(0, bottomSprites.Length)];

    return null;
}
 void CreateSpawner()
{
    GameObject spawnerGO = new GameObject("BubbleSpawner");
    spawner = spawnerGO.AddComponent<BubbleSpawner>();
}
// Call this to spawn a quick particle burst
public ParticleSystem CreateParticleBurst(Vector3 position, Color color, int count = 15, float size = 0.2f, float lifetime = 0.5f)
{
    GameObject psObj = new GameObject("ParticleBurst");
    psObj.transform.position = position;

    ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

    var main = ps.main;
    main.startColor = color;
    main.startSize = size;
    main.startLifetime = lifetime;
    main.maxParticles = count;
    main.simulationSpace = ParticleSystemSimulationSpace.World;

    var emission = ps.emission;
    emission.rateOverTime = 0;
    emission.SetBursts(new ParticleSystem.Burst[] {
        new ParticleSystem.Burst(0f, (short)count)
    });

    var shape = ps.shape;
    shape.shapeType = ParticleSystemShapeType.Circle;
    shape.radius = 0.2f;

    var renderer = ps.GetComponent<ParticleSystemRenderer>();
    renderer.renderMode = ParticleSystemRenderMode.Billboard;
    Material mat = new Material(Shader.Find("Sprites/Default"));
    mat.color = color;
    Sprite circle = GetCircleSprite();
    if (circle != null) mat.mainTexture = circle.texture;
    renderer.material = mat;

    ps.Play();
    Destroy(psObj, lifetime + 0.2f);

    return ps;
}
// public float GetDifficultyMultiplier()
// {
//     return Mathf.Clamp(Score * 0.1f, 0f, 10f);
// }
// =====================================================
// 5-TIER DIFFICULTY SYSTEM
// =====================================================
// Tier 1  EASY   : Score  0-14  — learn the game
// Tier 2  MEDIUM : Score 15-29  — getting harder
// Tier 3  HARD   : Score 30-49  — serious challenge
// Tier 4  EXPERT : Score 50-74  — top players
// Tier 5  MASTER : Score 75+    — never-ending pressure
// =====================================================

public struct DifficultyTier
{
    public float pipeSpd;      // world units/sec
    public float gap;          // world units
    public float gravScale;    // rigidbody gravity scale
    public float spawnInterval;// seconds between spawns
    public int   reliefEvery;  // relief pipe every N pipes
}

public DifficultyTier GetTier()
{
    if (Score < 10)  return new DifficultyTier { pipeSpd=4.0f,  gap=3.00f, gravScale=1.00f, spawnInterval=1.50f, reliefEvery=0  }; // EASY      0-9
    if (Score < 20)  return new DifficultyTier { pipeSpd=4.8f,  gap=2.80f, gravScale=1.06f, spawnInterval=1.40f, reliefEvery=10 }; // MEDIUM    10-19
    if (Score < 35)  return new DifficultyTier { pipeSpd=5.6f,  gap=2.55f, gravScale=1.12f, spawnInterval=1.30f, reliefEvery=8  }; // HARD      20-34
    if (Score < 55)  return new DifficultyTier { pipeSpd=6.6f,  gap=2.30f, gravScale=1.20f, spawnInterval=1.18f, reliefEvery=6  }; // EXPERT    35-54
    if (Score < 85)  return new DifficultyTier { pipeSpd=7.4f,  gap=2.10f, gravScale=1.28f, spawnInterval=1.05f, reliefEvery=5  }; // MASTER    55-84
    if (Score < 130) return new DifficultyTier { pipeSpd=8.2f,  gap=1.95f, gravScale=1.34f, spawnInterval=0.95f, reliefEvery=4  }; // LEGEND    85-129
                     return new DifficultyTier { pipeSpd=9.0f,  gap=1.80f, gravScale=1.40f, spawnInterval=0.88f, reliefEvery=3  }; // GODLIKE   130+
}

// Smooth interpolation between current and target tier values
// so transitions feel gradual not sudden
float tierLerpSpeed = 0.08f; // per-frame blend — smooth over ~1 second

float _currentSpeed   = 4.0f;
float _currentGravity = 1.0f;

public float GetDifficultyMultiplier()
{
    // Keep for legacy callers — return a 0-8 equivalent
    DifficultyTier t = GetTier();
    return Mathf.InverseLerp(4.0f, 8.5f, t.pipeSpd) * 8f;
}
void CreateHighScoreText()
{
    GameObject hsObj = new GameObject("HighScoreText");
    hsObj.transform.SetParent(gameOverText.transform.parent, false);

    // Use TextMeshProUGUI instead of legacy Text
    highScoreText = hsObj.AddComponent<TextMeshProUGUI>();

    // Assign a TMP font asset (replace with any TMP font you have in Resources)
    highScoreText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    highScoreText.fontSize = 36;
    highScoreText.alignment = TextAlignmentOptions.Center;
    highScoreText.color = Color.yellow;
    highScoreText.text = ""; // hidden initially

   RectTransform rect = highScoreText.GetComponent<RectTransform>();

rect.anchorMin = new Vector2(0.5f, 1);
rect.anchorMax = new Vector2(0.5f, 1);
rect.pivot = new Vector2(0.5f, 1);

rect.anchoredPosition = new Vector2(0, -40);
rect.sizeDelta = new Vector2(400, 80);
}
// void TogglePause()
//     {
//         isPaused = !isPaused;
//         Time.timeScale = isPaused ? 0 : 1;
//         ////debug.Log(isPaused ? "Game Paused" : "Game Resumed");
//     }
 // make sure this is at top

// void CreatePauseText()
// {
//     GameObject canvasObj = mainCanvas.gameObject;

//     GameObject obj = new GameObject("PauseText");
//     obj.transform.SetParent(canvasObj.transform, false);

//     // Use TextMeshProUGUI
//     pauseText = obj.AddComponent<TextMeshProUGUI>();
//     pauseText.fontSize = 50;
//     pauseText.alignment = TextAlignmentOptions.Center;
//     pauseText.color = Color.yellow;
//     pauseText.text = ""; // hidden initially

//     RectTransform rt = pauseText.rectTransform;
//     rt.anchorMin = new Vector2(0.5f, 0.5f);
//     rt.anchorMax = new Vector2(0.5f, 0.5f);
//     rt.anchoredPosition = Vector2.zero;
//     rt.sizeDelta = new Vector2(600, 200);
// }


void CreateAllUI()
    {
        // Ensure Canvas exists
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ensure EventSystem exists
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ===== SCORE (Top Left) =====
        scoreText = CreateTMPText(
            "ScoreText",
            46,
            Color.white,
            new Vector2(0, 1),
            new Vector2(0, 1),
            new Vector2(20, -40),
            new Vector2(300, 80),
            new Vector2(0, 1)
        );
        scoreText.fontStyle  = FontStyles.Bold;
        scoreText.outlineColor = new Color(0f, 0f, 0f, 1f);
        scoreText.outlineWidth = 0.25f;

        scoreText.gameObject.SetActive(false); // hidden until game starts

        // ===== HIGH SCORE (Top Center) =====
        highScoreText = CreateTMPText(
            "HighScoreText",
            36,
            Color.yellow,
            new Vector2(0.5f, 1),
            new Vector2(0.5f, 1),
            new Vector2(0, -40),
            new Vector2(400, 80),
            new Vector2(0.5f, 1)
        );

//         // ===== PAUSE BUTTON (Top Right) =====
//        pauseButtonObj = new GameObject("PauseButton");
// GameObject pauseObj = pauseButtonObj;
//         pauseObj.transform.SetParent(mainCanvas.transform, false);
//         pauseObj.transform.SetAsLastSibling(); // ensure on top for clicks

//         Button pauseBtn = pauseObj.AddComponent<Button>();
//         Image img = pauseObj.AddComponent<Image>();
//         img.color = Color.gray;

//         RectTransform rect = pauseObj.GetComponent<RectTransform>();
//         rect.anchorMin = new Vector2(1, 1);
//         rect.anchorMax = new Vector2(1, 1);
//         rect.pivot = new Vector2(1, 1);
//         rect.anchoredPosition = new Vector2(-20, -40);
//         rect.sizeDelta = new Vector2(160, 80);

//         GameObject txtObj = new GameObject("PauseText");
//         txtObj.transform.SetParent(pauseObj.transform, false);

//         TextMeshProUGUI btnText = txtObj.AddComponent<TextMeshProUGUI>();
//         btnText.font = tmpFont;
//         btnText.text = "PAUSE";
//         btnText.fontSize = 32;
//         btnText.alignment = TextAlignmentOptions.Center;
//         btnText.color = Color.black;

//         RectTransform txtRect = btnText.GetComponent<RectTransform>();
//         txtRect.anchorMin = Vector2.zero;
//         txtRect.anchorMax = Vector2.one;
//         txtRect.offsetMin = Vector2.zero;
//         txtRect.offsetMax = Vector2.zero;

//         pauseBtn.onClick.AddListener(TogglePause);
    }

    // 10 units spacing below score
 TextMeshProUGUI CreateTMPText(string name, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(mainCanvas.transform, false);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.font = tmpFont;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return tmp;
    }


    // Call this when the player dies
    public void ShowGameOver(int score)
    {
        gameOverText.gameObject.SetActive(true);
        restartText.gameObject.SetActive(true);

        highScoreText.gameObject.SetActive(true);
        highScoreText.text = "High Score: " + score;

        gameOverText.text = "Game Over!";
        restartText.text = "Tap to Restart";
    }
    public void StartPipeSpawning()
    {
        CancelInvoke(nameof(SpawnPipeRepeated));
        Invoke(nameof(SpawnPipeRepeated), 1f);
    }

    public void HideGameOverUI()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);
        if (restartText != null) restartText.gameObject.SetActive(false);
        if (highScoreText != null) highScoreText.gameObject.SetActive(false);
        if (gameOverImageObj != null) gameOverImageObj.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (reviveButton != null) reviveButton.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        // Note: skins button visibility handled separately in RestartGame/StartGame
    }
    void CreateGameOverImage()
{
    if (mainCanvas == null)
    {
        ////debug.LogError("Main Canvas not found!");
        return;
    }

    gameOverImageObj = new GameObject("GameOverImage");
    gameOverImageObj.transform.SetParent(mainCanvas.transform, false);

    Image img = gameOverImageObj.AddComponent<Image>();
    img.sprite = gameOverSprite;
    img.color = Color.white; // can adjust if needed
    img.preserveAspect = true;

    RectTransform rt = img.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = Vector2.zero;
    rt.sizeDelta = new Vector2(400, 200); // adjust as needed

    gameOverImageObj.SetActive(false); // hide initially
}
public void SetGameOverImageSize(float width, float height)
{
    if (gameOverImageObj == null) return;

    RectTransform rt = gameOverImageObj.GetComponent<RectTransform>();
    rt.sizeDelta = new Vector2(width, height); // Set desired size
}
string GetOrCreatePlayerID()
{
    if (PlayerPrefs.HasKey("PLAYER_ID"))
        return PlayerPrefs.GetString("PLAYER_ID");

    string id = System.Guid.NewGuid().ToString();

    PlayerPrefs.SetString("PLAYER_ID", id);
    PlayerPrefs.Save();

    ////debug.Log("New PlayerID created: " + id);

    return id;
}
string GetShortID(string playerID)
{
    return playerID.Substring(playerID.Length - 4);
}
string CreatePlayerHandle(string username)
{
    string playerID = GetOrCreatePlayerID();

    string shortID = GetShortID(playerID);

    return username + "_" + shortID;
}
public void SaveUsername(string username)
{
    string country = "XX";

    try
    {
        country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
    }
    catch
    {
        ////debug.Log("Country detection failed");
    }

    string shortID = System.Guid.NewGuid().ToString().Substring(0,4);

    string finalUsername = country + "_" + username + "_" + shortID;

    PlayerPrefs.SetString("USERNAME", finalUsername);
    PlayerPrefs.Save();

    ////debug.Log("Username saved: " + finalUsername);
}

   void CreateUsernameText()
{
    GameObject canvasGO = mainCanvas.gameObject;

    // ── BADGE BACKGROUND ─────────────────────────────────────────────────
    GameObject badgeGO = new GameObject("ProfileBadge");
    badgeGO.transform.SetParent(canvasGO.transform, false);

    Image bg = badgeGO.AddComponent<Image>();
    Sprite rounded = GetRoundedSprite();
    if (rounded != null) { bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 0.35f; }
    bg.color = new Color(0.04f, 0.18f, 0.32f, 0.88f); // deep ocean blue-dark

    RectTransform rt = badgeGO.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(1, 1);
    rt.anchorMax        = new Vector2(1, 1);
    rt.pivot            = new Vector2(1, 1);
    rt.anchoredPosition = new Vector2(-16, -16);
    rt.sizeDelta        = new Vector2(230, 58);

    // ── FISH ICON (themed, no emoji needed) ──────────────────────────────
    GameObject iconGO = new GameObject("FishIcon");
    iconGO.transform.SetParent(badgeGO.transform, false);
    TextMeshProUGUI iconText = iconGO.AddComponent<TextMeshProUGUI>();
    iconText.font      = tmpFont;
    iconText.text      = "><>";   // ASCII fish — works with any font
    iconText.fontSize  = 22;
    iconText.color     = new Color(1f, 0.85f, 0.3f); // gold
    iconText.alignment = TextAlignmentOptions.Left;
    RectTransform iconRT = iconText.rectTransform;
    iconRT.anchorMin        = new Vector2(0, 0);
    iconRT.anchorMax        = new Vector2(0, 1);
    iconRT.pivot            = new Vector2(0, 0.5f);
    iconRT.anchoredPosition = new Vector2(10, 0);
    iconRT.sizeDelta        = new Vector2(46, 0);

    // ── USERNAME TEXT ─────────────────────────────────────────────────────
    GameObject textGO = new GameObject("UsernameText");
    textGO.transform.SetParent(badgeGO.transform, false);
    usernameText = textGO.AddComponent<TextMeshProUGUI>();
    usernameText.font         = tmpFont;
    usernameText.fontSize     = 26;
    usernameText.fontStyle    = FontStyles.Bold;
    usernameText.color        = Color.white;
    usernameText.alignment    = TextAlignmentOptions.Left;
    usernameText.textWrappingMode = TextWrappingModes.NoWrap;
    usernameText.overflowMode = TextOverflowModes.Ellipsis;

    RectTransform textRT = usernameText.rectTransform;
    textRT.anchorMin = new Vector2(0, 0);
    textRT.anchorMax = new Vector2(1, 1);
    textRT.offsetMin = new Vector2(58, 4);
    textRT.offsetMax = new Vector2(-10, -4);

    usernameText.text = playerUsername;
}

   
    void  LoadUsername()
{
    if (PlayerPrefs.HasKey("USERNAME"))
    {
        playerUsername = PlayerPrefs.GetString("USERNAME");
        ////debug.Log("Loaded username: " + playerUsername);
    }
}
public void RefreshUsernameText()
{
    playerUsername = PlayerPrefs.GetString("USERNAME", "Player");

    if (usernameText != null)
        usernameText.text = playerUsername;

    ////debug.Log("Username UI Updated: " + playerUsername);
}
[System.Serializable]
public class CountryData
{
    public string country_name;
    public string country_code;
}
public IEnumerator GetCountryCode()
{
    if (PlayerPrefs.HasKey("COUNTRY"))
        yield break;

    UnityWebRequest req = UnityWebRequest.Get("https://ipapi.co/json/");

    yield return req.SendWebRequest();

    if (req.result == UnityWebRequest.Result.Success)
    {
        CountryData data = JsonUtility.FromJson<CountryData>(req.downloadHandler.text);

        PlayerPrefs.SetString("COUNTRY", data.country_code);
        PlayerPrefs.Save();

        ////debug.Log("Country detected: " + data.country_code);
    }
    else
    {
        ////debug.Log("Country detection failed");
        PlayerPrefs.SetString("COUNTRY", "XX");
    }
}
public void OnPlayButton()
{
    StartGame();
}
GameObject menuPanel;

void CreateMenuUI()
{
    Canvas canvas = FindObjectOfType<Canvas>();

    if(canvas == null)
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    menuPanel = new GameObject("MenuPanel");
    menuPanel.transform.SetParent(canvas.transform,false);

    RectTransform rt = menuPanel.AddComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;

    CreateMenuTitle();
    CreatePlayButton();
}
void CreateMenuTitle()
{
    GameObject titleGO = new GameObject("Title");
    titleGO.transform.SetParent(menuPanel.transform,false);

    TextMeshProUGUI text = titleGO.AddComponent<TextMeshProUGUI>();
    text.text = "FLIPPY FISH";
    text.fontSize = 90;
    text.alignment = TextAlignmentOptions.Center;
    text.color = Color.white;

    RectTransform rt = titleGO.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f,0.8f);
    rt.anchorMax = new Vector2(0.5f,0.8f);
    rt.sizeDelta = new Vector2(800,200);
    rt.anchoredPosition = Vector2.zero;
}
void CreatePlayButton()
{
    // Outer glow/shadow layer
    GameObject shadowGO = new GameObject("StartShadow");
    shadowGO.transform.SetParent(menuPanel.transform, false);
    Image shadowImg = shadowGO.AddComponent<Image>();
    Sprite rounded = GetRoundedSprite();
    if (rounded != null) { shadowImg.sprite = rounded; shadowImg.type = Image.Type.Sliced; }
    shadowImg.color = new Color(0f, 0.25f, 0.4f, 0.6f);
    RectTransform shadowRT = shadowGO.GetComponent<RectTransform>();
    shadowRT.anchorMin = new Vector2(0.5f, 0.5f);
    shadowRT.anchorMax = new Vector2(0.5f, 0.5f);
    shadowRT.pivot     = new Vector2(0.5f, 0.5f);
    shadowRT.sizeDelta = new Vector2(318, 108);
    shadowRT.anchoredPosition = new Vector2(0, -190);

    // Main button
    GameObject buttonGO = new GameObject("PlayButton");
    buttonGO.transform.SetParent(menuPanel.transform, false);

    Image img = buttonGO.AddComponent<Image>();
    if (rounded != null) { img.sprite = rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 0.4f; }
    img.color = new Color(0.05f, 0.55f, 0.8f); // deep ocean blue

    Button btn = buttonGO.AddComponent<Button>();
    ColorBlock cb = btn.colors;
    cb.highlightedColor = new Color(0.1f, 0.7f, 1f);
    cb.pressedColor     = new Color(0.02f, 0.38f, 0.58f);
    btn.colors = cb;
    btn.onClick.AddListener(OnPlayPressed);

    RectTransform rt = buttonGO.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(0.5f, 0.5f);
    rt.anchorMax        = new Vector2(0.5f, 0.5f);
    rt.pivot            = new Vector2(0.5f, 0.5f);
    rt.sizeDelta        = new Vector2(300, 100);
    rt.anchoredPosition = new Vector2(0, -190);

    // Label
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform, false);
    TextMeshProUGUI txt = textGO.AddComponent<TextMeshProUGUI>();
    txt.text      = "START";
    txt.font      = tmpFont;
    txt.fontSize  = 54;
    txt.fontStyle = FontStyles.Bold;
    txt.alignment = TextAlignmentOptions.Center;
    txt.color     = Color.white;
    txt.outlineColor = new Color(0f, 0.2f, 0.4f, 1f);
    txt.outlineWidth = 0.1f;

    RectTransform txtRT = textGO.GetComponent<RectTransform>();
    txtRT.anchorMin = Vector2.zero;
    txtRT.anchorMax = Vector2.one;
    txtRT.offsetMin = Vector2.zero;
    txtRT.offsetMax = Vector2.zero;

    // Pulse animation
    StartCoroutine(PulseButton(buttonGO, shadowGO));
}

IEnumerator PulseButton(GameObject btn, GameObject shadow)
{
    float t = 0f;
    Vector3 baseScale = Vector3.one;
    while (btn != null)
    {
        t += Time.unscaledDeltaTime * 1.8f;
        float s = 1f + Mathf.Sin(t) * 0.03f;
        btn.transform.localScale    = baseScale * s;
        if (shadow != null)
            shadow.transform.localScale = baseScale * (s * 1.02f);
        yield return null;
    }
}
void OnPlayPressed()
{
    ////debug.Log("Play button pressed");

    menuPanel.SetActive(false);

    StartGame();
}
void CreateBestScoreText()
{
    // Styled card background
    GameObject cardGO = new GameObject("BestScoreCard");
    bestScoreCard = cardGO; // store so StartGame can hide the whole card
    cardGO.transform.SetParent(mainCanvas.transform, false);

    Image cardImg = cardGO.AddComponent<Image>();
    cardImg.color = new Color(0f, 0f, 0f, 0.45f);
    Sprite rounded = GetRoundedSprite();
    if (rounded != null) { cardImg.sprite = rounded; cardImg.type = Image.Type.Sliced; }

    RectTransform cardRT = cardGO.GetComponent<RectTransform>();
    cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
    cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
    cardRT.pivot            = new Vector2(0.5f, 0.5f);
    cardRT.sizeDelta        = new Vector2(280, 64);
    cardRT.anchoredPosition = new Vector2(0, 120);

    // Text inside card
    GameObject bestGO = new GameObject("BestScoreText");
    bestGO.transform.SetParent(cardGO.transform, false);
    bestScoreText = bestGO.AddComponent<TextMeshProUGUI>();
    bestScoreText.font      = tmpFont;
    bestScoreText.fontSize  = 36;
    bestScoreText.fontStyle = FontStyles.Bold;
    bestScoreText.alignment = TextAlignmentOptions.Center;
    bestScoreText.color     = new Color(1f, 0.9f, 0.3f); // warm gold

    RectTransform rt = bestGO.GetComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = new Vector2(12, 4);
    rt.offsetMax = new Vector2(-12, -4);

    RefreshBestScore();
}
void RefreshBestScore()
{
    int best = PlayerPrefs.GetInt("BestScore", 0);
    if (bestScoreText != null)
        bestScoreText.text = "BEST  " + best;
}
void CreateGameLogo()
{
    Canvas canvas = FindObjectOfType<Canvas>();

    GameObject logoGO = new GameObject("GameLogo");
    logoGO.transform.SetParent(canvas.transform, false);

    gameLogo = logoGO.AddComponent<Image>();
    gameLogo.sprite = gameLogoSprite;
    gameLogo.preserveAspect = false; // allow stretch

    RectTransform rt = logoGO.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(0.5f, 1f);
    rt.anchorMax        = new Vector2(0.5f, 1f);
    rt.pivot            = new Vector2(0.5f, 1f);
    rt.sizeDelta        = new Vector2(620, 260);
    rt.anchoredPosition = new Vector2(0, -220);  // lower on screen
}

// void CreateGround()
// {
//     Camera cam = Camera.main;

//     float camHeight = cam.orthographicSize * 2f;
//     float camWidth = camHeight * cam.aspect;

//     ground = new GameObject("Ground");

//     SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
//     sr.sprite = Resources.Load<Sprite>("Ground");
//     sr.drawMode = SpriteDrawMode.Tiled;
//     sr.sortingOrder = 20;

//     ground.transform.localScale = Vector3.one;

//     // tiled width
//     sr.size = new Vector2(camWidth * 3f, sr.sprite.bounds.size.y);

//     float groundHeight = sr.size.y;

//     float groundY = -cam.orthographicSize + groundHeight / 2f;
//     ground.transform.position = new Vector3(0, groundY, 0);

//     // collider
//     BoxCollider2D col = ground.AddComponent<BoxCollider2D>();
//     col.size = new Vector2(sr.size.x, groundHeight);

//     // keep collider centered with ground
//     col.offset = Vector2.zero;

//     ground.tag = "Ground";

//     groundTop = groundY + groundHeight / 2f;
// }
void CreateGround()
{
    Camera cam = Camera.main;

    float camHeight = cam.orthographicSize * 2f;
    float camWidth = camHeight * cam.aspect;

    Sprite groundSprite = Resources.Load<Sprite>("Ground");

    // ---------- GROUND 1 ----------
    ground1 = new GameObject("Ground1");

    SpriteRenderer sr1 = ground1.AddComponent<SpriteRenderer>();
    sr1.sprite = groundSprite;
    sr1.drawMode = SpriteDrawMode.Tiled;
    sr1.sortingOrder = 20;

    sr1.size = new Vector2(camWidth * 2f, sr1.sprite.bounds.size.y);

    float groundHeight = sr1.size.y;

    float groundY = -cam.orthographicSize + groundHeight / 2f;

    ground1.transform.position = new Vector3(0, groundY, 0);

    BoxCollider2D col1 = ground1.AddComponent<BoxCollider2D>();
    col1.size = new Vector2(sr1.size.x, groundHeight);

    ground1.tag = "Ground";

    // store width
    groundWidth = sr1.size.x;

    // ---------- GROUND 2 ----------
    ground2 = new GameObject("Ground2");

    SpriteRenderer sr2 = ground2.AddComponent<SpriteRenderer>();
    sr2.sprite = groundSprite;
    sr2.drawMode = SpriteDrawMode.Tiled;
    sr2.sortingOrder = 20;

    sr2.size = sr1.size;

    ground2.transform.position =
        new Vector3(groundWidth, groundY, 0);

    BoxCollider2D col2 = ground2.AddComponent<BoxCollider2D>();
    col2.size = new Vector2(sr2.size.x, groundHeight);

    ground2.tag = "Ground";

    // compatibility with old code
    ground = ground1;

    groundTop = groundY + groundHeight / 2f;
}
public string GetUsername()
{
    return playerUsername;
}
public void CreateDeathEffect(Vector3 position)
{
    // 💥 Impact flash
    CreateParticleBurst(position, Color.white, 25, 0.25f, 0.15f);

    // ⚡ Electric sparks
    GameObject psObj = new GameObject("DeathSparks");
    psObj.transform.position = position;

    ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

    var main = ps.main;
    main.startColor = new Color(0.4f, 0.8f, 1f);
    main.startSize = 0.12f;
    main.startLifetime = 0.4f;
    main.maxParticles = 40;
    main.simulationSpace = ParticleSystemSimulationSpace.World;

    var emission = ps.emission;
    emission.rateOverTime = 0;
    emission.SetBursts(new ParticleSystem.Burst[] {
        new ParticleSystem.Burst(0f, 40)
    });

   var shape = ps.shape;
shape.shapeType = ParticleSystemShapeType.Circle;
shape.radius = 0.2f;

// ✅ FIXED VELOCITY
var velocity = ps.velocityOverLifetime;
velocity.enabled = true;

ParticleSystem.MinMaxCurve curve = new ParticleSystem.MinMaxCurve(-3f, 3f);

velocity.x = curve;
velocity.y = curve;
velocity.z = curve;

    var renderer = ps.GetComponent<ParticleSystemRenderer>();
    renderer.renderMode = ParticleSystemRenderMode.Billboard;
    Material mat = new Material(Shader.Find("Sprites/Default"));
    mat.color = Color.white;
    Sprite circle = GetCircleSprite();
    if (circle != null) mat.mainTexture = circle.texture;
    renderer.material = mat;

    ps.Play();
    Destroy(psObj, 0.6f);
}

public void ShowSadPlayer()
{
    if (player != null)
    {
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Use skin-specific sad sprite, fall back to inspector-assigned one
            Sprite sadSpr = SkinManager.GetSelectedSadSprite();
            if (sadSpr == null) sadSpr = playerSadSprite;

            Sprite normalSpr = SkinManager.GetSelectedSprite();
            if (normalSpr == null) normalSpr = playerSprite;

            if (sadSpr != null && normalSpr != null)
            {
                float scaleFactor = normalSpr.bounds.size.x / sadSpr.bounds.size.x;
                sr.sprite = sadSpr;
                sr.transform.localScale = originalPlayerScale * scaleFactor;
            }
            else if (sadSpr != null)
            {
                sr.sprite = sadSpr;
            }
        }
    }
}
public void ContinueAfterAd()
{

    IsGameOver = false;
    CurrentState = GameState.Playing;

    Time.timeScale = 1f;

    if (playerScript != null)
    {
        playerScript.RevivePlayer();
    }
}
public void RevivePlayer()
{
    Time.timeScale = 1f;
    IsGameOver = false;
    CurrentState = GameState.WaitingForRevive; // wait for first tap

    HideGameOverUI();

    if (playerScript != null)
    {
        Vector3 revivePos = new Vector3(0f, 1f, 0f);
        playerScript.StopAllCoroutines();
        playerScript.ResetPlayer(revivePos);
        GameBootstrap.Instance?.ResetPlayerVisual();
        playerScript.SetInvincible(10f); // long invincibility — cancelled on first tap

        Rigidbody2D rb = playerScript.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0.35f; // gentle float — no falling
        }
    }

    // Re-enable pipes but they won't move until CurrentState == Playing
    foreach (var moving in Object.FindObjectsOfType<Moving>())
        moving.enabled = true;

    CancelInvoke(nameof(SpawnPipeRepeated));
}
// Applies the currently selected skin sprite to the player
public void ApplySelectedSkin()
{
    if (player == null) return;
    Sprite sp = SkinManager.GetSelectedSprite();
    if (sp == null) return;
    SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
    if (sr != null) sr.sprite = sp;
}

public void ReviveAfterAd()
{
    wasRevived = true;

    // Hide ALL game over UI immediately — no flash
    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (reviveButton != null) reviveButton.SetActive(false);
    if (restartButton != null) restartButton.SetActive(false);
    if (gameOverImageObj != null) gameOverImageObj.SetActive(false);
    Time.timeScale = 1f;
    IsGameOver = false;
    CurrentState = GameState.Playing;
    if (scoreText != null) scoreText.gameObject.SetActive(true);

    // Restart background music after revive
    if (bgMusicSource != null && !bgMusicSource.isPlaying && bgMusicSource.clip != null)
        bgMusicSource.Play();

    HapticManager.Revive(); // double pulse on revive

    FirebaseGameManager manager = FindObjectOfType<FirebaseGameManager>();
    if (manager != null)
    {
        manager.hasRevivedThisRun = true;
    }

    RevivePlayer();
}
public void ResetPlayerVisual()
{
    if (player != null)
    {
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite playSpr = SkinManager.GetSelectedSprite();
            if (playSpr == null) playSpr = playerSprite;
            if (playSpr != null) sr.sprite = playSpr;
            sr.transform.localScale = originalPlayerScale;
        }
    }
}
void CreateNewRunText()
{
    GameObject obj = new GameObject("NewRunText");
    obj.transform.SetParent(mainCanvas.transform, false);

    newRunText = obj.AddComponent<TextMeshProUGUI>();
    newRunText.font = tmpFont;
    newRunText.fontSize = 48;
    newRunText.alignment = TextAlignmentOptions.Center;
    newRunText.color = Color.white;
    newRunText.text = "NEW RUN";

    RectTransform rt = newRunText.rectTransform;
    rt.anchorMin = new Vector2(0.5f, 0.7f);
    rt.anchorMax = new Vector2(0.5f, 0.7f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = Vector2.zero;
    rt.sizeDelta = new Vector2(400, 100);

    newRunText.gameObject.SetActive(false); // hidden by default
}
IEnumerator ShowNewRun()
{
    if (newRunText == null) yield break;

    newRunText.gameObject.SetActive(true);

    Color c = newRunText.color;
    c.a = 1f;
    newRunText.color = c;

    float duration = 1.2f;
    float t = 0f;

    while (t < duration)
    {
        t += Time.deltaTime;

        // fade out
        float alpha = Mathf.Lerp(1f, 0f, t / duration);
        c.a = alpha;
        newRunText.color = c;

        // slight upward float
        newRunText.rectTransform.anchoredPosition += new Vector2(0, Time.deltaTime * 20f);

        yield return null;
    }

    newRunText.gameObject.SetActive(false);

    // reset position
    newRunText.rectTransform.anchoredPosition = Vector2.zero;
}
GameObject CreateStyledButton(string name, string text, Vector2 position, Color bgColor)
{
    GameObject btnObj = new GameObject(name);
    btnObj.transform.SetParent(mainCanvas.transform, false);

    // Background
    Image img = btnObj.AddComponent<Image>();
    img.color = bgColor;

    // 🔥 Rounded feel (important if you later add sprite)
    img.type = Image.Type.Sliced;

    Button btn = btnObj.AddComponent<Button>();

    RectTransform rt = btnObj.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = position;
    rt.sizeDelta = new Vector2(260, 90);

    // TEXT
    GameObject txtObj = new GameObject("Text");
    txtObj.transform.SetParent(btnObj.transform, false);

    TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
    tmp.text = text;
    tmp.font = tmpFont;
    tmp.fontSize = 42;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.color = Color.black;

    RectTransform txtRT = tmp.GetComponent<RectTransform>();
    txtRT.anchorMin = Vector2.zero;
    txtRT.anchorMax = Vector2.one;
    txtRT.offsetMin = Vector2.zero;
    txtRT.offsetMax = Vector2.zero;

    // 🔥 SHADOW (huge visual upgrade)
    Shadow shadow = txtObj.AddComponent<Shadow>();
    shadow.effectColor = new Color(0, 0, 0, 0.6f);
    shadow.effectDistance = new Vector2(3, -3);

    // 🔥 CLICK ANIMATION
    btn.onClick.AddListener(() =>
    {
        btnObj.transform.localScale = Vector3.one * 0.9f;
        StartCoroutine(ResetButtonScale(btnObj));
    });

    return btnObj;
}

GameObject CreateRoundedButton(string name, string text, Color bgColor, Color textColor,
                                Vector2 anchoredPos, Vector2 size, Transform parent)
{
    GameObject btnObj = new GameObject(name);
    btnObj.transform.SetParent(parent, false);

    Image img = btnObj.AddComponent<Image>();
    // Rounded corners via built-in UISprite (9-sliced round rect)
    Sprite rounded = Resources.Load<Sprite>("UI/Skin/UISprite");
    if (rounded == null)
        rounded = GetRoundedSprite();
    if (rounded != null)
    {
        img.sprite = rounded;
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f; // more rounding
    }
    img.color = bgColor;

    Button btn = btnObj.AddComponent<Button>();

    // Hover/press tint
    ColorBlock cb = btn.colors;
    cb.highlightedColor = new Color(bgColor.r * 1.15f, bgColor.g * 1.15f, bgColor.b * 1.15f);
    cb.pressedColor     = new Color(bgColor.r * 0.75f, bgColor.g * 0.75f, bgColor.b * 0.75f);
    btn.colors = cb;

    RectTransform rt = btnObj.GetComponent<RectTransform>();
    rt.anchorMin        = new Vector2(0.5f, 1f);
    rt.anchorMax        = new Vector2(0.5f, 1f);
    rt.pivot            = new Vector2(0.5f, 1f);
    rt.anchoredPosition = anchoredPos;
    rt.sizeDelta        = size;

    // Drop shadow (child image slightly offset behind)
    GameObject shadowGO = new GameObject("Shadow");
    shadowGO.transform.SetParent(btnObj.transform, false);
    shadowGO.transform.SetAsFirstSibling();
    Image shadowImg = shadowGO.AddComponent<Image>();
    if (rounded != null) { shadowImg.sprite = rounded; shadowImg.type = Image.Type.Sliced; shadowImg.pixelsPerUnitMultiplier = 0.5f; }
    shadowImg.color = new Color(0f, 0f, 0f, 0.35f);
    RectTransform shadowRT = shadowGO.GetComponent<RectTransform>();
    shadowRT.anchorMin = Vector2.zero; shadowRT.anchorMax = Vector2.one;
    shadowRT.offsetMin = new Vector2(-4f, -6f);
    shadowRT.offsetMax = new Vector2(4f, -2f);

    // Label
    GameObject txtGO = new GameObject("Label");
    txtGO.transform.SetParent(btnObj.transform, false);
    TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
    tmp.text      = text;
    tmp.font      = tmpFont;
    tmp.fontSize  = 48;
    tmp.fontStyle = FontStyles.Bold;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.color     = textColor;

    RectTransform txtRT = tmp.GetComponent<RectTransform>();
    txtRT.anchorMin = Vector2.zero;
    txtRT.anchorMax = Vector2.one;
    txtRT.offsetMin = new Vector2(8f, 4f);
    txtRT.offsetMax = new Vector2(-8f, -4f);

    // Click scale animation
    btn.onClick.AddListener(() => StartCoroutine(ButtonPop(btnObj)));

    return btnObj;
}

IEnumerator ButtonPop(GameObject btn)
{
    btn.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
    yield return new WaitForSeconds(0.08f);
    btn.transform.localScale = Vector3.one;
}

void CreateGameOverButtons()
{
    // ── OUTER PANEL (dark semi-transparent card) ──────────────────────────
    GameObject panel = new GameObject("GameOverPanel");
    panel.transform.SetParent(mainCanvas.transform, false);

    Image panelImg = panel.AddComponent<Image>();

    // Use UISprite (built-in rounded square) for rounded corners
    Sprite roundedSprite = Resources.Load<Sprite>("UI/Skin/UISprite");
    if (roundedSprite == null)
        roundedSprite = GetRoundedSprite();
    if (roundedSprite != null)
    {
        panelImg.sprite = roundedSprite;
        panelImg.type   = Image.Type.Sliced;
    }
    panelImg.color = new Color(0.04f, 0.08f, 0.18f, 0.93f); // deep ocean dark

    RectTransform panelRT = panel.GetComponent<RectTransform>();
    panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
    panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
    panelRT.pivot            = new Vector2(0.5f, 0.5f);
    panelRT.sizeDelta        = new Vector2(460, 450); // increased from 380 to fit score + buttons
    panelRT.anchoredPosition = new Vector2(0, 60);

    // ── GAME OVER LABEL ───────────────────────────────────────────────────
    GameObject labelGO = new GameObject("GOLabel");
    labelGO.transform.SetParent(panel.transform, false);
    TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
    label.text      = "GAME  OVER";
    label.font      = tmpFont;
    label.fontSize  = 54;
    label.fontStyle = FontStyles.Bold;
    label.alignment = TextAlignmentOptions.Center;
    label.color     = new Color(1f, 0.35f, 0.1f); // fiery orange-red

    // Outline effect via TMP
    label.outlineColor = new Color(0.6f, 0f, 0f, 1f);
    label.outlineWidth = 0.15f;

    RectTransform labelRT = label.GetComponent<RectTransform>();
    labelRT.anchorMin        = new Vector2(0f, 1f);
    labelRT.anchorMax        = new Vector2(1f, 1f);
    labelRT.pivot            = new Vector2(0.5f, 1f);
    labelRT.anchoredPosition = new Vector2(0f, -20f);
    labelRT.sizeDelta        = new Vector2(0f, 70f);

    // Keep a reference so FadeInGameOverUI can still update it
    gameOverText = label;

    // ── SCORE DISPLAY ─────────────────────────────────────────────────────
    GameObject scoreGO = new GameObject("GOScore");
    scoreGO.transform.SetParent(panel.transform, false);
    TextMeshProUGUI scoreLbl = scoreGO.AddComponent<TextMeshProUGUI>();
    scoreLbl.font      = tmpFont;
    scoreLbl.fontSize  = 42;
    scoreLbl.fontStyle = FontStyles.Bold;
    scoreLbl.alignment = TextAlignmentOptions.Center;
    scoreLbl.color     = Color.white;
    scoreLbl.outlineColor = new Color(0f, 0f, 0.2f, 1f);
    scoreLbl.outlineWidth = 0.12f;
    RectTransform scoreRT = scoreLbl.GetComponent<RectTransform>();
    scoreRT.anchorMin        = new Vector2(0f, 1f);
    scoreRT.anchorMax        = new Vector2(1f, 1f);
    scoreRT.pivot            = new Vector2(0.5f, 1f);
    scoreRT.anchoredPosition = new Vector2(0f, -65f);
    scoreRT.sizeDelta        = new Vector2(0f, 95f); // tall enough for 2 lines
    gameOverScoreText = scoreLbl; // stored so TriggerGameOver can update it

    reviveButton = CreateRoundedButton(
        "ReviveButton", "REVIVE",
        new Color(1f, 0.72f, 0f),
        new Color(0.12f, 0.06f, 0f),
        new Vector2(0f, -155f),   // moved down to give score space
        new Vector2(360f, 90f),
        panel.transform
    );
    reviveButton.GetComponent<Button>().onClick.AddListener(OnReviveClicked);

    // ── RESTART BUTTON ────────────────────────────────────────────────────
    restartButton = CreateRoundedButton(
        "RestartButton", "RESTART",
        new Color(0.08f, 0.52f, 0.68f),
        Color.white,
        new Vector2(0f, -265f),   // moved down from -238
        new Vector2(360f, 90f),
        panel.transform
    );
    restartButton.GetComponent<Button>().onClick.AddListener(() =>
    {
        Time.timeScale = 1f; // reset from slow-motion before doing anything
        // Route through FirebaseGameManager so interstitial ad plays on every restart
        FirebaseGameManager firebase = FindObjectOfType<FirebaseGameManager>();
        if (firebase != null)
            firebase.TriggerRestartWithAd();
        else
            RestartGame(); // fallback if Firebase not present
    });

    // Hide panel initially — showing it brings everything at once, in correct order
    panel.SetActive(false);

    // Store panel ref so TriggerGameOver can show/hide it
    gameOverPanel = panel;
    reviveButton.SetActive(true);   // children start active; panel itself is hidden
    restartButton.SetActive(true);
}
void OnReviveClicked()
{
    // Hide UI immediately when player clicks revive — before ad loads
    if (gameOverPanel != null)   gameOverPanel.SetActive(false);
    if (reviveButton != null)    reviveButton.SetActive(false);
    if (restartButton != null)   restartButton.SetActive(false);
    if (gameOverImageObj != null) gameOverImageObj.SetActive(false);

    AdManager.Instance.ShowAd(
        onRewardEarned: () =>
        {
            // Update cooldown timer — rewarded ad counts as an ad shown
            FirebaseGameManager fbm = FindObjectOfType<FirebaseGameManager>();
            if (fbm != null) fbm.UpdateLastAdTime();
            ReviveAfterAd();
        },
        onSkipped: () =>
        {
            // Ad skipped — update timer so next interstitial respects cooldown
            FirebaseGameManager fbmSkip = FindObjectOfType<FirebaseGameManager>();
            if (fbmSkip != null) fbmSkip.UpdateLastAdTime();
            // Show game over panel again so player can restart
            if (gameOverPanel != null)   gameOverPanel.SetActive(true);
            if (restartButton != null)   restartButton.SetActive(true);
            if (gameOverImageObj != null) gameOverImageObj.SetActive(true);
        }
    );
}
IEnumerator ResetButtonScale(GameObject obj)
{
    yield return new WaitForSecondsRealtime(0.1f);

    if (obj != null)
        obj.transform.localScale = Vector3.one;
}
IEnumerator ReviveTimeoutFallback()
{
    yield return new WaitForSecondsRealtime(20f);

    _reviveTimeoutCoroutine = null;

    // Only force restart if player is STILL on game over screen
    // If IsGameOver is false, revive already succeeded — do nothing
    if (IsGameOver)
    {
        Debug.LogWarning("Revive timeout — forcing restart");
        FirebaseGameManager firebase = FindObjectOfType<FirebaseGameManager>();
        if (firebase != null) firebase.TriggerRestartWithAd();
        else RestartGame();
    }
}
void UpdateGroundScroll()
{
    if (ground1 == null || ground2 == null)
        return;

    if (IsGameOver)
        return;

    if (CurrentState != GameState.Playing)
        return;

    // Match pipe speed exactly (including difficulty scaling)
    float difficultyMultiplier = GetDifficultyMultiplier();
    float speed = pipeSpeed + difficultyMultiplier * 0.5f;

    // move both grounds
    ground1.transform.position += Vector3.left * speed * Time.deltaTime;
    ground2.transform.position += Vector3.left * speed * Time.deltaTime;

    // loop ground1
    if (ground1.transform.position.x <= -groundWidth)
    {
        ground1.transform.position =
            new Vector3(
                ground2.transform.position.x + groundWidth,
                ground1.transform.position.y,
                0
            );
    }

    // loop ground2
    if (ground2.transform.position.x <= -groundWidth)
    {
        ground2.transform.position =
            new Vector3(
                ground1.transform.position.x + groundWidth,
                ground2.transform.position.y,
                0
            );
    }
}
void ApplyReliefPipeVisual(GameObject pipe)
{
    if (pipe == null) return;
CreateParticleBurst(pipe.transform.position, Color.cyan, 8, 0.1f, 0.6f);
    SpriteRenderer sr = pipe.GetComponent<SpriteRenderer>();

    if (sr != null)
    {
        // Slightly brighter / softer color
        sr.color = new Color(1f, 1f, 1f, 0.85f);
    }

    // OPTIONAL: tiny floating effect
    StartCoroutine(ReliefPipePulse(pipe.transform));
}
IEnumerator ReliefPipePulse(Transform pipe)
{
    Vector3 originalScale = pipe.localScale;

    float timer = 0f;

    while (pipe != null && timer < 2f)
    {
        timer += Time.deltaTime * 2f;

        float scale = 1f + Mathf.Sin(timer * 6f) * 0.02f;

        pipe.localScale = originalScale * scale;

        yield return null;
    }

    if (pipe != null)
        pipe.localScale = originalScale;
}

}