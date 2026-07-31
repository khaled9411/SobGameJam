using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class PinTabView
    {
    private VisualElement root;
    private ListView pinListView;
    private Action onRefresh;
    public List<(string key, string type, string value, bool pinned, int updateCount)> items = new List<(string, string, string, bool, int)>();
    public Action<string, bool> OnPinToggle;
    private Dictionary<string, List<(string value, string timestamp)>> changeHistory = new Dictionary<string, List<(string, string)>>();
    
    public void SetChangeHistory(Dictionary<string, List<(string value, string timestamp)>> history)
    {
        changeHistory = history;
    }

    public PinTabView(VisualElement parent, Action onRefresh)
    {
        root = parent;
        this.onRefresh = onRefresh;
        
        // Add header with info about update counter
        // var headerContainer = new VisualElement();
        // headerContainer.style.flexDirection = FlexDirection.Row;
        // headerContainer.style.alignItems = Align.Center;
        // headerContainer.style.paddingLeft = 12;
        // headerContainer.style.paddingRight = 12;
        // headerContainer.style.paddingTop = 6;
        // headerContainer.style.paddingBottom = 15;
        // headerContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        // headerContainer.style.borderBottomWidth = 1;
        // headerContainer.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        // root.Add(headerContainer);
        
        // var headerTitle = new Label("Pinned PlayerPrefs");
        // headerTitle.style.fontSize = 13;
        // headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        // headerTitle.style.marginRight = 16;
        // headerContainer.Add(headerTitle);
        
        // var updateInfoLabel = new Label("💡 Update counters reset when entering Play Mode");
        // updateInfoLabel.style.fontSize = 10;
        // updateInfoLabel.style.color = new Color(0.7f, 0.9f, 1f, 1f);
        // updateInfoLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        // updateInfoLabel.tooltip = "Hover over 'Updates: X' to see the last 30 value changes with timestamps.\nHistory is cleared when entering Play Mode.";
        // headerContainer.Add(updateInfoLabel);
        
        pinListView = new ListView();
        root.Add(pinListView);
    }

    public void Refresh(List<(string key, string type, string value, bool pinned, int updateCount)> newItems)
    {
        items = newItems;
        pinListView.itemsSource = items;
        pinListView.fixedItemHeight = 32; // Match notifications tab height
        pinListView.makeItem = () => {
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
            
            // Pin button
            var pinBtn = new Button();
            pinBtn.style.width = 24;
            pinBtn.style.height = 20;
            pinBtn.style.marginRight = 8;
            pinBtn.style.fontSize = 12;
            pinBtn.style.flexShrink = 0;
            row.Add(pinBtn);
            
            // Key name
            var keyLabel = new Label();
            keyLabel.style.minWidth = 200;
            keyLabel.style.flexGrow = 1;
            keyLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            keyLabel.style.fontSize = 12;
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(keyLabel);
            
            // Type - styled like notifications tab
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
            
            // Update count
            var updateCountLabel = new Label();
            updateCountLabel.style.minWidth = 80;
            updateCountLabel.style.marginLeft = 8;
            updateCountLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            updateCountLabel.style.fontSize = 11;
            updateCountLabel.style.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.3f);
            updateCountLabel.style.paddingLeft = 6;
            updateCountLabel.style.paddingRight = 6;
            updateCountLabel.style.paddingTop = 2;
            updateCountLabel.style.paddingBottom = 2;
            updateCountLabel.style.borderTopLeftRadius = 3;
            updateCountLabel.style.borderTopRightRadius = 3;
            updateCountLabel.style.borderBottomLeftRadius = 3;
            updateCountLabel.style.borderBottomRightRadius = 3;
            updateCountLabel.style.color = new Color(0.7f, 0.9f, 1f, 1f);
            row.Add(updateCountLabel);
            
            return row;
        };
        pinListView.bindItem = (item, index) => {
            var row = item as VisualElement;
            var pinBtn = row.ElementAt(0) as Button;
            var keyLabel = row.ElementAt(1) as Label;
            var typeLabel = row.ElementAt(2) as Label;
            var valueLabel = row.ElementAt(3) as Label;
            var updateCountLabel = row.ElementAt(4) as Label;
            
            var entry = items[index];
            
            // Alternate row colors
            if (index % 2 == 1) {
                row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0f);
            } else {
                row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);
            }
            
            // Set data
            keyLabel.text = entry.key;
            typeLabel.text = entry.type;
            valueLabel.text = entry.value;
            pinBtn.text = entry.pinned ? "★" : "☆";
            updateCountLabel.text = $"Updates: {entry.updateCount}";
            
            // Build tooltip with change history
            if (changeHistory.ContainsKey(entry.key) && changeHistory[entry.key].Count > 0)
            {
                var history = changeHistory[entry.key];
                var tooltipText = $"Change History for '{entry.key}' (Last {history.Count} changes):\n\n";
                
                for (int i = history.Count - 1; i >= 0; i--) // Reverse order (newest first)
                {
                    var change = history[i];
                    tooltipText += $"[{change.timestamp}] → {change.value}\n";
                }
                
                updateCountLabel.tooltip = tooltipText;
            }
            else
            {
                updateCountLabel.tooltip = "No changes recorded yet.\nChanges will appear here when the value updates.";
            }
            
            // Set type color - matching notifications tab
            switch (entry.type.ToLower())
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
            
            // Clear previous event handlers to avoid duplicates
            pinBtn.clicked -= null;
            pinBtn.clicked += () => {
                OnPinToggle?.Invoke(entry.key, entry.pinned);
            };
        };
        pinListView.Rebuild();
        onRefresh?.Invoke();
    }
    }
}
