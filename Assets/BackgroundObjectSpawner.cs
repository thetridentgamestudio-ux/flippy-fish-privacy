using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BackgroundObjectSpawner : MonoBehaviour
{
    // Ground objects — spawn near ground
    private static readonly string[] GROUND_SPRITES = new string[]
    {
        "Seaweed_12",
        "Seaweed_21",
        "Stone_1",
        "Stone_2",
        "Stone_31",
        "Anchor1",
        "Steering-wheel1",
        "Bubble_1",
        "Bubble_2",
        "Bubble_3",
        "chest_closed1"
    };

    // Floating sprite prefixes — loads all numbered variants automatically
    // e.g. "Fish" loads Fish_1, Fish_2, Fish_3... until one is not found
    // Add any prefix here and just add matching PNGs to Resources/BgObjects/
    private static readonly string[] FLOATING_PREFIXES = new string[]
    {
        "Fish",
        "Jellyfish"
    };

    private List<Sprite> sprites = new List<Sprite>();
    private List<Sprite> floatingSprites = new List<Sprite>();
    private Dictionary<GameObject, float> bobPhase = new Dictionary<GameObject, float>();

    // How often to spawn a new object (seconds)
    private float spawnInterval = 1.0f; // spawn every 1 second
    private float spawnTimer = 0f;

    // Speed — slower than background to feel distant
    private float scrollSpeed = 5.0f; // faster scroll to clear screen before new ones spawn

    // Active objects list for cleanup
    private List<GameObject> activeObjects = new List<GameObject>();

    // Camera bounds
    private float camRight;
    private float camTop;
    private float camBottom;
    private float groundY;

    void Start()
    {
        Camera cam = Camera.main;
        // Recalculate based on actual camera size
        camRight = cam.orthographicSize * cam.aspect + 2f;
        camTop = cam.orthographicSize - 2f;
        camBottom = -cam.orthographicSize + 3f;
        groundY = -cam.orthographicSize + 2.5f;

        // Load ground sprites from Resources/BgObjects/
        foreach (string name in GROUND_SPRITES)
        {
            Sprite spr = Resources.Load<Sprite>("BgObjects/" + name);
            if (spr != null)
                sprites.Add(spr);
            else
                Debug.LogWarning("BackgroundObjectSpawner: Could not load BgObjects/" + name);
        }

        // Load floating sprites dynamically — tries Fish_1, Fish_2... until not found
        foreach (string prefix in FLOATING_PREFIXES)
        {
            for (int i = 1; i <= 20; i++) // supports up to 20 variants per prefix
            {
                Sprite spr = Resources.Load<Sprite>("BgObjects/" + prefix + "_" + i);
                if (spr != null)
                    floatingSprites.Add(spr);
                else
                    break; // stop when no more found
            }
        }
        Debug.Log($"BackgroundObjectSpawner: loaded {floatingSprites.Count} floating sprites");

        Debug.Log("BackgroundObjectSpawner: loaded " + sprites.Count + " sprites");
        Debug.Log($"BackgroundObjectSpawner: camRight={camRight} groundY={groundY}");

        // Spawn a few objects immediately so screen isn't empty at start
        // Spawn 1 object initially off screen right
        SpawnObject(camRight);
    }

    void Update()
    {
        if (GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.IsGameOver) return;
        if (GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing) return;

        // Spawn timer
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval; // reset to exact interval — consistent timing
            SpawnObject(camRight);
        }

        // Scroll all objects left and destroy off-screen ones
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            obj.transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

            // Animate floating objects
            bool isFloating = obj.name.Contains("Fish") || obj.name.Contains("Jellyfish");
            bool isTurtle = obj.name.Contains("Turtle");
            
            if (isFloating || isTurtle)
            {
                if (!bobPhase.ContainsKey(obj))
                    bobPhase[obj] = Random.Range(0f, Mathf.PI * 2f);
                bobPhase[obj] += Time.deltaTime * 2f;
                
                Vector3 pos = obj.transform.position;
                
                // Bob up/down — larger amplitude
                float bobAmount = Mathf.Sin(bobPhase[obj]) * 0.6f;
                pos.y += bobAmount * Time.deltaTime;
                
                // Shake side-to-side for fish and jellyfish — much more noticeable
                if (isFloating)
                {
                    float shakeAmount = Mathf.Sin(bobPhase[obj] * 3f) * 0.8f; // much larger
                    pos.x += shakeAmount * Time.deltaTime;
                }
                
                obj.transform.position = pos;
            }

            // Destroy when off left edge
            if (obj.transform.position.x < -(Camera.main.orthographicSize * Camera.main.aspect) - 4f)
            {
                bobPhase.Remove(obj);
                Destroy(obj);
                activeObjects.RemoveAt(i);
            }
        }
    }

    void SpawnObject(float spawnX)
    {
        if (sprites.Count == 0) return;
        // Update camRight dynamically
        Camera cam3 = Camera.main;
        camRight = cam3.orthographicSize * cam3.aspect + 2f;
        // Randomise X slightly so objects don't bunch together
        spawnX = spawnX + Random.Range(1f, 3f); // smaller spread for 1-second spawn interval

        // 30% chance of floating object if available, else ground object
        bool useFloating = floatingSprites.Count > 0 && Random.Range(0f, 1f) < 0.3f;
        Sprite spr = useFloating
            ? floatingSprites[Random.Range(0, floatingSprites.Count)]
            : sprites[Random.Range(0, sprites.Count)];

        // Dynamic Y based on current camera
        Camera cam2 = Camera.main;
        // Use groundTop from GameBootstrap for accurate ground position
        float groundTop = GameBootstrap.Instance != null ? GameBootstrap.Instance.groundTop : -cam2.orthographicSize + 2.5f;
        float randomY;
        if (useFloating)
        {
            // Check if turtle
            bool isTurtle = spr.name.StartsWith("Turtle");
            if (isTurtle)
                // Turtles spawn only in upper portion — away from player
                randomY = Random.Range(cam2.orthographicSize * 0.2f, cam2.orthographicSize * 0.7f);
            else
                // Fish/jellyfish spread across middle and upper portion
                randomY = Random.Range(-cam2.orthographicSize * 0.3f, cam2.orthographicSize * 0.5f);
        }
        else
            // Ground objects — near the floor
            randomY = Random.Range(groundTop - 0.9f, groundTop + 2.0f);

        // Create object
        GameObject obj = new GameObject("BgObj_" + spr.name);
        obj.transform.position = new Vector3(spawnX, randomY, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -5; // consistent back layer — always behind pipes and player

        // Small scale — background decoration
        float scale = useFloating
            ? Random.Range(0.30f, 0.50f)  // floating objects — 2x larger
            : Random.Range(0.8f, 1.4f);   // ground objects larger
        
        // Turtles 4x larger than other floating objects
        if (spr.name.StartsWith("Turtle"))
            scale *= 4f;
        
        obj.transform.localScale = new Vector3(scale, scale, 1f);

        Debug.Log($"Spawned {spr.name} at pos=({spawnX:F1},{randomY:F1}) layer={sr.sortingLayerName}");

        // Random transparency — distant objects are more transparent
        Color c = sr.color;
        c.a = 1.0f; // fully visible
        sr.color = c;

        activeObjects.Add(obj);
    }

    // Call this on game restart to clear all objects
    public void ClearAll()
    {
        foreach (var obj in activeObjects)
            if (obj != null) Destroy(obj);
        activeObjects.Clear();
        spawnTimer = 0f;
    }
}