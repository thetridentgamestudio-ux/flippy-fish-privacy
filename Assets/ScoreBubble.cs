using UnityEngine;
using TMPro;

public class ScoreBubble : MonoBehaviour
{
    private float        floatSpeed;
    private float        lifetime;
    private float        timer = 0f;
    private TextMeshPro  _tmp;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
    }

    public void Initialize(float speed, float life)
    {
        floatSpeed = speed;
        lifetime   = life;
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        timer += Time.deltaTime;

        if (_tmp != null)
        {
            Color c = _tmp.color;
            c.a = Mathf.Lerp(1f, 0f, timer / lifetime);
            _tmp.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
