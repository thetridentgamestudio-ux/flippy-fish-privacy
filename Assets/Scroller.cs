using UnityEngine;

public class Scroller : MonoBehaviour
{
    public float scrollSpeed = 3f;

    void Update()
    {
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Destroy if off-screen
        if (transform.position.x < -15f)
            Destroy(gameObject);
    }
}