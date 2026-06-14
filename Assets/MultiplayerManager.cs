using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// 2-Player real-time multiplayer via Firebase Realtime Database.
///
/// Flow:
///   Host:  CreateRoom() → gets a 4-digit code → waits for guest
///   Guest: JoinRoom(code) → room state flips to "countdown"
///   Both:  3-2-1 countdown using the shared Random seed for identical pipes
///   Both:  broadcast Y + score every 100ms; see opponent as translucent ghost
///   Win:   when opponent's alive=false you win; when you die you lose
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    // ── State ───────────────────────────────────────────────
    public enum MPState { Idle, CreatingRoom, WaitingForGuest, Joining, Countdown, Playing, Done }
    public MPState State { get; private set; } = MPState.Idle;

    public bool IsMultiplayerGame  { get; private set; }
    public bool IsHost             { get; private set; }
    public string RoomCode         { get; private set; }

    // ── Firebase ────────────────────────────────────────────
    private DatabaseReference _db;
    private DatabaseReference _roomRef;
    private DatabaseReference _myRef;
    private DatabaseReference _oppRef;

    // ── Room data ───────────────────────────────────────────
    private int    _seed;
    private string _mySlot;   // "host" or "guest"
    private string _oppSlot;
    private string _oppUsername;
    private bool   _opponentAlive = true;
    private int    _opponentScore;

    // ── Listener handles ────────────────────────────────────
    private EventHandler<ValueChangedEventArgs> _roomStateListener;
    private EventHandler<ValueChangedEventArgs> _oppListener;

    // ── Ghost visual ────────────────────────────────────────
    private GameObject     _ghostFish;
    private SpriteRenderer _ghostSR;
    private float          _ghostTargetY;
    private float          _ghostCurrentY;

    // ── Sync ────────────────────────────────────────────────
    private Coroutine _syncCoroutine;
    private const float SYNC_INTERVAL = 0.1f; // 10 fps

    // ── UI ──────────────────────────────────────────────────
    private GameObject         _roomPanel;
    private TextMeshProUGUI    _roomCodeText;
    private TextMeshProUGUI    _statusText;
    private TextMeshProUGUI    _countdownText;
    private TextMeshProUGUI    _oppScoreBadge;
    private Button             _cancelButton;

    // ── Lifecycle ───────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    public void Initialize(DatabaseReference db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────────────────
    // PUBLIC API
    // ──────────────────────────────────────────────────────

    /// <summary>Create a room as host. Calls back with the 4-digit code.</summary>
    public void CreateRoom(string username, System.Action<string> onCode, System.Action<string> onError)
    {
        if (_db == null) { onError("Firebase not ready"); return; }

        State = MPState.CreatingRoom;
        IsHost = true;
        _mySlot  = "host";
        _oppSlot = "guest";
        _seed    = Random.Range(10000, 99999);
        RoomCode = GenerateCode();

        var data = new Dictionary<string, object>
        {
            { "hostUsername",  username },
            { "guestUsername", "" },
            { "seed",          _seed },
            { "state",         "waiting" },
            { "host",  new Dictionary<string,object>{ {"y",0f},{"score",0},{"alive",true} } },
            { "guest", new Dictionary<string,object>{ {"y",0f},{"score",0},{"alive",true} } }
        };

        _db.Child("rooms").Child(RoomCode).SetValueAsync(data).ContinueWithOnMainThread(t =>
        {
            if (!t.IsCompletedSuccessfully) { onError("Create failed: " + t.Exception?.Message); return; }

            _roomRef = _db.Child("rooms").Child(RoomCode);
            _myRef   = _roomRef.Child(_mySlot);
            _oppRef  = _roomRef.Child(_oppSlot);

            State = MPState.WaitingForGuest;
            onCode(RoomCode);
            ListenForGuest();

            // Auto-delete room after 5 min if guest never joins
            StartCoroutine(RoomTimeout(300f));
        });
    }

    /// <summary>Join an existing room as guest.</summary>
    public void JoinRoom(string code, string username, System.Action onJoined, System.Action<string> onError)
    {
        if (_db == null) { onError("Firebase not ready"); return; }

        State = MPState.Joining;
        IsHost  = false;
        RoomCode = code.ToUpper();
        _mySlot  = "guest";
        _oppSlot = "host";

        _db.Child("rooms").Child(RoomCode).GetValueAsync().ContinueWithOnMainThread(t =>
        {
            if (!t.IsCompletedSuccessfully || !t.Result.Exists)
            { onError("Room not found"); return; }

            string roomState = t.Result.Child("state").Value?.ToString();
            if (roomState != "waiting")
            { onError("Room already started"); return; }

            _seed         = int.Parse(t.Result.Child("seed").Value.ToString());
            _oppUsername  = t.Result.Child("hostUsername").Value?.ToString() ?? "Host";

            _roomRef = _db.Child("rooms").Child(RoomCode);
            _myRef   = _roomRef.Child(_mySlot);
            _oppRef  = _roomRef.Child(_oppSlot);

            // Write guest name and flip state to countdown
            var update = new Dictionary<string, object>
            {
                { "guestUsername", username },
                { "state",         "countdown" }
            };
            _roomRef.UpdateChildrenAsync(update).ContinueWithOnMainThread(u =>
            {
                if (!u.IsCompletedSuccessfully) { onError("Join failed"); return; }
                State = MPState.Countdown;
                onJoined();
                ListenForOpponent();
                StartCoroutine(CountdownThenPlay());
            });
        });
    }

    // ──────────────────────────────────────────────────────
    // LISTENERS
    // ──────────────────────────────────────────────────────

    void ListenForGuest()
    {
        _roomStateListener = (_, args) =>
        {
            if (args.DatabaseError != null) return;
            string s = args.Snapshot.Child("state").Value?.ToString();
            if (s == "countdown" && State == MPState.WaitingForGuest)
            {
                _oppUsername = args.Snapshot.Child("guestUsername").Value?.ToString() ?? "Guest";
                State = MPState.Countdown;
                _roomRef.ValueChanged -= _roomStateListener;
                ListenForOpponent();
                StartCoroutine(CountdownThenPlay());
            }
        };
        _roomRef.ValueChanged += _roomStateListener;
    }

    void ListenForOpponent()
    {
        _oppListener = (_, args) =>
        {
            if (args.DatabaseError != null || !args.Snapshot.Exists) return;
            float.TryParse(args.Snapshot.Child("y").Value?.ToString(), out _ghostTargetY);
            int.TryParse(args.Snapshot.Child("score").Value?.ToString(), out _opponentScore);
            bool.TryParse(args.Snapshot.Child("alive").Value?.ToString(), out bool alive);
            if (!alive && _opponentAlive)
            {
                _opponentAlive = false;
                OnOpponentDied();
            }
        };
        _oppRef.ValueChanged += _oppListener;
    }

    // ──────────────────────────────────────────────────────
    // COUNTDOWN & START
    // ──────────────────────────────────────────────────────

    IEnumerator CountdownThenPlay()
    {
        ShowCountdownText("3");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("2");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("1");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("GO!");
        yield return new WaitForSecondsRealtime(0.5f);
        HideCountdown();

        // Seed the random so both players get identical pipes
        Random.InitState(_seed);

        State = MPState.Playing;
        IsMultiplayerGame = true;

        _roomRef.Child("state").SetValueAsync("playing");

        // Boot the game exactly like a solo restart
        GameBootstrap.Instance.StartMultiplayerRound();

        // Spawn ghost fish
        Sprite playerSprite = GameBootstrap.Instance.player?
            .GetComponent<SpriteRenderer>()?.sprite;
        float playerScale = GameBootstrap.Instance.player != null
            ? GameBootstrap.Instance.player.transform.localScale.x : 1.4f;
        CreateGhostFish(playerSprite, playerScale);
        CreateOppScoreBadge();

        // Start broadcasting our position
        _syncCoroutine = StartCoroutine(SyncLoop());
    }

    // ──────────────────────────────────────────────────────
    // SYNC LOOP
    // ──────────────────────────────────────────────────────

    IEnumerator SyncLoop()
    {
        while (State == MPState.Playing)
        {
            float myY     = GameBootstrap.Instance?.PlayerPosition.y ?? 0f;
            int   myScore = GameBootstrap.Instance?.Score ?? 0;

            _myRef.UpdateChildrenAsync(new Dictionary<string, object>
            {
                { "y",     myY },
                { "score", myScore },
                { "alive", true }
            });

            yield return new WaitForSecondsRealtime(SYNC_INTERVAL);
        }
    }

    // ──────────────────────────────────────────────────────
    // GAME EVENTS
    // ──────────────────────────────────────────────────────

    /// <summary>Call from GameBootstrap.TriggerGameOver when player dies in MP.</summary>
    public void OnLocalPlayerDied(int score)
    {
        if (State != MPState.Playing) return;
        State = MPState.Done;

        _myRef.UpdateChildrenAsync(new Dictionary<string, object>
        {
            { "alive", false },
            { "score", score }
        });

        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        DestroyGhost();
        ShowResult(won: false, myScore: score, oppScore: _opponentScore);
    }

    void OnOpponentDied()
    {
        if (State != MPState.Playing) return;
        // Opponent died — we're still alive, we win!
        State = MPState.Done;
        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        DestroyGhost();
        int myScore = GameBootstrap.Instance?.Score ?? 0;
        ShowResult(won: true, myScore: myScore, oppScore: _opponentScore);
    }

    // ──────────────────────────────────────────────────────
    // GHOST FISH VISUAL
    // ──────────────────────────────────────────────────────

    void CreateGhostFish(Sprite sprite, float playerScale)
    {
        _ghostFish = new GameObject("MP_GhostFish");
        _ghostFish.transform.position  = new Vector3(-1.8f, 0f, 0.1f);
        _ghostFish.transform.localScale = Vector3.one * playerScale * 0.85f;

        _ghostSR = _ghostFish.AddComponent<SpriteRenderer>();
        if (sprite != null) _ghostSR.sprite = sprite;
        _ghostSR.color       = new Color(1f, 0.55f, 0.2f, 0.40f); // translucent orange
        _ghostSR.sortingOrder = 4;

        // Name tag
        var labelGO = new GameObject("GhostLabel");
        labelGO.transform.SetParent(_ghostFish.transform);
        labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        labelGO.transform.localScale    = Vector3.one * 0.6f;

        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = $"<b>{_oppUsername}</b>";
        tmp.fontSize  = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.8f, 0.5f, 0.9f);
        tmp.sortingOrder = 5;
    }

    void DestroyGhost()
    {
        if (_ghostFish != null) { Destroy(_ghostFish); _ghostFish = null; }
    }

    void Update()
    {
        if (State != MPState.Playing || _ghostFish == null) return;

        // Smoothly move ghost fish to opponent's reported Y
        _ghostCurrentY = Mathf.Lerp(_ghostCurrentY, _ghostTargetY, Time.deltaTime * 12f);
        _ghostFish.transform.position = new Vector3(-1.8f, _ghostCurrentY, 0.1f);

        float vel = (_ghostTargetY - _ghostCurrentY);
        _ghostFish.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(vel * 8f, -40f, 20f));

        if (_oppScoreBadge != null)
            _oppScoreBadge.text = $"👤 {_oppUsername}: {_opponentScore}";
    }

    // ──────────────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────────────

    void CreateOppScoreBadge()
    {
        Canvas canvas = GameBootstrap.Instance?.GetMainCanvas();
        if (canvas == null) return;

        var go = new GameObject("OppScoreBadge");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-Screen.width * 0.03f, -Screen.height * 0.04f);
        rt.sizeDelta        = new Vector2(Screen.width * 0.4f, Screen.height * 0.07f);

        _oppScoreBadge          = go.AddComponent<TextMeshProUGUI>();
        _oppScoreBadge.fontSize  = Screen.height * 0.022f;
        _oppScoreBadge.alignment = TextAlignmentOptions.Right;
        _oppScoreBadge.color     = new Color(1f, 0.8f, 0.5f, 0.95f);
        _oppScoreBadge.text      = $"👤 {_oppUsername}: 0";
    }

    void ShowCountdownText(string txt)
    {
        Canvas canvas = GameBootstrap.Instance?.GetMainCanvas();
        if (canvas == null) return;

        if (_countdownText == null)
        {
            var go = new GameObject("MPCountdown");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400f, 200f);

            _countdownText          = go.AddComponent<TextMeshProUGUI>();
            _countdownText.fontSize  = Screen.height * 0.12f;
            _countdownText.alignment = TextAlignmentOptions.Center;
            _countdownText.color     = Color.white;
        }
        _countdownText.gameObject.SetActive(true);
        _countdownText.text = txt;
    }

    void HideCountdown()
    {
        if (_countdownText != null) _countdownText.gameObject.SetActive(false);
    }

    void ShowResult(bool won, int myScore, int oppScore)
    {
        Canvas canvas = GameBootstrap.Instance?.GetMainCanvas();
        if (canvas == null) return;

        var go = new GameObject("MPResult");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, Screen.height * 0.1f);
        rt.sizeDelta = new Vector2(Screen.width * 0.85f, Screen.height * 0.22f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = Screen.height * 0.038f;
        tmp.alignment = TextAlignmentOptions.Center;

        if (won)
        {
            tmp.text  = $"🏆 YOU WIN!\nYou: {myScore}   {_oppUsername}: {oppScore}";
            tmp.color = new Color(0.3f, 1f, 0.4f);
        }
        else
        {
            tmp.text  = $"💀 YOU LOST\nYou: {myScore}   {_oppUsername}: {oppScore}";
            tmp.color = new Color(1f, 0.35f, 0.35f);
        }

        // Remove result after 5 seconds (game over panel takes over)
        Destroy(go, 5f);

        // Clean up room from Firebase after match ends
        StartCoroutine(CleanupRoom(3f));

        // Remove opp score badge
        if (_oppScoreBadge != null) Destroy(_oppScoreBadge.gameObject);
    }

    // ──────────────────────────────────────────────────────
    // CLEANUP
    // ──────────────────────────────────────────────────────

    public void AbortMultiplayer()
    {
        State = MPState.Idle;
        IsMultiplayerGame = false;
        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);

        if (_roomRef != null && _myRef != null)
            _myRef.UpdateChildrenAsync(new Dictionary<string, object> { { "alive", false } });

        if (_oppListener != null && _oppRef != null)
            _oppRef.ValueChanged -= _oppListener;

        if (_roomStateListener != null && _roomRef != null)
            _roomRef.ValueChanged -= _roomStateListener;

        DestroyGhost();
        if (_oppScoreBadge != null) Destroy(_oppScoreBadge.gameObject);
        if (_countdownText  != null) Destroy(_countdownText.gameObject);
        _oppListener      = null;
        _roomStateListener = null;
    }

    IEnumerator CleanupRoom(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (_roomRef != null) _roomRef.RemoveValueAsync();
        AbortMultiplayer();
    }

    IEnumerator RoomTimeout(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (State == MPState.WaitingForGuest)
        {
            Debug.Log("[MP] Room timeout — no guest joined.");
            AbortMultiplayer();
            if (_roomRef != null) _roomRef.RemoveValueAsync();
        }
    }

    // ──────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────

    static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars
        var code = new System.Text.StringBuilder(4);
        for (int i = 0; i < 4; i++)
            code.Append(chars[Random.Range(0, chars.Length)]);
        return code.ToString();
    }
}
