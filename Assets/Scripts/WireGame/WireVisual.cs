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
        { WireColor.White, Color.white },
    };

    public void SetColor(WireColor color)
    {
        CurrentColor = color;
        wireRenderer.color = ColorMap[color];
    }

    private void OnMouseDown()
    {
        FindObjectOfType<WireCutController>().OnWireCut(CurrentColor);
    }
}