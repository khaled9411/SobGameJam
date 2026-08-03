using System.Collections.Generic;
using UnityEngine;
using SobGameJam.MiniGames;
using SobGameJam.Events;
using LightSide;

namespace SobGameJam.MiniGames.WireCut
{
    public class WireCutController : MiniGameBase
    {
        [Header("Clues")]
        [SerializeField] private List<WireClueSO> allClues;

        [Header("Wires (assign the 3 rectangles in scene)")]
        [SerializeField] private WireVisual[] wireSlots;

        [Header("Clue Difficulty Curve")]
        [Tooltip("Round number at which clue tier 2 (indirect) begins.")]
        [SerializeField] private int tier2StartRound = 3;
        [Tooltip("Round number at which clue tier 3 (hardest) begins and stays forever.")]
        [SerializeField] private int tier3StartRound = 6;

        [Header("Timer Difficulty Curve")]
        [SerializeField] private float startingTimeLimit = 8f;
        [SerializeField] private float timeReductionPerRound = 0.4f;
        [SerializeField] private float minimumTimeLimit = 4f;

        [Header("UI")]
        [SerializeField] private UniText clueText;
        [SerializeField] private UnityEngine.UI.Image timerFillImage; //should I make it a bar??

        [Header("TNT Shake")]
        [Tooltip("Assign the TntShake component on your TNT prefab in the scene.")]
        [SerializeField] private TntShake tntShake;
        [Tooltip("Seconds remaining at which the TNT starts shaking.")]
        [SerializeField] private float shakeThreshold = 3f;

        [Header("Explosion VFX")]
        [Tooltip("Assign the explosion ParticleSystem PREFAB from the Project window (not a scene instance).")]
        [SerializeField] private ParticleSystem explosionParticlesPrefab;
        [Tooltip("Where to spawn the explosion — drag the TNT's Transform here.")]
        [SerializeField] private Transform explosionSpawnPoint;

        [Header("Sound Events (Broadcasting)")]
        [Tooltip("Raised when the player cuts the correct wire.")]
        [SerializeField] private VoidEventChannelSO onWireCutCorrectEvent;
        [Tooltip("Raised when the player cuts the wrong wire.")]
        [SerializeField] private VoidEventChannelSO onWireCutWrongEvent;
        [Tooltip("Raised every frame while active, carrying seconds remaining.")]
        [SerializeField] private FloatEventChannelSO onTimerTickEvent;

        //broadcast
        [SerializeField] private VoidEventChannelSO OnGameStart;

        private WireClueSO activeClue;
        private float timer;
        private float timerMax;
        private bool isShaking;

        protected override void OnGameStarted(int roundNumber)
        {
            OnGameStart.RaiseEvent();
            int clueTier = GetClueTier(roundNumber);
            float timeLimit = GetTimeLimit(roundNumber);

            activeClue = PickRandomClue(clueTier);
            clueText.Text = activeClue.clueText;

            AssignColorsToWires(activeClue.answerColor);

            timerMax = timeLimit;
            timer = timerMax;

            isShaking = false;
            if (tntShake != null) tntShake.StopShake();
        }

        /// <summary>
        /// Clue tier climbs 1 -> 2 -> 3 as roundNumber crosses configured thresholds,
        /// then holds at 3 forever. Adjust tier2StartRound / tier3StartRound to retune the curve.
        /// </summary>
        private int GetClueTier(int roundNumber)
        {
            if (roundNumber >= tier3StartRound) return 3;
            if (roundNumber >= tier2StartRound) return 2;
            return 1;
        }

        /// <summary>
        /// Timer shrinks a fixed amount per round, independent of clue tier,
        /// clamped so endless play never goes below minimumTimeLimit.
        /// </summary>
        private float GetTimeLimit(int roundNumber)
        {
            float reduction = (roundNumber - 1) * timeReductionPerRound;
            return Mathf.Max(minimumTimeLimit, startingTimeLimit - reduction);
        }

        private void Update()
        {
            if (!isGameActive) return;

            timer -= Time.deltaTime;
            if (timerFillImage != null) timerFillImage.fillAmount = timer / timerMax;

            if (onTimerTickEvent != null) onTimerTickEvent.RaiseEvent(timer);

            if (!isShaking && timer <= shakeThreshold)
            {
                isShaking = true;
                if (tntShake != null) tntShake.StartShake();
            }

            if (timer <= 0f)
            {
                TriggerExplosion();
                LoseGame();
            }
        }

        private WireClueSO PickRandomClue(int tier)
        {
            List<WireClueSO> pool = allClues.FindAll(c => c.difficultyTier == tier);
            if (pool.Count == 0) pool = allClues;
            return pool[Random.Range(0, pool.Count)];
        }

        private void AssignColorsToWires(WireColor correctColor)
        {
            List<WireColor> allColors = new List<WireColor> { WireColor.Red, WireColor.Blue, WireColor.Yellow, WireColor.Green };
            allColors.Remove(correctColor);

            List<WireColor> chosen = new List<WireColor> { correctColor };
            for (int i = 0; i < wireSlots.Length - 1; i++)
            {
                int idx = Random.Range(0, allColors.Count);
                chosen.Add(allColors[idx]);
                allColors.RemoveAt(idx);
            }

            for (int i = 0; i < chosen.Count; i++)
            {
                int swap = Random.Range(i, chosen.Count);
                (chosen[i], chosen[swap]) = (chosen[swap], chosen[i]);
            }

            for (int i = 0; i < wireSlots.Length; i++)
            {
                wireSlots[i].SetColor(chosen[i]);
            }
        }

        public void OnWireCut(WireColor cutColor)
        {
            if (!isGameActive) return;

            if (tntShake != null) tntShake.StopShake();
            isShaking = false;

            if (cutColor == activeClue.answerColor)
            {
                if (onWireCutCorrectEvent != null) onWireCutCorrectEvent.RaiseEvent();
                WinGame();
            }
            else
            {
                if (onWireCutWrongEvent != null) onWireCutWrongEvent.RaiseEvent();
                TriggerExplosion();
                LoseGame(); // miniGameLostEvent (in MiniGameBase) covers the explosion sound
            }
        }

        /// <summary>
        /// Fires the explosion VFX. Called on wrong-wire cut here, and also
        /// needs to be called from timeout below.
        /// </summary>
        private void TriggerExplosion()
        {
            Debug.Log("[WireCutController] TriggerExplosion called");

            if (explosionParticlesPrefab != null)
            {
                Vector3 spawnPos = explosionSpawnPoint != null ? explosionSpawnPoint.position : transform.position;
                ParticleSystem instance = Instantiate(explosionParticlesPrefab, spawnPos, Quaternion.identity);
                instance.Play();
                Destroy(instance.gameObject, instance.main.duration + instance.main.startLifetime.constantMax);
            }
            else
            {
                Debug.LogWarning("[WireCutController] explosionParticlesPrefab is NOT assigned!");
            }
        }
    }
}