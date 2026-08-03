using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SobGameJam.MiniGames
{
    public class HeartGameController : MiniGameBase
    {
        [Header("Difficulty")]
        [SerializeField] private AnimationCurve difficultyCurve;

        [Header("Phase One")]
        [SerializeField] private PhaseOneCircleSpawner spawner;
        [SerializeField] private Slider scoreBar;
        [SerializeField] private float maxScore = 100;
        [SerializeField] private int perfectPoints = 10;
        [SerializeField] private int missedPoints = -5;
        [Header("Phase Two")]
        [SerializeField] private PhaseTwoCircleSpawner spawnerTwo;
        [SerializeField] private float phaseTransitionDelay = 2f;
        [SerializeField] private int staticPoints = 5;
        [SerializeField] private float drainPerSecond = 3f;
        [SerializeField] private float phaseTwoDuration = 20f;
        [SerializeField] private int safeMin = 60;
        [SerializeField] private int safeMax = 70;
        [SerializeField] private GameObject indicator;


        private bool isPhaseTwo = false;
        private float currentScore = 0;


        private void Start()
        {
            OnGameStarted(1);
        }


        protected override void OnGameStarted(int roundNumber)
        {
            Debug.Log($"Round {roundNumber} started!");

            float difficulty = difficultyCurve.Evaluate(roundNumber); //difficulty
            spawner.SetDifficulty(difficulty);

            phaseTwoDuration = Mathf.Lerp(20f, 10f, difficulty);

            scoreBar.maxValue = maxScore;
            scoreBar.value = currentScore;

            Debug.Log("Subscribing to events");
            spawner.OnOutOfCircles += HandleOutOfCircles;
            spawner.OnPerfectTiming += HandlePerfect;
            spawner.OnBadTiming += HandleBadTiming;
            spawner.OnMissed += HandleMissed;

            spawner.StartSpawning();

        }
        private void OnDestroy()
        {
            spawner.OnOutOfCircles -= HandleOutOfCircles;
            spawner.OnPerfectTiming -= HandlePerfect;
            spawner.OnBadTiming -= HandleBadTiming;
            spawner.OnMissed -= HandleMissed;
            spawnerTwo.OnClickedCircle -= StaticPressed;
            Time.timeScale = 1f;
        }


        private void AddScore(float amount)
        {
            currentScore += amount;

            currentScore = Mathf.Clamp(currentScore, 0f, maxScore);

            RefreshScoreBar();

            if (currentScore >= maxScore && !isPhaseTwo)
            {
                StartCoroutine(BeginPhaseTwo());
            }
        }
       

        private IEnumerator BeginPhaseTwo()
        {
            spawner.StopSpawning();

            // Optional: show "Phase 2" text or heartbeat animation here

            yield return new WaitForSeconds(phaseTransitionDelay);

            StartPhaseTwo();
        }

        private void HandlePerfect()
        {
            Debug.Log("Controller received perfect");
            AddScore(perfectPoints);
        }
        private void StaticPressed()
        {
            AddScore(staticPoints);
        }

        private void HandleBadTiming()
        {
            // no score change
        }

        private void HandleMissed()
        {
            AddScore(missedPoints);
        }
        private void RefreshScoreBar()
        {
            scoreBar.value = currentScore;
            //Add VFX here
        }

        private void HandleOutOfCircles()
        {
            if (!isPhaseTwo)
                LoseGame();
            Debug.Log("Game Lost");
        }
        private void StartPhaseTwo()
        {
            if (!isPhaseTwo && currentScore >= maxScore)
            {
                isPhaseTwo = true;

                currentScore = 0;
                RefreshScoreBar();
                indicator.SetActive(true);
                spawnerTwo.OnClickedCircle += StaticPressed;
                spawner.StopSpawning();
                spawnerTwo.StartSpawning();
                StartCoroutine(DrainRoutine());
                StartCoroutine(PhaseTwoTimer());

            }


        }
        IEnumerator DrainRoutine()
        {
            while (isPhaseTwo)
            {
                currentScore -= drainPerSecond * Time.deltaTime;
                RefreshScoreBar();

                yield return null;
            }
        }
        IEnumerator PhaseTwoTimer()
        {
            float timer = phaseTwoDuration;

            while (timer > 0)
            {
                Debug.Log(Mathf.CeilToInt(timer));

                yield return new WaitForSeconds(1f);

                timer -= 1f;
            }

            EndPhaseTwo();
        }
        private void EndPhaseTwo()
        {
            isPhaseTwo = false;

            spawnerTwo.StopSpawning();

            if (currentScore >= safeMin && currentScore <= safeMax)
            {
                WinGame();
                Debug.Log("GameWon");
            }
                
            else
            {
                LoseGame();
                Debug.Log("GameLost");

            }
        }
    }

 }
