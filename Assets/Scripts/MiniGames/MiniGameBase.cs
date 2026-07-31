using UnityEngine;
using SobGameJam.Events;

namespace SobGameJam.MiniGames
{
    /// <summary>
    /// The abstract base class for all mini-games.
    /// Handles listening for the start event and routing the Win/Loss events.
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        [Header("Event Listeners")]
        [Tooltip("Listens for the GameManager to broadcast the current round number to start the game.")]
        [SerializeField] private IntEventChannelSO onRoundStartedEvent;

        [Header("Event Broadcasters")]
        [Tooltip("Fired when the player successfully completes the mini-game.")]
        [SerializeField] protected VoidEventChannelSO miniGameWonEvent;
        
        [Tooltip("Fired when the player fails the mini-game (or time runs out).")]
        [SerializeField] protected VoidEventChannelSO miniGameLostEvent;

        protected int currentRound;
        protected bool isGameActive = false;

        protected virtual void OnEnable()
        {
            if (onRoundStartedEvent != null)
            {
                onRoundStartedEvent.OnEventRaised += InternalStartGame;
            }
        }

        protected virtual void OnDisable()
        {
            if (onRoundStartedEvent != null)
            {
                onRoundStartedEvent.OnEventRaised -= InternalStartGame;
            }
        }

        private void InternalStartGame(int roundNumber)
        {
            currentRound = roundNumber;
            isGameActive = true;
            OnGameStarted(roundNumber);
        }

        /// <summary>
        /// Called automatically when the GameManager signals the game to start.
        /// Use this to initialize your difficulty based on the round number.
        /// </summary>
        /// <param name="roundNumber">The current global round number.</param>
        protected abstract void OnGameStarted(int roundNumber);

        /// <summary>
        /// Call this method from your subclass when the player completes the objective.
        /// </summary>
        protected void WinGame()
        {
            if (!isGameActive) return;
            isGameActive = false;
            
            if (miniGameWonEvent != null)
                miniGameWonEvent.RaiseEvent();
            else
                Debug.LogError($"MiniGame {gameObject.name} won, but no WonEvent channel is assigned!");
        }

        /// <summary>
        /// Call this method from your subclass when the player fails the objective or time runs out.
        /// </summary>
        protected void LoseGame()
        {
            if (!isGameActive) return;
            isGameActive = false;
            
            if (miniGameLostEvent != null)
                miniGameLostEvent.RaiseEvent();
            else
                Debug.LogError($"MiniGame {gameObject.name} lost, but no LostEvent channel is assigned!");
        }
    }
}
