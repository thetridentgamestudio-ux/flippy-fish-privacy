using UnityEngine;
using System.Collections;

/// <summary>
/// Haptic feedback system — vibrate on collisions, coin collect, power-up.
/// All Unity APIs (Handheld.Vibrate) are called on the main thread via coroutines.
/// </summary>
public class HapticFeedback : MonoBehaviour
{
    static HapticFeedback _instance;

    void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public static void PlayCollisionHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }

    public static void PlayCoinHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }

    /// <summary>
    /// Two short vibrations separated by 100 ms.
    /// Uses a coroutine so both calls stay on the Unity main thread.
    /// </summary>
    public static void PlayPowerUpHaptic()
    {
        if (_instance == null) return;
        _instance.StartCoroutine(_instance.DoDoubleVibrate());
    }

    IEnumerator DoDoubleVibrate()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        yield return new WaitForSecondsRealtime(0.1f);
        Handheld.Vibrate();
        #else
        yield break;
        #endif
    }

    public static void PlayJumpHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }
}
