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

        [Header("Gameplay HUD")]
        [Tooltip("Assign in order, index 0 = first heart, etc.")]
        [SerializeField] private Image[] heartDimmedOverlays;
        [SerializeField] private UniText roundNumberText;

        [Header("Game Over Screen")]
        [SerializeField] private UniText finalRoundText;
        [SerializeField] private UniText highScoreText;

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

                    mainMenuIntroSequence.Append(btn.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
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

            instructionSequence.Append(gameTitle.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));

            instructionSequence.Join(gameSprite.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.1f));

            instructionSequence.Join(gameInstruction.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(0.2f));

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
        private void HandleGameOver(int finalRound)
        {
            gameplayHUD.SetActive(false);
            gameOverScreen.SetActive(true);
            if (finalRoundText != null) finalRoundText.Text = "الجولة: " + finalRound;
            int previousHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            if (finalRound > previousHighScore)
            {
                PlayerPrefs.SetInt(HighScoreKey, finalRound);
                PlayerPrefs.Save();
            }
            if (highScoreText != null) highScoreText.Text = "الرقم القياسي: " + PlayerPrefs.GetInt(HighScoreKey, 0);
        }
        public void OnRestartButtonPressed()
        {
            gameOverScreen.SetActive(false);
            gameplayHUD.SetActive(true);
            gameManager.RestartGame();
        }
        public void OnGameOverMainMenuPressed()
        {
            gameManager.ReturnToMenu();
            ShowMainMenu();
        }
        public void OnGameOverExitPressed()
        {
            OnExitButtonPressed();
        }
    }
}