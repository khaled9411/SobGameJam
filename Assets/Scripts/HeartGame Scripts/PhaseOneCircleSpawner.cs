using System.Collections;
using UnityEngine;


public class PhaseOneCircleSpawner : MonoBehaviour   
{
    public event System.Action OnPerfectTiming;
    public event System.Action OnBadTiming;
    public event System.Action OnMissed;
    public event System.Action OnOutOfCircles;
    [SerializeField] private CircleRoot circlePrefab;
    [SerializeField] private Transform parent;
    [SerializeField] private Vector2 minBounds = new Vector2(-6.77f, -1.61f);
    [SerializeField] private Vector2 maxBounds = new Vector2(7.43f, 4.63f);
    [SerializeField] private float baseGrowSpeed = 1.1f;
    [SerializeField] private float maxGrowSpeed = 2.5f;

    private float currentGrowSpeed;
    private int circlesSpawned;
    private int maxCircles = 30;


    public void StartSpawning()
    {
       StartCoroutine(SpawnRoutine());
    }
    public void StopSpawning()
    {
        StopAllCoroutines();
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
    IEnumerator SpawnRoutine()
    {
        while(true)
        {
            Vector2 spawnPosition = GetRandomPosition();


            CircleRoot root = Instantiate(circlePrefab, spawnPosition, Quaternion.identity, parent);

            GrowingCircle circle = root.GrowingCircle;

            circle.SetGrowSpeed(currentGrowSpeed);

            circlesSpawned++;
            if (circlesSpawned >= maxCircles)
            {
                OnOutOfCircles?.Invoke();
                yield break;
            }
            circle.OnPerfectTiming += () =>
            {
                Debug.Log("Spawner forwarding perfect");
                OnPerfectTiming?.Invoke();
            };
            circle.OnBadTiming += () => OnBadTiming?.Invoke();
            circle.OnMissed += () => OnMissed?.Invoke();

            while (root != null)
            {
                yield return null;
            }
        }
        
    }
    private Vector2 GetRandomPosition()
    {
        const float minDistance = 1.5f;

        while (true)
        {
            Vector2 position = new Vector2(
                Random.Range(minBounds.x, maxBounds.x),
                Random.Range(minBounds.y, maxBounds.y));

            bool overlaps = false;

            foreach (Transform child in parent)
            {
                if (Vector2.Distance(position, child.position) < minDistance)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                return position;
        }
    }
    public void SetDifficulty(float difficulty)
    {
        currentGrowSpeed = Mathf.Lerp(baseGrowSpeed, maxGrowSpeed, difficulty);
    }

}
