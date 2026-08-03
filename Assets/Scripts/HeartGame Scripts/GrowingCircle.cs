using System;
using UnityEngine;

public class GrowingCircle : MonoBehaviour
{
    public event Action OnPerfectTiming;
    public event Action OnBadTiming;
    public event Action OnMissed;
    



    [SerializeField] private float startingScale = 0f;
    [SerializeField] private float growSpeed;
    [SerializeField] float maxSize = 1.5f;
    [SerializeField] float minPerfectSize = 1.24f;
    [SerializeField] float maxPerfectSize = 1.5f;
    [SerializeField] private Color perfectColor;
    [SerializeField] private Color normalColor;


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

        Grow(); //Grow circle

        if (!resolved && currentScale >= maxSize)
        {
            resolved = true;
            OnMissed?.Invoke();
            Destroy(transform.parent.gameObject);
        }

        UpdateColour(); //change colour at perfect size


    }

    private void OnMouseDown()
    {
        if (resolved)
            return;

        resolved = true;

        if (IsPerfectSize())
        {
            OnPerfectTiming?.Invoke();
            Debug.Log("Perfect fired");

        }
        else
        {
            OnBadTiming?.Invoke();
            Debug.Log("Bad fired");
        }

        Destroy(transform.parent.gameObject);
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
            float t = Mathf.InverseLerp(minPerfectSize, maxPerfectSize, currentScale);
            spriteRenderer.color = Color.Lerp(normalColor, perfectColor, t);


        }
        else
        {
            spriteRenderer.color = normalColor;
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
