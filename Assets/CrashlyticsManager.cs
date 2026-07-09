using UnityEngine;
// Crashlytics API calls are guarded by FIREBASE_CRASHLYTICS_ENABLED.
// To enable: add Firebase Crashlytics Unity SDK (com.google.firebase.crashlytics)
// via the Firebase Unity SDK package, then add FIREBASE_CRASHLYTICS_ENABLED
// to Player Settings → Scripting Define Symbols.
#if FIREBASE_CRASHLYTICS_ENABLED
using Firebase.Crashlytics;
#endif

/// <summary>
/// Captures unhandled exceptions and Unity log errors, forwarding them to
/// Firebase Crashlytics in Release builds.
///
/// Initialization: call CrashlyticsManager.Initialize() once after Firebase
/// is ready (FirebaseGameManager.InitializeFirebase).
///
/// Privacy: player username is the only PII logged. No device IDs, emails,
/// locations, or financial data are ever sent.
/// </summary>
public static class CrashlyticsManager
{
    private static bool _initialized;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call once after FirebaseApp.CheckAndFixDependenciesAsync succeeds.
    /// Safe to call multiple times — duplicate calls are ignored.
    /// </summary>
    public static void Initialize(string username = null)
    {
        if (_initialized) return;
        _initialized = true;

#if FIREBASE_CRASHLYTICS_ENABLED && !DEBUG && !UNITY_EDITOR
        // Enable automatic data collection (crash reports) in Release builds only.
        Crashlytics.IsCrashlyticsCollectionEnabled = true;

        // Tag the session with the player's username so reports can be grouped.
        // Username is the only PII — it is already public on the leaderboard.
        if (!string.IsNullOrEmpty(username))
            Crashlytics.SetUserId(username.Substring(0, Mathf.Min(username.Length, 32)));
#elif FIREBASE_CRASHLYTICS_ENABLED && (DEBUG || UNITY_EDITOR)
        // Disable collection in editor / debug builds so test crashes don't pollute
        // the dashboard. Errors still hit the Unity console via the log handler below.
        Crashlytics.IsCrashlyticsCollectionEnabled = false;
#endif

        // Unity log callback captures all unhandled exceptions regardless of whether
        // the Crashlytics SDK is installed, giving a fallback for debug builds.
        Application.logMessageReceivedThreaded += OnLogMessage;
    }

    /// <summary>Attach the player's display name to all future crash reports.</summary>
    public static void SetUsername(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
#if FIREBASE_CRASHLYTICS_ENABLED
        Crashlytics.SetUserId(username.Substring(0, Mathf.Min(username.Length, 32)));
#endif
    }

    /// <summary>Record a non-fatal exception (e.g. Firebase timeout, parse error).</summary>
    public static void RecordException(System.Exception e)
    {
        if (e == null) return;
#if FIREBASE_CRASHLYTICS_ENABLED && !UNITY_EDITOR
        Crashlytics.LogException(e);
#else
        Debug.LogError($"[Crashlytics] Non-fatal: {e}");
#endif
    }

    /// <summary>Add a breadcrumb log line visible in the crash report.</summary>
    public static void Log(string message)
    {
#if FIREBASE_CRASHLYTICS_ENABLED && !UNITY_EDITOR
        Crashlytics.Log(message);
#endif
    }

    // ── Unity log handler ─────────────────────────────────────────────────────

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        // Only forward actual exceptions and errors — not warnings or info.
        if (type != LogType.Exception && type != LogType.Error) return;

        // Ignore known benign Unity internal messages.
        if (condition.StartsWith("ExecuteEvents.Execute")) return;

#if FIREBASE_CRASHLYTICS_ENABLED && !UNITY_EDITOR
        try
        {
            Crashlytics.Log($"{type}: {condition}\n{stackTrace}");
        }
        catch
        {
            // Never let the crash reporter itself crash the game.
        }
#endif
    }
}
