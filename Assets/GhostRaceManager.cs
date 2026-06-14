using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

/// <summary>
/// Ghost Race — async multiplayer for Flappy Fish.
///
/// Every time a player sets a new personal best the run is saved to Firebase
/// as a "ghost": tap timestamps + Y positions at each tap.
///
/// When a new game starts, a random top-10 ghost is fetched from Firebase and
/// replayed as a translucent fish swimming alongside the player. Y positions
/// are snapped on each tap so drift never accumulates over long runs.
///
/// Call order:
///   1. Initialize(dbRef)        — called once when Firebase is ready
///   2. LoadRandomGhost()        — called after Initialize; fetches opponent
///   3. StartRecording()         — called on first real tap (StartPipeSpawning)
///   4. RecordTap(y)             — called every flap in PlayerController
///   5. EndRun(score, username)  — called in TriggerGameOver; saves if PB
///   6. BeginGhostPlayback(spr)  — called in StartPipeSpawning after recording starts
///   7. StopGhostPlayback()      — called in RestartGame / StartGame reset
/// </summary>
public class GhostRaceManager : MonoBehaviour
{
    public static GhostRaceManager Instance { get; private set; }

    // ── Firebase ───────────────────────────────────────────
    private DatabaseReference _db;

    // ── Recording (current run) ────────────────────────────
    private readonly List<float> _tapTimes     = new();
    private readonly List<float> _tapPositions = new();
    private float _runStartTime;
    private bool  _isRecording;

    // ── Loaded ghost data ──────────────────────────────────
    private List<float> _ghostTapTimes;
    private List<float> _ghostTapPositions;
    private float       _ghostDuration;
    private string      _ghostUsername;
    private int         _ghostScore;

    // ── Ghost visual ───────────────────────────────────────
    private GameObject       _ghostFish;
    private SpriteRenderer   _ghostSR;
    private Coroutine        _playbackCoroutine;
    private bool             _isPlayingBack;

    // ── UI banner (top-right, shows who you're racing) ─────
    private TextMeshProUGUI  _racingBanner;

    // Physics constants — must match PlayerController / GameBootstrap values
    private const float GRAVITY    = -19.62f; // Physics2D.gravity.y × gravityScale 2
    private const float JUMP_FORCE =  12.5f;
    private const float GHOST_X    = -1.8f;   // slightly left of player spawn

    // ── Lifecycle ──────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    public void Initialize(DatabaseReference db)
    {
        _db = db;
    }

    // ── Recording ──────────────────────────────────────────

    public void StartRecording()
    {
        _tapTimes.Clear();
        _tapPositions.Clear();
        _runStartTime = Time.realtimeSinceStartup;
        _isRecording  = true;
    }

    // Called by PlayerController on every flap (including first-tap)
    public void RecordTap(float playerY)
    {
        if (!_isRecording) return;
        _tapTimes.Add(Time.realtimeSinceStartup - _runStartTime);
        _tapPositions.Add(playerY);
    }

    // Called in TriggerGameOver
    public void EndRun(int score, string username)
    {
        _isRecording = false;
        if (_tapTimes.Count < 3)
        {
            Debug.Log($"[Ghost] EndRun skipped: only {_tapTimes.Count} taps recorded (need >=3). Was StartRecording() called?");
            return;
        }

        int best = PlayerPrefs.GetInt("BestScore", 0);
        Debug.Log($"[Ghost] EndRun: score={score} best={score} taps={_tapTimes.Count} dbReady={_db != null}");

        if (score > 0 && _db != null)
            SaveGhost(username, score, Time.realtimeSinceStartup - _runStartTime);
        else if (_db == null)
            Debug.LogWarning("[Ghost] EndRun: Firebase DB not initialized yet — ghost not saved.");
    }

    // ── Saving ─────────────────────────────────────────────

    void SaveGhost(string username, int score, float duration)
    {
        string timesStr = string.Join(",",
            _tapTimes.ConvertAll(t => t.ToString("F3")));
        string posStr = string.Join(",",
            _tapPositions.ConvertAll(p => p.ToString("F3")));

        var data = new Dictionary<string, object>
        {
            { "score",     score },
            { "duration",  duration.ToString("F2") },
            { "taps",      timesStr },
            { "positions", posStr }
        };

        string key = SafeFirebaseKey(username);
        _db.Child("ghosts").Child(key).SetValueAsync(data)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsCompletedSuccessfully)
                    Debug.Log($"[Ghost] Saved ghost for {username} (score {score}, {_tapTimes.Count} taps)");
                else
                    Debug.LogWarning("[Ghost] Save failed: " + t.Exception?.Message);
            });
    }

    // ── Loading ────────────────────────────────────────────

    public void LoadRandomGhost(System.Action onLoaded = null)
    {
        if (_db == null) return;

        _db.Child("ghosts").OrderByChild("score").LimitToLast(10)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                {
                    Debug.Log("[Ghost] No ghosts found in Firebase yet.");
                    return;
                }

                string currentUser = SafeFirebaseKey(
                    PlayerPrefs.GetString("USERNAME", ""));

                var candidates = new List<DataSnapshot>();
                foreach (var child in task.Result.Children)
                    if (child.Key != currentUser)
                        candidates.Add(child);

                // Fall back to own ghost if no opponents yet
                if (candidates.Count == 0)
                    foreach (var child in task.Result.Children)
                        candidates.Add(child);

                if (candidates.Count == 0) return;

                var chosen = candidates[Random.Range(0, candidates.Count)];
                if (ParseGhost(chosen))
                    onLoaded?.Invoke();
            });
    }

    bool ParseGhost(DataSnapshot snap)
    {
        try
        {
            _ghostUsername = snap.Key;
            _ghostScore    = int.Parse(snap.Child("score").Value.ToString());
            _ghostDuration = float.Parse(snap.Child("duration").Value.ToString());

            string timesStr = snap.Child("taps").Value?.ToString()      ?? "";
            string posStr   = snap.Child("positions").Value?.ToString() ?? "";

            _ghostTapTimes     = new List<float>();
            _ghostTapPositions = new List<float>();

            foreach (var s in timesStr.Split(','))
                if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                    _ghostTapTimes.Add(v);

            foreach (var s in posStr.Split(','))
                if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                    _ghostTapPositions.Add(v);

            Debug.Log($"[Ghost] Loaded ghost: {_ghostUsername} score={_ghostScore} taps={_ghostTapTimes.Count}");
            return _ghostTapTimes.Count > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Ghost] Parse error: " + e.Message);
            _ghostTapTimes = null;
            return false;
        }
    }

    // ── Playback ───────────────────────────────────────────

    public void BeginGhostPlayback(Sprite fishSprite, Canvas uiCanvas, float playerScale = 1.4f)
    {
        if (_ghostTapTimes == null || _ghostTapTimes.Count == 0) return;

        StopGhostPlayback();
        CreateGhostFish(fishSprite, playerScale);
        CreateRacingBanner(uiCanvas);
        _isPlayingBack     = true;
        _playbackCoroutine = StartCoroutine(PlaybackCoroutine());
    }

    public void StopGhostPlayback()
    {
        _isPlayingBack = false;
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }
        if (_ghostFish != null) { Destroy(_ghostFish); _ghostFish = null; }
        if (_racingBanner != null) { _racingBanner.gameObject.SetActive(false); }
    }

    void CreateGhostFish(Sprite sprite, float playerScale = 1.4f)
    {
        _ghostFish = new GameObject("GhostFish");
        _ghostFish.transform.position = new Vector3(GHOST_X, 0f, 0.1f);
        _ghostFish.transform.localScale = Vector3.one * playerScale * 0.85f; // slightly smaller than player

        _ghostSR = _ghostFish.AddComponent<SpriteRenderer>();
        if (sprite != null) _ghostSR.sprite = sprite;
        _ghostSR.color        = new Color(0.55f, 0.88f, 1f, 0.35f); // translucent cyan
        _ghostSR.sortingOrder = 4; // player is at 5

        // World-space name tag above the ghost fish
        var labelGO = new GameObject("GhostLabel");
        labelGO.transform.SetParent(_ghostFish.transform);
        labelGO.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        labelGO.transform.localScale    = Vector3.one * 0.6f;

        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = $"<b>{_ghostUsername}</b>\n<size=80%>Best {_ghostScore}</size>";
        tmp.fontSize  = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.7f, 1f, 1f, 0.9f);
        tmp.sortingOrder = 5;
    }

    void CreateRacingBanner(Canvas canvas)
    {
        if (canvas == null) return;

        if (_racingBanner == null)
        {
            var go = new GameObject("GhostRacingBanner");
            go.transform.SetParent(canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-Screen.width * 0.03f, -Screen.height * 0.04f);
            rt.sizeDelta        = new Vector2(Screen.width * 0.38f, Screen.height * 0.08f);

            _racingBanner           = go.AddComponent<TextMeshProUGUI>();
            _racingBanner.fontSize  = Screen.height * 0.022f;
            _racingBanner.alignment = TextAlignmentOptions.Right;
            _racingBanner.color     = new Color(0.7f, 1f, 1f, 0.9f);
        }

        _racingBanner.text = $"👻 Racing <b>{_ghostUsername}</b> ({_ghostScore})";
        _racingBanner.gameObject.SetActive(true);
    }

    IEnumerator PlaybackCoroutine()
    {
        float elapsed    = 0f;
        int   nextTapIdx = 0;
        float ghostY     = 0f;
        float ghostVel   = 0f;

        while (_isPlayingBack && elapsed < _ghostDuration + 2f)
        {
            elapsed += Time.deltaTime;

            // Fire any taps whose timestamp has passed
            while (nextTapIdx < _ghostTapTimes.Count &&
                   elapsed >= _ghostTapTimes[nextTapIdx])
            {
                // Snap Y to recorded position — eliminates physics drift
                if (nextTapIdx < _ghostTapPositions.Count)
                    ghostY = _ghostTapPositions[nextTapIdx];

                // Cancel downward velocity then jump (mirrors SwimUp)
                ghostVel = Mathf.Max(ghostVel, 0f) + JUMP_FORCE * 0.82f;
                nextTapIdx++;
            }

            // Simple Euler integration
            ghostVel  += GRAVITY * Time.deltaTime;
            ghostVel   = Mathf.Clamp(ghostVel, -6f, 6f);
            ghostY    += ghostVel * Time.deltaTime;

            if (_ghostFish != null)
            {
                _ghostFish.transform.position = new Vector3(GHOST_X, ghostY, 0.1f);
                float angle = Mathf.Clamp(ghostVel * 5f, -45f, 25f);
                _ghostFish.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        yield return FadeOutGhost();
    }

    IEnumerator FadeOutGhost()
    {
        if (_ghostSR == null) yield break;
        float t          = 0f;
        Color startColor = _ghostSR.color;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            if (_ghostSR != null)
                _ghostSR.color = new Color(
                    startColor.r, startColor.g, startColor.b,
                    Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }
        if (_ghostFish != null) { Destroy(_ghostFish); _ghostFish = null; }
        if (_racingBanner != null) _racingBanner.gameObject.SetActive(false);
    }

    // ── Helpers ────────────────────────────────────────────

    public bool HasGhost => _ghostTapTimes != null && _ghostTapTimes.Count > 0;

    static string SafeFirebaseKey(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name ?? "anon", @"[.$#\[\]/\s]", "_");
}
