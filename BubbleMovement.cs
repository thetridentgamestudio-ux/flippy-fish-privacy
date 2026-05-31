using UnityEngine;

public class BubbleMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float lifetime = 4f;    // max lifetime
    public float speedY = 1f;      // upward speed
    public float driftX = 0f;      // small horizontal drift

    [Header("Wave Motion")]
    public bool enableWave = true;
    public float waveAmplitude = 0.05f;  // how much it swings left/right
    public float waveFrequency = 1.5f;   // speed of swing

    private float elapsedTime = 0f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Vertical movement
        float deltaY = speedY * Time.deltaTime;

        // Horizontal drift + wave oscillation
        float deltaX = driftX * Time.deltaTime;
        if (enableWave)
        {
            deltaX += Mathf.Sin(Time.time * waveFrequency + startPos.y) * waveAmplitude * Time.deltaTime;
        }

        transform.Translate(deltaX, deltaY, 0);

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}