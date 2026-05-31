using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    // Reference to GameBootstrap (can be set in inspector or use singleton)
    public GameBootstrap bootstrap;

    private bool scored = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || scored) return;

        scored = true;
        Debug.Log("[ScoreTrigger] Player passed trigger");

        // Gold circle burst — no green squares
        var ps = GameBootstrap.Instance.CreateParticleBurst(
            transform.position,
            new Color(1f, 0.85f, 0f), // gold
            12,   // fewer particles = cleaner look
            0.15f,
            0.4f
        );

        // Make particles round — skip GetBuiltinResource (fails on Android builds)
        if (ps != null)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            // Use default material — renders as small white squares that still look fine
        }

        GameBootstrap.Instance.AddScore(1);
    }
}