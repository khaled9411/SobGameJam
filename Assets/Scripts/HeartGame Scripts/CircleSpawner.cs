using System.Collections;
using UnityEngine;


public class CircleSpawner : MonoBehaviour   
{
    public event System.Action OnPerfectTiming;
    public event System.Action OnBadTiming;
    public event System.Action OnMissed;

    [SerializeField] private GrowingCircle circlePrefab;
    [SerializeField] private Transform parent;
    [SerializeField] private Vector2 minBounds = new Vector2(-7f, -3.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(7f, 5f);

    public void StartSpawning()
    {
       StartCoroutine(SpawnRoutine());
    }
    public void StopSpawning()
    {
        StopCoroutine(SpawnRoutine());
    }
    IEnumerator SpawnRoutine()
    {
        while(true)
        {
            Vector2 spawnPosition = new Vector2(
            Random.Range(minBounds.x, maxBounds.x),
            Random.Range(minBounds.y, maxBounds.y));


            GrowingCircle circle =  Instantiate(circlePrefab, spawnPosition, Quaternion.identity, parent);
            circle.OnPerfectTiming += () => OnPerfectTiming?.Invoke();
            circle.OnBadTiming += () => OnBadTiming?.Invoke();
            circle.OnMissed += () => OnMissed?.Invoke();

            while (circle != null)
            {
              yield return null;
            }
        }
        
    }
   
}
