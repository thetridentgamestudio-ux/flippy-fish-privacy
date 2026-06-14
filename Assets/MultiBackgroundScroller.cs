using UnityEngine;

[System.Serializable]
public class BackgroundLayer
{
    public Transform layerTransform;
    public float speed = 0.3f; // slower = further back
    [HideInInspector] public float width;
}

public class MultiBackgroundScroller : MonoBehaviour
{
    public BackgroundLayer[] layers;

    void Start()
    {
        foreach (var layer in layers)
        {
            if (layer.layerTransform != null)
                layer.width = layer.layerTransform.GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void Update()
    {
        if (GameBootstrap.Instance != null && GameBootstrap.Instance.IsGameOver)
            return;

        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;

            layer.layerTransform.position += Vector3.left * layer.speed * Time.deltaTime;

            // Loop seamlessly
            if (layer.layerTransform.position.x <= -layer.width)
            {
                layer.layerTransform.position += new Vector3(layer.width * 2f, 0, 0);
            }
        }
    }
}