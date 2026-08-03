using System;
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
        public AnimationCurve difficultyCurve;
        public int currentDifficultyIndex = 0;

        [Header("Spawning Settings")]
        public GameObject nodePrefab;
        public Vector2 spawnAreaMin;
        public Vector2 spawnAreaMax;

        [Header("Visual Feedback - Reactor Setup")]
        [Tooltip("The 3 non-interactive handles used to visualize player accuracy.")]
        [SerializeField] private Transform[] reactorHandles = new Transform[3];
        [Tooltip("The central reactor symbol to punch/shake on failure.")]
        [SerializeField] private Transform reactorSymbol;

        [Header("Visual Feedback - Catastrophic Failure")]
        [SerializeField] private SpriteRenderer screenFlashRenderer;
        [SerializeField] private ParticleSystem explosionParticles;
        [SerializeField] private Transform cameraTransform;

        [Header("DOTween - Feel Settings")]
        [SerializeField] private float handleMinX = 1.5f;
        [SerializeField] private float handleMaxX = -1.5f;
        [SerializeField] private float idleMovementStrength = 0.02f;
        [SerializeField] private float idleDuration = 1.5f;

        [Header("DOTween - Accuracy Thresholds")]
        [SerializeField, Range(0, 1)] private float perfectThreshold = 0.98f;
        [SerializeField, Range(0, 1)] private float greatThreshold = 0.80f;
        [SerializeField, Range(0, 1)] private float goodThreshold = 0.40f;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;

        [SerializeField] private AudioClip waveStartSound;
        [SerializeField] private AudioClip nodeSpawnSound;
        [SerializeField] private AudioClip perfectHitSound;
        [SerializeField] private AudioClip greatHitSound;
        [SerializeField] private AudioClip goodHitSound;
        [SerializeField] private AudioClip badHitSound;
        [SerializeField] private AudioClip explosionSound;

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
            if (difficultyLevels == null || difficultyLevels.Length == 0 || difficultyCurve.length == 0)
            {
                Debug.LogWarning("Difficulty Levels or Animation Curve is not set up correctly!");
                return;
            }

            float maxCurveTime = difficultyCurve.keys[difficultyCurve.length - 1].time;

            float evaluatedTime = Mathf.PingPong(roundNumber, maxCurveTime);

            float curveValue = difficultyCurve.Evaluate(evaluatedTime);

            currentDifficultyIndex = Mathf.Clamp(Mathf.RoundToInt(curveValue), 0, difficultyLevels.Length - 1);

            // Reset visual state before the game starts
            ResetHandles();
            StartWave();
        }

        public void StartWave()
        {
            DifficultyLevel currentLevel = difficultyLevels[currentDifficultyIndex];
            activeNodes.Clear();

            PlaySound(waveStartSound);

            List<KeyCode> availableKeys = new List<KeyCode>(alphabetKeys);

            for (int i = 0; i < currentLevel.numberOfLetters; i++)
            {
                Vector2 randomPos = new Vector2(
                    UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y)
                );

                int randomKeyIndex = UnityEngine.Random.Range(0, availableKeys.Count);
                KeyCode chosenKey = availableKeys[randomKeyIndex];
                availableKeys.RemoveAt(randomKeyIndex);

                GameObject newNodeObj = Instantiate(nodePrefab, randomPos, Quaternion.identity);
                NuclearNode nodeScript = newNodeObj.GetComponent<NuclearNode>();

                nodeScript.Setup(chosenKey, currentLevel.shrinkDuration);

                // Assign the specific handle index in the exact order nodes are spawned (0, 1, or 2)
                nodeScript.assignedHandleIndex = i;

                activeNodes.Add(nodeScript);
                allNodes.Add(nodeScript);

                PlaySound(nodeSpawnSound);
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


        public void AnimateHandle(int handleIndex, float accuracy)
        {
            if (handleIndex < 0 || handleIndex >= reactorHandles.Length) return;

            // Kill active tweens on this specific handle immediately
            reactorHandles[handleIndex].DOKill();

            float targetX = Mathf.Lerp(handleMinX, handleMaxX, accuracy);

            // Play exact aesthetic response based on accuracy tiers
            if (accuracy >= perfectThreshold)
                PlayPerfectHandleEffect(handleIndex, targetX);
            else if (accuracy >= greatThreshold)
                PlayGreatHandleEffect(handleIndex, targetX);
            else if (accuracy >= goodThreshold)
                PlayGoodHandleEffect(handleIndex, targetX);
            else
                PlayBadHandleEffect(handleIndex, targetX);
        }

        public void PlayPerfectHandleEffect(int handleIndex, float targetX)
        {
            PlaySound(perfectHitSound);

            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            seq.Append(handle.DOLocalMoveX(targetX, 0.15f).SetEase(Ease.OutBack, 2.0f));
            seq.Append(handle.DOLocalMoveX(targetX + 0.02f, 0.05f).SetEase(Ease.Linear));
            seq.Append(handle.DOShakePosition(0.1f, new Vector3(0f, 0.04f, 0f), 30, 90, false, true));
            seq.Join(handle.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.15f, 1));

            seq.SetTarget(handle);
        }

        public void PlayGreatHandleEffect(int handleIndex, float targetX)
        {
            PlaySound(greatHitSound);

            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            seq.Append(handle.DOLocalMoveX(targetX, 0.25f).SetEase(Ease.OutBack, 1.2f));
            seq.Append(handle.DOLocalMoveX(targetX, 0.05f).SetEase(Ease.InOutSine));

            seq.SetTarget(handle);
        }

        public void PlayGoodHandleEffect(int handleIndex, float targetX)
        {
            PlaySound(goodHitSound);

            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            seq.Append(handle.DOLocalMoveX(targetX, 0.25f).SetEase(Ease.OutQuad));

            seq.SetTarget(handle);
        }

        public void PlayBadHandleEffect(int handleIndex, float targetX)
        {
            PlaySound(badHitSound);

            Transform handle = reactorHandles[handleIndex];
            Sequence seq = DOTween.Sequence();

            seq.Append(handle.DOLocalMoveX(targetX, 0.40f).SetEase(Ease.OutSine));
            seq.Append(handle.DOShakePosition(0.2f, new Vector3(0f, 0.03f, 0f), 15, 90, false, true));

            seq.SetTarget(handle);
        }

        public void PlayWrongKeyExplosion()
        {
            PlaySound(explosionSound);

            // End active input processing immediately
            activeNodes.Clear();
            base.isGameActive = false;

            StopIdleAnimation();
            foreach (var handle in reactorHandles) handle.DOKill();
            if (reactorSymbol != null) reactorSymbol.DOKill();
            if (cameraTransform != null) cameraTransform.DOKill();
            if (screenFlashRenderer != null) screenFlashRenderer.DOKill();

            Sequence seq = DOTween.Sequence();

            foreach (var handle in reactorHandles)
            {
                seq.Join(handle.DOShakePosition(0.5f, new Vector3(0.1f, 0.15f, 0f), 30, 90, false, true));
            }

            if (reactorSymbol != null)
            {
                seq.Join(reactorSymbol.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.5f, 15, 1));
                seq.Join(reactorSymbol.DOShakeRotation(0.5f, new Vector3(0, 0, 25f), 25, 90, false));
            }

            if (screenFlashRenderer != null)
            {
                screenFlashRenderer.color = Color.white;
                seq.Join(screenFlashRenderer.DOColor(Color.red, 0.05f));
                seq.Append(screenFlashRenderer.DOFade(0f, 0.4f).SetEase(Ease.InExpo));
            }

            if (cameraTransform != null)
            {
                seq.Join(cameraTransform.DOShakePosition(0.5f, 0.3f, 30, 90, false, true));
            }

            if (explosionParticles != null)
            {
                explosionParticles.Play();
            }

            seq.OnComplete(LoseMiniGame);
        }

        private void PlaySound(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void StartIdleAnimation()
        {
            StopIdleAnimation();

            for (int i = 0; i < reactorHandles.Length; i++)
            {
                float randomOffset = i * 0.3f;
                reactorHandles[i].DOLocalMoveX(handleMinX - idleMovementStrength, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(randomOffset)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetId("ReactorIdle");
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
                Vector3 localPos = reactorHandles[i].localPosition;
                localPos.x = handleMinX;
                reactorHandles[i].localPosition = localPos;
                reactorHandles[i].localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }
        }


        public void LoseMiniGame()
        {
            Debug.Log("Player lost the mini-game!");
            LoseGame();
        }

        public void WinMiniGame()
        {
            WinGame();
        }

        private void OnDestroy()
        {
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