using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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

    // Fired with opponent's name when a match is confirmed, before 3-2-1 starts.
    public static event System.Action<string> OnMatchFound;

    // Fired when 60s expires and no real player was found — UI should update before bot starts.
    public static event System.Action OnNoPlayersFound;

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
        _seed    = UnityEngine.Random.Range(10000, 99999);
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
        // Give the UI a moment to show the match-found overlay before we take over the screen.
        OnMatchFound?.Invoke(_oppUsername);
        yield return new WaitForSecondsRealtime(1.5f);

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
        UnityEngine.Random.InitState(_seed);

        State = MPState.Playing;
        IsMultiplayerGame = true;

        _roomRef.Child("state").SetValueAsync("playing");

        // Stop any solo ghost race that might be replaying — MP has its own ghost fish.
        GhostRaceManager.Instance?.StopGhostPlayback();

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

        if (!_isBotMatch && _myRef != null)
            _myRef.UpdateChildrenAsync(new Dictionary<string, object>
            {
                { "alive", false },
                { "score", score }
            });

        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        if (_botCoroutine  != null) StopCoroutine(_botCoroutine);
        DestroyGhost();
        ShowResult(won: false, myScore: score, oppScore: _opponentScore);
    }

    void OnOpponentDied()
    {
        if (State != MPState.Playing) return;
        State = MPState.Done;
        if (_syncCoroutine != null) StopCoroutine(_syncCoroutine);
        DestroyGhost();
        int myScore = GameBootstrap.Instance?.Score ?? 0;
        ShowResult(won: true, myScore: myScore, oppScore: _opponentScore);
        // Pause briefly so player can see the win banner, then end the game normally
        StartCoroutine(EndGameAfterDelay(2f));
    }

    IEnumerator EndGameAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        GameBootstrap.Instance?.TriggerGameOver();
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

        // Clean up Firebase room (skip for bot matches — no room was created)
        if (!_isBotMatch) StartCoroutine(CleanupRoom(3f));
        else              AbortMultiplayer();

        // Remove opp score badge
        if (_oppScoreBadge != null) Destroy(_oppScoreBadge.gameObject);
    }

    // ──────────────────────────────────────────────────────
    // QUICKPLAY — random matchmaking, no code needed
    // ──────────────────────────────────────────────────────

    // Firebase structure:
    //   matchmaking/waiting/<myId> = { username, timestamp, seed }
    // First player sits there. Second player arrives, claims it,
    // creates a room with the first player's seed, both join.

    private string _myMatchId;
    private EventHandler<ValueChangedEventArgs> _matchListener;
    private Coroutine _quickPlayTimeout;

    public void QuickPlay(string username, System.Action<string> onStatus, System.Action<string> onError)
    {
        if (_db == null) { onError("Firebase not ready"); return; }
        if (State != MPState.Idle) { onError("Already in a match"); return; }

        State = MPState.Joining;
        _myMatchId = SafeId(username);

        // Check if anyone is already waiting
        _db.Child("matchmaking").Child("waiting")
            .OrderByChild("timestamp").LimitToFirst(1)
            .GetValueAsync().ContinueWithOnMainThread(t =>
        {
            if (!t.IsCompletedSuccessfully) { onError("Matchmaking error"); State = MPState.Idle; return; }

            DataSnapshot waiting = t.Result;
            bool foundOpponent = false;

            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var child in waiting.Children)
            {
                // Don't match with yourself (same session key)
                if (child.Key == _myMatchId) continue;

                // Skip stale entries older than 2 minutes — they're from crashed/abandoned sessions
                long.TryParse(child.Child("timestamp").Value?.ToString(), out long ts);
                if (now - ts > 120)
                {
                    _db.Child("matchmaking").Child("waiting").Child(child.Key).RemoveValueAsync();
                    continue;
                }

                string oppUsername = child.Child("username").Value?.ToString() ?? "Player";
                int    sharedSeed  = int.Parse(child.Child("seed").Value?.ToString() ?? "12345");
                string oppId       = child.Key;

                foundOpponent = true;

                // Claim the slot — remove opponent from waiting, create room
                _db.Child("matchmaking").Child("waiting").Child(oppId).RemoveValueAsync()
                    .ContinueWithOnMainThread(rem =>
                {
                    if (!rem.IsCompletedSuccessfully)
                    {
                        // Someone else grabbed them first — go to waiting instead
                        JoinWaitingQueue(username, onStatus, onError);
                        return;
                    }

                    // We are the "guest", opponent is "host"
                    IsHost       = false;
                    _mySlot      = "guest";
                    _oppSlot     = "host";
                    _seed        = sharedSeed;
                    _oppUsername = oppUsername;
                    RoomCode     = "QP_" + oppId.Substring(0, Mathf.Min(4, oppId.Length));

                    var data = new Dictionary<string, object>
                    {
                        { "hostUsername",  oppUsername },
                        { "guestUsername", username },
                        { "seed",          _seed },
                        { "state",         "countdown" },
                        { "host",  new Dictionary<string,object>{ {"y",0f},{"score",0},{"alive",true} } },
                        { "guest", new Dictionary<string,object>{ {"y",0f},{"score",0},{"alive",true} } }
                    };

                    _db.Child("rooms").Child(RoomCode).SetValueAsync(data).ContinueWithOnMainThread(cr =>
                    {
                        if (!cr.IsCompletedSuccessfully) { onError("Room create failed"); State = MPState.Idle; return; }

                        _roomRef = _db.Child("rooms").Child(RoomCode);
                        _myRef   = _roomRef.Child(_mySlot);
                        _oppRef  = _roomRef.Child(_oppSlot);

                        // Signal the waiting player via a special node they are listening to
                        _db.Child("matchmaking").Child("matched").Child(oppId).SetValueAsync(RoomCode);

                        State = MPState.Countdown;
                        onStatus("Opponent found! Starting...");
                        ListenForOpponent();
                        StartCoroutine(CountdownThenPlay());
                    });
                });
                break;
            }

            if (!foundOpponent)
                JoinWaitingQueue(username, onStatus, onError);
        });
    }

    void JoinWaitingQueue(string username, System.Action<string> onStatus, System.Action<string> onError)
    {
        _seed = UnityEngine.Random.Range(10000, 99999);

        var entry = new Dictionary<string, object>
        {
            { "username",  username },
            { "timestamp", System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "seed",      _seed }
        };

        _db.Child("matchmaking").Child("waiting").Child(_myMatchId).SetValueAsync(entry)
            .ContinueWithOnMainThread(t =>
        {
            if (!t.IsCompletedSuccessfully) { onError("Queue failed"); State = MPState.Idle; return; }

            State = MPState.WaitingForGuest;
            onStatus("Looking for opponent...");

            // Listen for someone to match us — they write our room code to matched/<myId>
            _matchListener = (_, args) =>
            {
                if (args.DatabaseError != null || !args.Snapshot.Exists) return;

                string roomCode = args.Snapshot.Value?.ToString();
                if (string.IsNullOrEmpty(roomCode)) return;

                // Clean up listeners and waiting entry
                _db.Child("matchmaking").Child("matched").Child(_myMatchId).ValueChanged -= _matchListener;
                _db.Child("matchmaking").Child("waiting").Child(_myMatchId).RemoveValueAsync();
                _db.Child("matchmaking").Child("matched").Child(_myMatchId).RemoveValueAsync();

                // We are the "host" slot in the room the guest created
                IsHost   = true;
                _mySlot  = "host";
                _oppSlot = "guest";
                RoomCode = roomCode;

                _roomRef = _db.Child("rooms").Child(RoomCode);
                _myRef   = _roomRef.Child(_mySlot);
                _oppRef  = _roomRef.Child(_oppSlot);

                // Read guest username from room
                _roomRef.GetValueAsync().ContinueWithOnMainThread(r =>
                {
                    if (r.IsCompletedSuccessfully)
                        _oppUsername = r.Result.Child("guestUsername").Value?.ToString() ?? "Player";

                    State = MPState.Countdown;
                    onStatus("Opponent found! Starting...");
                    ListenForOpponent();
                    StartCoroutine(CountdownThenPlay());
                });
            };

            _db.Child("matchmaking").Child("matched").Child(_myMatchId).ValueChanged += _matchListener;

            // Timeout after 60s if no one joins
            if (_quickPlayTimeout != null) StopCoroutine(_quickPlayTimeout);
            _quickPlayTimeout = StartCoroutine(QuickPlayTimeout(onError));
        });
    }

    IEnumerator QuickPlayTimeout(System.Action<string> onError)
    {
        yield return new WaitForSecondsRealtime(60f);
        if (State != MPState.WaitingForGuest) yield break;

        // Clean up matchmaking queue
        _db.Child("matchmaking").Child("waiting").Child(_myMatchId).RemoveValueAsync();
        if (_matchListener != null)
        {
            _db.Child("matchmaking").Child("matched").Child(_myMatchId).ValueChanged -= _matchListener;
            _matchListener = null;
        }

        // Notify UI so it can show a friendly "no players found" message before the bot starts.
        OnNoPlayersFound?.Invoke();

        // Give the UI 2 seconds to display the message, then start the bot match.
        yield return new WaitForSecondsRealtime(2f);
        StartBotMatch();
    }

    // ──────────────────────────────────────────────────────
    // BOT OPPONENT — fires when no real player found in 60s
    // ──────────────────────────────────────────────────────

    private bool       _isBotMatch;
    private Coroutine  _botCoroutine;

    static readonly string[] BOT_NAMES = {
        "BlowfishBot", "TunaBot", "NemoBot", "DoryBot", "MarlinBot"
    };

    void StartBotMatch()
    {
        _isBotMatch  = true;
        _oppUsername = BOT_NAMES[UnityEngine.Random.Range(0, BOT_NAMES.Length)];

        // No Firebase room needed — bot is purely local
        _mySlot  = "player";
        _oppSlot = "bot";
        IsHost   = true;

        State = MPState.Countdown;
        StartCoroutine(BotCountdownThenPlay());
    }

    IEnumerator BotCountdownThenPlay()
    {
        // Notify UI — shows match-found overlay then closes panel.
        OnMatchFound?.Invoke(_oppUsername);
        yield return new WaitForSecondsRealtime(1.5f);

        ShowCountdownText("3");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("2");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("1");
        yield return new WaitForSecondsRealtime(1f);
        ShowCountdownText("GO!");
        yield return new WaitForSecondsRealtime(0.5f);
        HideCountdown();

        GhostRaceManager.Instance?.StopGhostPlayback();

        UnityEngine.Random.InitState(_seed);
        State = MPState.Playing;
        IsMultiplayerGame = true;

        GameBootstrap.Instance.StartMultiplayerRound();

        Sprite playerSprite = GameBootstrap.Instance.player?.GetComponent<SpriteRenderer>()?.sprite;
        float  playerScale  = GameBootstrap.Instance.player != null
            ? GameBootstrap.Instance.player.transform.localScale.x : 1.4f;
        CreateGhostFish(playerSprite, playerScale);
        CreateOppScoreBadge();

        // No real sync needed — bot drives itself locally
        _botCoroutine = StartCoroutine(BotSimulation());
    }

    IEnumerator BotSimulation()
    {
        // Skill-matched death score: bot targets the player's personal best ± variance.
        // This ensures the match always feels competitive regardless of player level.
        int pb = PlayerPrefs.GetInt("BestScore", 0);
        int botDeathScore;
        if (pb <= 5)
            // New player: bot dies slightly above their PB so winning feels earned
            botDeathScore = Mathf.Max(3, pb + UnityEngine.Random.Range(1, 4));
        else if (pb <= 20)
            // Developing player: close race, bot might beat or lose to them
            botDeathScore = pb + UnityEngine.Random.Range(-2, 4);
        else
            // Experienced player: bot is a real challenge, dies around their PB
            botDeathScore = Mathf.Max(10, pb + UnityEngine.Random.Range(-5, 3));

        float botY   = 0f;
        float botVel = 0f;
        const float GRAVITY    = -19.62f;
        const float JUMP_FORCE = 10.5f;

        float nextTapIn = UnityEngine.Random.Range(0.3f, 0.6f);

        while (State == MPState.Playing)
        {
            float dt = Time.deltaTime;
            nextTapIn -= dt;

            if (nextTapIn <= 0f)
            {
                botVel    = Mathf.Max(botVel, 0f) + JUMP_FORCE;
                nextTapIn = UnityEngine.Random.Range(0.42f, 0.72f);
            }

            botVel += GRAVITY * dt;
            botVel  = Mathf.Clamp(botVel, -14f, 14f);
            botY   += botVel * dt;

            float halfScreen = Camera.main != null ? Camera.main.orthographicSize - 1f : 10f;
            float groundTop  = GameBootstrap.Instance != null ? GameBootstrap.Instance.groundTop + 0.5f : -9f;
            if (botY > halfScreen) { botY = halfScreen; botVel = -2f; }
            if (botY < groundTop)  { botY = groundTop;  botVel = 0f;  }

            _ghostTargetY  = botY;
            _opponentScore = Mathf.Min(GameBootstrap.Instance?.Score ?? 0, botDeathScore);

            // Bot dies the moment player passes its death score
            if ((GameBootstrap.Instance?.Score ?? 0) >= botDeathScore)
            {
                _opponentAlive = false;
                OnOpponentDied();
                yield break;
            }

            yield return null;
        }
    }

    static string SafeId(string username)
    {
        string clean = System.Text.RegularExpressions.Regex.Replace(username ?? "anon", @"[.$#\[\]/\s]", "_");
        return clean + "_" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(8);
    }

    // ──────────────────────────────────────────────────────
    // CLEANUP
    // ──────────────────────────────────────────────────────

    public void AbortMultiplayer()
    {
        State = MPState.Idle;
        IsMultiplayerGame = false;
        _opponentAlive = true;
        _isBotMatch    = false;

        if (_syncCoroutine    != null) StopCoroutine(_syncCoroutine);
        if (_quickPlayTimeout != null) StopCoroutine(_quickPlayTimeout);
        if (_botCoroutine     != null) StopCoroutine(_botCoroutine);

        if (!_isBotMatch && _roomRef != null && _myRef != null)
            _myRef.UpdateChildrenAsync(new Dictionary<string, object> { { "alive", false } });

        if (_oppListener != null && _oppRef != null)
            _oppRef.ValueChanged -= _oppListener;
        if (_roomStateListener != null && _roomRef != null)
            _roomRef.ValueChanged -= _roomStateListener;
        if (_matchListener != null && _db != null && _myMatchId != null)
        {
            _db.Child("matchmaking").Child("matched").Child(_myMatchId).ValueChanged -= _matchListener;
            _db.Child("matchmaking").Child("waiting").Child(_myMatchId).RemoveValueAsync();
        }

        DestroyGhost();
        if (_oppScoreBadge != null) Destroy(_oppScoreBadge.gameObject);
        if (_countdownText  != null) Destroy(_countdownText.gameObject);
        _oppListener       = null;
        _roomStateListener = null;
        _matchListener     = null;
        _myMatchId         = null;
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
            code.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return code.ToString();
    }
}
