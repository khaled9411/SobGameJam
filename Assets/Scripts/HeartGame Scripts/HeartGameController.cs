using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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
        [Header("Timer")]
        [SerializeField] private Image timerFill;
        [SerializeField] private HeartMonitor heartMonitor;
        [Header("SFX")]
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private AudioClip perfectSFX;
        [SerializeField] private AudioSource backgroundAudio;

       
        [SerializeField] private AudioClip heartbeatLoop;
        [SerializeField] private AudioClip flatlineLoop;
        [SerializeField] private Volume volume;
        [SerializeField] private float maxVignetteIntensity = 0.3f;
        [SerializeField] private float vignetteSmoothSpeed = 5f;

        private Vignette vignette;



        private bool isPhaseTwo = false;
        private bool gameEnded;
        private float currentScore = 0;
        private Coroutine spikeRoutine;


     


        protected override void OnGameStarted(int roundNumber)
        {
            gameEnded = false;
            backgroundAudio.clip = flatlineLoop;
            backgroundAudio.loop = true;
            backgroundAudio.Play();
            volume.profile.TryGet(out vignette);
            Debug.Log($"Round {roundNumber} started!");

            float difficulty = difficultyCurve.Evaluate(roundNumber); //difficulty
            spawner.SetDifficulty(difficulty);
           
           
            backgroundAudio.Play();
            heartMonitor.SetSpikeHeight(0f,0f);

            phaseTwoDuration = Mathf.Lerp(20f, 10f, difficulty);

            scoreBar.maxValue = maxScore;
            scoreBar.value = currentScore;

            timerFill.gameObject.SetActive(false);
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
            heartMonitor.SetSpikeHeight(1f, 1f);

            audioSource.PlayOneShot(perfectSFX);

            if (spikeRoutine != null)
                StopCoroutine(spikeRoutine);

            heartMonitor.SetSpikeHeight(1f, 1f);
            spikeRoutine = StartCoroutine(ResetSpikeRoutine());

        }
        private void StaticPressed()
        {
            AddScore(staticPoints);
            audioSource.PlayOneShot(perfectSFX);
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
            
        }
        private void RefreshVignette()
        {
            if (vignette == null)
                return;

            float targetIntensity;

            if (!isPhaseTwo)
            {
                // Phase 1
                float t = currentScore / maxScore;
                targetIntensity = Mathf.Lerp(maxVignetteIntensity, 0f, t);
            }
            else
            {
                // Phase 2
                float distanceOutsideSafeZone = 0f;

                if (currentScore < safeMin)
                {
                    distanceOutsideSafeZone = safeMin - currentScore;
                }
                else if (currentScore > safeMax)
                {
                    distanceOutsideSafeZone = currentScore - safeMax;
                }

                float maxDistance = Mathf.Max(safeMin, maxScore - safeMax);

                float normalized = Mathf.Clamp01(distanceOutsideSafeZone / 20f);

                targetIntensity = Mathf.Lerp(0f, maxVignetteIntensity, normalized);

            }

            vignette.intensity.value = Mathf.Lerp(
                vignette.intensity.value,
                targetIntensity,
                Time.deltaTime * vignetteSmoothSpeed);
        }
        private void Update()
        {
            if (gameEnded)
                return;

            RefreshVignette();
        }

        private void HandleOutOfCircles()
        {
            if (!isPhaseTwo)
                LoseGame();
            gameEnded = true;
            vignette.intensity.value = maxVignetteIntensity;
            Debug.Log("Game Lost");
        }
        private void StartPhaseTwo()
        {
            if (!isPhaseTwo && currentScore >= maxScore)
            {
                isPhaseTwo = true;
                backgroundAudio.clip = heartbeatLoop;
                backgroundAudio.Play();

                currentScore = 0;
                RefreshScoreBar();
                heartMonitor.SetSpikeHeight(1f, 1f);

                indicator.SetActive(true);
                timerFill.gameObject.SetActive(true);
                timerFill.fillAmount = 1f;
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
                timer -= Time.deltaTime;

                timerFill.fillAmount = timer / phaseTwoDuration;

                yield return null;
            }

            timerFill.fillAmount = 0f;

            EndPhaseTwo();
        }

        private IEnumerator ResetSpikeRoutine()
        {
            yield return new WaitForSeconds(0.2f);

            if (!isPhaseTwo)
            {
                heartMonitor.SetSpikeHeight(0f, 0f);
            }

            spikeRoutine = null;
        }
        private void EndPhaseTwo()
        {
            isPhaseTwo = false;
            timerFill.gameObject.SetActive(false);

            spawnerTwo.StopSpawning();

            if (currentScore >= safeMin && currentScore <= safeMax)
            {
                WinGame();
                vignette.intensity.value = 0f;
                Debug.Log("GameWon");
            }
                
            else
            {

                LoseGame();
                Debug.Log("GameLost");

                gameEnded = true;
                vignette.intensity.value = maxVignetteIntensity;
                heartMonitor.SetSpikeHeight(0f, 0f);
                spawnerTwo.StopSpawning();
                backgroundAudio.clip = flatlineLoop;
                backgroundAudio.Play();



            }
        }
    }

 }
