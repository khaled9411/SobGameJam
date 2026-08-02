using UnityEngine;
using SobGameJam.MiniGames.WireCut;

public class WireVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer wireRenderer;
    public WireColor CurrentColor { get; private set; }

    private static readonly System.Collections.Generic.Dictionary<WireColor, Color> ColorMap = new()
    {
        { WireColor.Red, Color.red },
        { WireColor.Blue, Color.blue },
        { WireColor.Yellow, Color.yellow },
        { WireColor.Green, Color.green },
    };

    public void SetColor(WireColor color)
    {
        CurrentColor = color;
        wireRenderer.color = ColorMap[color];
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button down detected, at least Input is registering.");
        }
    }
    private void OnMouseDown()
    {
        Debug.Log($"[WireVisual] OnMouseDown fired on {gameObject.name}");
        FindObjectOfType<WireCutController>().OnWireCut(CurrentColor);
    }
}