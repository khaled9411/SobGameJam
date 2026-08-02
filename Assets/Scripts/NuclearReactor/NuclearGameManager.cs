using System.Collections.Generic;
using UnityEngine;


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
                activeNodes.Add(nodeScript);
                allNodes.Add(nodeScript);
            }
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
                        LoseMiniGame();
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

            if (activeNodes.Count == 0 && base.isGameActive)
            {
                WinMiniGame();
            }
        }

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
            foreach (NuclearNode node in allNodes)
            {
                if (node != null)
                {
                    Destroy(node.gameObject);
                }
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