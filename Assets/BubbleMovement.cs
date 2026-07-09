using UnityEngine;

public class BubbleMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float lifetime      = 4f;
    public float speedY        = 1f;
    public float driftX        = 0f;

    [Header("Wave Motion")]
    public bool  enableWave     = true;
    public float waveAmplitude  = 0.05f;
    public float waveFrequency  = 1.5f;

    private float         _elapsed;
    private Vector3       _startPos;
    private BubbleSpawner _spawner;

    // Called by BubbleSpawner after pulling from pool
    public void SetSpawner(BubbleSpawner spawner) => _spawner = spawner;

    void OnEnable()
    {
        _elapsed  = 0f;
        _startPos = transform.position;
    }

    void Update()
    {
        float deltaX = driftX * Time.deltaTime;
        if (enableWave)
            deltaX += Mathf.Sin(Time.time * waveFrequency + _startPos.y) * waveAmplitude * Time.deltaTime;

        transform.Translate(deltaX, speedY * Time.deltaTime, 0);

        _elapsed += Time.deltaTime;
        if (_elapsed >= lifetime)
            ReturnOrDestroy();
    }

    void ReturnOrDestroy()
    {
        if (_spawner != null)
            _spawner.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}
