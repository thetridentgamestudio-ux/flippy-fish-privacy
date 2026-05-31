using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPos;
    private float shakeTime = 0f;
    private float shakePower = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTime > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakePower;
            shakeTime -= Time.deltaTime;
        }
        else
        {
            shakeTime = 0;
            transform.localPosition = originalPos;
        }
    }

    public void Shake(float duration, float power)
    {
        shakeTime = duration;
        shakePower = power;
    }
}