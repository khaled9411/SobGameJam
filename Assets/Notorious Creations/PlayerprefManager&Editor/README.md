## Installation

1. Copy the `Notorious Creations` folder into your Unity project's `Assets` directory
2. The tool will automatically appear in Unity's menu system
3. Access via **Tools → Notorious Creations → PlayerPrefs Editor**


# PlayerPrefs Editor & Manager

A comprehensive Unity Editor tool for managing, monitoring, and organizing PlayerPrefs with advanced features including real-time notifications, filtering, and data export/import capabilities.

## Overview

The PlayerPrefs Editor & Manager is a powerful Unity Editor window that provides a complete solution for working with Unity's PlayerPrefs system. It offers multiple views and features to help developers efficiently manage game settings, user preferences, and persistent data during development and testing.

## Features

### 🔍 **All Tab - Complete Overview**
- View all PlayerPrefs in your project with their types and values
- Search functionality with real-time filtering
- Type-based filtering (int, float, string, or all)
- Alternating row colors for better readability
- Bulk operations support

### 📌 **Pin Tab - Favorite Management**
- Pin frequently used PlayerPrefs for quick access
- Persistent pinning across Unity sessions
- Toggle pin/unpin with star indicators
- Organized view of only your most important settings

### ⚡ **Live Tab - Real-time Monitoring** (Alpha)
- Monitor PlayerPrefs changes in real-time
- Sort by last updated time
- Track when values were modified
- Ideal for debugging and testing scenarios

### 🔒 **Hide & Seek Tab - Advanced Filtering**
- Create custom hide filters based on string patterns
- Exception lists for specific keys you want to keep visible
- Tab-specific hiding (configure which tabs show/hide filtered items)
- Multiple filter rules with individual controls
- Perfect for hiding Unity/third-party library PlayerPrefs

### 🔔 **Notifications Tab - Change Tracking**
- Enable notifications for specific PlayerPrefs
- Real-time change detection with visual alerts
- Sliding notification windows from top-right corner
- Track value changes, deletions, and new additions
- Persistent tracking across Unity sessions

## Installation

1. Copy the `Notorious Creations` folder into your Unity project's `Assets` directory
2. The tool will automatically appear in Unity's menu system
3. Access via **Tools → Notorious Creations → PlayerPrefs Editor**

## Usage

### Opening the Tool

Navigate to **Tools → Notorious Creations → PlayerPrefs Editor** in Unity's menu bar.

### Navigation

The tool features a tabbed interface at the top:
- **All**: Browse all PlayerPrefs
- **Pin**: View pinned favorites
- **Live**: Real-time monitoring (Alpha feature)
- **Hide & Seek**: Configure filtering rules
- **Notifications**: Set up change tracking

### Basic Operations

#### Viewing PlayerPrefs
- All PlayerPrefs are displayed with their key names, types (int/float/string), and current values
- Use the search field to filter by key name
- Select type filter dropdown to show only specific data types

#### Creating Test Data
- Use the "Create Test Prefs" button to generate sample PlayerPrefs for testing
- Creates `test_string`, `test_int`, and `test_float` entries

#### Bulk Operations
- "Delete All Prefs" button removes all PlayerPrefs (with confirmation dialog)
- Export/Import functionality for backing up and restoring PlayerPrefs

### Advanced Features

#### Pinning System
1. Navigate to any tab showing PlayerPrefs
2. Click the star icon (☆) next to any PlayerPref to pin it
3. Pinned items (★) appear in the Pin tab for quick access
4. Click again to unpin

#### Setting Up Notifications
1. Go to the **Notifications** tab
2. Check the box next to any PlayerPref you want to monitor
3. When that PlayerPref changes, a notification window will slide in from the top-right
4. Notifications persist across Unity sessions

#### Configuring Hide Filters
1. Open the **Hide & Seek** tab
2. Click "Add Hide Filter" to create a new filter rule
3. Enter a string pattern that keys should contain to be hidden
4. Configure which tabs the filter applies to
5. Add exceptions for specific keys you want to keep visible
6. Use "Remove" to delete individual filters or "Clear All Filters" to reset

#### Data Export/Import
- **Export**: Creates a JSON file with all current PlayerPrefs
- **Import**: Restores PlayerPrefs from a previously exported JSON file
- Useful for backing up settings or transferring between projects

## Code Architecture

**Namespace:** All classes are contained within the `NotoriousCreations.PlayerPrefsEditor` namespace to prevent naming conflicts with other packages.

### Core Components

#### `PlayerPrefsEditorWindow.cs`
- Main editor window and UI controller
- Manages tab switching and overall application state
- Handles PlayerPrefs polling and UI refresh logic
- Coordinates between different tab views

#### `AllTabView.cs`
- Displays complete list of PlayerPrefs
- Handles search and filtering functionality
- Provides base ListView implementation used by other tabs

#### `PinTabView.cs`
- Manages pinned PlayerPrefs display
- Handles pin/unpin toggle functionality
- Maintains persistent pinning state

#### `LiveTabView.cs`
- Real-time monitoring of PlayerPrefs changes
- Tracks and displays last updated timestamps
- Sorts entries by modification time

#### `HideSeekTabView.cs`
- Advanced filtering configuration interface
- Manages multiple hide filter rules
- Handles exceptions and tab-specific filtering

#### `NotificationsTabView.cs`
- Notification tracking configuration UI
- Displays tracking status for each PlayerPref
- Manages notification toggle states

#### `PlayerPrefChangeNotifier.cs`
- Background monitoring system for PlayerPrefs changes
- Handles notification triggering and state management
- Persistent tracking configuration storage

#### `PlayerPrefNotificationWindow.cs`
- Custom notification popup window
- Animated slide-in from top-right corner
- Displays change details (old value → new value)

#### `PlayerPrefsExporter.cs`
- JSON export functionality
- Registry key mapping for Windows platforms
- Data serialization and file operations

#### `PlayerPrefsImporter.cs`
- JSON import functionality
- Data validation and error handling
- Type-safe PlayerPrefs restoration

### Data Models

#### `HidePlayerPrefItem`
- Filter rule configuration
- String patterns and exceptions
- Tab-specific visibility settings

#### `PlayerPrefNotificationData`
- Notification tracking state
- Last known values for change detection
- Persistence support

## Platform Support

- **Windows**: Full support including registry-based PlayerPrefs detection

## Technical Details

### PlayerPrefs Detection
The tool uses platform-specific methods to detect PlayerPrefs:
- **Windows**: Reads from Windows Registry 

### Performance Optimization
- Efficient polling system (0.5-second intervals) for change detection
- UI virtualization for large PlayerPrefs lists
- Lazy loading and caching of PlayerPrefs data

### Data Persistence
- Window state (current tab, pinned items) persists across Unity sessions
- Notification tracking settings are stored in EditorPrefs
- Hide filter configurations are serialized with the editor window


### Common Issues

**PlayerPrefs not appearing:**
- Ensure you have PlayerPrefs created in your project
- Use "Create Test Prefs" to verify the tool is working
- Check if hide filters are affecting visibility

**Notifications not working:**
- Verify the notification system is initialized (automatic on tool startup)
- Check that the PlayerPref key still exists
- Restart Unity if notification tracking seems stuck

**Performance issues:**
- Consider using hide filters to reduce the number of displayed PlayerPrefs
- The Live tab may impact performance with very large numbers of PlayerPrefs

### Debug Information
The tool includes debug logging for notification system events. Check Unity's Console for detailed information if issues occur.

## Version Information

- **Current Version**: Latest
- **Compatibility**: Unity 2020.3 LTS and newer
- **Status**: Live Tab is in Alpha (may have some instability)

## Support

For issues, feature requests, or contributions, please refer to the project documentation or contact the development team.

---

*Developed by Notorious Creations - Enhancing Unity development workflows*
