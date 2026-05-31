using UnityEngine;

public class Moving : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.IsGameOver) return;
        if (GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing) return;

        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < -15f)
            Destroy(gameObject);
    }
}