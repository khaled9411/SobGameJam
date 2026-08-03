using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Added DOTween namespace

namespace SobGameJam.MiniGames
{
    [System.Serializable]
    public class DifficultyLevel
    {
        public string levelName;
        public int numberOfLetters;
        public float shrinkDuration;
    }

    public class NuclearGameManager : MiniGameBase
    {
        public static NuclearGameManager Instance;

        [Header("Difficulty Settings")]
        public DifficultyLevel[] difficultyLevels;
        public int currentDifficultyIndex = 0;

        [Header("Spawning Settings")]
        public GameObject nodePrefab;
        public Vector2 spawnAreaMin;
        public Vector2 spawnAreaMax;

        [Header("Visual Feedback - Reactor Setup")]
        [Tooltip("The 3 non-interactive handles used to visualize player accuracy.")]
        [SerializeField] private Transform[] reactorHandles = new Transform[3];
        [Tooltip("Optional glow renderers for the Perfect hit effect.")]
        [SerializeField] private SpriteRenderer[] handleGlows = new SpriteRenderer[3];
        [Tooltip("The central reactor symbol to punch/shake on failure.")]
        [SerializeField] private Transform reactorSymbol;

        [Header("Visual Feedback - Catastrophic Failure")]
        [SerializeField] private SpriteRenderer screenFlashRenderer;
        [SerializeField] private ParticleSystem explosionParticles;
        [SerializeField] private Transform cameraTransform;

        [Header("DOTween - Feel Settings")]
        [SerializeField] private float handleMinY = -1.5f;
        [SerializeField] private float handleMaxY = 1.5f;
        [SerializeField] private float idleMovementStrength = 0.02f;
        [SerializeField] private float idleDuration = 1.5f;

        [Header("DOTween - Accuracy Thresholds")]
        [SerializeField, Range(0, 1)] private float perfectThreshold = 0.98f;
        [SerializeField, Range(0, 1)] private float greatThreshold = 0.80f;
        [SerializeField, Range(0, 1)] private float goodThreshold = 0.40f;

        private List<NuclearNode> activeNodes = new List<NuclearNode>();
        private List<NuclearNode> allNodes = new List<NuclearNode>();

        private KeyCode[] alphabetKeys = new KeyCode[]
        {
            KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N,
            KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
            KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        protected override void OnGameStarted(int roundNumber)
        {
            // Reset visual state before the game starts
            ResetHandles();
            StartWave();
        }

        public void StartWave()
        {
            DifficultyLevel currentLevel = difficultyLevels[currentDifficultyIndex];
            activeNodes.Clear();

            List<KeyCode> availableKeys = new List<KeyCode>(alphabetKeys);

            for (int i = 0; i < currentLevel.numberOfLetters; i++)
            {
                Vector2 randomPos = new Vector2(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y)
                );

                int randomKeyIndex = Random.Range(0, availableKeys.Count);
                KeyCode chosenKey = availableKeys[randomKeyIndex];
                availableKeys.RemoveAt(randomKeyIndex);

                GameObject newNodeObj = Instantiate(nodePrefab, randomPos, Quaternion.identity);
                NuclearNode nodeScript = newNodeObj.GetComponent<NuclearNode>();

                nodeScript.Setup(chosenKey, currentLevel.shrinkDuration);

                // Assign the specific handle index in the exact order nodes are spawned (0, 1, or 2)
                nodeScript.assignedHandleIndex = i;

                activeNodes.Add(nodeScript);
                allNodes.Add(nodeScript);
            }

            // Begin subtle reactor life
            StartIdleAnimation();
        }

        private void Update()
        {
            if (!base.isGameActive || !Input.anyKeyDown) return;

            foreach (KeyCode key in alphabetKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    NuclearNode targetNode = activeNodes.Find(n => n.targetKey == key);

                    if (targetNode != null)
                    {
                        targetNode.CheckHitTiming();
                    }
                    else
                    {
                        // Any wrong key pressed immediately causes catastrophic failure
                        PlayWrongKeyExplosion();
                    }
                }
            }
        }

        public void RemoveNode(NuclearNode node)
        {
            if (activeNodes.Contains(node))
            {
                activeNodes.Remove(node);
            }

            // Win condition: Player survives and clears all assigned nodes
            if (activeNodes.Count == 0 && base.isGameActive)
            {
                WinMiniGame();
            }
        }

        // ==================================================
        // VISUAL FEEDBACK SYSTEM (DOTWEEN)
        // ==================================================

        /// <summary>
        /// Translates the normalized accuracy (0 to 1) into physical handle height 
        /// and determines the visual juice required to sell the impact.
        /// </summary>
        public void AnimateHandle(int handleIndex, float accuracy)
        {
            if (handleIndex < 0 || handleIndex >= reactorHandles.Length) return;

            // Kill active tweens on this specific handle immediately
            reactorHandles[handleIndex].DOKill();
            if (handleGlows[handleIndex] != null) handleGlows[handleIndex].DOKill();

            // Calculate exact physical destination mapping (0..1 -> -1.5..1.5)
            float targetY = Mathf.Lerp(handleMinY, handleMaxY, accuracy);

            // Play exact aesthetic response based on accuracy tiers
            if (accuracy >= perfectThreshold)
                PlayPerfectHandleEffect(handleIndex, targetY);
            else if (accuracy >= greatThreshold)
                PlayGreatHandleEffect(handleIndex, targetY);
            else if (accuracy >= goodThreshold)
                PlayGoodHandleEffect(handleIndex, targetY);
            else
                PlayBadHandleEffect(handleIndex, targetY);
        }

        public void PlayPerfectHandleEffect(int handleIndex, float targetY)
        {
            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            // 1. Shoots up quickly with a sharp overshoot
            seq.Append(handle.DOLocalMoveY(targetY, 0.15f).SetEase(Ease.OutBack, 2.0f));
            // 2. Heavy industrial settle 
            seq.Append(handle.DOLocalMoveY(targetY - 0.02f, 0.05f).SetEase(Ease.Linear));
            // 3. Metallic vibration (very fast, tight X-axis shake)
            seq.Append(handle.DOShakePosition(0.1f, new Vector3(0.04f, 0, 0), 30, 90, false, true));
            // 4. Clack feeling via scale punch
            seq.Join(handle.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.15f, 1));

            // Optional Brief White Glow
            if (handleGlows[handleIndex] != null)
            {
                handleGlows[handleIndex].color = Color.white;
                seq.Join(handleGlows[handleIndex].DOFade(0f, 0.25f).SetEase(Ease.InQuad));
            }

            seq.SetTarget(handle);
        }

        public void PlayGreatHandleEffect(int handleIndex, float targetY)
        {
            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            // 1. Smooth, snappy move with tiny overshoot
            seq.Append(handle.DOLocalMoveY(targetY, 0.25f).SetEase(Ease.OutBack, 1.2f));
            // 2. Tiny mechanical settle, no vibration
            seq.Append(handle.DOLocalMoveY(targetY, 0.05f).SetEase(Ease.InOutSine));

            seq.SetTarget(handle);
        }

        public void PlayGoodHandleEffect(int handleIndex, float targetY)
        {
            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            // Just smooth movement. Not bouncy, just industrial shift.
            seq.Append(handle.DOLocalMoveY(targetY, 0.25f).SetEase(Ease.OutQuad));

            seq.SetTarget(handle);
        }

        public void PlayBadHandleEffect(int handleIndex, float targetY)
        {
            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            // 1. Move slower, feeling stuck or resistant
            seq.Append(handle.DOLocalMoveY(targetY, 0.40f).SetEase(Ease.OutSine));
            // 2. Weak, grinding shake feeling at the end
            seq.Append(handle.DOShakePosition(0.2f, new Vector3(0.03f, 0.01f, 0f), 15, 90, false, true));

            seq.SetTarget(handle);
        }

        public void PlayWrongKeyExplosion()
        {
            // End active input processing immediately
            activeNodes.Clear();
            base.isGameActive = false; // Prevent further keystrokes while the sequence plays

            // 1. Stop everything currently happening
            StopIdleAnimation();
            foreach (var handle in reactorHandles) handle.DOKill();
            if (reactorSymbol != null) reactorSymbol.DOKill();
            if (cameraTransform != null) cameraTransform.DOKill();
            if (screenFlashRenderer != null) screenFlashRenderer.DOKill();

            Sequence seq = DOTween.Sequence();

            // 2. Shake all THREE handles catastrophically
            foreach (var handle in reactorHandles)
            {
                seq.Join(handle.DOShakePosition(0.5f, new Vector3(0.1f, 0.15f, 0f), 30, 90, false, true));
            }

            // 3. Reactor symbol failure feedback
            if (reactorSymbol != null)
            {
                seq.Join(reactorSymbol.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.5f, 15, 1));
                seq.Join(reactorSymbol.DOShakeRotation(0.5f, new Vector3(0, 0, 25f), 25, 90, false));
            }

            // 4 & 5. Cinematic White to Red Flash
            if (screenFlashRenderer != null)
            {
                screenFlashRenderer.color = Color.white;
                seq.Join(screenFlashRenderer.DOColor(Color.red, 0.05f));
                seq.Append(screenFlashRenderer.DOFade(0f, 0.4f).SetEase(Ease.InExpo));
            }

            // 6. Camera Shake
            if (cameraTransform != null)
            {
                seq.Join(cameraTransform.DOShakePosition(0.5f, 0.3f, 30, 90, false, true));
            }

            // 7. Particles
            if (explosionParticles != null)
            {
                explosionParticles.Play();
            }

            // 8. Trigger real mechanical game over at the end of the visual sequence
            seq.OnComplete(LoseMiniGame);
        }

        public void StartIdleAnimation()
        {
            StopIdleAnimation(); // Ensure no duplicates

            for (int i = 0; i < reactorHandles.Length; i++)
            {
                // Unused handles should vibrate almost invisibly to feel "alive"
                // Desync them slightly using their index to make it feel organic
                float randomOffset = i * 0.3f;

                reactorHandles[i].DOLocalMoveY(handleMinY + idleMovementStrength, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(randomOffset)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetId("ReactorIdle"); // Tagged to easily kill later
            }
        }

        public void StopIdleAnimation()
        {
            DOTween.Kill("ReactorIdle");
        }

        public void ResetHandles()
        {
            StopIdleAnimation();

            for (int i = 0; i < reactorHandles.Length; i++)
            {
                reactorHandles[i].DOKill();
                // Snap back to minimum resting position
                Vector3 localPos = reactorHandles[i].localPosition;
                localPos.y = handleMinY;
                reactorHandles[i].localPosition = localPos;
                reactorHandles[i].localScale = Vector3.one;

                if (handleGlows[i] != null)
                {
                    handleGlows[i].DOKill();
                    Color c = handleGlows[i].color;
                    c.a = 0f;
                    handleGlows[i].color = c;
                }
            }
        }

        // ==================================================
        // STATE MANAGEMENT
        // ==================================================

        public void LoseMiniGame()
        {
            activeNodes.Clear();
            LoseGame();
        }

        public void WinMiniGame()
        {
            activeNodes.Clear();
            WinGame();
        }

        private void OnDestroy()
        {
            // Safety cleanup for DOTween so we don't bleed memory outside play mode
            foreach (var handle in reactorHandles)
            {
                if (handle != null) handle.DOKill();
            }
            if (reactorSymbol != null) reactorSymbol.DOKill();
            if (cameraTransform != null) cameraTransform.DOKill();
            if (screenFlashRenderer != null) screenFlashRenderer.DOKill();

            foreach (NuclearNode node in allNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            Vector3 center = new Vector3(
                (spawnAreaMin.x + spawnAreaMax.x) * 0.5f,
                (spawnAreaMin.y + spawnAreaMax.y) * 0.5f,
                0f);

            Vector3 size = new Vector3(
                Mathf.Abs(spawnAreaMax.x - spawnAreaMin.x),
                Mathf.Abs(spawnAreaMax.y - spawnAreaMin.y),
                0f);

            Gizmos.DrawWireCube(center, size);
        }
    }
}