using UnityEngine;
using UnityEngine.Events;
using SobGameJam.Events;

namespace SobGameJam.Audio
{

    public class VoidEventChannelListener : MonoBehaviour
    {
        [Header("Listen to Event Channels")]
        [Tooltip("The Event Channel to listen to (e.g., GameOver, WinGame).")]
        [SerializeField] private VoidEventChannelSO eventChannel;

        [Header("Response")]
        [Tooltip("What happens when the event is raised (e.g., Play Audio).")]
        [SerializeField] public UnityEvent response;

        private void OnEnable()
        {
            if (eventChannel != null)
            {
                eventChannel.OnEventRaised += Respond;
            }
        }

        private void OnDisable()
        {
            if (eventChannel != null)
            {
                eventChannel.OnEventRaised -= Respond;
            }
        }

        private void Respond()
        {
            response?.Invoke();
        }
    }
}