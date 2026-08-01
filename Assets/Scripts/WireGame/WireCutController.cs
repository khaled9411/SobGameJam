using System.Collections.Generic;
using UnityEngine;
using SobGameJam.MiniGames;

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
        [SerializeField] private float minimumTimeLimit = 3f;

        [Header("UI")]
        [SerializeField] private TMPro.TextMeshProUGUI clueText;
        [SerializeField] private UnityEngine.UI.Image timerFillImage;

        private WireClueSO activeClue;
        private float timer;
        private float timerMax;

        protected override void OnGameStarted(int roundNumber)
        {
            int clueTier = GetClueTier(roundNumber);
            float timeLimit = GetTimeLimit(roundNumber);

            activeClue = PickRandomClue(clueTier);
            clueText.text = activeClue.clueText;

            AssignColorsToWires(activeClue.answerColor);

            timerMax = timeLimit;
            timer = timerMax;
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

            if (timer <= 0f) LoseGame();
        }

        private WireClueSO PickRandomClue(int tier)
        {
            List<WireClueSO> pool = allClues.FindAll(c => c.difficultyTier == tier);
            if (pool.Count == 0) pool = allClues;
            return pool[Random.Range(0, pool.Count)];
        }

        private void AssignColorsToWires(WireColor correctColor)
        {
            List<WireColor> allColors = new List<WireColor> { WireColor.Red, WireColor.Blue, WireColor.Yellow, WireColor.Green, WireColor.White };
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

            if (cutColor == activeClue.answerColor)
                WinGame();
            else
                LoseGame();
        }
    }
}