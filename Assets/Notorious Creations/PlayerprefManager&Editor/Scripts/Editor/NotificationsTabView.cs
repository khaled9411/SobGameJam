using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class NotificationsTabView
    {
    private VisualElement root;
    private ListView notificationsListView;
    private Action onRefresh;
    public List<(string key, string type, string value, bool isTracked)> items = new List<(string, string, string, bool)>();
    public Action<string, bool> OnNotificationToggle;

    public NotificationsTabView(VisualElement parent, Action onRefresh)
    {
        root = parent;
        this.onRefresh = onRefresh;
        
        // Create header
        var headerContainer = new VisualElement();
        headerContainer.style.flexDirection = FlexDirection.Row;
        headerContainer.style.alignItems = Align.Center;
        headerContainer.style.paddingLeft = 12;
        headerContainer.style.paddingRight = 12;
        headerContainer.style.paddingTop = 8;
        headerContainer.style.paddingBottom = 8;
        headerContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        headerContainer.style.borderBottomWidth = 1;
        headerContainer.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        root.Add(headerContainer);
        
        var headerTitle = new Label("PlayerPrefs Notifications");
        headerTitle.style.fontSize = 14;
        headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerTitle.style.marginRight = 16;
        headerContainer.Add(headerTitle);
        
        var headerDescription = new Label("Enable notifications for PlayerPrefs you want to monitor for changes");
        headerDescription.style.fontSize = 11;
        headerDescription.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        headerContainer.Add(headerDescription);
        
        // Create list view
        notificationsListView = new ListView();
        notificationsListView.fixedItemHeight = 32;
        notificationsListView.style.flexGrow = 1;
        root.Add(notificationsListView);
    }

    public void Refresh(List<(string key, string type, string value, bool isTracked)> newItems)
    {
        items = newItems;
        notificationsListView.itemsSource = items;
        
        notificationsListView.makeItem = () => {
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
            
            // Checkbox for notification tracking
            var checkbox = new Toggle();
            checkbox.style.marginRight = 12;
            checkbox.style.flexShrink = 0;
            checkbox.style.width = 20;
            checkbox.style.height = 20;
            checkbox.style.minWidth = 20;
            checkbox.style.minHeight = 20;
            checkbox.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            checkbox.style.borderTopWidth = 1;
            checkbox.style.borderBottomWidth = 1;
            checkbox.style.borderLeftWidth = 1;
            checkbox.style.borderRightWidth = 1;
            checkbox.style.borderTopColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            checkbox.style.borderBottomColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            checkbox.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            checkbox.style.borderRightColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            checkbox.style.borderTopLeftRadius = 3;
            checkbox.style.borderTopRightRadius = 3;
            checkbox.style.borderBottomLeftRadius = 3;
            checkbox.style.borderBottomRightRadius = 3;
            checkbox.tooltip = "Enable notifications for this PlayerPref";
            row.Add(checkbox);
            
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
            
            // Status indicator
            var statusLabel = new Label();
            statusLabel.style.minWidth = 80;
            statusLabel.style.marginLeft = 8;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLabel.style.fontSize = 10;
            statusLabel.style.paddingLeft = 4;
            statusLabel.style.paddingRight = 4;
            statusLabel.style.paddingTop = 1;
            statusLabel.style.paddingBottom = 1;
            statusLabel.style.borderTopLeftRadius = 2;
            statusLabel.style.borderTopRightRadius = 2;
            statusLabel.style.borderBottomLeftRadius = 2;
            statusLabel.style.borderBottomRightRadius = 2;
            row.Add(statusLabel);
            
            return row;
        };
        
        notificationsListView.bindItem = (item, index) => {
            var row = item as VisualElement;
            var checkbox = row.ElementAt(0) as Toggle;
            var keyLabel = row.ElementAt(1) as Label;
            var typeLabel = row.ElementAt(2) as Label;
            var valueLabel = row.ElementAt(3) as Label;
            var statusLabel = row.ElementAt(4) as Label;
            
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
            
            // Set checkbox state
            checkbox.SetValueWithoutNotify(entry.isTracked);
            
            // Set status
            if (entry.isTracked)
            {
                statusLabel.text = "TRACKING";
                statusLabel.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 0.8f);
                statusLabel.style.color = Color.white;
            }
            else
            {
                statusLabel.text = "INACTIVE";
                statusLabel.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                statusLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
            
            // Set type color
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
            checkbox.UnregisterValueChangedCallback(null);
            
            // Handle checkbox changes
            checkbox.RegisterValueChangedCallback(evt => {
                OnNotificationToggle?.Invoke(entry.key, evt.newValue);
            });
        };
        
        notificationsListView.Rebuild();
        onRefresh?.Invoke();
    }
    }
}
