using UnityEngine;
using UnityEngine.Events;

namespace SobGameJam.Events
{
    /// <summary>
    /// A ScriptableObject-based event channel that carries an integer payload.
    /// Useful for passing the current Round Number to a mini-game.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Float Event Channel", fileName = "NewIntEventChannel")]
    public class FloatEventChannelSO : ScriptableObject
    {
        public event UnityAction<float> OnEventRaised;

        public void RaiseEvent(float value)
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
