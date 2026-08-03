using UnityEngine;
using SobGameJam.Events;

namespace SobGameJam.Audio
{
    public class SoundManagerMariam : MonoBehaviour
    {
        [Header("Global Mini-Game Events")]
        [Tooltip("Same asset MiniGameBase raises on any mini-game win.")]
        [SerializeField] private VoidEventChannelSO miniGameWonEvent;
        [Tooltip("Same asset MiniGameBase raises on any mini-game loss (wrong wire OR timeout) — drives the explosion.")]
        [SerializeField] private VoidEventChannelSO miniGameLostEvent;

        [Header("Wire Cut Specific Events")]
        [SerializeField] private VoidEventChannelSO onWireCutCorrectEvent;
        [SerializeField] private VoidEventChannelSO onWireCutWrongEvent;
        [SerializeField] private FloatEventChannelSO onTimerTickEvent;

        [Header("Clips")]
        [SerializeField] private AudioClip correctCutClip;
        [SerializeField] private AudioClip wrongCutClip;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private AudioClip timerLoopClip; // full looping timer clip instead of a tick sound

        [Header("Timer Sound Settings")]
        [Tooltip("Seconds remaining at which the timer loop sound starts playing.")]
        [SerializeField] private float timerSoundThreshold = 3f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource tickSource; // now used to loop the full timer clip

        private bool isTimerSoundPlaying;

        private void OnEnable()
        {
            if (onWireCutCorrectEvent != null) onWireCutCorrectEvent.OnEventRaised += HandleCorrectCut;
            if (onWireCutWrongEvent != null) onWireCutWrongEvent.OnEventRaised += HandleWrongCut;
            if (onTimerTickEvent != null) onTimerTickEvent.OnEventRaised += HandleTimerTick;
            if (miniGameLostEvent != null) miniGameLostEvent.OnEventRaised += HandleMiniGameLost;
        }

        private void OnDisable()
        {
            if (onWireCutCorrectEvent != null) onWireCutCorrectEvent.OnEventRaised -= HandleCorrectCut;
            if (onWireCutWrongEvent != null) onWireCutWrongEvent.OnEventRaised -= HandleWrongCut;
            if (onTimerTickEvent != null) onTimerTickEvent.OnEventRaised -= HandleTimerTick;
            if (miniGameLostEvent != null) miniGameLostEvent.OnEventRaised -= HandleMiniGameLost;
        }

        private void HandleCorrectCut()
        {
            PlaySfx(correctCutClip);
            StopTimerSound();
        }

        private void HandleWrongCut()
        {
            PlaySfx(wrongCutClip);
            StopTimerSound();
        }

        /// <summary>Covers BOTH timeout and wrong-wire-cut, since MiniGameBase.LoseGame() raises this in both cases.</summary>
        private void HandleMiniGameLost()
        {
            PlaySfx(explosionClip);
            StopTimerSound();
        }

        /// <summary>
        /// Called every frame with seconds remaining. Starts the full timer clip looping
        /// once under threshold, and keeps it playing continuously (not re-triggered per tick)
        /// until time is no longer under threshold or the round ends.
        /// </summary>
        private void HandleTimerTick(float secondsRemaining)
        {
            Debug.Log($"[SoundManager] Tick received: {secondsRemaining}");

            if (secondsRemaining > timerSoundThreshold)
            {
                StopTimerSound();
                return;
            }

            if (!isTimerSoundPlaying)
            {
                Debug.Log("[SoundManager] Starting timer sound");
                StartTimerSound();
            }
        }

        private void StartTimerSound()
        {
            if (tickSource == null || timerLoopClip == null) return;

            tickSource.clip = timerLoopClip;
            tickSource.loop = true;
            tickSource.Play();
            isTimerSoundPlaying = true;
        }

        private void StopTimerSound()
        {
            if (!isTimerSoundPlaying) return;

            if (tickSource != null)
                tickSource.Stop();

            isTimerSoundPlaying = false;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}