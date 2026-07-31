using UnityEngine;
using System.Collections;

namespace SobGameJam.MiniGames
{
    /// <summary>
    /// An example of how to implement a specific mini-game using the MiniGameBase.
    /// This specific game requires the player to press 'Space' within a time limit that gets shorter each round.
    /// </summary>
    public class ExampleMiniGameController : MiniGameBase
    {
        [Header("Example Game Settings")]
        [Tooltip("The base time the player has to complete the game on Round 1.")]
        [SerializeField] private float baseTimeLimit = 3.0f;
        
        [Tooltip("How much time is subtracted per round to increase difficulty.")]
        [SerializeField] private float timeReductionPerRound = 0.2f;
        
        [Tooltip("The absolute minimum time the player will have, regardless of round.")]
        [SerializeField] private float minimumTimeLimit = 0.5f;

        private float currentTimeLimit;
        private float timeRemaining;

        protected override void OnGameStarted(int roundNumber)
        {
            // 1. Calculate Difficulty based on Round Number
            // In this example, they get less time in later rounds.
            currentTimeLimit = baseTimeLimit - (timeReductionPerRound * (roundNumber - 1));
            currentTimeLimit = Mathf.Max(currentTimeLimit, minimumTimeLimit);
            
            timeRemaining = currentTimeLimit;

            Debug.Log($"[ExampleMiniGame] Started Round {roundNumber}. Time limit: {currentTimeLimit:F2} seconds.");

            // 2. Start any specific game logic loops or coroutines
            StartCoroutine(GameTimerRoutine());
        }

        private void Update()
        {
            // Only process input if the game is actively running
            if (!isGameActive) return;

            // Example Win Condition: Player presses Space before time runs out
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[ExampleMiniGame] Player pressed Space in time!");
                WinGame(); // This fires the WonEvent and sets isGameActive to false
            }
        }

        private IEnumerator GameTimerRoutine()
        {
            while (isGameActive && timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // If the timer reaches 0 and the game is still active, they failed.
            if (isGameActive && timeRemaining <= 0)
            {
                Debug.Log("[ExampleMiniGame] Time ran out!");
                LoseGame(); // This fires the LostEvent and sets isGameActive to false
            }
        }
    }
}
