using UnityEngine;
using UnityEngine.UI;
using SobGameJam.Core;
using SobGameJam.Events;
using TMPro;
using LightSide;

namespace SobGameJam.UI
{
    /// <summary>
    /// Listens to GameManager's broadcast events and updates all UI screens accordingly.
    /// Owns no game logic — purely reactive display + button-triggered calls
    /// back into GameManager (Start/Restart) and Unity's own app APIs (Exit).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens (assign the root GameObject of each panel)")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject gameplayHUD;
        [SerializeField] private GameObject gameOverScreen;

        [Header("Gameplay HUD")]
        [Tooltip("Assign in order, index 0 = first heart, etc.")]
        [SerializeField] private Image[] heartDimmedOverlays;
        [SerializeField] private UniText roundNumberText; // swap for TMPro.TextMeshProUGUI if your project uses TMP

        [Header("Game Over Screen")]
        [SerializeField] private UniText finalRoundText;
        [SerializeField] private UniText highScoreText;

        [Header("Main Menu Screen")]
        [SerializeField] private UniText mainMenuHighScoreText;

        [Header("Game Manager Reference")]
        [Tooltip("Drag the GameManager GameObject here so Start/Restart can call back into it.")]
        [SerializeField] private GameManager gameManager;

        [Header("Events (Listening To)")]
        [SerializeField] private IntEventChannelSO onLivesChangedEvent;
        [SerializeField] private IntEventChannelSO onGameOverEvent;
        [SerializeField] private IntEventChannelSO onRoundStartedEvent; // same asset GameManager broadcasts on

        private const string HighScoreKey = "HighScore";

        private void OnEnable()
        {
            if (onLivesChangedEvent != null) onLivesChangedEvent.OnEventRaised += HandleLivesChanged;
            if (onGameOverEvent != null) onGameOverEvent.OnEventRaised += HandleGameOver;
            if (onRoundStartedEvent != null) onRoundStartedEvent.OnEventRaised += HandleRoundStarted;
        }

        private void OnDisable()
        {
            if (onLivesChangedEvent != null) onLivesChangedEvent.OnEventRaised -= HandleLivesChanged;
            if (onGameOverEvent != null) onGameOverEvent.OnEventRaised -= HandleGameOver;
            if (onRoundStartedEvent != null) onRoundStartedEvent.OnEventRaised -= HandleRoundStarted;
        }

        private void Start()
        {
            // GameManager.Start() no longer auto-begins a run, so it is safe for
            // the Main Menu to simply be the first thing shown — nothing is loading
            // behind it yet.
            ShowMainMenu();
        }

        // ---------- MAIN MENU ----------

        public void ShowMainMenu()
        {
            mainMenuScreen.SetActive(true);
            gameplayHUD.SetActive(false);
            gameOverScreen.SetActive(false);

            if (mainMenuHighScoreText != null)
                mainMenuHighScoreText.Text = "Best: " + PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        /// <summary>Wire this to the Main Menu's Start button OnClick().</summary>
        public void OnStartButtonPressed()
        {
            mainMenuScreen.SetActive(false);
            gameplayHUD.SetActive(true);
            gameOverScreen.SetActive(false);

            // THIS is the actual trigger for gameplay to begin loading —
            // nothing happens on GameManager's side until this fires.
            gameManager.BeginNewRun();
        }

        /// <summary>Wire this to the Main Menu's Exit button OnClick().</summary>
        public void OnExitButtonPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // lets Exit work when testing inside the Editor too
#endif
        }

        // ---------- GAMEPLAY HUD ----------

        /// <summary>
        /// Fires every time GameManager's currentLives changes (including the initial
        /// broadcast at the start of a run). newLives is the number of lives REMAINING.
        /// </summary>
        private void HandleLivesChanged(int newLives)
        {
            for (int i = 0; i < heartDimmedOverlays.Length; i++)
            {
                if (heartDimmedOverlays[i] == null) continue;
                heartDimmedOverlays[i].gameObject.SetActive(i >= newLives);
            }
        }

        /// <summary>
        /// Fires every time GameManager broadcasts a new round number.
        /// </summary>
        private void HandleRoundStarted(int roundNumber)
        {
            if (roundNumberText != null)
                roundNumberText.Text = "Round " + roundNumber;
        }

        // ---------- GAME OVER ----------

        private void HandleGameOver(int finalRound)
        {
            gameplayHUD.SetActive(false);
            gameOverScreen.SetActive(true);

            if (finalRoundText != null)
                finalRoundText.Text = "Round Reached: " + finalRound;

            int previousHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            if (finalRound > previousHighScore)
            {
                PlayerPrefs.SetInt(HighScoreKey, finalRound);
                PlayerPrefs.Save();
            }

            if (highScoreText != null)
                highScoreText.Text = "Best: " + PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        /// <summary>Wire this to the Game Over screen's Restart button OnClick().</summary>
        public void OnRestartButtonPressed()
        {
            gameOverScreen.SetActive(false);
            gameplayHUD.SetActive(true);
            gameManager.RestartGame(); // resets lives/round; high score is untouched since it lives in PlayerPrefs, not GameManager
        }

        /// <summary>Wire this to the Game Over screen's Main Menu button OnClick().</summary>
        public void OnGameOverMainMenuPressed()
        {
            // Stop any in-flight run and return fully to the menu.
            // We don't call BeginNewRun() here — that only happens when
            // Start is pressed again from the Main Menu.
            gameManager.RestartGame();
            ShowMainMenu();
        }

        /// <summary>Wire this to the Game Over screen's Exit button OnClick(), if you keep that option there.</summary>
        public void OnGameOverExitPressed()
        {
            OnExitButtonPressed();
        }
    }
}