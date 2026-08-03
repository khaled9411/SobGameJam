using UnityEngine;
using DG.Tweening;

namespace SobGameJam.MiniGames.WireCut
{
    public class TntShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float shakeStrength = 0.15f;
        [SerializeField] private int vibrato = 20;
        [SerializeField] private float randomness = 90f;
        [SerializeField] private bool fadeOut = true;

        private Vector3 originalPosition;
        private Tween shakeTween;

        private void Awake()
        {
            originalPosition = transform.localPosition;
        }

        /// <summary>
        /// Starts an infinite, ever-so-slight shake to sell "about to explode" tension.
        /// Call this when the timer crosses your danger threshold.
        /// </summary>
        public void StartShake()
        {
            StopShake();

            shakeTween = transform
                .DOShakePosition(1f, shakeStrength, vibrato, randomness, false, fadeOut)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(UpdateType.Normal, true); // ignores timescale in case you pause gameplay elsewhere
        }

        /// <summary>
        /// Stops the shake and snaps back to the original position.
        /// Call this on win/loss/round end so the TNT doesn't stay jittering.
        /// </summary>
        public void StopShake()
        {
            if (shakeTween != null && shakeTween.IsActive())
                shakeTween.Kill();

            transform.localPosition = originalPosition;
        }

        private void OnDisable()
        {
            StopShake();
        }
    }
}