using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public static class PlayerPrefsExporter
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

    public static void ExportAllPrefsAsJson()
    {
        var keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        var exportList = new List<PlayerPrefEntry>();
        foreach (var k in allKeys)
        {
            if (!PlayerPrefs.HasKey(k)) continue;
            var entry = new PlayerPrefEntry();
            entry.key = k;
            int i = PlayerPrefs.GetInt(k, int.MinValue);
            float f = PlayerPrefs.GetFloat(k, float.MinValue);
            string s = PlayerPrefs.GetString(k, "__NULL__");
            if (i != int.MinValue) { entry.type = "int"; entry.value = i.ToString(); }
            else if (f != float.MinValue) { entry.type = "float"; entry.value = f.ToString(); }
            else if (s != "__NULL__") { entry.type = "string"; entry.value = s; }
            else { entry.type = "unknown"; entry.value = ""; }
            exportList.Add(entry);
        }
        string json = JsonUtility.ToJson(new PlayerPrefsExportWrapper { prefs = exportList }, true);
        string path = EditorUtility.SaveFilePanel("Export PlayerPrefs as JSON", Application.dataPath, "PlayerPrefsExport.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            EditorUtility.RevealInFinder(path);
        }
    }

    // Utility to get mapping from user key to registry key (Editor only)
    private static Dictionary<string, string> GetUserKeyToRegistryKeyMap()
    {
#if UNITY_EDITOR_WIN
        string regKey = @"Software\\Unity\\UnityEditor\\" + Application.companyName + "\\" + Application.productName;
        var map = new Dictionary<string, string>();
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKey))
            {
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        int hashIdx = valueName.LastIndexOf("_h");
                        if (hashIdx > 0)
                        {
                            string userKey = valueName.Substring(0, hashIdx);
                            if (!map.ContainsKey(userKey))
                                map.Add(userKey, valueName);
                        }
                        else
                        {
                            if (!map.ContainsKey(valueName))
                                map.Add(valueName, valueName);
                        }
                    }
                }
            }
        }
        catch { }
        return map;
#else
        return new Dictionary<string, string>();
#endif
    }
    }
}
