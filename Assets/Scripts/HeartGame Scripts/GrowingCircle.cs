using System;
using UnityEngine;

public class GrowingCircle : MonoBehaviour
{
    public event Action OnPerfectTiming;
    public event Action OnBadTiming;
    public event Action OnMissed;
    public event Action OnReachedPerfectZone;



    [SerializeField] private float startingScale = 0f;
    [SerializeField] private float growSpeed;
    [SerializeField] float maxSize = 1.5f;
    [SerializeField] float minPerfectSize = 1f;
    [SerializeField] float maxPerfectSize = 1.35f;
    [SerializeField] private Color perfectColor = Color.yellow;


    private float currentScale;
    private bool resolved;
    private bool enteredPerfectZone;
 

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentScale = startingScale;
        transform.localScale = Vector3.one * startingScale;
    }

    private void Update()
    {
        if (!enteredPerfectZone && IsPerfectSize())
        {
            enteredPerfectZone = true;
            OnReachedPerfectZone?.Invoke();
        }

        Grow(); //Grow circle

        if (!resolved && currentScale >= maxSize)
        {
            resolved = true;
            OnMissed?.Invoke();
            Destroy(gameObject);
        }

        UpdateColour(); //change colour at perfect size


    }

    private void OnMouseDown()
    {
        if (resolved)
            return;

        resolved = true;

        if (IsPerfectSize())
            OnPerfectTiming?.Invoke();
        else
            OnBadTiming?.Invoke();

        Destroy(gameObject);
    }

    private void Grow()
    {
        currentScale += growSpeed * Time.deltaTime;   //current scale calculation

        transform.localScale = Vector3.one * currentScale;  //increase circle size
    }
    private void UpdateColour()
    {
        if (IsPerfectSize()) //change colour on perfect size
        {
            spriteRenderer.color = perfectColor;
           
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }
    private bool IsPerfectSize()
    {
        return minPerfectSize <= currentScale && currentScale <= maxPerfectSize;
    }

    public void SetGrowSpeed(float speed)
    {
        growSpeed = speed;
    }

}
