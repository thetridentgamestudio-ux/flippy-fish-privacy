// using UnityEngine;
// using TMPro;

// public class GameFlowManager : MonoBehaviour
// {
//     private string username;
//     private GameUIManager uiManager;
//     private FirebaseGameManager firebase;

//     void Start()
//     {
//         Debug.Log("[GameFlowManager] Start called");

//         // Find Firebase manager
//         firebase = FindObjectOfType<FirebaseGameManager>();
//         if (firebase == null)
//         {
//             Debug.LogError("[GameFlowManager] FirebaseGameManager NOT found in scene!");
//         }

//         // Find Game UI manager
//         uiManager = FindObjectOfType<GameUIManager>();
//         if (uiManager == null)
//         {
//             Debug.LogWarning("[GameFlowManager] GameUIManager NOT found in scene!");
//         }

//         // Show username input at start if UI manager exists
//         uiManager?.ShowUsernamePanel(OnUsernameSubmitted);
//     }

//     void OnUsernameSubmitted(string input)
//     {
//         username = string.IsNullOrEmpty(input) ? "Player" : input;
//         Debug.Log("✅ Username set: " + username);

//         // Game can start now
//         StartGame();
//     }

//     void StartGame()
//     {
//         Debug.Log("🎮 Game Started for: " + username);
//     }

//     // Call when game is over
//     public void GameOver(int finalScore)
//     {
//         Debug.Log("💀 Game Over. Score: " + finalScore);

//         if (firebase != null)
//         {
//            FirebaseGameManager fm = FindObjectOfType<FirebaseGameManager>();
// fm?.GameOver(username, finalScore);
//         }
//     }
// }