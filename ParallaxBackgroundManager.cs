using UnityEngine;
using System.Collections.Generic;

public class ParallaxBackgroundManager : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Sprite sprite;
        public float speed = 0.3f;
        public float zPosition = 10f;

        [HideInInspector] public GameObject bg1;
        [HideInInspector] public GameObject bg2;
        [HideInInspector] public float width;
        [HideInInspector] public float initialY;
    }

    public Layer[] layers;

    // All background sprites in order — default first then BgEvent1-5
    private List<Sprite> playlist = new List<Sprite>();
    private int nextIndex = 1; // next sprite to slide in (0 is already showing)

    void Start()
    {
        Camera cam = Camera.main;
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        float camY = cam.transform.position.y;

        foreach (var layer in layers)
        {
            if (layer.sprite == null) continue;

            // Build playlist — default bg first
            playlist.Clear();
            playlist.Add(layer.sprite); // index 0 = currently showing

            for (int i = 1; i <= 5; i++)
            {
                Sprite spr = Resources.Load<Sprite>("BgEvent" + i);
                if (spr != null)
                {
                    playlist.Add(spr);
                    Debug.Log("Loaded BgEvent" + i);
                }
            }

            // Shuffle playlist so order is random each session
            for (int i = playlist.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Sprite tmp = playlist[i];
                playlist[i] = playlist[j];
                playlist[j] = tmp;
            }

            // Setup bg1 and bg2
            float sprW = layer.sprite.bounds.size.x;
            float sprH = layer.sprite.bounds.size.y;
            float scale = Mathf.Max(camWidth / sprW, camHeight / sprH);
            float scaledW = sprW * scale;

            layer.bg1 = new GameObject(layer.sprite.name + "_1");
            var sr1 = layer.bg1.AddComponent<SpriteRenderer>();
            sr1.sprite = playlist[0];
            sr1.sortingLayerName = "Background";
            sr1.sortingOrder = 0;
            // Align bg bottom edge with ground collider at y=-3.5f
            // bg center = ground level + half of scaled sprite height
            float groundLevel = -3.5f;
            float scaledH = sprH * scale;
            float bgY = groundLevel + (scaledH / 2f);

            layer.bg1.transform.localScale = new Vector3(scale, scale, 1);
            layer.bg1.transform.position = new Vector3(0, bgY, layer.zPosition);

            layer.bg2 = new GameObject(layer.sprite.name + "_2");
            var sr2 = layer.bg2.AddComponent<SpriteRenderer>();
            sr2.sprite = GetNext(); // next in playlist
            sr2.sortingLayerName = "Background";
            sr2.sortingOrder = 0;
            layer.bg2.transform.localScale = new Vector3(scale, scale, 1);
            layer.bg2.transform.position = new Vector3(scaledW, bgY, layer.zPosition);

            layer.width = scaledW;
            layer.initialY = bgY;
        }
    }

    Sprite GetNext()
    {
        if (playlist.Count == 0) return null;
        Sprite s = playlist[nextIndex % playlist.Count];
        nextIndex++;
        return s;
    }

    void Update()
    {
        if (GameBootstrap.Instance != null && GameBootstrap.Instance.IsGameOver) return;

        for (int li = 0; li < layers.Length; li++)
        {
            var layer = layers[li];
            if (layer.bg1 == null || layer.bg2 == null) continue;

            float move = layer.speed * Time.deltaTime;

            layer.bg1.transform.position += Vector3.left * move;
            layer.bg2.transform.position += Vector3.left * move;

            // When bg1 scrolls off left — move it to right of bg2 with next sprite
            if (layer.bg1.transform.position.x <= -layer.width)
            {
                layer.bg1.transform.position = new Vector3(
                    layer.bg2.transform.position.x + layer.width,
                    layer.initialY,
                    layer.bg1.transform.position.z);

                var sr = layer.bg1.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = GetNext();
            }

            // When bg2 scrolls off left — move it to right of bg1 with next sprite
            if (layer.bg2.transform.position.x <= -layer.width)
            {
                layer.bg2.transform.position = new Vector3(
                    layer.bg1.transform.position.x + layer.width,
                    layer.initialY,
                    layer.bg2.transform.position.z);

                var sr = layer.bg2.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = GetNext();
            }
        }
    }

    public void ResetZone()
    {
        // Nothing to reset — backgrounds just keep cycling
    }
}