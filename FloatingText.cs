using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private float speed = 1f;

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        // Fade out
        TextMesh tm = GetComponent<TextMesh>();
        if (tm != null)
        {
            Color c = tm.color;
            c.a -= Time.deltaTime;
            tm.color = c;
        }

        Destroy(gameObject, 1f);
    }
}