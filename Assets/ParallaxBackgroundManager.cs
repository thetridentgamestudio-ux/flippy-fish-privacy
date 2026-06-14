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
        [HideInInspector] public float tileW;
        [HideInInspector] public float bgY;
    }

    public Layer[] layers;
    private List<Sprite> playlist = new List<Sprite>();

    void Start()
    {
        Camera cam = Camera.main;
        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        float camCenterY = cam.transform.position.y;

        // Build playlist
        playlist.Clear();
        for (int i = 1; i <= 5; i++)
        {
            Sprite spr = Resources.Load<Sprite>("BgEvent" + i);
            if (spr != null) { playlist.Add(spr); Debug.Log("Loaded BgEvent" + i); }
        }
        if (playlist.Count == 0)
            foreach (var l in layers)
                if (l.sprite != null) playlist.Add(l.sprite);
        if (playlist.Count == 0) return;

        foreach (var layer in layers)
        {
            if (layer.sprite == null) continue;

            Sprite spr0 = playlist[0];
            float sprW = spr0.bounds.size.x;
            float sprH = spr0.bounds.size.y;

            // Scale to fill screen height — portrait images fill height
            float scale = camH / sprH;
            // If still not wide enough, scale to fill width
            if (sprW * scale < camW)
                scale = camW / sprW;

            float tileW = sprW * scale;

            // Center vertically on camera
            float bgY = camCenterY;

            layer.tileW = tileW;
            layer.bgY = bgY;

            // bg1 centered on screen
            layer.bg1 = new GameObject("BG_1");
            var sr1 = layer.bg1.AddComponent<SpriteRenderer>();
            sr1.sprite = playlist[0];
            sr1.sortingLayerName = "BackgroundDeepblue";
            sr1.sortingOrder = -1;
            layer.bg1.transform.localScale = new Vector3(scale, scale, 1f);
            layer.bg1.transform.position = new Vector3(0f, bgY, 10f); // push back

            // bg2 immediately to the right
            layer.bg2 = new GameObject("BG_2");
            var sr2 = layer.bg2.AddComponent<SpriteRenderer>();
            sr2.sprite = playlist.Count > 1 ? playlist[1] : playlist[0];
            sr2.sortingLayerName = "BackgroundDeepblue";
            sr2.sortingOrder = -1;
            layer.bg2.transform.localScale = new Vector3(scale, scale, 1f);
            layer.bg2.transform.position = new Vector3(tileW, bgY, 10f); // push back
        }
    }

    Sprite GetByScore()
    {
        if (playlist.Count == 0) return null;
        int score = GameBootstrap.Instance != null ? GameBootstrap.Instance.Score : 0;
        int idx = (score / 15) % playlist.Count;
        return playlist[idx];
    }

    void Update()
    {
        if (GameBootstrap.Instance != null && GameBootstrap.Instance.IsGameOver) return;

        Camera cam = Camera.main;
        float camHalfW = cam.orthographicSize * cam.aspect;

        foreach (var layer in layers)
        {
            if (layer.bg1 == null || layer.bg2 == null) continue;

            float move = layer.speed * Time.deltaTime;
            layer.bg1.transform.position += Vector3.left * move;
            layer.bg2.transform.position += Vector3.left * move;

            // bg1 off screen left — move to right of bg2
            if (layer.bg1.transform.position.x < -camHalfW - layer.tileW * 0.5f)
            {
                layer.bg1.transform.position = new Vector3(
                    layer.bg2.transform.position.x + layer.tileW,
                    layer.bgY,
                    layer.bg1.transform.position.z);
                var sr = layer.bg1.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = GetByScore();
            }

            // bg2 off screen left — move to right of bg1
            if (layer.bg2.transform.position.x < -camHalfW - layer.tileW * 0.5f)
            {
                layer.bg2.transform.position = new Vector3(
                    layer.bg1.transform.position.x + layer.tileW,
                    layer.bgY,
                    layer.bg2.transform.position.z);
                var sr = layer.bg2.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = GetByScore();
            }
        }
    }

    public void ResetZone()
    {
        // Nothing to reset
    }
}