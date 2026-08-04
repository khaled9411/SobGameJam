using UnityEngine;
using LightSide;
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
        public UniText nextPumpText;
        public UniText timerText;
        public UniText resultText;

        [Header("Difficulty")]
        public BalloonDifficulty[] difficultyLevels;
        public AnimationCurve difficultyCurve;
        public int currentDifficultyIndex = 0;

        [Header("Explosion")]
        public int particleCount = 10;
        public ParticleSystem explosionParticles;

        [Header("Danger Zones")]
        public float explosionWarningPercent = 0.85f;
        public float shrinkWarningPercent = 0.15f;

        [Header("Explosion Warning")]
        public float maxShakeStrength = 0.15f;
        public float maxShakeDuration = 0.1f;
        public int maxShakeVibrato = 10;

        [Header("Shrink Warning")]
        public float wobbleScale = 0.05f;
        public float wobbleSpeed = 0.3f;
        public float wobbleRotation = 5f;

        [Header("Audio (SFX)")]
        public AudioSource sfxSource;
        public AudioSource warningSource;

        public AudioClip gameStartSound;
        public AudioClip pumpSound;
        public AudioClip explosionWarningSound;
        public AudioClip shrinkWarningSound;

        private float currentScale = 1f;
        private int currentStepIndex = 0;
        private float survivalTimer = 0f;
        private BalloonDifficulty currentLevel;

        // --- Danger System Variables ---
        private Transform shakeDummy;
        private Tween explosionTween;
        private Tween shrinkTween;

        private bool inExplosionZone = false;
        private bool inShrinkZone = false;

        private float _explosionShakeValue = 0f;
        private float _shrinkScaleWobble = 0f;
        private float _shrinkRotWobble = 0f;
        private float _shrinkWobbleDir = 1f;

        void Start()
        {
            if (resultText != null) resultText.Text = "";

            // Create an empty dummy object to calculate DOShakeScale safely.
            shakeDummy = new GameObject("ExplosionShakeDummy").transform;
            shakeDummy.SetParent(this.transform);
        }

        protected override void OnGameStarted(int roundNumber)
        {
            if (difficultyLevels == null || difficultyLevels.Length == 0 || difficultyCurve.length == 0)
            {
                Debug.LogWarning("Difficulty Levels or Animation Curve is not set up correctly!");
                return;
            }

            float maxCurveTime = difficultyCurve.keys[difficultyCurve.length - 1].time;

            float evaluatedTime = Mathf.PingPong(roundNumber, maxCurveTime);

            float curveValue = difficultyCurve.Evaluate(evaluatedTime);

            currentDifficultyIndex = Mathf.Clamp(Mathf.RoundToInt(curveValue), 0, difficultyLevels.Length - 1);

            StartBalloonGame();
        }

        public void StartBalloonGame()
        {
            currentLevel = difficultyLevels[currentDifficultyIndex];

            currentScale = 1f;
            balloonTransform.localScale = Vector3.one * currentScale;
            currentStepIndex = 0;
            survivalTimer = 0f;

            if (resultText != null) resultText.Text = "";

            if (sfxSource != null && gameStartSound != null)
                sfxSource.PlayOneShot(gameStartSound);

            KillDangerTweens(); // Ensure a clean slate on restarts
            UpdateUI();
        }

        void Update()
        {
            if (!base.isGameActive) return;

            survivalTimer += Time.deltaTime;
            currentScale -= currentLevel.leakRate * Time.deltaTime;

            if (Input.GetMouseButtonDown(0))
            {
                PumpBalloon();
            }

            // 1. Evaluate danger logic and handle tween lifecycles
            UpdateDangerEffects();

            // 2. Apply visual scale, merging gameplay scale with our danger feedback offsets
            float finalScale = currentScale + _explosionShakeValue + _shrinkScaleWobble;
            balloonTransform.localScale = Vector3.one * finalScale;
            balloonTransform.localEulerAngles = new Vector3(0, 0, _shrinkRotWobble);

            UpdateUI();

            // 3. Evaluate Win/Lose States
            if (survivalTimer >= currentLevel.timeToSurvive)
            {
                WinMiniGame();
            }
            else if (currentScale >= currentLevel.maxScale)
            {
                PlayExplosionEffect();
                LoseMiniGame("انفجر في وشك");
            }
            else if (currentScale <= currentLevel.minScale)
            {
                KillDangerTweens();

                balloonTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                LoseMiniGame("انكمش البالون!");
            }
        }

        #region Danger Feedback System

        private void UpdateDangerEffects()
        {
            float normalScale = 1f;

            // --- Explosion Evaluation ---
            float expThreshold = Mathf.Lerp(normalScale, currentLevel.maxScale, explosionWarningPercent);
            if (currentScale >= expThreshold && currentScale < currentLevel.maxScale)
            {
                if (!inExplosionZone) StartExplosionWarning();
            }
            else
            {
                if (inExplosionZone) StopExplosionWarning();
            }

            // --- Shrink Evaluation ---
            float shrinkThreshold = Mathf.Lerp(currentLevel.minScale, normalScale, shrinkWarningPercent);
            if (currentScale <= shrinkThreshold && currentScale > currentLevel.minScale)
            {
                if (!inShrinkZone) StartShrinkWarning();
            }
            else
            {
                if (inShrinkZone) StopShrinkWarning();
            }
        }

        // --- EXPLOSION WARNING ---

        private void StartExplosionWarning()
        {
            inExplosionZone = true;
            PlayExplosionLoop();

            if (warningSource != null && explosionWarningSound != null)
            {
                warningSource.clip = explosionWarningSound;
                warningSource.loop = true;
                warningSource.Play();
            }
        }

        private void StopExplosionWarning()
        {
            inExplosionZone = false;
            if (explosionTween != null)
            {
                explosionTween.Kill();
                explosionTween = null;
            }
            _explosionShakeValue = 0f;

            if (warningSource != null && warningSource.clip == explosionWarningSound)
            {
                warningSource.Stop();
            }
        }

        private void PlayExplosionLoop()
        {
            if (!inExplosionZone) return;

            float expThreshold = Mathf.Lerp(1f, currentLevel.maxScale, explosionWarningPercent);
            float t = Mathf.InverseLerp(expThreshold, currentLevel.maxScale, currentScale);

            float currentStrength = Mathf.Lerp(0f, maxShakeStrength, t);
            int currentVibrato = (int)Mathf.Lerp(2, maxShakeVibrato, t);
            float currentDuration = Mathf.Lerp(maxShakeDuration * 1.5f, maxShakeDuration, t);

            shakeDummy.localScale = Vector3.zero;

            explosionTween = shakeDummy.DOShakeScale(currentDuration, currentStrength, currentVibrato, 90f, true)
                .OnUpdate(UpdateExplosionShakeValue)
                .OnComplete(PlayExplosionLoop);
        }

        private void UpdateExplosionShakeValue()
        {
            _explosionShakeValue = shakeDummy.localScale.x;
        }

        // --- SHRINK WARNING ---

        private void StartShrinkWarning()
        {
            inShrinkZone = true;
            _shrinkWobbleDir = 1f;
            PlayShrinkLoop();

            if (warningSource != null && shrinkWarningSound != null)
            {
                warningSource.clip = shrinkWarningSound;
                warningSource.loop = true;
                warningSource.Play();
            }
        }

        private void StopShrinkWarning()
        {
            inShrinkZone = false;
            if (shrinkTween != null)
            {
                shrinkTween.Kill();
                shrinkTween = null;
            }

            _shrinkScaleWobble = 0f;
            _shrinkRotWobble = 0f;
            balloonTransform.DORotate(Vector3.zero, 0.2f);

            if (warningSource != null && warningSource.clip == shrinkWarningSound)
            {
                warningSource.Stop();
            }
        }

        private void PlayShrinkLoop()
        {
            if (!inShrinkZone) return;

            float shrinkThreshold = Mathf.Lerp(currentLevel.minScale, 1f, shrinkWarningPercent);
            float t = Mathf.InverseLerp(shrinkThreshold, currentLevel.minScale, currentScale);

            float currentWobble = Mathf.Lerp(0f, wobbleScale, t);
            float currentRot = Mathf.Lerp(0f, wobbleRotation, t) * _shrinkWobbleDir;
            float currentSpeed = Mathf.Lerp(wobbleSpeed * 1.5f, wobbleSpeed, t);

            shrinkTween = DOTween.Sequence()
                .Join(DOTween.To(() => _shrinkScaleWobble, x => _shrinkScaleWobble = x, currentWobble, currentSpeed).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo))
                .Join(DOTween.To(() => _shrinkRotWobble, x => _shrinkRotWobble = x, currentRot, currentSpeed).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo))
                .OnComplete(OnShrinkLoopComplete);
        }

        private void OnShrinkLoopComplete()
        {
            _shrinkWobbleDir *= -1f;
            PlayShrinkLoop();
        }

        private void KillDangerTweens()
        {
            inExplosionZone = false;
            inShrinkZone = false;

            if (explosionTween != null) explosionTween.Kill();
            if (shrinkTween != null) shrinkTween.Kill();
            if (shakeDummy != null) shakeDummy.DOKill();

            _explosionShakeValue = 0f;
            _shrinkScaleWobble = 0f;
            _shrinkRotWobble = 0f;

            if (warningSource != null) warningSource.Stop();
        }

        #endregion

        private void PumpBalloon()
        {
            if (sfxSource != null && pumpSound != null)
                sfxSource.PlayOneShot(pumpSound);

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
                timerText.Text = "باقي من الوقت: " + timeLeft.ToString("F1") + "ث";
            }

            if (nextPumpText != null)
            {
                float nextPumpPercentage = currentLevel.inflationSteps[currentStepIndex] * 100f;
                nextPumpText.Text = "النفخه الجايه: +" + nextPumpPercentage.ToString("F0") + "%" +"\n اضغط بالماوس";
            }
        }

        private void PlayExplosionEffect()
        {
            KillDangerTweens(); // Stop all warnings immediately
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
            KillDangerTweens();
            if (resultText != null) resultText.Text = "عاشش مفرقعتش";
            Debug.Log("You have won! The balloon has been successfully kept at the critical point.");
            WinGame();
        }

        public void LoseMiniGame(string reason)
        {
            explosionParticles?.Play();
            if (resultText != null) resultText.Text = reason;
            Debug.Log("You lost! The reason: " + reason);
            LoseGame();
        }
    }
}