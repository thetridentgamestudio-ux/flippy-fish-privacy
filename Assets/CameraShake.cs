using UnityEngine;
using System.Collections;

/// <summary>
/// Camera shake for major events (collision, power-ups)
/// Creates visual impact without complex particle effects
/// </summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private Camera cam;
    private static CameraShake instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        originalPos = cam.transform.position;
    }

    public static void ShakeOnCollision(float duration = 0.15f, float intensity = 0.2f)
    {
        if (instance != null)
            instance.StartCoroutine(instance.Shake(duration, intensity));
    }

    public static void ShakePowerUp(float duration = 0.2f, float intensity = 0.3f)
    {
        if (instance != null)
            instance.StartCoroutine(instance.Shake(duration, intensity));
    }

    private IEnumerator Shake(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            
            cam.transform.position = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.position = originalPos;
    }
}