using UnityEngine;
using TMPro;
using DG.Tweening;


namespace SobGameJam.MiniGames
{
    [System.Serializable]
    public class BalloonDifficulty
    {
        public string levelName = "Level 1";

        [Header("Inflation")]
        public float[] inflationSteps = { 0.05f, 0.10f, 0.15f };
        public bool loopSteps = true;

        [Header("Leak")]
        public float leakRate = 0.2f;

        [Header("Boundaries")]
        public float maxScale = 2.5f;
        public float minScale = 0.3f;

        [Header("Win Condition")]
        public float timeToSurvive = 10f;
    }

    public class BalloonGameManager : MiniGameBase
    {
        [Header("References")]
        public Transform balloonTransform;

        [Header("UI Elements")]
        public TextMeshProUGUI nextPumpText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI resultText;

        [Header("Difficulty ")]
        public BalloonDifficulty[] difficultyLevels;
        public int currentDifficultyIndex = 0;

        [Header("Explosion")]
        public int particleCount = 10;

        private float currentScale = 1f;
        private int currentStepIndex = 0;
        private float survivalTimer = 0f;

        private BalloonDifficulty currentLevel;

        void Start()
        {
            if (resultText != null) resultText.text = "";
        }

        protected override void OnGameStarted(int roundNumber)
        {
            StartBalloonGame();
        }

        public void StartBalloonGame()
        {
            currentLevel = difficultyLevels[currentDifficultyIndex];

            currentScale = 1f;
            balloonTransform.localScale = Vector3.one * currentScale;
            currentStepIndex = 0;
            survivalTimer = 0f;

            if (resultText != null) resultText.text = "";

            UpdateUI();
        }

        void Update()
        {
            if (!base.isGameActive) return;

            survivalTimer += Time.deltaTime;

            currentScale -= currentLevel.leakRate * Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                PumpBalloon();
            }

            balloonTransform.localScale = Vector3.one * currentScale;

            UpdateUI();

            if (survivalTimer >= currentLevel.timeToSurvive)
            {
                WinMiniGame();
            }
            else if (currentScale >= currentLevel.maxScale)
            {
                PlayExplosionEffect();
                LoseMiniGame("The balloon burst!");
            }
            else if (currentScale <= currentLevel.minScale)
            {
                balloonTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                LoseMiniGame("The balloon shrunk!");
            }
        }

        private void PumpBalloon()
        {
            float pumpAmount = currentLevel.inflationSteps[currentStepIndex];
            currentScale += pumpAmount;

            balloonTransform.DOKill();
            balloonTransform.DOPunchScale(Vector3.one * (pumpAmount / 2f), 0.15f, 2, 1f);

            currentStepIndex++;
            if (currentStepIndex >= currentLevel.inflationSteps.Length)
            {
                currentStepIndex = currentLevel.loopSteps ? 0 : currentLevel.inflationSteps.Length - 1;
            }

            if (nextPumpText != null)
            {
                nextPumpText.transform.DOKill(true);
                nextPumpText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
                nextPumpText.DOColor(Color.yellow, 0.1f).OnComplete(() => nextPumpText.DOColor(Color.white, 0.2f));
            }
        }

        private void UpdateUI()
        {
            float timeLeft = Mathf.Max(0, currentLevel.timeToSurvive - survivalTimer);

            if (timerText != null)
            {
                timerText.text = "Time Left: " + timeLeft.ToString("F1") + "s";
            }

            if (nextPumpText != null)
            {
                float nextPumpPercentage = currentLevel.inflationSteps[currentStepIndex] * 100f;
                nextPumpText.text = "Next Pump: +" + nextPumpPercentage.ToString("F0") + "%";
            }
        }

        private void PlayExplosionEffect()
        {
            balloonTransform.DOKill();

            SpriteRenderer sr = balloonTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                balloonTransform.DOScale(currentScale * 1.5f, 0.1f).SetEase(Ease.OutExpo);

                sr.DOColor(Color.white, 0.05f).OnComplete(() =>
                {
                    sr.DOFade(0f, 0.1f);
                });
            }

            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.4f, 0.5f, 20, 90f);
            }

            CreateDOTweenParticles();
        }

        private void CreateDOTweenParticles()
        {
            SpriteRenderer originalSr = balloonTransform.GetComponent<SpriteRenderer>();
            if (originalSr == null) return;

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = new GameObject("PopParticle");
                particle.transform.position = balloonTransform.position;

                SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = originalSr.sprite;
                sr.color = originalSr.color;

                particle.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);

                Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
                Vector3 targetPos = particle.transform.position + (Vector3)randomDir;

                particle.transform.DOMove(targetPos, 0.4f).SetEase(Ease.OutCubic);
                particle.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack);
                sr.DOFade(0, 0.4f).SetEase(Ease.InCubic);

                Destroy(particle, 0.5f);
            }
        }

        public void WinMiniGame()
        {
            if (resultText != null) resultText.text = "You Survived!";
            Debug.Log("You have won! The balloon has been successfully kept at the critical point.");
            WinGame(); // This will trigger the WonEvent in MiniGameBase
        }

        public void LoseMiniGame(string reason)
        {
            if (resultText != null) resultText.text = reason;
            Debug.Log("You lost! The reason: " + reason);
            LoseGame(); // This will trigger the LostEvent in MiniGameBase
        }
    }
}