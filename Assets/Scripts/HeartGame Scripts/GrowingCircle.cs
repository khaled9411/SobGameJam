using UnityEngine;

public class GrowingCircle : MonoBehaviour
{
    [SerializeField] private float startingScale = 0f;
    [SerializeField] private float growSpeed = 1.1f;
    [SerializeField] float maxSize = 1.5f;

    private void Awake()
    {
        transform.localScale = Vector3.one * startingScale;
    }

    private void Update()
    {
        transform.localScale += Vector3.one * growSpeed * Time.deltaTime;  //increase circle size

         if(transform.localScale.x >= maxSize)
        {
            Destroy(gameObject);
        }
    }
}
