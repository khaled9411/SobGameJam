using UnityEngine;
using UnityEngine.UI;
using SobGameJam.Core;
using SobGameJam.Events;
using LightSide;
using DG.Tweening;

namespace SobGameJam.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Screens (assign the root GameObject of each panel)")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject gameplayHUD;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject InstructionScreen;

        [Header("Main Menu Animations")]
        [SerializeField] private RectTransform mainMenuLogo;
        [SerializeField] private RectTransform[] mainMenuButtons;
        private Sequence mainMenuIntroSequence;

        [Header("Name Slots (Team Credits - drag the 4 name RectTransforms here)")]
        [Tooltip("Assign the 4 name text RectTransforms. They'll float/wobble independently, staggered.")]
        [SerializeField] private RectTransform[] nameSlots;
        [Tooltip("How far each name bobs up/down, in UI units.")]
        [SerializeField] private float nameFloatDistance = 10f;
        [Tooltip("How long one bob cycle takes, in seconds.")]
        [SerializeField] private float nameFloatDuration = 1.2f;
        [Tooltip("How much each name tilts side to side, in degrees.")]
        [SerializeField] private float nameTiltAngle = 4f;
        [Tooltip("Delay added between each successive name slot's animation start, for a staggered wave effect.")]
        [SerializeField] private float nameStaggerDelay = 0.15f;

        [Header("Gameplay HUD")]
        [Tooltip("Assign in order, index 0 = first heart, etc.")]
        [SerializeField] private Image[] heartDimmedOverlays;
        [SerializeField] private UniText roundNumberText;

        [Header("Game Over Screen")]
        [SerializeField] private UniText finalRoundText;
        [SerializeField] private UniText highScoreText;

        [Header("Game Over Animations")]
        [SerializeField] private RectTransform gameOverLogo;
        [SerializeField] private RectTransform[] gameOverButtons;
        private Sequence gameOverSequence;

        [Header("Main Menu Screen")]
        [SerializeField] private UniText mainMenuHighScoreText;

        [Header("Instruction Screen")]
        [SerializeField] private UniText gameTitle;
        [SerializeField] private UniText gameInstruction;
        [SerializeField] private Image gameSprite;

        [Header("Game Manager Reference")]
        [SerializeField] private GameManager gameManager;

        [Header("Events (Listening To)")]
        [SerializeField] private IntEventChannelSO onLivesChangedEvent;
        [SerializeField] private IntEventChannelSO onGameOverEvent;
        [SerializeField] private IntEventChannelSO onRoundStartedEvent;
        [SerializeField] private MiniGameEventChannelSO OnMiniGamechooseEvent;
        [SerializeField] private VoidEventChannelSO OnInstructionTimeEnd;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip appearSound;
        [SerializeField] private AudioClip heartLossSound;

        [Header("Button Click Particle Effect")]
        [Tooltip("Drag the ParticleSystem you want to play on button click here.")]
        [SerializeField] private ParticleSystem buttonClickParticle;

        private const string HighScoreKey = "HighScore";
        private int _currentLives = -1;

        private Sequence instructionSequence;

        private void OnEnable()
        {
            if (onLivesChangedEvent != null) onLivesChangedEvent.OnEventRaised += HandleLivesChanged;
            if (onGameOverEvent != null) onGameOverEvent.OnEventRaised += HandleGameOver;
            if (onRoundStartedEvent != null) onRoundStartedEvent.OnEventRaised += HandleRoundStarted;
            if (OnMiniGamechooseEvent != null) OnMiniGamechooseEvent.OnEventRaised += HandleInstruction;
            if (OnInstructionTimeEnd != null) OnInstructionTimeEnd.OnEventRaised += HideInsturction;
        }

        private void OnDisable()
        {
            if (onLivesChangedEvent != null) onLivesChangedEvent.OnEventRaised -= HandleLivesChanged;
            if (onGameOverEvent != null) onGameOverEvent.OnEventRaised -= HandleGameOver;
            if (onRoundStartedEvent != null) onRoundStartedEvent.OnEventRaised -= HandleRoundStarted;
        }

        private void Start()
        {
            ShowMainMenu();
            StartLogoFloatingAnimation();
            StartNameSlotsAnimation();
        }

        private void PlayAppearSound()
        {
            if (uiAudioSource != null && appearSound != null)
            {
                uiAudioSource.PlayOneShot(appearSound);
            }
        }

        private void PlayHeartLossSound()
        {
            if (uiAudioSource != null && heartLossSound != null)
            {
                uiAudioSource.PlayOneShot(heartLossSound);
            }
        }

        // ---------- BUTTON CLICK PARTICLE EFFECT ----------

        /// Call this from a Button's OnClick() event to play the assigned particle effect.
        public void PlayButtonClickParticle()
        {
            if (buttonClickParticle != null)
            {
                buttonClickParticle.Play();
            }
        }

        private void StartLogoFloatingAnimation()
        {
            if (mainMenuLogo != null)
            {
                mainMenuLogo.DOAnchorPosY(15f, 1.5f)
                    .SetRelative(true)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                mainMenuLogo.DORotate(new Vector3(0, 0, 2f), 2f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        // ---------- NAME SLOTS (Team Credits) ----------

        private void StartNameSlotsAnimation()
        {
            if (nameSlots == null || nameSlots.Length == 0) return;

            for (int i = 0; i < nameSlots.Length; i++)
            {
                RectTransform slot = nameSlots[i];
                if (slot == null) continue;

                float startDelay = i * nameStaggerDelay;

                slot.DOAnchorPosY(nameFloatDistance, nameFloatDuration)
                    .SetRelative(true)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(startDelay);

                slot.DORotate(new Vector3(0, 0, nameTiltAngle), nameFloatDuration * 1.3f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(startDelay);
            }
        }

        // ---------- MAIN MENU ----------

        private void AnimateMainMenuOut()
        {
            mainMenuIntroSequence?.Kill();

            Sequence fadeOutSeq = DOTween.Sequence();
            if (mainMenuButtons != null)
            {
                foreach (var btn in mainMenuButtons)
                {
                    fadeOutSeq.Join(btn.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
                }
            }

            if (mainMenuLogo != null)
            {
                fadeOutSeq.Join(mainMenuLogo.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            }

            fadeOutSeq.OnComplete(() =>
            {
                mainMenuScreen.SetActive(false);
                gameplayHUD.SetActive(true);
                gameOverScreen.SetActive(false);

                gameManager.BeginNewRun();
            });
        }

        public void ShowMainMenu()
        {
            mainMenuScreen.SetActive(true);
            gameplayHUD.SetActive(false);
            gameOverScreen.SetActive(false);

            if (mainMenuHighScoreText != null)
                mainMenuHighScoreText.Text = "الرقم القياسي: " + PlayerPrefs.GetInt(HighScoreKey, 0);

            if (mainMenuLogo != null)
            {
                mainMenuLogo.localScale = Vector3.one;
            }

            AnimateMainMenuIntro();
        }

        private void AnimateMainMenuIntro()
        {
            mainMenuIntroSequence?.Kill();
            mainMenuIntroSequence = DOTween.Sequence();

            if (mainMenuButtons != null && mainMenuButtons.Length > 0)
            {
                foreach (var btn in mainMenuButtons)
                {
                    btn.localScale = Vector3.zero;
                    mainMenuIntroSequence.Append(btn.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).OnStart(PlayAppearSound));
                }
            }
        }

        public void OnStartButtonPressed()
        {
            AnimateMainMenuOut();
        }

        public void OnExitButtonPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void HandleInstruction(MiniGameData arg0)
        {
            InstructionScreen.SetActive(true);
            gameTitle.Text = arg0.gameName;
            gameSprite.sprite = arg0.instructionSprite;
            gameInstruction.Text = arg0.instructionPrompt;

            instructionSequence?.Kill();
            instructionSequence = DOTween.Sequence();

            gameTitle.transform.localScale = Vector3.zero;
            gameSprite.transform.localScale = Vector3.zero;
            gameInstruction.transform.localScale = Vector3.zero;

            instructionSequence.Append(gameTitle.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnStart(PlayAppearSound));
            instructionSequence.Join(gameSprite.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.1f).OnStart(PlayAppearSound));
            instructionSequence.Join(gameInstruction.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(0.2f).OnStart(PlayAppearSound));

            instructionSequence.Join(gameSprite.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f, 5, 0.5f).SetDelay(0.15f));
        }

        private void HideInsturction()
        {
            instructionSequence?.Kill();
            instructionSequence = DOTween.Sequence();

            instructionSequence.Append(gameTitle.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            instructionSequence.Join(gameSprite.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            instructionSequence.Join(gameInstruction.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));

            instructionSequence.OnComplete(() =>
            {
                InstructionScreen.SetActive(false);
            });
        }

        private void HandleLivesChanged(int newLives)
        {
            bool didLoseLife = _currentLives != -1 && newLives < _currentLives;
            int lostHeartIndex = newLives;

            _currentLives = newLives;

            for (int i = 0; i < heartDimmedOverlays.Length; i++)
            {
                if (heartDimmedOverlays[i] == null) continue;

                bool shouldBeDimmed = (i >= newLives);

                if (shouldBeDimmed && !heartDimmedOverlays[i].gameObject.activeSelf)
                {
                    heartDimmedOverlays[i].gameObject.SetActive(true);

                    if (didLoseLife && i == lostHeartIndex)
                    {
                        AnimateHeartLoss(heartDimmedOverlays[i]);
                    }
                }
                else if (!shouldBeDimmed)
                {
                    heartDimmedOverlays[i].gameObject.SetActive(false);
                }
            }
        }

        private void AnimateHeartLoss(Image dimmedHeartOverlay)
        {
            RectTransform rect = dimmedHeartOverlay.rectTransform;

            rect.DOKill();
            dimmedHeartOverlay.DOKill();

            rect.localScale = Vector3.one;
            dimmedHeartOverlay.color = Color.white;

            Sequence lossSeq = DOTween.Sequence();

            lossSeq.OnStart(PlayHeartLossSound);

            lossSeq.Append(rect.DOScale(Vector3.one * 1.6f, 0.15f).SetEase(Ease.OutBack));
            lossSeq.Join(dimmedHeartOverlay.DOColor(Color.red, 0.15f));
            lossSeq.Join(rect.DOShakeRotation(0.3f, new Vector3(0, 0, 45f), 12, 90f));
            lossSeq.Append(rect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBounce));
            lossSeq.Join(dimmedHeartOverlay.DOColor(Color.white, 0.3f));

            if (rect.parent != null)
            {
                rect.parent.DOKill(true);
                rect.parent.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 25, 90f, false, true);
            }
        }

        private void HandleRoundStarted(int roundNumber)
        {
            if (roundNumberText != null) roundNumberText.Text = "الجولة: " + roundNumber;
        }

        private void AnimateGameOverOut(System.Action onComplete)
        {
            gameOverSequence?.Kill();
            gameOverSequence = DOTween.Sequence();

            if (gameOverButtons != null)
            {
                foreach (var btn in gameOverButtons)
                {
                    gameOverSequence.Join(btn.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
                }
            }

            if (gameOverLogo != null) gameOverSequence.Join(gameOverLogo.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            if (finalRoundText != null) gameOverSequence.Join(finalRoundText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            if (highScoreText != null) gameOverSequence.Join(highScoreText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));

            gameOverSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void AnimateGameOverScreen()
        {
            gameOverSequence?.Kill();
            gameOverSequence = DOTween.Sequence();

            if (gameOverLogo != null) gameOverLogo.localScale = Vector3.zero;
            if (finalRoundText != null) finalRoundText.transform.localScale = Vector3.zero;
            if (highScoreText != null) highScoreText.transform.localScale = Vector3.zero;

            if (gameOverButtons != null)
            {
                foreach (var btn in gameOverButtons) btn.localScale = Vector3.zero;
            }

            if (gameOverLogo != null)
            {
                gameOverSequence.Append(gameOverLogo.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce).OnStart(PlayAppearSound));
                gameOverSequence.Join(gameOverLogo.DOPunchRotation(new Vector3(0, 0, 8f), 0.5f, 6, 0.5f));
            }

            if (gameOverButtons != null)
            {
                foreach (var btn in gameOverButtons)
                {
                    gameOverSequence.Append(btn.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).OnStart(PlayAppearSound));
                }
            }

            if (finalRoundText != null)
                gameOverSequence.Join(finalRoundText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnStart(PlayAppearSound));

            if (highScoreText != null)
                gameOverSequence.Join(highScoreText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(0.1f).OnStart(PlayAppearSound));
        }

        private void HandleGameOver(int finalRound)
        {
            gameplayHUD.SetActive(false);
            gameOverScreen.SetActive(true);

            if (finalRoundText != null)
                finalRoundText.Text = "الجولة: " + finalRound;

            int previousHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            if (finalRound > previousHighScore)
            {
                PlayerPrefs.SetInt(HighScoreKey, finalRound);
                PlayerPrefs.Save();
            }

            if (highScoreText != null)
                highScoreText.Text = "الرقم القياسي: " + PlayerPrefs.GetInt(HighScoreKey, 0);

            AnimateGameOverScreen();
        }

        public void OnRestartButtonPressed()
        {
            AnimateGameOverOut(() =>
            {
                gameOverScreen.SetActive(false);
                gameplayHUD.SetActive(true);
                gameManager.RestartGame();
            });
        }

        public void OnGameOverMainMenuPressed()
        {
            AnimateGameOverOut(() =>
            {
                gameManager.ReturnToMenu();
                ShowMainMenu();
            });
        }

        public void OnGameOverExitPressed()
        {
            OnExitButtonPressed();
        }
    }
}