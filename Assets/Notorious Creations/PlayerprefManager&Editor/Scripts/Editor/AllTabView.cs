using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class AllTabView
    {
    private VisualElement root;
    private ListView leftPane;
    private Action<string> onSelectKey;
    private Action onRefresh; 
    public List<string> playerPrefKeys = new List<string>();

    public AllTabView(VisualElement parent, Action<string> onSelectKey, Action onRefresh)
    {
        root = parent;
        this.onSelectKey = onSelectKey;
        this.onRefresh = onRefresh;
        leftPane = new ListView();
        leftPane.style.flexGrow = 1;
        leftPane.style.height = StyleKeyword.Auto;
        root.Add(leftPane);
        leftPane.selectionType = SelectionType.Single;
        leftPane.selectionChanged += (selectedItems) => {
            if (leftPane.selectedIndex >= 0 && leftPane.selectedIndex < playerPrefKeys.Count)
                onSelectKey?.Invoke(playerPrefKeys[leftPane.selectedIndex]);
        };
    }

    public void Refresh(List<string> keys)
    {
        playerPrefKeys = keys;
        leftPane.itemsSource = playerPrefKeys;
        leftPane.fixedItemHeight = 32; // Match notifications tab height
        leftPane.makeItem = () => {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 28;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            // Key name
            var keyLabel = new Label();
            keyLabel.style.minWidth = 200;
            keyLabel.style.flexGrow = 1;
            keyLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            keyLabel.style.fontSize = 12;
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(keyLabel);
            
            // Type
            var typeLabel = new Label();
            typeLabel.style.minWidth = 60;
            typeLabel.style.marginLeft = 8;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            typeLabel.style.fontSize = 11;
            typeLabel.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            typeLabel.style.paddingLeft = 6;
            typeLabel.style.paddingRight = 6;
            typeLabel.style.paddingTop = 2;
            typeLabel.style.paddingBottom = 2;
            typeLabel.style.borderTopLeftRadius = 3;
            typeLabel.style.borderTopRightRadius = 3;
            typeLabel.style.borderBottomLeftRadius = 3;
            typeLabel.style.borderBottomRightRadius = 3;
            row.Add(typeLabel);
            
            // Current value
            var valueLabel = new Label();
            valueLabel.style.minWidth = 150;
            valueLabel.style.marginLeft = 8;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            valueLabel.style.fontSize = 11;
            valueLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            row.Add(valueLabel);
            
            return row;
        };
        leftPane.bindItem = (item, index) =>
        {
            var row = item as VisualElement;
            var keyLabel = row.ElementAt(0) as Label;
            var typeLabel = row.ElementAt(1) as Label;
            var valueLabel = row.ElementAt(2) as Label;
            
            var key = playerPrefKeys[index];
            
            // Alternate row colors
            if (index % 2 == 1) {
                row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0f);
            } else {
                row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);
            }
            
            // Set key name
            keyLabel.text = key;
            
            // Determine type and value
            string type = "";
            string value = "";
            
            if (PlayerPrefs.HasKey(key))
            {
                // Try to determine the type by attempting to get each type
                try
                {
                    int intValue = PlayerPrefs.GetInt(key, int.MinValue);
                    if (intValue != int.MinValue || PlayerPrefs.GetInt(key, int.MaxValue) != int.MaxValue)
                    {
                        type = "int";
                        value = PlayerPrefs.GetInt(key).ToString();
                    }
                }
                catch { }
                
                if (string.IsNullOrEmpty(type))
                {
                    try
                    {
                        float floatValue = PlayerPrefs.GetFloat(key, float.MinValue);
                        if (floatValue != float.MinValue || PlayerPrefs.GetFloat(key, float.MaxValue) != float.MaxValue)
                        {
                            type = "float";
                            value = PlayerPrefs.GetFloat(key).ToString("F3");
                        }
                    }
                    catch { }
                }
                
                if (string.IsNullOrEmpty(type))
                {
                    type = "string";
                    value = PlayerPrefs.GetString(key, "");
                }
            }
            else
            {
                type = "unknown";
                value = "N/A";
            }
            
            // Set type and color
            typeLabel.text = type;
            switch (type.ToLower())
            {
                case "int":
                    typeLabel.style.color = new Color(0.5f, 0.8f, 1f, 1f); // Light blue
                    break;
                case "float":
                    typeLabel.style.color = new Color(1f, 0.8f, 0.5f, 1f); // Light orange
                    break;
                case "string":
                    typeLabel.style.color = new Color(0.8f, 1f, 0.5f, 1f); // Light green
                    break;
                default:
                    typeLabel.style.color = Color.white;
                    break;
            }
            
            // Set value (truncate if too long)
            if (value.Length > 30)
            {
                valueLabel.text = value.Substring(0, 27) + "...";
            }
            else
            {
                valueLabel.text = value;
            }
        };
        leftPane.Rebuild();
        onRefresh?.Invoke();
    }
    }
}
