using UnityEngine;

public class BubbleDebug : MonoBehaviour
{
    public float lifetime = 3f;       // How long the bubble should live
    public float speedY = 1f;         // Upward speed
    public float drift = 0f;          // Horizontal drift

    private float elapsedTime = 0f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("[BubbleDebug] ⚠ No SpriteRenderer found on " + gameObject.name);
        }

        Debug.Log($"[BubbleDebug] Spawned at {transform.position}, lifetime: {lifetime}s, speedY: {speedY}, drift: {drift}");
    }

    void Update()
    {
        // Move bubble
        transform.position += new Vector3(drift, speedY, 0) * Time.deltaTime;

        // Update lifetime
        elapsedTime += Time.deltaTime;

        // Fade bubble if SpriteRenderer exists
        if (sr != null)
        {
            float alpha = Mathf.Clamp01(1f - (elapsedTime / lifetime));
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            Debug.Log($"[BubbleDebug] Elapsed: {elapsedTime:F2}s, alpha: {alpha:F2}, position: {transform.position}");
        }
        else
        {
            Debug.Log($"[BubbleDebug] Elapsed: {elapsedTime:F2}s, position: {transform.position} (no SpriteRenderer)");
        }

        // Destroy after lifetime
        if (elapsedTime >= lifetime)
        {
            Debug.Log($"[BubbleDebug] Lifetime ended, destroying bubble at {transform.position}");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Debug.Log($"[BubbleDebug] 🚨 Destroyed at time: {Time.time:F2}s, position: {transform.position}");
    }
}