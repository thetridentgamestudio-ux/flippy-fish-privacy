using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public float speed = 0.5f;
    private Transform[] backgrounds;
    private float bgWidth;
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        backgrounds = new Transform[2];
        backgrounds[0] = transform.GetChild(0);
        backgrounds[1] = transform.GetChild(1);
        bgWidth = backgrounds[0].GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        if (GameBootstrap.Instance != null && GameBootstrap.Instance.IsGameOver) return;

        foreach (var bg in backgrounds)
        {
            bg.position += Vector3.left * speed * Time.deltaTime;

            if (bg.position.x <= -bgWidth + _cam.transform.position.x)
            {
                Transform other = backgrounds[0] == bg ? backgrounds[1] : backgrounds[0];
                bg.position = new Vector3(other.position.x + bgWidth, bg.position.y, bg.position.z);
            }
        }
    }
}