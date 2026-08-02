using System.Collections;
using UnityEngine;


public class PhaseOneCircleSpawner : MonoBehaviour   
{
    public event System.Action OnPerfectTiming;
    public event System.Action OnBadTiming;
    public event System.Action OnMissed;
    public event System.Action OnReachedPerfectZone;

    [SerializeField] private GrowingCircle circlePrefab;
    [SerializeField] private Transform parent;
    [SerializeField] private Vector2 minBounds = new Vector2(-7f, -3.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(7f, 5f);
    [SerializeField] private float baseGrowSpeed = 1.1f;
    [SerializeField] private float maxGrowSpeed = 2.5f;

    private float currentGrowSpeed;

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

            GrowingCircle circle =  Instantiate(circlePrefab, spawnPosition, Quaternion.identity, parent);
            circle.SetGrowSpeed(currentGrowSpeed);
            circle.OnPerfectTiming += () => OnPerfectTiming?.Invoke();
            circle.OnBadTiming += () => OnBadTiming?.Invoke();
            circle.OnMissed += () => OnMissed?.Invoke();
            circle.OnReachedPerfectZone += () => OnReachedPerfectZone?.Invoke();

            while (circle != null)
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
