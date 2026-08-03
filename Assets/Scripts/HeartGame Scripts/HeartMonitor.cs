using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HeartMonitor : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField, Tooltip("Number of points to draw. Higher means a smoother line but costs more performance.")]
    private int resolution = 200;

    [SerializeField, Tooltip("Total physical width of the monitor line in local space.")]
    private float width = 10f;

    [SerializeField, Tooltip("How fast the monitor scrolls to the left.")]
    private float scrollSpeed = 3f;

    [Header("Heartbeat Settings")]
    [SerializeField, Tooltip("Time in seconds between the start of each heartbeat.")]
    private float beatInterval = 1.2f;

    [SerializeField, Tooltip("How long a single heartbeat wave lasts in seconds.")]
    private float beatDuration = 0.6f;

    [SerializeField, Tooltip("The shape of the heartbeat. X is time (0 to 1), Y is height.")]
    private AnimationCurve heartbeatCurve;

    [Header("Height Range")]
    [SerializeField, Tooltip("Minimum height multiplier for the heartbeat spike.")]
    private float minSpikeHeight = 0.8f;

    [SerializeField, Tooltip("Maximum height multiplier for the heartbeat spike.")]
    private float maxSpikeHeight = 1.5f;

    [Header("Analog Noise")]
    [SerializeField, Tooltip("Amount of random vertical jitter added to the line.")]
    private float noiseAmplitude = 0.05f;

    [SerializeField, Tooltip("How fast the noise fluctuates.")]
    private float noiseFrequency = 20f;

    private LineRenderer line;
    private Vector3[] points;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false; // Ensures the line moves with the GameObject
        line.positionCount = resolution;

        // Pre-allocate the array to avoid garbage generation in Update
        points = new Vector3[resolution];
    }

    private void Update()
    {
        float spacing = width / (resolution - 1);
        float currentRealTime = Time.time * scrollSpeed;

        for (int i = 0; i < resolution; i++)
        {
            // X position goes from 0 (left) to width (right)
            float xPos = i * spacing;

            // Calculate what "time" this point represents. 
            // The rightmost point is the present time. Points to the left are in the past.
            float timeOffset = width - xPos;
            float sampleTime = currentRealTime - timeOffset;

            float yPos = EvaluateECG(sampleTime);

            points[i] = new Vector3(xPos, yPos, 0f);
        }

        // Apply to line renderer
        line.SetPositions(points);
    }

    private float EvaluateECG(float t)
    {
        if (t < 0) t = 0;

        float cycleTime = t % beatInterval;
        int cycleIndex = Mathf.FloorToInt(t / beatInterval);

        // Get a deterministic random height for this specific heartbeat cycle using a simple hash.
        // This avoids calling Unity's Random.Range inside a loop, which is much faster.
        float currentHeight = GetRandomHeightForCycle(cycleIndex);

        float y = 0f;

        // If we are currently inside a heartbeat duration, evaluate the curve
        if (cycleTime < beatDuration)
        {
            float normalizedTime = cycleTime / beatDuration;
            y = heartbeatCurve.Evaluate(normalizedTime) * currentHeight;
        }

        // Add slight perlin noise for a realistic analog monitor look
        y += (Mathf.PerlinNoise(t * noiseFrequency, 0f) - 0.5f) * noiseAmplitude;

        return y;
    }

    private float GetRandomHeightForCycle(int cycleIndex)
    {
        // Simple fast integer hash to generate a pseudo-random value between 0.0 and 1.0
        uint hash = (uint)cycleIndex;
        hash ^= hash << 13;
        hash ^= hash >> 17;
        hash ^= hash << 5;

        float random01 = (hash % 10000) / 10000f;

        return Mathf.Lerp(minSpikeHeight, maxSpikeHeight, random01);
    }

    // Automatically set up a realistic P-QRS-T wave when the script is added
    private void Reset()
    {
        heartbeatCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.00f),
            new Keyframe(0.15f, 0.10f), // P wave
            new Keyframe(0.25f, 0.00f),
            new Keyframe(0.35f, -0.1f), // Q wave
            new Keyframe(0.40f, 1.00f), // R spike
            new Keyframe(0.45f, -0.3f), // S wave
            new Keyframe(0.55f, 0.00f),
            new Keyframe(0.70f, 0.15f), // T wave
            new Keyframe(1.00f, 0.00f)
        );

        // Ensure tangents look sharp at the spikes and smooth at the bumps
        for (int i = 0; i < heartbeatCurve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(heartbeatCurve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(heartbeatCurve, i, AnimationUtility.TangentMode.Auto);
        }
    }
}