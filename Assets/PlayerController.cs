using UnityEngine;
using System.Collections;  // Needed for IEnumerator
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{

    [Header("Gravity & Falling")]
public float baseGravity = 2f;       // normal gravity scale
public float fallMultiplier = 1.8f; 

    public float jumpForce = 12.5f; // increased for faster, snappier feel
    public BubbleSpawner bubbleSpawner;
    public float rotationSpeed = 5f;
    public bool IsAlive { get; private set; } = true;
    private Rigidbody2D rb;
    [Header("Player Size")]
public float playerScale = 1.4f;
    public ParticleSystem hitParticles ;
    
    private SpriteRenderer finSR;
    public float finRotationMultiplier = 15f; 
    private float lastScoreSoundTime = 0f;
    private float scoreSoundCooldown = 0.05f;
    private bool isInvincible = false;

    // Audio
    private AudioSource audioSource;
    private AudioClip[] flapSounds;        // regular taps
    private AudioClip[] rapidFlapSounds;   // rapid taps
    private AudioClip collisionSound;      // hit obstacle
    private AudioClip scoreSound;          // scoring

    // Rapid tap tracking
    private float lastFlapTime = 0f;
    public float rapidTapWindow = 0.12f;
    public float rapidFlapMultiplier = 1.4f;

    // PlayerController.cs
    private float tiltSmooth = 5f;          // How smooth the tilt is
    private float maxTilt = 25f;            // Maximum tilt angle in degrees
    private float minTilt = -45f;           // Minimum tilt angle in degrees
    private SpriteRenderer sr;              // Already have this

    // Sprite for the fin (optional: assign in inspector or leave null for default)
    public Sprite finSprite;

    // Transform of the dynamically created fin
    [HideInInspector] 
    public Transform fin;

    void Awake()
    {
        // -----------------------------
        // Set Player Name
        // -----------------------------
        gameObject.name = "Player";
        transform.localScale = Vector3.one * playerScale;

        // -----------------------------
        // Rigidbody Setup
        // -----------------------------
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // -----------------------------
        // Collider Setup
        // -----------------------------
        // -----------------------------
// Collider Setup (FIXED)
// -----------------------------
var col = gameObject.AddComponent<CircleCollider2D>();
col.isTrigger = false;
col.radius = 0.55f; // 🔥 tweak between 0.22 - 0.3 for best feel
col.offset = Vector2.zero;

        // -----------------------------
        // AudioSource Setup
        // -----------------------------
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // -----------------------------
        // Load Sound Assets
        // -----------------------------
        flapSounds = Resources.LoadAll<AudioClip>("Sounds/Flap");       // normal flap sounds
        rapidFlapSounds = Resources.LoadAll<AudioClip>("Sounds/Rapid"); // fast tap sounds
        collisionSound = Resources.Load<AudioClip>("Sounds/Collision");
        scoreSound = Resources.Load<AudioClip>("Sounds/Score");

        // //debug logs to confirm sounds loaded
        //debug.Log("Collision sound loaded: " + (collisionSound != null));
        //debug.Log("Score sound loaded: " + (scoreSound != null));

        // -----------------------------
        // Create fin dynamically
        // -----------------------------
        //CreateFin();
    }

void Update()
{
    if (GameBootstrap.Instance == null) return;

    // First tap after revive — start the game
    if (GameBootstrap.Instance.CurrentState == GameBootstrap.GameState.WaitingForRevive)
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            GameBootstrap.Instance.CurrentState = GameBootstrap.GameState.Playing;
            GameBootstrap.Instance.StartPipeSpawning();
            SetInvincible(0f); // cancel invincibility on first tap — player is now live
            // Do the flap immediately on first tap
            if(rb.gravityScale < 1f) rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            GhostRaceManager.Instance?.RecordTap(transform.position.y);
        }
        return; // don't process anything else until first tap
    }

    if (GameBootstrap.Instance.CurrentState != GameBootstrap.GameState.Playing)
        return;
    if (!IsAlive || GameBootstrap.Instance.IsGameOver)
        return;

    // -----------------------------
    // Flap input (mouse click / space)
    // -----------------------------
    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
    {
         if(rb.gravityScale < 1f)
        rb.gravityScale = 1f;
        float timeSinceLastFlap = Time.time - lastFlapTime;
        lastFlapTime = Time.time;

 float difficulty = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetDifficultyMultiplier() : 0f;

    float dynamicRapidWindow = Mathf.Lerp(0.18f, 0.08f, difficulty); 
            bool isRapid = timeSinceLastFlap < dynamicRapidWindow;

        float flapForceToApply = jumpForce;
        if (isRapid)
            flapForceToApply *= rapidFlapMultiplier;

        SwimUp(flapForceToApply);
        GhostRaceManager.Instance?.RecordTap(transform.position.y);
        PlayFlapSound(isRapid);

       if (bubbleSpawner != null && GameBootstrap.Instance != null && !GameBootstrap.Instance.IsGameOver)
    {
        int bubbleCount = (timeSinceLastFlap < rapidTapWindow) ? 5 : 2;
        for (int i = 0; i < bubbleCount; i++)
        {
            bubbleSpawner.SpawnBubble(transform.position);
        }
    }

        CameraShake.ShakeOnCollision(0.1f, 0.05f);

        // -----------------------------
        // Rotate fin forward on flap
        // -----------------------------
        if (fin != null)
            fin.localRotation = Quaternion.Euler(0, 0, 30f); // adjust 30° forward on flap
    }

    // -----------------------------
    // Clamp vertical speed
    // -----------------------------
    float clampedY = Mathf.Clamp(rb.linearVelocity.y, -6f, 6f);
    rb.linearVelocity = new Vector2(0, clampedY);

    // -----------------------------
    // Smooth tilt based on velocity
    // -----------------------------
    float targetAngle = Mathf.Clamp(clampedY * 5f, minTilt, maxTilt); // speed -> tilt
    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), Time.deltaTime * tiltSmooth);

    // -----------------------------
    // Smooth fin rotation back
    // -----------------------------
    if (fin != null)
        fin.localRotation = Quaternion.Lerp(fin.localRotation, Quaternion.identity, Time.deltaTime * finRotationMultiplier);
}

    void SwimUp(float force)
{
    rb.linearVelocity = new Vector2(0, Mathf.Max(rb.linearVelocity.y, 0f)); // cancel downward velocity
    float difficultyMultiplier = GameBootstrap.Instance != null ? GameBootstrap.Instance.GetDifficultyMultiplier() : 0f;
    rb.AddForce(Vector2.up * (force * _speedBoostMultiplier + difficultyMultiplier * 0.5f), ForceMode2D.Impulse);
}

private float _speedBoostMultiplier = 1f;
private Coroutine _speedBoostCoroutine;

public void ApplySpeedBoost(float duration)
{
    if (_speedBoostCoroutine != null) StopCoroutine(_speedBoostCoroutine);
    _speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(duration));
}

IEnumerator SpeedBoostCoroutine(float duration)
{
    _speedBoostMultiplier = 1.55f; // 55% stronger jump — fish lunges visibly faster
    yield return new WaitForSeconds(duration);
    _speedBoostMultiplier = 1f;
    _speedBoostCoroutine  = null;
}

//   void CreateFin()
// {
//     //debug.Log("[Fin] Creating fin...");

//     // Create Fin
//     GameObject finGO = new GameObject("Fin");
//     finGO.transform.SetParent(transform);

//     // Reset transform FIRST
//     finGO.transform.localPosition = new Vector3(-0.35f, 0f, 0f); // 👈 moved slightly back
//     finGO.transform.localRotation = Quaternion.identity;

//     // 🔥 IMPORTANT: Reset scale so it ignores parent distortion
//     finGO.transform.localScale = Vector3.one;

//     // Add renderer
//     finSR = finGO.AddComponent<SpriteRenderer>();

//     Sprite spriteToUse = Resources.Load<Sprite>("Images/Fin");

//     if (spriteToUse != null)
//     {
//         finSR.sprite = spriteToUse;
//         //debug.Log("[Fin] ✅ Fin sprite loaded successfully");

//         // ✅ SCALE FIX (bigger so clearly visible)
//         float scaleFactor = 0.6f; // 👈 increased from 0.3 → 0.6
//         finGO.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

//         //debug.Log($"[Fin] 📏 Scale applied: {scaleFactor}");
//     }
//     else
//     {
//         //debug.LogError("[Fin] ❌ Fin sprite NOT found");
//     }

//     // 🔥 VERY IMPORTANT: sorting
//     finSR.sortingOrder = 2; // 👈 ensure visible above player

//     //debug.Log($"[Fin] Sorting Order: {finSR.sortingOrder}");

//     // Save reference
//     fin = finGO.transform;

//     //debug.Log("[Fin] 🎯 Fin created and attached to player");
// }
    void PlayFlapSound(bool isRapid)
    {
        AudioClip clip;
        if (isRapid && rapidFlapSounds.Length > 0)
            clip = rapidFlapSounds[Random.Range(0, rapidFlapSounds.Length)];
        else if (flapSounds.Length > 0)
            clip = flapSounds[Random.Range(0, flapSounds.Length)];
        else
            return;

        audioSource.PlayOneShot(clip);
        //debug.Log($"[Flap] Flap sound played. Rapid: {isRapid}");
    }

 void OnCollisionEnter2D(Collision2D collision)
{
    if (!IsAlive) return;

    bool isGround   = collision.gameObject.CompareTag("Ground");
    bool isObstacle = collision.gameObject.CompareTag("Obstacle");

    if (!isGround && !isObstacle) return;

    // Invincibility only protects against pipes — ground always kills
    if (isObstacle && isInvincible) return;

    if (collisionSound != null)
        audioSource.PlayOneShot(collisionSound);

    GameBootstrap.Instance?.CreateDeathEffect(transform.position);
    CameraShake.ShakeOnCollision(0.2f, 0.15f);

    IsAlive = false;
    StartCoroutine(DeathSequence());
}
    IEnumerator DeathSequence()
{
    GameBootstrap.Instance?.ShowSadPlayer();

    rb.linearVelocity = Vector2.zero;
    rb.gravityScale = 0f;

    yield return new WaitForSeconds(0.4f);

    Die(); // tumble

    yield return new WaitForSeconds(0.3f);

    // 🚨 THIS MUST ALWAYS RUN
    GameBootstrap.Instance?.TriggerGameOver();
}

    // void DelayedGameOver()
    // {
    //      //debug.Log("[DelayedGameOver] Method called");
    //     if (GameBootstrap.Instance != null){
    //         //debug.Log("[DelayedGameOver] GameBootstrap instance exists, triggering Game Over");
    //         GameBootstrap.Instance.TriggerGameOver();}
    //         else
    // {
    //     //debug.LogError("[DelayedGameOver] GameBootstrap instance is NULL!");
    // }
    // }
   private void DelayedGameOver()
{
    //debug.Log("[DelayedGameOver] Method called");
    if (GameBootstrap.Instance != null)
    {
        //debug.Log("[DelayedGameOver] GameBootstrap instance exists, triggering Game Over");
        GameBootstrap.Instance.TriggerGameOver();
    }

   
  
}



    public void PlayScoreSound()
    {
        if (scoreSound == null) return;

        if (Time.time - lastScoreSoundTime < scoreSoundCooldown)
            return; // skip if called too fast

        lastScoreSoundTime = Time.time;
        audioSource.PlayOneShot(scoreSound);
        //debug.Log("SCORE SOUND TRIGGERED");
    }
void FixedUpdate()
{
    if (!IsAlive || GameBootstrap.Instance?.CurrentState != GameBootstrap.GameState.Playing)
        return;

    // -----------------------------
    // Dynamic gravity for falling
    // -----------------------------
    if (rb.linearVelocity.y < 0f)
    {
        rb.gravityScale = baseGravity * fallMultiplier; // fall faster
    }
    else
    {
        rb.gravityScale = baseGravity; // normal gravity when rising
    }

    // -----------------------------
    // Clamp vertical speed
    // -----------------------------
    float maxUpSpeed = 8f;
    float maxDownSpeed = -10f;
    rb.linearVelocity = new Vector2(0f, Mathf.Clamp(rb.linearVelocity.y, maxDownSpeed, maxUpSpeed));
}
    public void Die()
{
    if (!IsAlive) return;
    IsAlive = false;

    // Stop movement
    rb.linearVelocity = Vector2.zero;

    // Enable gravity and rotation
    rb.gravityScale = 2.5f;    // fall faster
    rb.freezeRotation = false;  // allow rotation

    // Add torque to tumble forward
    rb.AddTorque(200f);

    // Optional: small forward push
    rb.linearVelocity = new Vector2(1f, -1f); 
}

    public void ResetPlayer(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0.35f;   // gentle gravity — matches StartGame()
        rb.freezeRotation = true;  // stop tumble from previous death

        IsAlive = true;
    }
    // DeathSequence is defined above near OnCollisionEnter2D — removed duplicate
void DelayedDie()
{
    Die(); // existing tumble logic

    Invoke(nameof(DelayedGameOver), 0.2f);
}
public void RevivePlayer()
{
    IsAlive = true;

    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    rb.linearVelocity = Vector2.zero;

    transform.position = new Vector3(-2f, 0f, 0f);
}
public void SetInvincible(float duration)
{
    StartCoroutine(InvincibleRoutine(duration));
}

IEnumerator InvincibleRoutine(float duration)
{
    isInvincible = true;
    yield return new WaitForSeconds(duration);
    isInvincible = false;
}
}