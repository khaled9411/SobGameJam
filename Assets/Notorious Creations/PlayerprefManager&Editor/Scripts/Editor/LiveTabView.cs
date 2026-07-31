using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class LiveTabView
    {
    private VisualElement root;
    private ListView liveListView;
    private Action onRefresh;
    public List<(string key, string type, string value, string lastUpdated)> items = new List<(string, string, string, string)>();

    public LiveTabView(VisualElement parent, Action onRefresh)
    {
        root = parent;
        this.onRefresh = onRefresh;
        liveListView = new ListView();
        root.Add(liveListView);
    }

    public void Refresh(List<(string key, string type, string value, string lastUpdated)> newItems)
    {
        items = newItems;
        liveListView.itemsSource = items;
        liveListView.fixedItemHeight = 32; // Match notifications tab height
        liveListView.makeItem = () => {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 28;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new UnityEngine.Color(0.3f, 0.3f, 0.3f, 0.5f);
            
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
            typeLabel.style.backgroundColor = new UnityEngine.Color(0.4f, 0.4f, 0.4f, 0.3f);
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
            valueLabel.style.color = new UnityEngine.Color(0.9f, 0.9f, 0.9f, 1f);
            row.Add(valueLabel);
            
            // Time label
            var timeLabel = new Label();
            timeLabel.style.minWidth = 150;
            timeLabel.style.marginLeft = 8;
            timeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            timeLabel.style.fontSize = 11;
            timeLabel.style.color = new UnityEngine.Color(0.8f, 0.8f, 0.8f, 1f);
            row.Add(timeLabel);
            
            return row;
        };
        liveListView.bindItem = (item, index) => {
            if (index < 0 || index >= items.Count) return;
            var row = item as VisualElement;
            var keyLabel = row.ElementAt(0) as Label;
            var typeLabel = row.ElementAt(1) as Label;
            var valueLabel = row.ElementAt(2) as Label;
            var timeLabel = row.ElementAt(3) as Label;
            
            var entry = items[index];
            
            // Alternate row colors
            if (index % 2 == 1) {
                row.style.backgroundColor = new UnityEngine.Color(0.0f, 0.0f, 0.0f, 0f);
            } else {
                row.style.backgroundColor = new UnityEngine.Color(0.0f, 0.0f, 0.0f, 0.1f);
            }
            
            // Set data
            keyLabel.text = entry.key;
            typeLabel.text = entry.type;
            valueLabel.text = entry.value;
            timeLabel.text = entry.lastUpdated;
            
            // Set type color - matching notifications tab
            switch (entry.type.ToLower())
            {
                case "int":
                    typeLabel.style.color = new UnityEngine.Color(0.5f, 0.8f, 1f, 1f); // Light blue
                    break;
                case "float":
                    typeLabel.style.color = new UnityEngine.Color(1f, 0.8f, 0.5f, 1f); // Light orange
                    break;
                case "string":
                    typeLabel.style.color = new UnityEngine.Color(0.8f, 1f, 0.5f, 1f); // Light green
                    break;
                default:
                    typeLabel.style.color = UnityEngine.Color.white;
                    break;
            }
        };
        liveListView.Rebuild();
        onRefresh?.Invoke();
    }
    }
}
