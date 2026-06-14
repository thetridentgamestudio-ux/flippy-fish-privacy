using UnityEngine;

public class Bubble : MonoBehaviour
{
    public float ignoreTime = 0.3f; // small delay before player can collect
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (timer < ignoreTime)
            return; // ignore early collisions

        if (other.CompareTag("Player"))
        {
            Destroy(gameObject); // destroy when collected after delay
        }
    }
}