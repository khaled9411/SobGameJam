using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class HideSeekTabView
    {
    private VisualElement root;
    public HidePlayerPrefItem[] HidePlayerPrefContains = new HidePlayerPrefItem[0];
    private Action<HidePlayerPrefItem[]> onArrayChanged;

    public HideSeekTabView(VisualElement parent, Action<HidePlayerPrefItem[]> onArrayChanged)
    {
        root = parent;
        this.onArrayChanged = onArrayChanged;
        
        // Initialize array if null
        if (HidePlayerPrefContains == null)
            HidePlayerPrefContains = new HidePlayerPrefItem[0];
            
        BuildUI();
    }

    private void BuildUI()
    {
        root.Clear();
        
        // Ensure root uses full space
        root.style.flexGrow = 1;
        root.style.flexDirection = FlexDirection.Column;
        
        // Title
        var titleLabel = new Label("Hide & Seek - PlayerPref Filter Settings");
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 16;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.marginTop = 10;
        titleLabel.style.flexShrink = 0;
        root.Add(titleLabel);
        
        // Description
        var descLabel = new Label("Add strings below. PlayerPrefs containing these strings will be hidden from selected tabs. Use exceptions to exclude specific PlayerPrefs.");
        descLabel.style.fontSize = 12;
        descLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        descLabel.style.marginBottom = 15;
        descLabel.style.flexShrink = 0;
        descLabel.style.whiteSpace = WhiteSpace.Normal; // Allow text wrapping
        root.Add(descLabel);
        
        // Container for array items with scroll support
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1; // Use all available space
        scrollView.style.flexShrink = 1; // Allow shrinking if needed
        
        var arrayContainer = new VisualElement();
        arrayContainer.style.flexDirection = FlexDirection.Column;
        arrayContainer.style.flexGrow = 1;
        arrayContainer.style.flexShrink = 0;
        
        scrollView.Add(arrayContainer);
        root.Add(scrollView);
        
        // Button container at the bottom
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.marginTop = 10;
        buttonContainer.style.marginBottom = 10;
        buttonContainer.style.flexShrink = 0; // Don't shrink the button area
        
        // Add button
        var addButton = new Button(() => {
            var newArray = new HidePlayerPrefItem[HidePlayerPrefContains.Length + 1];
            Array.Copy(HidePlayerPrefContains, newArray, HidePlayerPrefContains.Length);
            newArray[HidePlayerPrefContains.Length] = new HidePlayerPrefItem();
            HidePlayerPrefContains = newArray;
            onArrayChanged?.Invoke(HidePlayerPrefContains);
            // Use delayed refresh to prevent UI conflicts
            EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
        }) { text = "Add Hide Filter" };
        addButton.style.width = 120;
        addButton.style.marginRight = 10;
        buttonContainer.Add(addButton);
        
        // Clear all button
        var clearButton = new Button(() => {
            HidePlayerPrefContains = new HidePlayerPrefItem[0];
            onArrayChanged?.Invoke(HidePlayerPrefContains);
            // Use delayed refresh to prevent UI conflicts
            EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
        }) { text = "Clear All Filters" };
        clearButton.style.width = 120;
        clearButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.4f, 0.8f);
        buttonContainer.Add(clearButton);
        
        root.Add(buttonContainer);
        
        RefreshArrayUI(arrayContainer);
    }
    
    private void RefreshArrayUI(VisualElement arrayContainer)
    {
        // Ensure we have a valid container
        if (arrayContainer == null) return;
        
        // Clear all children properly
        while (arrayContainer.childCount > 0)
        {
            arrayContainer.RemoveAt(0);
        }
        
        // Force a layout update
        arrayContainer.MarkDirtyRepaint();
        
        for (int i = 0; i < HidePlayerPrefContains.Length; i++)
        {
            var item = HidePlayerPrefContains[i];
            int index = i; // Capture for closure - moved to top
            
            // Main container for this filter item
            var itemContainer = new VisualElement();
            itemContainer.style.marginBottom = 10;
            itemContainer.style.paddingLeft = 15;
            itemContainer.style.paddingRight = 15;
            itemContainer.style.paddingTop = 15;
            itemContainer.style.paddingBottom = 15;
            itemContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            itemContainer.style.borderTopLeftRadius = 5;
            itemContainer.style.borderTopRightRadius = 5;
            itemContainer.style.borderBottomLeftRadius = 5;
            itemContainer.style.borderBottomRightRadius = 5;
            itemContainer.style.flexShrink = 0; // Prevent item from shrinking
            
            // Header row with filter label and remove button
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 8;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.flexShrink = 0; // Prevent shrinking
            
            var indexLabel = new Label($"Filter {i + 1}:");
            indexLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(indexLabel);
            
            var removeButton = new Button(() => {
                var newArray = new HidePlayerPrefItem[HidePlayerPrefContains.Length - 1];
                int newIndex = 0;
                for (int j = 0; j < HidePlayerPrefContains.Length; j++)
                {
                    if (j != index)
                        newArray[newIndex++] = HidePlayerPrefContains[j];
                }
                HidePlayerPrefContains = newArray;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
                // Use delayed refresh to prevent UI conflicts
                EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
            }) { text = "Remove" };
            removeButton.style.width = 80;
            removeButton.style.height = 22;
            removeButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.4f, 0.9f);
            removeButton.style.color = Color.white;
            removeButton.style.flexShrink = 0;
            removeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            headerRow.Add(removeButton);
            itemContainer.Add(headerRow);
            
            // Contains string field on new line
            var containsField = new TextField("Contains String:");
            containsField.style.marginBottom = 8;
            containsField.style.flexShrink = 0; // Prevent shrinking
            containsField.labelElement.style.marginRight = 5; // Adjust gap between label and input
            containsField.labelElement.style.minWidth = 100; // Set label width
            containsField.value = item.containsString;
            
            containsField.RegisterValueChangedCallback(evt => {
                HidePlayerPrefContains[index].containsString = evt.newValue;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
            });
            
            itemContainer.Add(containsField);
            
            // Tab checkboxes row
            var tabRow = new VisualElement();
            tabRow.style.flexDirection = FlexDirection.Row;
            tabRow.style.alignItems = Align.Center;
            tabRow.style.marginBottom = 8;
            tabRow.style.flexShrink = 0; // Prevent shrinking
            tabRow.style.flexWrap = Wrap.NoWrap; // Prevent wrapping
            
            var tabLabel = new Label("Hide from tabs:");
            tabLabel.style.width = 100;
            tabRow.Add(tabLabel);
            
            var allCheckbox = new Toggle();
            allCheckbox.value = item.hideFromAll;
            allCheckbox.RegisterValueChangedCallback(evt => {
                HidePlayerPrefContains[index].hideFromAll = evt.newValue;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
            });
            var allLabel = new Label("All");
            allLabel.style.marginLeft = 5;
            var allContainer = new VisualElement();
            allContainer.style.flexDirection = FlexDirection.Row;
            allContainer.style.alignItems = Align.Center;
            allContainer.style.marginRight = 10;
            allContainer.Add(allCheckbox);
            allContainer.Add(allLabel);
            tabRow.Add(allContainer);
            
            var pinCheckbox = new Toggle();
            pinCheckbox.value = item.hideFromPin;
            pinCheckbox.RegisterValueChangedCallback(evt => {
                HidePlayerPrefContains[index].hideFromPin = evt.newValue;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
            });
            var pinLabel = new Label("Pin");
            pinLabel.style.marginLeft = 5;
            var pinContainer = new VisualElement();
            pinContainer.style.flexDirection = FlexDirection.Row;
            pinContainer.style.alignItems = Align.Center;
            pinContainer.style.marginRight = 10;
            pinContainer.Add(pinCheckbox);
            pinContainer.Add(pinLabel);
            tabRow.Add(pinContainer);
            
            var liveCheckbox = new Toggle();
            liveCheckbox.value = item.hideFromLive;
            liveCheckbox.RegisterValueChangedCallback(evt => {
                HidePlayerPrefContains[index].hideFromLive = evt.newValue;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
            });
            var liveLabel = new Label("Live");
            liveLabel.style.marginLeft = 5;
            var liveContainer = new VisualElement();
            liveContainer.style.flexDirection = FlexDirection.Row;
            liveContainer.style.alignItems = Align.Center;
            liveContainer.style.marginRight = 10;
            liveContainer.Add(liveCheckbox);
            liveContainer.Add(liveLabel);
            tabRow.Add(liveContainer);
            
            itemContainer.Add(tabRow);
            
            // Exceptions section
            var exceptionsLabel = new Label("Exceptions (exact matches that won't be hidden):");
            exceptionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            exceptionsLabel.style.marginBottom = 5;
            exceptionsLabel.style.marginTop = 5;
            exceptionsLabel.style.flexShrink = 0; // Prevent shrinking
            itemContainer.Add(exceptionsLabel);
            
            var exceptionsContainer = new VisualElement();
            exceptionsContainer.style.marginLeft = 10;
            exceptionsContainer.style.marginBottom = 5;
            exceptionsContainer.style.flexDirection = FlexDirection.Column;
            exceptionsContainer.style.flexShrink = 0; // Prevent shrinking
            itemContainer.Add(exceptionsContainer);
            
            // Add exception button
            var addExceptionButton = new Button(() => {
                var newExceptions = new string[item.exceptions.Length + 1];
                Array.Copy(item.exceptions, newExceptions, item.exceptions.Length);
                newExceptions[item.exceptions.Length] = "";
                HidePlayerPrefContains[index].exceptions = newExceptions;
                onArrayChanged?.Invoke(HidePlayerPrefContains);
                // Use delayed refresh to prevent UI conflicts
                EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
            }) { text = "Add Exception" };
            addExceptionButton.style.width = 120;
            addExceptionButton.style.marginBottom = 8;
            addExceptionButton.style.marginLeft = 10;
            itemContainer.Add(addExceptionButton);
            
            // Exception items
            for (int j = 0; j < item.exceptions.Length; j++)
            {
                var exceptionRow = new VisualElement();
                exceptionRow.style.flexDirection = FlexDirection.Row;
                exceptionRow.style.alignItems = Align.Center;
                exceptionRow.style.marginBottom = 5;
                exceptionRow.style.marginLeft = 10;
                exceptionRow.style.flexShrink = 0; // Prevent shrinking
                exceptionRow.style.flexWrap = Wrap.NoWrap; // Prevent wrapping
                
                var exceptionLabel = new Label($"Exception {j + 1}:");
                exceptionLabel.style.width = 80; // Fixed width for consistent layout
                exceptionLabel.style.marginRight = 5; // Small gap after label
                exceptionRow.Add(exceptionLabel);
                
                var exceptionField = new TextField();
                exceptionField.style.flexGrow = 1;
                exceptionField.style.marginRight = 5; // Small gap before remove button
                exceptionField.style.maxWidth = 200; // Limit max width to ensure remove button is visible
                exceptionField.style.minWidth = 120; // Ensure minimum usable width
                exceptionField.value = item.exceptions[j];
                
                int exceptionIndex = j; // Capture for closure
                exceptionField.RegisterValueChangedCallback(evt => {
                    HidePlayerPrefContains[index].exceptions[exceptionIndex] = evt.newValue;
                    onArrayChanged?.Invoke(HidePlayerPrefContains);
                });
                
                exceptionRow.Add(exceptionField);
                
                var removeExceptionButton = new Button(() => {
                    var newExceptions = new string[item.exceptions.Length - 1];
                    int newExceptionIndex = 0;
                    for (int k = 0; k < item.exceptions.Length; k++)
                    {
                        if (k != exceptionIndex)
                            newExceptions[newExceptionIndex++] = item.exceptions[k];
                    }
                    HidePlayerPrefContains[index].exceptions = newExceptions;
                    onArrayChanged?.Invoke(HidePlayerPrefContains);
                    // Use delayed refresh to prevent UI conflicts
                    EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
                }) { text = "Remove" };
                removeExceptionButton.style.width = 70; // Compact width
                removeExceptionButton.style.height = 20; // Match input field height
                removeExceptionButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.4f, 0.9f);
                removeExceptionButton.style.color = Color.white;
                removeExceptionButton.style.marginLeft = 0; // No extra margin - right next to input
                removeExceptionButton.style.flexShrink = 0; // Prevent button from shrinking
                removeExceptionButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                
                exceptionRow.Add(removeExceptionButton);
                exceptionsContainer.Add(exceptionRow);
            }
            
            if (item.exceptions.Length == 0)
            {
                var noExceptionsLabel = new Label("No exceptions set.");
                noExceptionsLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                noExceptionsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                noExceptionsLabel.style.marginLeft = 10;
                exceptionsContainer.Add(noExceptionsLabel);
            }
            
            arrayContainer.Add(itemContainer);
        }
        
        if (HidePlayerPrefContains.Length == 0)
        {
            var emptyLabel = new Label("No hide filters set. PlayerPrefs will be visible in all tabs.");
            emptyLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            arrayContainer.Add(emptyLabel);
        }
    }

    public void Refresh(List<string> newKeys)
    {
        // Hide & Seek tab doesn't show PlayerPrefs, only the filter settings
        // So we don't need to do anything here
    }
    
    // Call this method when the HidePlayerPrefContains array is updated externally
    public void RefreshUI()
    {
        if (root != null && root.childCount > 2)
        {
            var arrayContainer = root[2] as VisualElement; // The container is the 3rd child (index 2)
            if (arrayContainer != null)
            {
                // Use delayed call to ensure UI is ready
                EditorApplication.delayCall += () => RefreshArrayUI(arrayContainer);
            }
        }
    }

    // Returns true if the key should be hidden from the specified tab
    public bool ShouldHideKey(string key, string tabName)
    {
        if (HidePlayerPrefContains == null || HidePlayerPrefContains.Length == 0)
            return false;
            
        foreach (var item in HidePlayerPrefContains)
        {
            if (string.IsNullOrEmpty(item.containsString))
                continue;
                
            // Check if this key contains the filter string
            if (key.Contains(item.containsString))
            {
                // Check if this key is in the exceptions list (exact match)
                bool isException = false;
                if (item.exceptions != null)
                {
                    foreach (var exception in item.exceptions)
                    {
                        if (!string.IsNullOrEmpty(exception) && key == exception)
                        {
                            isException = true;
                            break;
                        }
                    }
                }
                
                // If it's an exception, don't hide it
                if (isException)
                    continue;
                
                // Check if we should hide from this specific tab
                switch (tabName.ToLower())
                {
                    case "all":
                        if (item.hideFromAll) return true;
                        break;
                    case "pin":
                        if (item.hideFromPin) return true;
                        break;
                    case "live":
                        if (item.hideFromLive) return true;
                        break;
                }
            }
        }
        
        return false;
    }
    }
}
