using UnityEngine;
using SobGameJam.MiniGames.WireCut;

public class WireVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer wireRenderer;

    [Header("Click VFX")]
    [Tooltip("Assign the electricity ParticleSystem prefab from the Project window.")]
    [SerializeField] private ParticleSystem electricitySparkPrefab;
    [Tooltip("Optional offset point for where the spark spawns. Leave empty to spawn at this wire's position.")]
    [SerializeField] private Transform sparkSpawnPoint;

    public WireColor CurrentColor { get; private set; }

    private static readonly System.Collections.Generic.Dictionary<WireColor, Color> ColorMap = new()
    {
        { WireColor.Red, new Color32(0xFF, 0x37, 0x37, 0xFF) },
        { WireColor.Blue, new Color32(0x4D, 0xA7, 0xF3, 0xFF) },
        { WireColor.Yellow, new Color32(0xFF, 0xDE, 0x39, 0xFF) },
        { WireColor.Green, new Color32(0x92, 0xD2, 0x51, 0xFF) },
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

        PlaySparkEffect();

        FindObjectOfType<WireCutController>().OnWireCut(CurrentColor);
    }

    private void PlaySparkEffect()
    {
        if (electricitySparkPrefab == null) return;

        Vector3 spawnPos = sparkSpawnPoint != null ? sparkSpawnPoint.position : transform.position;
        ParticleSystem instance = Instantiate(electricitySparkPrefab, spawnPos, Quaternion.identity);
        instance.Play();
        Destroy(instance.gameObject, instance.main.duration + instance.main.startLifetime.constantMax);
    }
}