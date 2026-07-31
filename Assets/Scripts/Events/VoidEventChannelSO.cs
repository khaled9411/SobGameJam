using UnityEngine;
using UnityEngine.Events;

namespace SobGameJam.Events
{
    /// <summary>
    /// A ScriptableObject-based event channel that carries no data.
    /// Used for simple triggers like "GameWon", "GameLost", or "StartMiniGame".
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Void Event Channel", fileName = "NewVoidEventChannel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public event UnityAction OnEventRaised;

        public void RaiseEvent()
        {
            if (OnEventRaised != null)
            {
                OnEventRaised.Invoke();
            }
            else
            {
                Debug.LogWarning($"A Void event was raised on {this.name}, but no one was listening.");
            }
        }
    }
}
