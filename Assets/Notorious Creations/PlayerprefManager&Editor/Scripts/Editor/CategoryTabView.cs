using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class CategoryTabView
    {
        private VisualElement root;
        private Action onRefresh;
        private Toolbar subTabsToolbar;
        private ListView categoryListView;
        
        // Data populated from refresh
        private List<string> availableCategories = new List<string>();
        // Key -> Category mapping
        private Dictionary<string, string> prefCategories = new Dictionary<string, string>();
        // Item list containing key, type, value
        private List<(string key, string type, string value)> allItems = new List<(string, string, string)>();
        
        // Track the currently selected category sub-tab
        private string activeCategory = "UnCategorized";

        public CategoryTabView(VisualElement parent, Action onRefresh)
        {
            root = parent;
            this.onRefresh = onRefresh;

            // Structure: Top Toolbar for categories, remaining space for ListView
            subTabsToolbar = new Toolbar();
            subTabsToolbar.style.flexWrap = Wrap.Wrap; // Allow wrapping if many categories
            subTabsToolbar.style.minHeight = 24;
            subTabsToolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Sub-tabs area goes into root
            root.Add(subTabsToolbar);

            categoryListView = new ListView();
            categoryListView.style.flexGrow = 1;
            categoryListView.fixedItemHeight = 32;
            root.Add(categoryListView);
        }

        public void Refresh(List<string> updatedCategories, Dictionary<string, string> updatedPrefCategories, List<(string key, string type, string value)> updatedItems)
        {
            this.availableCategories = updatedCategories ?? new List<string> { "UnCategorized" };
            this.prefCategories = updatedPrefCategories ?? new Dictionary<string, string>();
            this.allItems = updatedItems ?? new List<(string, string, string)>();

            // Ensure active category is still valid
            if (!availableCategories.Contains(activeCategory))
            {
                activeCategory = availableCategories.FirstOrDefault() ?? "UnCategorized";
            }

            RebuildSubTabs();
            RebuildListView();
        }

        private void RebuildSubTabs()
        {
            subTabsToolbar.Clear();

            foreach (var category in availableCategories)
            {
                var btn = new Button(() =>
                {
                    activeCategory = category;
                    RebuildSubTabs(); // Re-apply visual selection
                    RebuildListView(); // Refresh items
                });
                
                btn.text = category;
                
                // Mimic the main window's tab selection styling
                if (category == activeCategory)
                {
                    btn.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
                    btn.style.borderBottomColor = new Color(0.4f, 0.6f, 1f, 1f);
                    btn.style.borderBottomWidth = 2;
                }
                else
                {
                    btn.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                    btn.style.borderBottomWidth = 0;
                }
                
                subTabsToolbar.Add(btn);
            }
        }

        private void RebuildListView()
        {
            // Filter items by active category
            var filteredItems = new List<(string key, string type, string value)>();
            foreach (var item in allItems)
            {
                string itemCat = prefCategories.ContainsKey(item.key) ? prefCategories[item.key] : "UnCategorized";
                if (itemCat == activeCategory)
                {
                    filteredItems.Add(item);
                }
            }

            categoryListView.itemsSource = filteredItems;

            categoryListView.makeItem = () =>
            {
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

                return row;
            };

            categoryListView.bindItem = (element, index) =>
            {
                if (index < 0 || index >= filteredItems.Count) return;
                var row = element as VisualElement;
                var keyLabel = row.ElementAt(0) as Label;
                var typeLabel = row.ElementAt(1) as Label;
                var valueLabel = row.ElementAt(2) as Label;

                var entry = filteredItems[index];

                // Alternate row colors
                if (index % 2 == 1)
                {
                    row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0f);
                }
                else
                {
                    row.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);
                }

                // Set data
                keyLabel.text = entry.key;
                typeLabel.text = entry.type;
                valueLabel.text = entry.value;

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
            };

            categoryListView.Rebuild();
            // Optional: call external refresh if mapped in the future
            // onRefresh?.Invoke();
        }
    }
}
