using UnityEngine;

public class FloatingLetter : MonoBehaviour
{
    float speed;
    float amplitude;
    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
        speed = Random.Range(1f, 2f);
        amplitude = Random.Range(10f, 20f);
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = startPos + new Vector3(0, y, 0);
    }
}