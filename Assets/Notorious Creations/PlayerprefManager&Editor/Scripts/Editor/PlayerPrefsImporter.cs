using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public static class PlayerPrefsImporter
    {
    [Serializable]
    private class PlayerPrefEntry
    {
        public string key;
        public string type;
        public string value;
    }

    [Serializable]
    private class PlayerPrefsExportWrapper
    {
        public List<PlayerPrefEntry> prefs;
    }

    public static void ImportAllPrefsFromJson()
    {
        string path = EditorUtility.OpenFilePanel("Import PlayerPrefs from JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No file selected for import.");
            return;
        }
        string json = System.IO.File.ReadAllText(path);
        PlayerPrefsExportWrapper wrapper = null;
        try
        {
            wrapper = JsonUtility.FromJson<PlayerPrefsExportWrapper>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex.Message);
            return;
        }
        if (wrapper == null || wrapper.prefs == null)
        {
            Debug.LogError("JSON file is not in the correct format (missing 'prefs' array).");
            return;
        }
        int imported = 0;
        foreach (var entry in wrapper.prefs)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.type))
            {
                Debug.LogWarning($"Skipping invalid entry: {JsonUtility.ToJson(entry)}");
                continue;
            }
            try
            {
                switch (entry.type)
                {
                    case "int":
                        if (int.TryParse(entry.value, out int i))
                        {
                            PlayerPrefs.SetInt(entry.key, i);
                            imported++;
                        }
                        else
                        {
                            Debug.LogWarning($"Invalid int value for key '{entry.key}': {entry.value}");
                        }
                        break;
                    case "float":
                        if (float.TryParse(entry.value, out float f))
                        {
                            PlayerPrefs.SetFloat(entry.key, f);
                            imported++;
                        }
                        else
                        {
                            Debug.LogWarning($"Invalid float value for key '{entry.key}': {entry.value}");
                        }
                        break;
                    case "string":
                        PlayerPrefs.SetString(entry.key, entry.value ?? "");
                        imported++;
                        break;
                    default:
                        Debug.LogWarning($"Unknown type for key '{entry.key}': {entry.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error importing key '{entry.key}': {ex.Message}");
            }
        }
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Import Complete", $"Imported {imported} PlayerPrefs from JSON.", "OK");
    }
    }
}
