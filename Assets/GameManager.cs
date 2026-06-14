using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // ---------------- SINGLETON ----------------
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject usernamePanelPrefab;
    public TMP_InputField usernameInput; // Assign in prefab
    public GameObject gameOverPanelPrefab;
    public TextMeshProUGUI gameOverText; 
    public GameObject leaderboardPanelPrefab;
    public TextMeshProUGUI leaderboardText;

    [Header("Firebase")]
    public string databaseURL = "https://flappymobilegame-default-rtdb.firebaseio.com/";

    private DatabaseReference dbReference;
    private bool isFirebaseReady = false;
    private string playerUsername = "Player";

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeFirebase();
        ShowUsernamePanel();
    }

    // ---------------- FIREBASE ----------------
    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                dbReference = FirebaseDatabase.GetInstance(app, databaseURL).RootReference;
                isFirebaseReady = true;
                Debug.Log("✅ Firebase Initialized Successfully");
            }
            else
            {
                isFirebaseReady = false;
                Debug.LogError("❌ Firebase NOT Ready: " + task.Result);
            }
        });
    }

    // ---------------- USERNAME PANEL ----------------
    public void ShowUsernamePanel()
    {
        if (usernamePanelPrefab != null)
        {
            usernamePanelPrefab.SetActive(true);
            if (leaderboardPanelPrefab != null) leaderboardPanelPrefab.SetActive(false);
        }
    }

    public void OnSubmitUsername()
    {
        playerUsername = string.IsNullOrEmpty(usernameInput?.text) ? "Player" : usernameInput.text;
        Debug.Log($"USERNAME SET: {playerUsername}");

        if (usernamePanelPrefab != null) usernamePanelPrefab.SetActive(false);

        // Optionally store locally
        PlayerPrefs.SetString("username", playerUsername);

        StartGame();
    }

    public void PlayAsGuest()
    {
        playerUsername = "Player";
        usernamePanelPrefab.SetActive(false);
        StartGame();
    }

    void StartGame()
    {
        Debug.Log($"Game Started for: {playerUsername}");
        // Enable player input
        PlayerController player = FindObjectOfType<PlayerController>();
       // if (player != null) player.EnableInput();

        // Notify GameBootstrap if exists
        GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
        //bootstrap?.StartGameFromMenu();
    }

    // ---------------- GAMEOVER FLOW ----------------
    public void GameOver(int score)
    {
        Debug.Log($"Game Over: {playerUsername}, Score: {score}");

        if (!isFirebaseReady)
        {
            StartCoroutine(WaitForFirebaseAndSave(score));
            return;
        }

        SaveScore(playerUsername, score);
        ShowGameOverPanel(score);
    }

    IEnumerator WaitForFirebaseAndSave(int score)
    {
        while (!isFirebaseReady) yield return null;
        GameOver(score);
    }

    void ShowGameOverPanel(int score)
    {
        if (gameOverPanelPrefab != null)
        {
            gameOverPanelPrefab.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = $"GAME OVER\nScore: {score}";
        }

        ShowLeaderboard();
    }

    // ---------------- SAVE SCORE ----------------
    void SaveScore(string username, int score)
    {
        if (!isFirebaseReady) return;

        string key = dbReference.Child("leaderboard").Push().Key;
        var data = new Dictionary<string, object>
        {
            { "username", username },
            { "score", score },
            { "timestamp", System.DateTime.UtcNow.ToString() }
        };

        dbReference.Child("leaderboard").Child(key).SetValueAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log($"🔥 Score Saved: {username} -> {score}");
            else Debug.LogError("❌ Failed to save score: " + task.Exception);
        });
    }

    // ---------------- LEADERBOARD ----------------
    void ShowLeaderboard(int topCount = 10)
    {
        if (!isFirebaseReady) return;

        dbReference.Child("leaderboard").OrderByChild("score").LimitToLast(topCount)
            .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("❌ Failed to fetch leaderboard: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            List<(string username, int score)> scores = new List<(string, int)>();

            foreach (var child in snapshot.Children)
            {
                string user = child.Child("username").Value?.ToString() ?? "Player";
                int s = 0;
                int.TryParse(child.Child("score").Value?.ToString(), out s);
                scores.Add((user, s));
            }

            scores = scores.OrderByDescending(x => x.score).ToList();

            string display = "🏆 LEADERBOARD 🏆\n";
            for (int i = 0; i < scores.Count; i++)
            {
                string line = $"{i + 1}. {scores[i].username}: {scores[i].score}";
                if (scores[i].username == playerUsername) line = $"<color=yellow>{line}</color>";
                display += line + "\n";
            }

            int playerRank = scores.FindIndex(x => x.username == playerUsername) + 1;
            display += $"\nYour Rank: {playerRank}";

            if (leaderboardText != null)
            {
                leaderboardText.text = display;
                if (leaderboardPanelPrefab != null) leaderboardPanelPrefab.SetActive(true);
            }

            Debug.Log("Leaderboard displayed:\n" + display);
        });
    }

    public void RestartGame()
    {
        if (gameOverPanelPrefab != null) gameOverPanelPrefab.SetActive(false);
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.ResetPlayer(Vector3.zero);
        StartGame();
    }
}