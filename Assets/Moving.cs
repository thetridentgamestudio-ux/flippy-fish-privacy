using UnityEngine;

public class Moving : MonoBehaviour
{
    public float speed = 3f;

    // When true, returning to PipePool instead of Destroy — set by SpawnPipe for pipeGroups only
    [HideInInspector] public bool returnToPool = false;

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.IsGameOver) return;
        if (GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing) return;

        transform.position += Vector3.left * speed * PowerUpManager.PipeSpeedMultiplier * Time.deltaTime;

        if (transform.position.x < -15f)
        {
            if (returnToPool)
                PipePool.ReturnPipeGroup(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
