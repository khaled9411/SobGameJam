using SobGameJam.Core;
using UnityEngine;
using UnityEngine.Events;

namespace SobGameJam.Events
{
    /// <summary>
    /// A ScriptableObject-based event channel that carries an integer payload.
    /// Useful for passing the current Round Number to a mini-game.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Mini Game Event Channel", fileName = "NewIntEventChannel")]
    public class MiniGameEventChannelSO : ScriptableObject
    {
        public event UnityAction<MiniGameData> OnEventRaised;

        public void RaiseEvent(MiniGameData value)
        {
            if (OnEventRaised != null)
            {
                OnEventRaised.Invoke(value);
            }
            else
            {
                Debug.LogWarning($"An Int event was raised on {this.name}, but no one was listening.");
            }
        }
    }
}
