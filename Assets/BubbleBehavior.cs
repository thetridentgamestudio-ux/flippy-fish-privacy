using UnityEngine;
public class BubbleBehavior : MonoBehaviour
{
    public float shrinkSpeed = 0.2f;

    void Update()
    {
        transform.localScale -= Vector3.one * shrinkSpeed * Time.deltaTime;
        if (transform.localScale.x <= 0.1f)
            Destroy(gameObject);
    }
}