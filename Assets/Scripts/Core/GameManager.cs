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
        [SerializeField] private IntEventChannelSO onLivesChangedEvent; // Sends current lives count whenever it changes
        [SerializeField] private IntEventChannelSO onGameOverEvent;     // Sends final round number when the run ends

        private MiniGameData currentMiniGame;
        private int lastMiniGameIndex = -1;
        private bool isTransitioning = false;

        // NOTE: Start() no longer auto-begins a run. GameManager now sits idle until
        // UIManager calls BeginNewRun() (wired to the Main Menu's Start button).
        // This lets the Main Menu screen actually appear first, with nothing loading
        // behind it, instead of mini-game loading racing ahead in the background
        // while the player is still looking at the menu.
        private void Start()
        {
            // Intentionally empty. Waiting for UIManager to call BeginNewRun().
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

        /// <summary>
        /// Resets runtime state (lives, round) and starts loading the first mini-game.
        /// PUBLIC so UIManager can call this directly when the player presses Start
        /// on the Main Menu, and again on Restart from the Game Over screen.
        /// Does NOT touch high score — that lives entirely in UIManager via PlayerPrefs.
        /// </summary>
        public void BeginNewRun()
        {
            currentLives = startingLives;
            currentRound = 1;
            isTransitioning = false;

            // Broadcast starting lives immediately so any UI already active
            // (Gameplay HUD) is in sync from frame one.
            if (onLivesChangedEvent != null) onLivesChangedEvent.RaiseEvent(currentLives);

            StartCoroutine(LoadNextMiniGameRoutine());
        }

        /// <summary>
        /// Public entry point for a Restart button. Call this from UIManager.
        /// Handles unloading whatever mini-game scene is currently active before
        /// starting a fresh run, so we don't leak an old additive scene.
        /// </summary>
        public void RestartGame()
        {
            StopAllCoroutines(); // safety: cancel any in-flight transition before restarting
            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            if (currentMiniGame != null)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentMiniGame.sceneName);
                if (asyncUnload != null)
                {
                    while (!asyncUnload.isDone)
                    {
                        yield return null;
                    }
                }
                currentMiniGame = null;
            }

            BeginNewRun();
        }

        private void HandleMiniGameWon()
        {
            if (isTransitioning) return;
            isTransitioning = true; // set synchronously here, not inside the coroutine,
                                    // so a second event firing in the same frame can't slip past this check
            StartCoroutine(TransitionToNextGameRoutine(true));
        }

        private void HandleMiniGameLost()
        {
            if (isTransitioning) return;
            isTransitioning = true; // same reasoning as above
            currentLives--;

            // Broadcast the updated lives count right where it changes,
            // so this is the single source of truth UI can never desync from.
            if (onLivesChangedEvent != null) onLivesChangedEvent.RaiseEvent(currentLives);

            StartCoroutine(TransitionToNextGameRoutine(false));
        }

        private IEnumerator TransitionToNextGameRoutine(bool wasWin)
        {
            // 1. Show Win/Loss UI/Animation (This would tie into a UIManager)
            Debug.Log(wasWin ? "Mini-Game WON!" : "Mini-Game LOST!");

            // Short delay for the result to read
            yield return new WaitForSeconds(1.5f);

            // 2. Check for Game Over
            if (currentLives <= 0)
            {
                Debug.Log("GAME OVER! Final round: " + currentRound);

                // Broadcast game over with the final round number reached.
                // UIManager listens for this to show the Game Over screen.
                if (onGameOverEvent != null) onGameOverEvent.RaiseEvent(currentRound);

                isTransitioning = false;
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
            int randomIndex;
            do {
                // Pick a random game (could add logic here to not repeat the last game)
                randomIndex = Random.Range(0, availableMiniGames.Count);
            } while (lastMiniGameIndex == randomIndex);
            lastMiniGameIndex = randomIndex;

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