using System;
using UnityEngine;

public class StaticCircle : MonoBehaviour
{
    public event Action OnClicked;
    [SerializeField] private ParticleSystem perfectClickEffect;

    private void OnMouseDown()
    {
        OnClicked?.Invoke();
        Instantiate(perfectClickEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
