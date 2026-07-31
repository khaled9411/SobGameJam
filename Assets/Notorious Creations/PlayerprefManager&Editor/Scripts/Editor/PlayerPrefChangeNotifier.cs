using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NotoriousCreations.PlayerPrefsEditor
{
    [System.Serializable]
    public class PlayerPrefNotificationData
    {
    public string key;
    public string lastValue;
    public bool isTracked;
}

public static class PlayerPrefChangeNotifier
{
    private static Dictionary<string, PlayerPrefNotificationData> trackedPrefs = new Dictionary<string, PlayerPrefNotificationData>();
    private static EditorWindow targetWindow;
    private static double lastCheckTime;
    private static bool isInitialized = false;
    private static readonly double CHECK_INTERVAL = 0.5; // Check every 0.5 seconds
    
    // EditorPrefs key for persistence
    private const string TRACKED_KEYS_PREF = "PlayerPrefChangeNotifier_TrackedKeys";

    static PlayerPrefChangeNotifier()
    {
        // Static constructor - initialization will be done via Initialize() method
    }

    public static void Initialize()
    {
        if (!isInitialized)
        {
            // Restore tracked keys from EditorPrefs
            RestoreTrackedKeys();
            
            EditorApplication.update += CheckForChanges;
            isInitialized = true;
        }
    }
    
    private static void RestoreTrackedKeys()
    {
        string savedKeys = EditorPrefs.GetString(TRACKED_KEYS_PREF, "");
        if (!string.IsNullOrEmpty(savedKeys))
        {
            string[] keyArray = savedKeys.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string key in keyArray)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    // Re-track this key
                    if (!trackedPrefs.ContainsKey(key))
                    {
                        trackedPrefs[key] = new PlayerPrefNotificationData();
                    }
                    trackedPrefs[key].key = key;
                    trackedPrefs[key].isTracked = true;
                    trackedPrefs[key].lastValue = GetPlayerPrefValue(key);
                    
                }
            }
        }
    }
    
    private static void SaveTrackedKeys()
    {
        var trackedKeysList = new List<string>();
        foreach (var kvp in trackedPrefs)
        {
            if (kvp.Value.isTracked)
            {
                trackedKeysList.Add(kvp.Key);
            }
        }
        string savedKeys = string.Join("|", trackedKeysList);
        EditorPrefs.SetString(TRACKED_KEYS_PREF, savedKeys);
        
    }

    public static void Cleanup()
    {
        if (isInitialized)
        {
            EditorApplication.update -= CheckForChanges;
            // Save tracking state before cleanup, but don't clear it
            SaveTrackedKeys();
            isInitialized = false;
        }
    }

    public static void SetTargetWindow(EditorWindow window)
    {
        targetWindow = window;
    }

    public static void TrackPlayerPref(string key)
    {
        Debug.Log($"[PlayerPrefChangeNotifier] TrackPlayerPref called for key: '{key}'");
        if (!trackedPrefs.ContainsKey(key))
        {
            trackedPrefs[key] = new PlayerPrefNotificationData();
        }
        
        trackedPrefs[key].key = key;
        trackedPrefs[key].isTracked = true;
        trackedPrefs[key].lastValue = GetPlayerPrefValue(key);
        
        // Save the updated tracking state
        SaveTrackedKeys();
        Debug.Log($"[PlayerPrefChangeNotifier] Now tracking '{key}' with initial value: '{trackedPrefs[key].lastValue}'");
    }

    public static void UntrackPlayerPref(string key)
    {
        Debug.Log($"[PlayerPrefChangeNotifier] UntrackPlayerPref called for key: '{key}'");
        if (trackedPrefs.ContainsKey(key))
        {
            trackedPrefs[key].isTracked = false;
        }
        
        // Save the updated tracking state
        SaveTrackedKeys();
        Debug.Log($"[PlayerPrefChangeNotifier] Stopped tracking '{key}'");
    }

    public static bool IsTracked(string key)
    {
        return trackedPrefs.ContainsKey(key) && trackedPrefs[key].isTracked;
    }
    
    public static List<string> GetTrackedKeys()
    {
        var trackedKeysList = new List<string>();
        foreach (var kvp in trackedPrefs)
        {
            if (kvp.Value.isTracked)
            {
                trackedKeysList.Add(kvp.Key);
            }
        }
        return trackedKeysList;
    }

    private static void CheckForChanges()
    {
        if (EditorApplication.timeSinceStartup - lastCheckTime < CHECK_INTERVAL)
            return;

        lastCheckTime = EditorApplication.timeSinceStartup;

        var keysToRemove = new List<string>();
        
        foreach (var kvp in trackedPrefs)
        {
            if (!kvp.Value.isTracked) continue;
            
            string currentValue = GetPlayerPrefValue(kvp.Key);
            
            // Check if key still exists
            if (!PlayerPrefs.HasKey(kvp.Key))
            {
                if (!string.IsNullOrEmpty(kvp.Value.lastValue))
                {
                    ShowNotification(kvp.Key, "DELETED", kvp.Value.lastValue);
                    keysToRemove.Add(kvp.Key);
                }
                continue;
            }
            
            // Check if value changed
            if (kvp.Value.lastValue != currentValue)
            {
                
                ShowNotification(kvp.Key, currentValue, kvp.Value.lastValue);
                kvp.Value.lastValue = currentValue;
            }
        }
        
        // Remove deleted keys
        foreach (string key in keysToRemove)
        {
            trackedPrefs.Remove(key);
        }
    }

    private static string GetPlayerPrefValue(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return "";
            
        // Try different types to get the actual value
        try
        {
            // Try as int first
            int intVal = PlayerPrefs.GetInt(key, int.MinValue);
            if (intVal != int.MinValue)
                return intVal.ToString();
                
            // Try as float
            float floatVal = PlayerPrefs.GetFloat(key, float.MinValue);
            if (Math.Abs(floatVal - float.MinValue) > 0.0001f)
                return floatVal.ToString("F6");
                
            // Default to string
            return PlayerPrefs.GetString(key, "");
        }
        catch
        {
            return PlayerPrefs.GetString(key, "");
        }
    }

    private static void ShowNotification(string key, string newValue, string oldValue = "")
    {
        string message;
        if (newValue == "DELETED")
        {
            message = $"PlayerPref '{key}' was deleted\nPrevious value: {oldValue}";
        }
        else
        {
            message = string.IsNullOrEmpty(oldValue) 
                ? $"PlayerPref '{key}' changed to: {newValue}"
                : $"PlayerPref '{key}' changed\nFrom: {oldValue}\nTo: {newValue}";
        }

        // Show custom slider notification from top-right corner
        ShowSliderNotification(key, newValue, oldValue);
        
       
    }

    private static void ShowSliderNotification(string key, string newValue, string oldValue = "")
    {
        
        
        // Create a custom notification window that slides from top-right
        var notificationWindow = ScriptableObject.CreateInstance<PlayerPrefNotificationWindow>();
        notificationWindow.titleContent = new GUIContent("PlayerPref Changed");
        
        // Calculate position for top-right corner based on screen size
        Vector2 size = new Vector2(300, 80);
        Vector2 position;
        
        // Position relative to the actual screen's top-right corner
        position = new Vector2(Screen.currentResolution.width - size.x - 20, 20);
        
        notificationWindow.position = new Rect(position, size);
        notificationWindow.SetNotificationData(key, newValue, oldValue);
        notificationWindow.ShowPopup();
        
        // Auto-hide after 3 seconds
        EditorApplication.delayCall += () => {
            var delayTime = 3.0f;
            var startTime = EditorApplication.timeSinceStartup;
            
            UnityEditor.EditorApplication.CallbackFunction checkClose = null;
            checkClose = () => {
                if (EditorApplication.timeSinceStartup - startTime >= delayTime)
                {
                    if (notificationWindow != null)
                        notificationWindow.Close();
                    EditorApplication.update -= checkClose;
                }
            };
            
            EditorApplication.update += checkClose;
        };
    }

    public static void ClearAllTracking()
    {
        trackedPrefs.Clear();
    }
    }
}
