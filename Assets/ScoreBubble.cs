using UnityEngine;
using TMPro;

public class ScoreBubble : MonoBehaviour
{
    private float floatSpeed;
    private float lifetime;
    private float timer = 0f;

    public void Initialize(float speed, float life)
    {
        floatSpeed = speed;
        lifetime = life;
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        timer += Time.deltaTime;

        // fade out gradually
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        TextMeshPro tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}