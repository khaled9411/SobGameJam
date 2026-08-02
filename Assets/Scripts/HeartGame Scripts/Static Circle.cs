using System;
using UnityEngine;

public class StaticCircle : MonoBehaviour
{
    public event Action OnClicked;

    private void OnMouseDown()
    {
        OnClicked?.Invoke();
        Destroy(gameObject);
    }
}
