using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DifficultyLevel
{
    public string levelName;
    public int numberOfLetters;
    public float shrinkDuration;
}

public class NuclearGameManager : MonoBehaviour
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
    private bool isPlaying = false;

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

    private void Start()
    {
        StartWave();
    }

    public void StartWave()
    {
        DifficultyLevel currentLevel = difficultyLevels[currentDifficultyIndex];
        activeNodes.Clear();
        isPlaying = true;

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
        }
    }

    private void Update()
    {
        if (!isPlaying || !Input.anyKeyDown) return;

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
                    LoseGame();
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

        if (activeNodes.Count == 0 && isPlaying)
        {
            WinGame();
        }
    }

    public void WinGame()
    {

    }

    public void LoseGame()
    {

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