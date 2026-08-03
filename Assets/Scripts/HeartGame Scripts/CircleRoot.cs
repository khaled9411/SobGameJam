using UnityEngine;

public class CircleRoot : MonoBehaviour
{
    [SerializeField] private GrowingCircle growingCircle;

    public GrowingCircle GrowingCircle => growingCircle;
}