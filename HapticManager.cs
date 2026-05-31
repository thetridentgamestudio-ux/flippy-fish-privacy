using UnityEngine;

/// <summary>
/// Haptic feedback — works on Android API 26+ (including API 36 as confirmed in logs).
/// Uses VibrationEffect API (required since API 26, old vibrate(long) removed in API 33).
/// Requires VIBRATE permission in AndroidManifest.xml — see note below.
///
/// AndroidManifest.xml — add inside <manifest> tag (not inside <application>):
///   <uses-permission android:name="android.permission.VIBRATE"/>
///
/// Usage:
///   HapticManager.Score()    light tick on each pipe passed
///   HapticManager.Death()    firm thud on collision
///   HapticManager.Revive()   double pulse on revive
///   HapticManager.NewBest()  triple escalating pulse on new record
/// </summary>
public static class HapticManager
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject _vibrator;
    static AndroidJavaClass  _effectClass;
    static bool _ready;

    static void Init()
    {
        if (_ready) return;
        _ready = true; // set early so failed init doesn't retry every frame
        try
        {
            AndroidJavaClass  player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");

            // API 31+: use VibratorManager to get the default vibrator
            if (AndroidVersion() >= 31)
            {
                AndroidJavaObject mgr = activity.Call<AndroidJavaObject>(
                    "getSystemService", "vibrator_manager");
                if (mgr != null)
                    _vibrator = mgr.Call<AndroidJavaObject>("getDefaultVibrator");
            }

            // Fallback for API 26-30
            if (_vibrator == null)
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
        }
        catch { /* fail silently on devices without vibrator */ }
    }

    static int AndroidVersion()
    {
        try
        {
            AndroidJavaClass build = new AndroidJavaClass("android.os.Build$VERSION");
            return build.GetStatic<int>("SDK_INT");
        }
        catch { return 26; }
    }

    // Single buzz using VibrationEffect.createOneShot(ms, amplitude)
    // amplitude: 1=lightest, 255=strongest, -1=device default
    static void Buzz(long ms, int amplitude = -1)
    {
        Init();
        if (_vibrator == null || _effectClass == null) return;
        try
        {
            AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>(
                "createOneShot", ms, amplitude);
            _vibrator.Call("vibrate", effect);
        }
        catch { }
    }

    // Waveform: timings = [off, on, off, on...], amplitudes = matching levels
    static void Wave(long[] timings, int[] amplitudes)
    {
        Init();
        if (_vibrator == null || _effectClass == null) return;
        try
        {
            AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>(
                "createWaveform", timings, amplitudes, -1); // -1 = no repeat
            _vibrator.Call("vibrate", effect);
        }
        catch { }
    }

#else
    // Editor / iOS fallback
    static void Buzz(long ms, int amplitude = -1) { }
    static void Wave(long[] t, int[] a) { }
#endif

    /// Light tick — every pipe passed
    public static void Score()
        => Buzz(15, 80);

    /// Firm thud — normal death
    public static void Death()
        => Buzz(80, 200);

    /// Double pulse — successful revive
    public static void Revive()
        => Wave(new long[] { 0, 40, 60, 40 }, new int[] { 0, 160, 0, 160 });

    /// Triple escalating pulse — new personal best
    public static void NewBest()
        => Wave(new long[] { 0, 40, 50, 60, 50, 100 }, new int[] { 0, 120, 0, 180, 0, 255 });
}