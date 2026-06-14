using UnityEngine;

/// <summary>
/// Haptic feedback system — vibrate on collisions, coin collect, power-up
/// Works on Android/iOS via Handheld.Vibrate()
/// </summary>
public class HapticFeedback : MonoBehaviour
{
    public static void PlayCollisionHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }

    public static void PlayCoinHaptic()
    {
        // Light tap
        #if UNITY_ANDROID
        Handheld.Vibrate();
        #elif UNITY_IOS
        // iOS haptics via plugins would go here
        Handheld.Vibrate();
        #endif
    }

    public static void PlayPowerUpHaptic()
    {
        // Strong vibration with delay
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        // Schedule second vibration
        var task = System.Threading.Tasks.Task.Delay(100);
        task.ContinueWith(_ => {
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
        });
        #endif
    }

    public static void PlayJumpHaptic()
    {
        // Quick tap
        #if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        #endif
    }
}