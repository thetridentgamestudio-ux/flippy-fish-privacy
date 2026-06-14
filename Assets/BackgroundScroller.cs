using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    public float speed = 1f;
    private Vector3 startPos;
    private GameBootstrap bootstrap;

    void Start()
    {
        startPos = transform.position;
        bootstrap = FindObjectOfType<GameBootstrap>();

    }

    void Update()
    {
        // Stop moving if game is over
        if (bootstrap != null && bootstrap.IsGameOver)
            return;

        transform.position += Vector3.left * speed * Time.deltaTime;

        // Optional: Loop background for parallax
        if (transform.position.x < -20f)
            transform.position = startPos;
    }
}