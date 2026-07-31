using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NotoriousCreations.PlayerPrefsEditor
{
    /// <summary>
    /// Custom notification window that slides from top-right corner
    /// </summary>
    public class PlayerPrefNotificationWindow : EditorWindow
    {
    private string playerPrefKey; 
    private string newValue;
    private string oldValue;
    private float animationStartTime;
    private Vector2 originalPosition;
    private Vector2 targetPosition;
    private bool isAnimating = true;
    
    private const float ANIMATION_DURATION = 0.3f;
    private const float SLIDE_DISTANCE = 320f; // Distance to slide from off-screen

    public void SetNotificationData(string key, string newValue, string oldValue = "")
    {
        this.playerPrefKey = key;
        this.newValue = newValue;
        this.oldValue = oldValue;
        
        // Store the final position and calculate slide-in start position
        targetPosition = position.position;
        originalPosition = new Vector2(targetPosition.x + SLIDE_DISTANCE, targetPosition.y);
        
        // Start from off-screen position
        var rect = position;
        rect.position = originalPosition;
        position = rect;
        
        animationStartTime = (float)EditorApplication.timeSinceStartup;
        
        // Update the UI with the new data
        UpdateNotificationUI();
    }

    private void OnEnable()
    {
        // Create UI using UIElements
        CreateGUI();
        
        // Start animation update
        EditorApplication.update += UpdateAnimation;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateAnimation;
    }

    public void CreateGUI()
    {
        // Create root container
        var root = rootVisualElement;
        root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        root.style.borderTopColor = new Color(0.4f, 0.6f, 1f, 1f);
        root.style.borderTopWidth = 3;
        root.style.borderLeftWidth = 1;
        root.style.borderRightWidth = 1;
        root.style.borderBottomWidth = 1;
        root.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        root.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        root.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        root.style.paddingTop = 8;
        root.style.paddingBottom = 8;
        root.style.paddingLeft = 12;
        root.style.paddingRight = 12;

        // Title
        var titleLabel = new Label("PlayerPref Changed");
        titleLabel.name = "title";
        titleLabel.style.fontSize = 12;
        titleLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 4;
        root.Add(titleLabel);

        // Key name - will be updated later
        var keyLabel = new Label("Key: ");
        keyLabel.name = "keyLabel";
        keyLabel.style.fontSize = 11;
        keyLabel.style.color = new Color(0.8f, 0.9f, 1f, 1f);
        keyLabel.style.marginBottom = 2;
        root.Add(keyLabel);

        // Value change info - will be updated later
        var valueLabel = new Label("Value: ");
        valueLabel.name = "valueLabel";
        valueLabel.style.fontSize = 10;
        valueLabel.style.color = new Color(0.7f, 0.8f, 0.7f, 1f);
        valueLabel.style.whiteSpace = WhiteSpace.Normal;
        root.Add(valueLabel);

        // Close button (optional)
        var closeButton = new Button(() => Close()) { text = "×" };
        closeButton.style.position = Position.Absolute;
        closeButton.style.top = 2;
        closeButton.style.right = 2;
        closeButton.style.width = 16;
        closeButton.style.height = 16;
        closeButton.style.fontSize = 12;
        closeButton.style.backgroundColor = Color.clear;
        closeButton.style.borderTopWidth = 0;
        closeButton.style.borderBottomWidth = 0;
        closeButton.style.borderLeftWidth = 0;
        closeButton.style.borderRightWidth = 0;
        closeButton.style.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        root.Add(closeButton);
    }

    private void UpdateNotificationUI()
    {
        var root = rootVisualElement;
        var keyLabel = root.Q<Label>("keyLabel");
        var valueLabel = root.Q<Label>("valueLabel");
        
        
        
        if (keyLabel != null && valueLabel != null)
        {
            // Update key label
            keyLabel.text = $"Key: {playerPrefKey}";
            
            // Update value label
            string valueText;
            if (newValue == "DELETED")
            {
                valueText = $"DELETED (was: {oldValue})";
                keyLabel.style.color = new Color(1f, 0.6f, 0.6f, 1f);
            }
            else if (string.IsNullOrEmpty(oldValue))
            {
                valueText = $"New value: {newValue}";
            }
            else
            {
                valueText = $"Changed to: {newValue}";
            }
            
            valueLabel.text = valueText;
            
        }
        else
        {
            Debug.LogError("[PlayerPrefNotificationWindow] Could not find keyLabel or valueLabel elements!");
        }
    }

    private void UpdateAnimation()
    {
        if (!isAnimating) return;

        float elapsed = (float)EditorApplication.timeSinceStartup - animationStartTime;
        float progress = Mathf.Clamp01(elapsed / ANIMATION_DURATION);
        
        // Ease-out animation
        float easedProgress = 1f - (1f - progress) * (1f - progress);
        
        Vector2 currentPos = Vector2.Lerp(originalPosition, targetPosition, easedProgress);
        
        var rect = position;
        rect.position = currentPos;
        position = rect;
        
        if (progress >= 1f)
        {
            isAnimating = false;
        }
        
        Repaint();
    }
    }
}
