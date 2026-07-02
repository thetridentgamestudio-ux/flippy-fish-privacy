using UnityEngine;

/// Attached to a pipeGroup to make it oscillate vertically while moving left.
/// Both top and bottom pipes are children of the group, so the gap stays intact.
/// Only active while the game is playing — freezes on game over.
public class PipeOscillator : MonoBehaviour
{
    [HideInInspector] public float amplitude = 2.5f;  // world units (cam ortho=12.5, so 2.5 = 20% screen)
    [HideInInspector] public float frequency = 0.50f; // Hz, set by SpawnPipe

    float _originY;
    float _phase;

    void Awake()
    {
        _originY = transform.position.y; // always 0 since pipeGroup spawns at Y=0
        _phase   = Random.Range(0f, Mathf.PI * 2f); // each pipe starts at a random point in the wave
    }

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.IsGameOver) return;
        if (GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing) return;

        float y = _originY + Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + _phase) * amplitude;
        var p = transform.position;
        transform.position = new Vector3(p.x, y, p.z);
    }
}
