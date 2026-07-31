using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SobGameJam.Events;

namespace SobGameJam.Core
{
    /// <summary>
    /// The core system that manages the flow of the game, lives, rounds, and transitions.
    /// This should live in a persistent 'Manager' scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private int startingLives = 4;
        private int currentLives;
        private int currentRound = 1;

        [Header("Mini-Game Database")]
        [SerializeField] private List<MiniGameData> availableMiniGames;

        [Header("Events (Listening To)")]
        [SerializeField] private VoidEventChannelSO miniGameWonEvent;
        [SerializeField] private VoidEventChannelSO miniGameLostEvent;

        [Header("Events (Broadcasting)")]
        [SerializeField] private IntEventChannelSO onRoundStartedEvent; // Sends round number to the mini-game
        
        private MiniGameData currentMiniGame;
        private bool isTransitioning = false;

        private void Start()
        {
            currentLives = startingLives;
            currentRound = 1;

            // Start the first game
            StartCoroutine(LoadNextMiniGameRoutine());
        }

        private void OnEnable()
        {
            if (miniGameWonEvent != null) miniGameWonEvent.OnEventRaised += HandleMiniGameWon;
            if (miniGameLostEvent != null) miniGameLostEvent.OnEventRaised += HandleMiniGameLost;
        }

        private void OnDisable()
        {
            if (miniGameWonEvent != null) miniGameWonEvent.OnEventRaised -= HandleMiniGameWon;
            if (miniGameLostEvent != null) miniGameLostEvent.OnEventRaised -= HandleMiniGameLost;
        }

        private void HandleMiniGameWon()
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionToNextGameRoutine(true));
        }

        private void HandleMiniGameLost()
        {
            if (isTransitioning) return;
            currentLives--;
            StartCoroutine(TransitionToNextGameRoutine(false));
        }

        private IEnumerator TransitionToNextGameRoutine(bool wasWin)
        {
            isTransitioning = true;

            // 1. Show Win/Loss UI/Animation (This would tie into a UIManager)
            Debug.Log(wasWin ? "Mini-Game WON!" : "Mini-Game LOST!");
            
            // Short delay for the result to read
            yield return new WaitForSeconds(1.5f);

            // 2. Check for Game Over
            if (currentLives <= 0)
            {
                Debug.Log("GAME OVER! Final round: " + currentRound);
                // Handle Game Over (e.g., load main menu or game over screen)
                yield break;
            }

            // 3. Unload current mini-game scene
            if (currentMiniGame != null)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentMiniGame.sceneName);
                while (!asyncUnload.isDone)
                {
                    yield return null;
                }
            }

            // 4. Increment Round
            currentRound++;

            // 5. Load next game
            yield return StartCoroutine(LoadNextMiniGameRoutine());
        }

        private IEnumerator LoadNextMiniGameRoutine()
        {
            if (availableMiniGames == null || availableMiniGames.Count == 0)
            {
                Debug.LogError("No MiniGames assigned to GameManager!");
                yield break;
            }

            // Pick a random game (could add logic here to not repeat the last game)
            int randomIndex = Random.Range(0, availableMiniGames.Count);
            currentMiniGame = availableMiniGames[randomIndex];

            // Load scene additively
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentMiniGame.sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Tell the UI to show the instruction prompt
            Debug.Log($"Instruction: {currentMiniGame.instructionPrompt}");
            yield return new WaitForSeconds(currentMiniGame.instructionDuration);

            // Broadcast the Round Number. The newly loaded MiniGameController should be listening for this to start.
            if (onRoundStartedEvent != null)
            {
                onRoundStartedEvent.RaiseEvent(currentRound);
            }
            else
            {
                Debug.LogWarning("No IntEventChannelSO assigned for onRoundStartedEvent!");
            }

            isTransitioning = false;
        }
    }
}
