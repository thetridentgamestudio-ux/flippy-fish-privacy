using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private float    speed = 1f;
    private TextMesh _tm;

    void Awake()
    {
        _tm = GetComponent<TextMesh>();
    }

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        if (_tm != null)
        {
            Color c = _tm.color;
            c.a -= Time.deltaTime;
            _tm.color = c;
        }

        Destroy(gameObject, 1f);
    }
}
