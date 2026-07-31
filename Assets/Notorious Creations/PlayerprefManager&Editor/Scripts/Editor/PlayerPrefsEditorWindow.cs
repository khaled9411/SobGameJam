using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NotoriousCreations.PlayerPrefsEditor
{
    [System.Serializable]
    public class HidePlayerPrefItem
    {
    public string containsString = "";
    public string[] exceptions = new string[0];
    public bool hideFromAll = true;
    public bool hideFromPin = true;
    public bool hideFromLive = true;
}

public class PlayerPrefsEditorWindow : EditorWindow
{
    private Dictionary<string, string> keyNameToRegistryKey = new Dictionary<string, string>(); // Maps user key to registry key

    private ListView leftPane;
    private ListView tab3ListView;
    private LiveTabView liveTabView;
    private PinTabView pinTabView;
    private HideSeekTabView hideSeekTabView;
    private NotificationsTabView notificationsTabView;
    private CategoryTabView categoryTabView;
    private VisualElement categoryTabContent; // The empty view for Category
    private List<string> playerPrefKeys = new List<string>(); 
    private double lastCheckTime = 0;
    private int lastPrefsCount = 0;
    private ToolbarSearchField searchField;
    private string lastSelectedKey = ""; // Track the last selected key to maintain selection across refreshes
    
    // Serialized fields to persist between sessions 
    [SerializeField] private int currentTabIndex = 0;
    [SerializeField] private List<string> serializedPinnedKeys = new List<string>();
    [SerializeField] public HidePlayerPrefItem[] HidePlayerPrefContains = new HidePlayerPrefItem[0];
    private HashSet<string> pinnedKeys = new HashSet<string>();
    
    // Category tracking
    [SerializeField] private List<string> availableCategories = new List<string> { "UnCategorized" };
    [SerializeField] private List<string> categoryKeys = new List<string>();
    [SerializeField] private List<string> categoryValues = new List<string>();
    private Dictionary<string, string> prefCategories = new Dictionary<string, string>();
    
    // Track last updated date and time for each PlayerPref
    [SerializeField] private Dictionary<string, string> lastUpdatedTimes = new Dictionary<string, string>();
    private HashSet<string> currentPlayerPrefs = new HashSet<string>();
    
    // Track update counts for Pin Tab (resets on Play Mode)
    private Dictionary<string, int> updateCounts = new Dictionary<string, int>();
    private Dictionary<string, string> lastKnownValuesForCounting = new Dictionary<string, string>();
    
    // Track change history for each PlayerPref (last 30 changes)
    private Dictionary<string, List<(string value, string timestamp)>> changeHistory = new Dictionary<string, List<(string, string)>>();
    private const int MAX_HISTORY_ENTRIES = 30;

    [MenuItem("Tools/Notorious Creations/PlayerPrefs Editor")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<PlayerPrefsEditorWindow>();
        wnd.titleContent = new GUIContent("PlayerPrefs Editor");
        wnd.minSize = new Vector2(720, 450);
        wnd.maxSize = new Vector2(1920, 1080);
    }

    public void CreateGUI()
    {
        // Rebuild category dictionary from serialized lists
        prefCategories.Clear();
        for (int i = 0; i < Math.Min(categoryKeys.Count, categoryValues.Count); i++)
        {
            prefCategories[categoryKeys[i]] = categoryValues[i];
        }

        // Initialize notification system
        PlayerPrefChangeNotifier.Initialize();
        
        // Subscribe to Play Mode state changes to reset update counts
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Tabs UI (should be at the top)
        var tabs = new Toolbar();
        tabs.AddToClassList("tabsBar");
        // Set root to vertical layout
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.Add(tabs);

        // --- MAIN CONTENT CONTAINER ---
        var mainContentContainer = new VisualElement();
        mainContentContainer.style.flexGrow = 1;
        mainContentContainer.style.flexDirection = FlexDirection.Column;
        rootVisualElement.Add(mainContentContainer);
        // --- TAB CONTENTS SPLIT TO SEPARATE CLASSES ---
    VisualElement tab2Content = new VisualElement();
    tab2Content.name = "Tab2Content";
    tab2Content.style.flexGrow = 1;
    tab2Content.style.display = DisplayStyle.None;
    mainContentContainer.Add(tab2Content);
    pinTabView = new PinTabView(tab2Content, null);

    VisualElement hideSeekTabContent = new VisualElement();
    hideSeekTabContent.name = "HideSeekTabContent";
    hideSeekTabContent.style.flexGrow = 1;
    hideSeekTabContent.style.display = DisplayStyle.None;
    mainContentContainer.Add(hideSeekTabContent);
    hideSeekTabView = new HideSeekTabView(hideSeekTabContent, (newArray) => {
        HidePlayerPrefContains = newArray;
        SaveChanges();
        // Refresh other tabs to apply filtering
        if (currentTabIndex == 0) RefreshPlayerPrefsList();
        else if (currentTabIndex == 1) RefreshTab2ListView();
        else if (currentTabIndex == 2) RefreshTab3ListView();
    });
    hideSeekTabView.HidePlayerPrefContains = HidePlayerPrefContains;
    hideSeekTabView.RefreshUI(); // Refresh UI after setting the array

        // Create Notifications tab content
        VisualElement notificationsTabContent = new VisualElement();
        notificationsTabContent.name = "NotificationsTabContent";
        notificationsTabContent.style.flexGrow = 1;
        notificationsTabContent.style.display = DisplayStyle.None;
        mainContentContainer.Add(notificationsTabContent);
        notificationsTabView = new NotificationsTabView(notificationsTabContent, null);

        // Provide callback for notification toggle
        notificationsTabView.OnNotificationToggle = (key, isTracked) => {
            if (isTracked) {
                PlayerPrefChangeNotifier.TrackPlayerPref(key);
            } else {
                PlayerPrefChangeNotifier.UntrackPlayerPref(key);
            }
            // Refresh other tabs to update checkbox states
            if (currentTabIndex == 0) RefreshPlayerPrefsList();
            else if (currentTabIndex == 4) RefreshNotificationsTab();
        };

        // Create Category tab content
        categoryTabContent = new VisualElement();
        categoryTabContent.name = "CategoryTabContent";
        categoryTabContent.style.flexGrow = 1;
        categoryTabContent.style.display = DisplayStyle.None;
        mainContentContainer.Add(categoryTabContent);

        categoryTabView = new CategoryTabView(categoryTabContent, () => {
            // Optional: Actions when category list is interacted with
            RefreshPlayerPrefsList();
        });

        // Provide a callback to PinTabView for pin/unpin
        pinTabView.OnPinToggle = (key, pinned) => {
            if (pinnedKeys.Contains(key)) {
                pinnedKeys.Remove(key);
                serializedPinnedKeys.Remove(key);
            } else {
                pinnedKeys.Add(key);
                serializedPinnedKeys.Add(key);
            }
            SaveChanges();
            RefreshTab2ListView();
        };

        VisualElement tab3Content = new VisualElement();
        tab3Content.name = "Tab3Content";
        tab3Content.style.flexGrow = 1;
        tab3Content.style.display = DisplayStyle.None;
        mainContentContainer.Add(tab3Content);
    liveTabView = new LiveTabView(tab3Content, null);

        // Initialize pinned keys from serialized data
        pinnedKeys = new HashSet<string>(serializedPinnedKeys);

        // Helper to refresh Live tab
        void RefreshTab3ListView()
        {
            keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
            var allKeys = new List<string>(keyNameToRegistryKey.Keys);
            
            // Filter out keys that should be hidden
            if (HidePlayerPrefContains != null && HidePlayerPrefContains.Length > 0)
                allKeys = allKeys.Where(k => !hideSeekTabView.ShouldHideKey(k, "Live")).ToList();
                
            var items = new List<(string key, string type, string value, string lastUpdated)>();
            foreach (var k in allKeys)
            {
                string type = "";
                string value = "";
                if (PlayerPrefs.HasKey(k))
                {
                    int i = PlayerPrefs.GetInt(k, int.MinValue);
                    float f = PlayerPrefs.GetFloat(k, float.MinValue);
                    string s = PlayerPrefs.GetString(k, "__NULL__");
                    if (i != int.MinValue) { type = "int"; value = i.ToString(); }
                    else if (f != float.MinValue) { type = "float"; value = f.ToString(); }
                    else if (s != "__NULL__") { type = "string"; value = s; }
                }
                string lastUpdated = lastUpdatedTimes.TryGetValue(k, out string time) ? time : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                items.Add((k, type, value, lastUpdated));
            }
            items.Sort((a, b) => DateTime.Parse(b.lastUpdated).CompareTo(DateTime.Parse(a.lastUpdated)));
            liveTabView.Refresh(items);
        }

        // Initialize Tab3 ListView with last updated times
        if (currentTabIndex == 2) {
            RefreshTab3ListView();
        }
    {
        EditorApplication.update -= PollPrefs;
        EditorApplication.update += PollPrefs;


        var tabNames = new List<string> { "All", "Pin", "Live", "Hide & Seek", "Notifications", "Category" }; 
        var tabButtons = new List<Button>(); 
        int currentTab = currentTabIndex;
        VisualElement allTabContent = new VisualElement();
        allTabContent.name = "AllTabContent";
        allTabContent.style.flexGrow = 1;
        allTabContent.style.flexDirection = FlexDirection.Column;
        mainContentContainer.Add(allTabContent);

        // Message label for Live tab alpha warning
        var liveTabAlphaLabel = new Label("This Feature is still in alpha");
        liveTabAlphaLabel.style.marginLeft = 16;
        liveTabAlphaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        liveTabAlphaLabel.style.color = new Color(1f, 0.8f, 0.2f, 1f); // yellowish
        liveTabAlphaLabel.style.display = DisplayStyle.None;

        for (int i = 0; i < tabNames.Count; i++)
        {
            int tabIdx = i;
            var tabBtn = new Button(() => {
                currentTab = tabIdx;
                currentTabIndex = tabIdx; // Save the current tab index
                foreach (var btn in tabButtons) btn.RemoveFromClassList("selectedTab");
                tabButtons[tabIdx].AddToClassList("selectedTab");
                // Show/hide tab content
                allTabContent.style.display = (tabIdx == 0) ? DisplayStyle.Flex : DisplayStyle.None;
                tab2Content.style.display = (tabIdx == 1) ? DisplayStyle.Flex : DisplayStyle.None;
                tab3Content.style.display = (tabIdx == 2) ? DisplayStyle.Flex : DisplayStyle.None;
                hideSeekTabContent.style.display = (tabIdx == 3) ? DisplayStyle.Flex : DisplayStyle.None;
                notificationsTabContent.style.display = (tabIdx == 4) ? DisplayStyle.Flex : DisplayStyle.None;
                categoryTabContent.style.display = (tabIdx == 5) ? DisplayStyle.Flex : DisplayStyle.None;
                // Show/hide Live tab alpha label
                liveTabAlphaLabel.style.display = (tabIdx == 2) ? DisplayStyle.Flex : DisplayStyle.None;
                if (tabIdx == 0) RefreshPlayerPrefsList(); // Refresh All tab when switching to it
                else if (tabIdx == 1) RefreshTab2ListView();
                else if (tabIdx == 2) RefreshTab3ListView();
                else if (tabIdx == 3) RefreshHideSeekTabView();
                else if (tabIdx == 4) RefreshNotificationsTab();
                else if (tabIdx == 5) RefreshCategoryTabView();
            }) { text = tabNames[i] };
            tabButtons.Add(tabBtn);
            tabs.Add(tabBtn);
        }
        // Add the Live tab alpha label to the tabs bar, after the tab buttons
        tabs.Add(liveTabAlphaLabel);

        // Set the initial tab based on saved state
        if (tabButtons.Count > 0) {
            int tabToSelect = Mathf.Clamp(currentTabIndex, 0, tabButtons.Count - 1);
            tabButtons[tabToSelect].AddToClassList("selectedTab");

            // Set initial visibility based on saved tab
            allTabContent.style.display = (tabToSelect == 0) ? DisplayStyle.Flex : DisplayStyle.None;
            tab2Content.style.display = (tabToSelect == 1) ? DisplayStyle.Flex : DisplayStyle.None;
            tab3Content.style.display = (tabToSelect == 2) ? DisplayStyle.Flex : DisplayStyle.None;
            hideSeekTabContent.style.display = (tabToSelect == 3) ? DisplayStyle.Flex : DisplayStyle.None;
            notificationsTabContent.style.display = (tabToSelect == 4) ? DisplayStyle.Flex : DisplayStyle.None;
            categoryTabContent.style.display = (tabToSelect == 5) ? DisplayStyle.Flex : DisplayStyle.None;
            liveTabAlphaLabel.style.display = (tabToSelect == 2) ? DisplayStyle.Flex : DisplayStyle.None;

            // If we're starting on All tab, refresh its content 
            if (tabToSelect == 0) {
                // Defer refresh until after searchField is initialized
                // This will be handled after the UI setup is complete
            }
            // If we're starting on Tab2, refresh its content
            else if (tabToSelect == 1) RefreshTab2ListView();
            // If we're starting on Tab3, refresh its content
            else if (tabToSelect == 2) RefreshTab3ListView();
            // If we're starting on Hide & Seek tab, refresh its content
            else if (tabToSelect == 3) RefreshHideSeekTabView();
            // If we're starting on Notifications tab, refresh its content
            else if (tabToSelect == 4) RefreshNotificationsTab();
            // If we're starting on Category tab, refresh its content
            else if (tabToSelect == 5) RefreshCategoryTabView();
        }

        // Toolbar (search, filter, delete all)
        var headerBar = new Toolbar();
        headerBar.AddToClassList("headerBar");
        allTabContent.Add(headerBar);

        searchField = new ToolbarSearchField();
        headerBar.Add(searchField);
        var typeFilterDropdown = new DropdownField(new System.Collections.Generic.List<string> { "All", "int", "float", "string" }, 0);
        headerBar.Add(typeFilterDropdown);
        var deleteAllPrefsButton = new Button(() => {
            keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
            int totalKeys = keyNameToRegistryKey.Keys.Count;
            
            if (totalKeys == 0)
            {
                EditorUtility.DisplayDialog("No PlayerPrefs", "There are no PlayerPrefs to delete.", "OK");
                return;
            }
            
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete All PlayerPrefs", 
                $"Are you sure you want to delete all {totalKeys} PlayerPrefs?\n\nThis action cannot be undone.", 
                "Delete All", 
                "Cancel"
            );
            
            if (confirmed)
            {
                foreach (var userKey in keyNameToRegistryKey.Keys)
                {
                    PlayerPrefs.DeleteKey(userKey); 
                }
                PlayerPrefs.Save();
                RefreshPlayerPrefsList("");
            }
        });
        deleteAllPrefsButton.text = "Delete All Prefs";
        headerBar.Add(deleteAllPrefsButton);

        // Create Test PlayerPrefs button for debugging
        var createTestButton = new Button(() => {
            PlayerPrefs.SetString("test_string", "Hello World");
            PlayerPrefs.SetInt("test_int", 42);
            PlayerPrefs.SetFloat("test_float", 3.14f);
            PlayerPrefs.Save();
            RefreshPlayerPrefsList("");
        });
        createTestButton.text = "Create Test Prefs";
        headerBar.Add(createTestButton);

        // Export All Prefs as JSON button
        var exportPrefsButton = new Button(() => {
            PlayerPrefsExporter.ExportAllPrefsAsJson();
        });
        exportPrefsButton.text = "Export All Prefs (JSON)";
        headerBar.Add(exportPrefsButton);

        // Import All Prefs from JSON button
        var importPrefsButton = new Button(() => {
            PlayerPrefsImporter.ImportAllPrefsFromJson();
            RefreshPlayerPrefsList("");
        });
        importPrefsButton.text = "Import PlayerPrefs (JSON)";
        headerBar.Add(importPrefsButton);

        // Split view (list and details)
        var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1;
        splitView.style.flexShrink = 1;
        allTabContent.Add(splitView);

        leftPane = new ListView();
        leftPane.selectionType = SelectionType.Single;
        leftPane.style.flexGrow = 1;
        leftPane.style.flexShrink = 1;
        leftPane.style.paddingBottom = 60; // Add space below last item
        leftPane.style.marginBottom = 10; // Additional margin
        splitView.Add(leftPane);
        
        // Set up ListView structure immediately
        leftPane.makeItem = () => {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 24;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;

            var label = new Label();
            label.name = "keyLabel";
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.fontSize = 12;
            label.style.flexGrow = 1;
            label.style.minWidth = 100;
            row.Add(label);

            var categoryDropdown = new DropdownField();
            categoryDropdown.name = "categoryDropdown";
            categoryDropdown.style.width = 120;
            categoryDropdown.style.marginLeft = 8;
            row.Add(categoryDropdown);
            
            return row;
        };
        
        leftPane.bindItem = (item, index) =>
        {
            var row = item as VisualElement;
            var label = row.Q<Label>("keyLabel");
            var dropdown = row.Q<DropdownField>("categoryDropdown");
            
            if (index >= playerPrefKeys.Count) return;
            
            string key = playerPrefKeys[index];
            label.text = key;

            // Update dropdown choices to include all available categories + "Add New..."
            var choices = new List<string>(availableCategories);
            choices.Add("Add New...");
            dropdown.choices = choices;

            // Set current category
            string currentCategory = prefCategories.ContainsKey(key) ? prefCategories[key] : "UnCategorized";
            dropdown.SetValueWithoutNotify(currentCategory);

            // Handle category changes — unregister previous callback if one exists
            var previousCallback = dropdown.userData as EventCallback<ChangeEvent<string>>;
            if (previousCallback != null)
                dropdown.UnregisterValueChangedCallback(previousCallback);
            EventCallback<ChangeEvent<string>> callback = evt => {
                if (evt.newValue == "Add New...")
                {
                    // Revert to previous value temporarily
                    dropdown.SetValueWithoutNotify(evt.previousValue);
                    
                    // Show dialog to add new category
                    TextInputDialog.Show("New Category", "Enter name for new category:", "", newCat => {
                        if (!string.IsNullOrEmpty(newCat) && newCat != "Add New...")
                        {
                            if (!availableCategories.Contains(newCat))
                            {
                                availableCategories.Add(newCat);
                            }
                            prefCategories[key] = newCat;
                            SaveCategoryData();
                            RefreshPlayerPrefsList();
                        }
                    });
                }
                else
                {
                    prefCategories[key] = evt.newValue;
                    SaveCategoryData();
                }
            };
            dropdown.userData = callback;
            dropdown.RegisterValueChangedCallback(callback);
        };
        // Add a right pane with details and edit controls
        var rightPane = new VisualElement();
        splitView.Add(rightPane);

        // Details UI
        var detailsBox = new Box();
        rightPane.Add(detailsBox);
        var keyLabel = new Label("PlayerPref Key:");
        detailsBox.Add(keyLabel);
        var keyField = new TextField { isReadOnly = true };
        detailsBox.Add(keyField);
        var typeLabel = new Label("Type:");
        detailsBox.Add(typeLabel);
        var typeField = new TextField { isReadOnly = true };
        detailsBox.Add(typeField);
        var valueLabel = new Label("Value:");
        detailsBox.Add(valueLabel);
        var valueField = new TextField { isReadOnly = true };
        detailsBox.Add(valueField);
        keyField.SetEnabled(false);
        typeField.SetEnabled(false);
        valueField.SetEnabled(false);

        var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var editButton = new Button() { text = "Edit" };
        var saveButton = new Button() { text = "Save" };
        var cancelButton = new Button() { text = "Cancel" };
        var deleteButton = new Button() { text = "Delete" };
        buttonRow.Add(editButton);
        buttonRow.Add(saveButton);
        buttonRow.Add(cancelButton);
        buttonRow.Add(deleteButton);
        detailsBox.Add(buttonRow);

        // Button logic state
        void ResetEditControls()
        {
            editButton.SetEnabled(true);
            saveButton.SetEnabled(false);
            cancelButton.SetEnabled(false);
            valueField.isReadOnly = true;
            valueField.SetEnabled(false);
        }

        void EnableEditControls()
        {
            editButton.SetEnabled(false);
            saveButton.SetEnabled(true);
            cancelButton.SetEnabled(true);
            valueField.isReadOnly = false;
            valueField.SetEnabled(true);
        }

        // Store original value for cancel
        string originalValue = "";

        editButton.clicked += () => {
            EnableEditControls();
            originalValue = valueField.value;
        };

        saveButton.clicked += () => {
            int idx = leftPane.selectedIndex;
            if (idx >= 0 && idx < playerPrefKeys.Count)
            {
                string userKey = playerPrefKeys[idx];
                // Save based on detected type
                string type = typeField.value;
                bool valueChanged = false;
                
                if (type == "int")
                {
                    if (int.TryParse(valueField.value, out int i))
                    {
                        int currentValue = PlayerPrefs.GetInt(userKey, int.MinValue);
                        if (currentValue != i)
                        {
                            PlayerPrefs.SetInt(userKey, i);
                            valueChanged = true;
                        }
                    }
                }
                else if (type == "float")
                {
                    if (float.TryParse(valueField.value, out float f))
                    {
                        float currentValue = PlayerPrefs.GetFloat(userKey, float.MinValue);
                        if (Math.Abs(currentValue - f) > 0.0001f)
                        {
                            PlayerPrefs.SetFloat(userKey, f);
                            valueChanged = true;
                        }
                    }
                }
                else if (type == "string")
                {
                    string currentValue = PlayerPrefs.GetString(userKey, "__NULL__");
                    if (currentValue != valueField.value)
                    {
                        PlayerPrefs.SetString(userKey, valueField.value);
                        valueChanged = true;
                    }
                }
                
                // Update last modified time if value changed
                if (valueChanged)
                {
                    lastUpdatedTimes[userKey] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    SaveChanges();
                }
                
                PlayerPrefs.Save();
                RefreshPlayerPrefsList();
                // Restore selection after refresh
                leftPane.selectedIndex = idx;
            }
            ResetEditControls();
        };

        cancelButton.clicked += () => {
            valueField.value = originalValue;
            ResetEditControls();
        };

        deleteButton.clicked += () => {
            int idx = leftPane.selectedIndex;
            if (idx >= 0 && idx < playerPrefKeys.Count)
            {
                string userKey = playerPrefKeys[idx];
                
                bool confirmed = EditorUtility.DisplayDialog(
                    "Delete PlayerPref", 
                    $"Are you sure you want to delete the PlayerPref '{userKey}'?\n\nThis action cannot be undone.", 
                    "Delete", 
                    "Cancel"
                );
                
                if (confirmed)
                {
                    PlayerPrefs.DeleteKey(userKey);
                    PlayerPrefs.Save();
                    RefreshPlayerPrefsList();
                }
            }
            // Clear details panel if deletion confirmed
            if (idx >= 0 && idx < playerPrefKeys.Count)
            {
                keyField.value = "";
                typeField.value = "";
                valueField.value = "";
                ResetEditControls();
            }
        };

        ResetEditControls();

        // Update details when selection changes
        leftPane.selectionChanged += (selectedItems) => {
            // Use selectedIndex for reliable selection
            int idx = leftPane.selectedIndex;
            if (idx >= 0 && idx < playerPrefKeys.Count)
            {
                lastSelectedKey = playerPrefKeys[idx]; // Track selected key
                UpdateDetailsPanel(new List<object> { playerPrefKeys[idx] }, keyField, typeField, valueField);
            }
            else
            {
                lastSelectedKey = "";
                UpdateDetailsPanel(new List<object>(), keyField, typeField, valueField);
            }
        };

// Ensure searchField.value is used for both search and filter
searchField.RegisterValueChangedCallback(evt => {
    RefreshPlayerPrefsList(evt.newValue);
});
typeFilterDropdown.RegisterValueChangedCallback(evt => {
    selectedTypeFilter = evt.newValue;
    RefreshPlayerPrefsList(searchField.value);
});

        // If we started on All tab, refresh it now that searchField is initialized
        if (currentTabIndex == 0) {
            RefreshPlayerPrefsList("");
        }

        // --- BEGIN FOOTER BAR ---
        var footerBar = new VisualElement();
        footerBar.style.flexDirection = FlexDirection.Row;
        footerBar.style.alignItems = Align.Center;
        footerBar.style.paddingTop = 8;
        footerBar.style.paddingBottom = 8;
        footerBar.style.paddingLeft = 12;
        footerBar.style.paddingRight = 12;
        footerBar.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
        footerBar.style.position = Position.Absolute;
        footerBar.style.bottom = 0;
        footerBar.style.left = 0;
        footerBar.style.right = 0;
        footerBar.style.height = 50;
        rootVisualElement.Add(footerBar);

        var newPlayerPrefTitle = new Label("Add New PlayerPref");
        newPlayerPrefTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        newPlayerPrefTitle.style.marginRight = 16;
        footerBar.Add(newPlayerPrefTitle);

        var newPrefKeyLabel = new Label("Key");
        newPrefKeyLabel.style.marginRight = 4;
        footerBar.Add(newPrefKeyLabel);
        var newPrefKeyField = new TextField();
        newPrefKeyField.style.width = 120;
        newPrefKeyField.style.marginRight = 16;
        footerBar.Add(newPrefKeyField);

        var newPrefTypeLabel = new Label("Type");
        newPrefTypeLabel.style.marginRight = 4;
        footerBar.Add(newPrefTypeLabel);
        var newPrefTypeDropdown = new DropdownField(new List<string> { "int", "float", "string" }, 0);
        newPrefTypeDropdown.style.width = 80;
        newPrefTypeDropdown.style.marginRight = 16;
        footerBar.Add(newPrefTypeDropdown);

        var newPrefValueLabel = new Label("Value");
        newPrefValueLabel.style.marginRight = 4;
        footerBar.Add(newPrefValueLabel);
        var newPrefValueField = new TextField();
        newPrefValueField.style.width = 120;
        newPrefValueField.style.marginRight = 16;
        footerBar.Add(newPrefValueField);

        var newPrefSaveButton = new Button(() => {
            string key = newPrefKeyField.value;
            string type = newPrefTypeDropdown.value;
            string value = newPrefValueField.value;
            if (!string.IsNullOrEmpty(key))
            {
                if (type == "int" && int.TryParse(value, out int i))
                    PlayerPrefs.SetInt(key, i);
                else if (type == "float" && float.TryParse(value, out float f))
                    PlayerPrefs.SetFloat(key, f);
                else if (type == "string")
                    PlayerPrefs.SetString(key, value);
                
                // Update last modified time for new PlayerPref
                lastUpdatedTimes[key] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveChanges();
                
                PlayerPrefs.Save();
                RefreshPlayerPrefsList(searchField.value);
                newPrefKeyField.value = "";
                newPrefTypeDropdown.index = 0;
                newPrefValueField.value = "";
            }
        }) { text = "Add PlayerPref" };
        newPrefSaveButton.style.marginRight = 8;
        newPrefSaveButton.style.width = 110;
        footerBar.Add(newPrefSaveButton);

        var newPrefClearButton = new Button(() => {
            newPrefKeyField.value = "";
            newPrefTypeDropdown.index = 0;
            newPrefValueField.value = "";
        }) { text = "Clear" };
        newPrefClearButton.style.width = 70;
        footerBar.Add(newPrefClearButton);
        // --- END FOOTER BAR ---
    }
    }

    // Helper to refresh Pin tab
    private void RefreshTab2ListView()
    {
        keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        
        // Filter out keys that should be hidden
        if (HidePlayerPrefContains != null && HidePlayerPrefContains.Length > 0)
            allKeys = allKeys.Where(k => !hideSeekTabView.ShouldHideKey(k, "Pin")).ToList();
            
        allKeys.Sort((a, b) => {
            bool aPinned = pinnedKeys.Contains(a);
            bool bPinned = pinnedKeys.Contains(b);
            if (aPinned && !bPinned) return -1;
            if (!aPinned && bPinned) return 1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        });
        var items = new List<(string key, string type, string value, bool pinned, int updateCount)>();
        foreach (var k in allKeys)
        {
            string type = "";
            string value = "";
            if (PlayerPrefs.HasKey(k))
            {
                int i = PlayerPrefs.GetInt(k, int.MinValue);
                float f = PlayerPrefs.GetFloat(k, float.MinValue);
                string s = PlayerPrefs.GetString(k, "__NULL__");
                if (i != int.MinValue) { type = "int"; value = i.ToString(); }
                else if (f != float.MinValue) { type = "float"; value = f.ToString(); }
                else if (s != "__NULL__") { type = "string"; value = s; }
            }
            
            // Get update count for this key
            int count = updateCounts.ContainsKey(k) ? updateCounts[k] : 0;
            
            items.Add((k, type, value, pinnedKeys.Contains(k), count));
        }
        
        // Pass change history to PinTabView
        pinTabView.SetChangeHistory(changeHistory);
        pinTabView.Refresh(items);
    }

    // Refresh Hide & Seek tab
    private void RefreshHideSeekTabView()
    {
        keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        hideSeekTabView.HidePlayerPrefContains = HidePlayerPrefContains;
        hideSeekTabView.RefreshUI(); // Refresh UI to show current items
        hideSeekTabView.Refresh(allKeys);
    }

    // Refresh Notifications tab
    private void RefreshNotificationsTab()
    {
        keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        
        // Filter out keys that should be hidden
        if (HidePlayerPrefContains != null && HidePlayerPrefContains.Length > 0)
            allKeys = allKeys.Where(k => !hideSeekTabView.ShouldHideKey(k, "Notifications")).ToList();
            
        var items = new List<(string key, string type, string value, bool isTracked)>();
        foreach (var k in allKeys)
        {
            string type = "";
            string value = "";
            if (PlayerPrefs.HasKey(k))
            {
                int i = PlayerPrefs.GetInt(k, int.MinValue);
                float f = PlayerPrefs.GetFloat(k, float.MinValue);
                string s = PlayerPrefs.GetString(k, "__NULL__");
                if (i != int.MinValue) { type = "int"; value = i.ToString(); }
                else if (f != float.MinValue) { type = "float"; value = f.ToString(); }
                else if (s != "__NULL__") { type = "string"; value = s; }
            }
            bool isTracked = PlayerPrefChangeNotifier.IsTracked(k);
            items.Add((k, type, value, isTracked));
        }
        
        // Sort by tracked status first, then by key name
        items.Sort((a, b) => {
            if (a.isTracked && !b.isTracked) return -1;
            if (!a.isTracked && b.isTracked) return 1;
            return string.Compare(a.key, b.key, StringComparison.OrdinalIgnoreCase);
        });
        
        notificationsTabView.Refresh(items);
    }

    // Updates the details panel with the selected PlayerPref
    private void UpdateDetailsPanel(IEnumerable<object> selectedItems, TextField keyField, TextField typeField, TextField valueField)
    {
        keyField.SetEnabled(false);
        typeField.SetEnabled(false);
        if (selectedItems is IList<object> sel && sel.Count > 0)
        {
            string userKey = sel[0] as string;
            keyField.value = userKey;
            keyField.isReadOnly = true;
            typeField.isReadOnly = true;
            // Try to get type and value
            if (PlayerPrefs.HasKey(userKey))
            {
                int i = PlayerPrefs.GetInt(userKey, int.MinValue);
                float f = PlayerPrefs.GetFloat(userKey, float.MinValue);
                string s = PlayerPrefs.GetString(userKey, "__NULL__");
                if (i != int.MinValue)
                {
                    typeField.value = "int";
                    valueField.value = i.ToString();
                }
                else if (f != float.MinValue)
                {
                    typeField.value = "float";
                    valueField.value = f.ToString();
                }
                else if (s != "__NULL__")
                {
                    typeField.value = "string";
                    valueField.value = s;
                }
                else
                {
                    typeField.value = "Unknown";
                    valueField.value = "";
                }
                
                // Add last updated time to the details panel
                if (lastUpdatedTimes.TryGetValue(userKey, out string lastUpdated))
                {
                    // Add or update the last updated time label
                    var detailsBox = valueField.parent as Box;
                    if (detailsBox != null)
                    {
                        // Look for existing last updated label
                        Label lastUpdatedLabel = null;
                        Label lastUpdatedValueLabel = null;
                        
                        for (int idx = 0; idx < detailsBox.childCount; idx++)
                        {
                            if (detailsBox[idx] is Label label && label.text == "Last Updated:")
                            {
                                lastUpdatedLabel = label;
                                if (idx + 1 < detailsBox.childCount && detailsBox[idx + 1] is Label valueLabel)
                                {
                                    lastUpdatedValueLabel = valueLabel;
                                }
                                break;
                            }
                        }
                        
                        // Create or update labels
                        if (lastUpdatedLabel == null)
                        {
                            lastUpdatedLabel = new Label("Last Updated:");
                            detailsBox.Add(lastUpdatedLabel);
                            
                            lastUpdatedValueLabel = new Label(lastUpdated);
                            detailsBox.Add(lastUpdatedValueLabel);
                        }
                        else if (lastUpdatedValueLabel != null)
                        {
                            lastUpdatedValueLabel.text = lastUpdated;
                        }
                    }
                }
            }
            else
            {
                typeField.value = "";
                valueField.value = "";
            }
            valueField.isReadOnly = true;
            valueField.SetEnabled(false);
        }
        else
        {
            keyField.value = "";
            typeField.value = "";
            valueField.value = "";
            keyField.isReadOnly = true;
            typeField.isReadOnly = true;
            valueField.isReadOnly = true;
            keyField.SetEnabled(false);
            typeField.SetEnabled(false);
            valueField.SetEnabled(false);
            
            // Remove last updated labels if they exist
            var detailsBox = valueField.parent as Box;
            if (detailsBox != null)
            {
                List<VisualElement> elementsToRemove = new List<VisualElement>();
                
                for (int idx = 0; idx < detailsBox.childCount; idx++)
                {
                    if (detailsBox[idx] is Label label && label.text == "Last Updated:")
                    {
                        elementsToRemove.Add(label);
                        if (idx + 1 < detailsBox.childCount)
                        {
                            elementsToRemove.Add(detailsBox[idx + 1]);
                        }
                        break;
                    }
                }
                
                foreach (var element in elementsToRemove)
                {
                    detailsBox.Remove(element);
                }
            }
        }
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Reset update counts when entering Play Mode
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode)
        {
            updateCounts.Clear();
            lastKnownValuesForCounting.Clear();
            changeHistory.Clear(); // Clear change history on Play Mode
            
            // Refresh Pin tab if it's currently active
            if (currentTabIndex == 1)
            {
                RefreshTab2ListView();
            }
        }
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= PollPrefs;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        PlayerPrefChangeNotifier.Cleanup();
        SaveChanges();
    }
    
    private new void SaveChanges()
    {
        // Save window state to persist settings between sessions
        EditorUtility.SetDirty(this);
    }

    private void SaveCategoryData()
    {
        categoryKeys.Clear();
        categoryValues.Clear();
        foreach (var kvp in prefCategories)
        {
            categoryKeys.Add(kvp.Key);
            categoryValues.Add(kvp.Value);
        }
        SaveChanges();
    }

    // Track last known values for change detection
    private Dictionary<string, string> lastKnownValues = new Dictionary<string, string>();
    private void PollPrefs()
    {
        if (EditorApplication.timeSinceStartup - lastCheckTime > 0.5f)
        {
            lastCheckTime = EditorApplication.timeSinceStartup;
            var keys = GetAllPlayerPrefKeys();
            var userKeys = GetUserKeyToRegistryKeyMap().Keys.ToList();
            currentPlayerPrefs = new HashSet<string>(userKeys);

            // Remove deleted PlayerPrefs from lastUpdatedTimes and lastKnownValues
            List<string> keysToRemove = new List<string>();
            foreach (var key in lastUpdatedTimes.Keys)
            {
                if (!currentPlayerPrefs.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                lastUpdatedTimes.Remove(key);
                lastKnownValues.Remove(key);
            }

            // Track if any value actually changed
            bool anyValueChanged = false;

            // Check for new or changed PlayerPrefs
            foreach (var key in userKeys)
            {
                string value = "";
                if (PlayerPrefs.HasKey(key))
                {
                    int i = PlayerPrefs.GetInt(key, int.MinValue);
                    float f = PlayerPrefs.GetFloat(key, float.MinValue);
                    string s = PlayerPrefs.GetString(key, "__NULL__");
                    if (i != int.MinValue) { value = i.ToString(); }
                    else if (f != float.MinValue) { value = f.ToString(); }
                    else if (s != "__NULL__") { value = s; }
                }

                if (!lastKnownValues.ContainsKey(key))
                {
                    // New PlayerPref found
                    lastKnownValues[key] = value;
                    lastUpdatedTimes[key] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    anyValueChanged = true;
                    
                    // Initialize tracking for update count
                    lastKnownValuesForCounting[key] = value;
                    if (!updateCounts.ContainsKey(key))
                        updateCounts[key] = 0;
                }
                else if (lastKnownValues[key] != value)
                {
                    // Value changed
                    lastKnownValues[key] = value;
                    lastUpdatedTimes[key] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    anyValueChanged = true;
                    
                    // Increment update count if value actually changed
                    if (!lastKnownValuesForCounting.ContainsKey(key) || lastKnownValuesForCounting[key] != value)
                    {
                        lastKnownValuesForCounting[key] = value;
                        if (updateCounts.ContainsKey(key))
                            updateCounts[key]++;
                        else
                            updateCounts[key] = 1;
                        
                        // Add to change history
                        if (!changeHistory.ContainsKey(key))
                            changeHistory[key] = new List<(string, string)>();
                        
                        string timestamp = DateTime.Now.ToString("HH:mm:ss");
                        changeHistory[key].Add((value, timestamp));
                        
                        // Keep only last 30 entries
                        if (changeHistory[key].Count > MAX_HISTORY_ENTRIES)
                            changeHistory[key].RemoveAt(0);
                    }
                }
            }

            // Remove from lastKnownValues if key deleted
            var knownKeysToRemove = lastKnownValues.Keys.Except(userKeys).ToList();
            foreach (var key in knownKeysToRemove)
            {
                lastKnownValues.Remove(key);
            }

            if (keys.Count != lastPrefsCount || anyValueChanged)
            {
                // Only refresh All tab when it's active to maintain selection
                if (currentTabIndex == 0)
                {
                    RefreshPlayerPrefsList();
                }
                if (currentTabIndex == 2)
                {
                    RefreshTab3ListView();
                }
                // Update Pin tab in real time if open
                if (currentTabIndex == 1)
                {
                    RefreshTab2ListView();
                }
                // Update Notifications tab in real time if open
                if (currentTabIndex == 4)
                {
                    RefreshNotificationsTab();
                }
                // Update Category tab in real time if open
                if (currentTabIndex == 5)
                {
                    RefreshCategoryTabView();
                }
                lastPrefsCount = keys.Count;
                SaveChanges();
            }
        }
    }

    private void RefreshCategoryTabView()
    {
        if (categoryTabView == null) return;

        var userToRawMap = GetUserKeyToRegistryKeyMap();
        var allItems = new List<(string key, string type, string value)>();

        foreach (var kvp in userToRawMap)
        {
            string userKey = kvp.Key;
            
            if (hideSeekTabView != null && hideSeekTabView.ShouldHideKey(userKey, "All"))
            {
                continue; // Apply All tab filters to category view too
            }

            string typeDesc = "Unknown";
            string valueStr = "";
            
            // Re-use logic to get type/value
            int iVal = PlayerPrefs.GetInt(userKey, int.MinValue);
            bool isInt = (iVal != int.MinValue) || PlayerPrefs.GetInt(userKey, int.MaxValue) == int.MinValue;
            if (isInt)
            {
                typeDesc = "int";
                valueStr = iVal.ToString();
            }
            else
            {
                float fVal = PlayerPrefs.GetFloat(userKey, float.MinValue);
                bool isFloat = (fVal != float.MinValue) || PlayerPrefs.GetFloat(userKey, float.MaxValue) == float.MinValue;
                if (isFloat)
                {
                    typeDesc = "float";
                    valueStr = fVal.ToString("F3");
                }
                else
                {
                    string sVal = PlayerPrefs.GetString(userKey, "\0");
                    bool isString = (sVal != "\0" || PlayerPrefs.GetString(userKey, " ") == "\0");
                    if (isString)
                    {
                        typeDesc = "string";
                        valueStr = sVal;
                    }
                }
            }

            allItems.Add((userKey, typeDesc, valueStr));
        }

        categoryTabView.Refresh(availableCategories, prefCategories, allItems);
    }

    // Helper to refresh Tab3 ListView (Last Updated)
    private void RefreshTab3ListView()
    {
        keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        var items = new List<(string key, string type, string value, string lastUpdated)>();
        foreach (var k in allKeys)
        {
            string type = "";
            string value = "";
            if (PlayerPrefs.HasKey(k))
            {
                int i = PlayerPrefs.GetInt(k, int.MinValue);
                float f = PlayerPrefs.GetFloat(k, float.MinValue);
                string s = PlayerPrefs.GetString(k, "__NULL__");
                if (i != int.MinValue) { type = "int"; value = i.ToString(); }
                else if (f != float.MinValue) { type = "float"; value = f.ToString(); }
                else if (s != "__NULL__") { type = "string"; value = s; }
            }
            string lastUpdated = lastUpdatedTimes.TryGetValue(k, out string time) ? time : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            items.Add((k, type, value, lastUpdated));
        }
        items.Sort((a, b) => DateTime.Parse(b.lastUpdated).CompareTo(DateTime.Parse(a.lastUpdated)));
        liveTabView.Refresh(items);
    }
    
    private string selectedTypeFilter = "All";
    private void RefreshPlayerPrefsList(string search = "")
    {
        keyNameToRegistryKey = GetUserKeyToRegistryKeyMap();
        var allKeys = new List<string>(keyNameToRegistryKey.Keys);
        
        // Filter out keys that should be hidden
        if (HidePlayerPrefContains != null && HidePlayerPrefContains.Length > 0 && hideSeekTabView != null)
            allKeys = allKeys.Where(k => !hideSeekTabView.ShouldHideKey(k, "All")).ToList();
            
        // Defensive: always use searchField.value for search
        string searchText = search;
        if (string.IsNullOrEmpty(searchText) && searchField != null)
            searchText = searchField.value;
       
        
        playerPrefKeys = allKeys.FindAll(k => {
            bool matchesSearch = string.IsNullOrEmpty(searchText) || k.ToLower().Contains(searchText.ToLower());
            if (!matchesSearch) return false;
            if (selectedTypeFilter == "All") return true;
            // Type detection
            if (PlayerPrefs.HasKey(k))
            {
                int i = PlayerPrefs.GetInt(k, int.MinValue);
                float f = PlayerPrefs.GetFloat(k, float.MinValue);
                string s = PlayerPrefs.GetString(k, "__NULL__");
                if (selectedTypeFilter == "int" && i != int.MinValue) return true;
                if (selectedTypeFilter == "float" && f != float.MinValue) return true;
                if (selectedTypeFilter == "string" && s != "__NULL__") return true;
            }
            return false;
        });
        
        leftPane.itemsSource = playerPrefKeys;
        leftPane.Rebuild(); 

        // Always update details panel after list changes
        // Defensive: check for splitView and detailsBox
        VisualElement rightPane = null;
        Box detailsBox = null;
        if (leftPane.parent is TwoPaneSplitView sv && sv.childCount > 1)
        {
            rightPane = sv[1] as VisualElement;
            if (rightPane != null && rightPane.childCount > 0)
            {
                detailsBox = rightPane[0] as Box;
            }
        }
        if (detailsBox != null && detailsBox.childCount >= 6)
        {
            var keyField = detailsBox[1] as TextField;
            var typeField = detailsBox[3] as TextField;
            var valueField = detailsBox[5] as TextField;
            
            // Try to restore previous selection if it still exists
            if (!string.IsNullOrEmpty(lastSelectedKey) && playerPrefKeys.Contains(lastSelectedKey))
            {
                int newIndex = playerPrefKeys.IndexOf(lastSelectedKey);
                leftPane.selectedIndex = newIndex;
                UpdateDetailsPanel(new List<object> { lastSelectedKey }, keyField, typeField, valueField);
            }
            else if (playerPrefKeys.Count > 0)
            {
                leftPane.selectedIndex = 0;
                lastSelectedKey = playerPrefKeys[0];
                UpdateDetailsPanel(new List<object> { playerPrefKeys[0] }, keyField, typeField, valueField);
            }
            else
            {
                lastSelectedKey = "";
                UpdateDetailsPanel(new List<object>(), keyField, typeField, valueField);
            }
        }
    }

    // Utility to get all PlayerPrefs keys (Editor only)
    private static List<string> GetAllPlayerPrefKeys()
    {
#if UNITY_EDITOR_WIN
        string regKey = @"Software\\Unity\\UnityEditor\\" + Application.companyName + "\\" + Application.productName;
        var keys = new List<string>();
        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKey))
            {
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        keys.Add(valueName);
                    }
                }
            }
        }
        catch { }
        return keys;
#else
        // On Mac/Linux, PlayerPrefs are stored in plist files, which is more complex to parse
        return new List<string>();
#endif
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
                        // Unity stores as: <userKey>_h<hash>
                        int hashIdx = valueName.LastIndexOf("_h");
                        if (hashIdx > 0)
                        {
                            string userKey = valueName.Substring(0, hashIdx);
                            if (!map.ContainsKey(userKey))
                                map.Add(userKey, valueName);
                        }
                        else
                        {
                            // fallback, add as is
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
        // On Mac/Linux, PlayerPrefs are stored in plist files, which is more complex to parse
        return new Dictionary<string, string>();
#endif
    }
    }
}
