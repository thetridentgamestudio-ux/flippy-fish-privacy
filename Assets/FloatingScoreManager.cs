using UnityEngine;
using System.Collections;

public class FloatingScoreManager : MonoBehaviour
{
    private Transform player;
    private int currentScore = 0;

    void Start()
    {
        // Pre-warm the TextMesh shader so the first pipe-pass doesn't trigger
        // a shader compile and cause a visible frame hitch.
        var dummy = new GameObject("_TextMeshPreWarm");
        var tm = dummy.AddComponent<TextMesh>();
        tm.text = "+1";
        dummy.transform.position = new Vector3(-9999f, -9999f, 0f);
        Destroy(dummy, 0.1f);
    }

    void Update()
    {
        // Auto-find player (runtime safe)
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                player = p.transform;
        }
    }

   public void AddScore(int amount)
{
    if (player == null) return;

    Vector3 spawnPos = player.position + new Vector3(0.8f, 0.5f, 0);

    GameObject textObj = new GameObject("ScoreText");
    textObj.transform.position = spawnPos;

    TextMesh tm = textObj.AddComponent<TextMesh>();

tm.text = "+1";                 // ✅ +1 instead of 1
tm.fontSize = 100;              // bigger font
tm.characterSize = 0.08f;       // scale up
tm.anchor = TextAnchor.MiddleCenter;
tm.color = Color.yellow;

textObj.transform.localScale = Vector3.zero;
StartCoroutine(ScalePop(textObj.transform));
    textObj.AddComponent<FloatingText>();
}
    public void ResetScore()
    {
        currentScore = 0;
    }
    IEnumerator ScalePop(Transform t)
{
    float time = 0f;
    float duration = 0.2f;

    while (time < duration)
    {
        float scale = Mathf.Lerp(0f, 1.5f, time / duration);
        t.localScale = Vector3.one * scale;

        time += Time.deltaTime;
        yield return null;
    }

    t.localScale = Vector3.one;
}
}