using System.Collections;
using UnityEngine;

public class PhaseTwoCircleSpawner : MonoBehaviour
{
    public event System.Action OnClickedCircle;

    [SerializeField] StaticCircle circle;
    [SerializeField] private Transform parent;

    private int secondsBetweenWaves = 3;
    private int maxCircleAmount = 8;

    [SerializeField] private float emptyGap = 1f;
    [SerializeField] private Vector2 minBounds = new Vector2(-6.77f, -1.61f);
    [SerializeField] private Vector2 maxBounds = new Vector2(7.43f, 4.63f);
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
        while (true)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }

            yield return new WaitForSeconds(emptyGap);

            for (int i = 0; i < maxCircleAmount; i++)
            {
                Vector2 spawnPosition = GetRandomPosition();
                StaticCircle spawnedCircle = Instantiate(circle, spawnPosition, Quaternion.identity, parent);
                spawnedCircle.OnClicked += () => OnClickedCircle?.Invoke();
            }

            yield return new WaitForSeconds(secondsBetweenWaves);
        }
    }
    public Vector2 GetRandomPosition()
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
}
