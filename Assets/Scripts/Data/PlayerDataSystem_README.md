# Player Data Save/Load System Documentation

## Overview

This system provides a comprehensive save/load mechanism for persistent player data in your Feeding Frenzy-style game. The system consists of several components that work together to manage player progression, statistics, and preferences across game sessions.

## Components

### 1. PlayerData.cs
The core data structure that holds all persistent player information:

**Player Info:**
- `playerName`: Player's display name
- `lastSaveTime`: Timestamp of last save

**Game Progression:**
- `highestLevelReached`: Highest level the player has reached
- `totalExperienceEarned`: Total XP accumulated across all sessions
- `bestScore`: Best score achieved
- `totalPlayTime`: Total play time in seconds

**Game Statistics:**
- `totalFishEaten`: Total number of fish eaten
- `gamesPlayed`: Total number of games played
- `totalDeaths`: Total number of deaths
- `largestSizeAchieved`: Largest fish size achieved

**Level Progression:**
- `unlockedLevels`: Array of unlocked level names
- `completedLevels`: Array of completed level names

**Settings:**
- `preferredDifficulty`: Player's preferred difficulty (1-3)

### 2. PlayerDataManager.cs (Static)
The main static manager that handles all save/load operations:

#### Key Features:
- **Static Access**: Can be used from anywhere without instantiation
- **Dual Storage**: Saves to both file system and PlayerPrefs for redundancy
- **Auto-Initialization**: Initializes automatically when first accessed
- **Event System**: Provides events for data loaded/saved/deleted/changed
- **Data Validation**: Ensures data integrity on load
- **Comprehensive API**: Provides utility methods for common operations

#### Main Methods:
```csharp
// Basic Operations
PlayerDataManager.SavePlayerData();
PlayerDataManager.LoadPlayerData();
PlayerDataManager.DeletePlayerData();
PlayerDataManager.CreateNewPlayerData();

// Data Access
PlayerData data = PlayerDataManager.CurrentPlayerData;
bool hasSave = PlayerDataManager.HasSaveData;

// Data Updates
PlayerDataManager.UpdatePlayerData(data => {
    data.playerName = "NewName";
});

// Convenience Methods
PlayerDataManager.AddExperience(100f);
PlayerDataManager.UpdateBestScore(5000f);
PlayerDataManager.AddFishEaten(10);
PlayerDataManager.UnlockLevel("Level2");
PlayerDataManager.CompleteLevel("Level1");
```

### 3. PlayerDataHelper.cs (MonoBehaviour)
A MonoBehaviour wrapper that provides Unity-friendly integration:

#### Features:
- **Unity Events**: Exposes UnityEvents for designer integration
- **Inspector Integration**: Configure settings in the inspector
- **Auto-Save**: Automatically saves on focus loss/app pause
- **Game Integration**: Methods for common game events

#### Usage:
1. Add to a GameObject in your scene
2. Configure settings in the inspector
3. Connect UnityEvents to other systems
4. Call methods for game events:

```csharp
playerDataHelper.OnGameStart();
playerDataHelper.OnGameEnd(score, fishEaten, playTime, maxSize, level);
playerDataHelper.OnPlayerDeath();
playerDataHelper.OnLevelComplete("Level1");
```

### 4. GameSessionToPlayerDataBridge.cs
Bridges the gap between temporary session data and persistent player data:

#### Features:
- **Automatic Integration**: Connects GameDataManager with PlayerDataManager
- **Session Tracking**: Tracks game sessions and updates persistent data
- **Real-time Updates**: Optionally updates data during gameplay
- **Level Progression**: Handles level completion and unlocking

### 5. PlayerDataUI.cs
Example UI implementation for testing and demonstration:

#### Features:
- **Live Display**: Shows current player data in real-time
- **Test Controls**: Buttons to test all save/load functionality
- **Data Simulation**: Can simulate game sessions for testing
- **Debug Information**: Provides debug output and logging

## Setup Instructions

### Quick Setup:
1. **Add PlayerDataHelper to your scene:**
   - Create an empty GameObject
   - Add the `PlayerDataHelper` component
   - Configure auto-save settings in the inspector

2. **Initialize in your code:**
   ```csharp
   // The system auto-initializes, but you can force it:
   PlayerDataManager.Initialize();
   ```

3. **Save/Load operations:**
   ```csharp
   // Save current data
   PlayerDataManager.SavePlayerData();
   
   // Load data (automatically called on first access)
   PlayerDataManager.LoadPlayerData();
   
   // Delete save data
   PlayerDataManager.DeletePlayerData();
   ```

### Integration with Existing GameDataManager:
1. **Add GameSessionToPlayerDataBridge:**
   - Add the component to the same GameObject as GameDataManager
   - It will automatically connect the two systems

2. **Session Management:**
   ```csharp
   // The bridge automatically handles session start/end
   // But you can manually trigger:
   bridge.OnLevelCompleted("Level1");
   bridge.OnLevelUnlocked("Level2");
   ```

## File Locations

### Save File Location:
- **Path**: `Application.persistentDataPath + "/PlayerSave.json"`
- **Backup**: PlayerPrefs key "FeedingFrenzyPlayerData"

### Windows Example:
`C:/Users/[Username]/AppData/LocalLow/[CompanyName]/[ProductName]/PlayerSave.json`

## Events System

Subscribe to events for custom integration:

```csharp
// Subscribe to events
PlayerDataManager.OnPlayerDataLoaded += (data) => {
    Debug.Log($"Data loaded for {data.playerName}");
};

PlayerDataManager.OnPlayerDataSaved += (data) => {
    Debug.Log("Data saved successfully");
};

PlayerDataManager.OnPlayerDataChanged += (data) => {
    // Update UI, achievements, etc.
    UpdatePlayerUI(data);
};
```

## Common Use Cases

### 1. Game Session End:
```csharp
public void OnGameSessionEnd(float finalScore, int fishEaten, float playTime)
{
    PlayerDataManager.IncrementGamesPlayed();
    PlayerDataManager.UpdateBestScore(finalScore);
    PlayerDataManager.AddFishEaten(fishEaten);
    PlayerDataManager.AddPlayTime(playTime);
    PlayerDataManager.SavePlayerData();
}
```

### 2. Level Progression:
```csharp
public void OnLevelComplete(string levelName, int levelNumber)
{
    PlayerDataManager.CompleteLevel(levelName);
    PlayerDataManager.UpdateHighestLevel(levelNumber);
    
    // Unlock next level
    string nextLevel = $"Level{levelNumber + 1}";
    PlayerDataManager.UnlockLevel(nextLevel);
}
```

### 3. Player Death:
```csharp
public void OnPlayerDied()
{
    PlayerDataManager.IncrementDeaths();
    // Data is automatically saved due to auto-save
}
```

### 4. Settings Management:
```csharp
public void OnDifficultyChanged(int difficulty)
{
    PlayerDataManager.SetPreferredDifficulty(difficulty);
}

public void OnPlayerNameChanged(string newName)
{
    PlayerDataManager.SetPlayerName(newName);
}
```

## Testing

### Using PlayerDataUI:
1. Create a Canvas in your scene
2. Add the `PlayerDataUI` component
3. Assign UI elements in the inspector
4. Use the buttons to test save/load functionality

### Manual Testing:
```csharp
// Test save/load cycle
PlayerDataManager.UpdatePlayerData(data => {
    data.playerName = "TestPlayer";
    data.bestScore = 10000f;
});

PlayerDataManager.SavePlayerData();
PlayerDataManager.DeletePlayerData(); // Reset
PlayerDataManager.LoadPlayerData(); // Should load saved data

Debug.Log(PlayerDataManager.CurrentPlayerData.playerName); // Should be "TestPlayer"
```

## Error Handling

The system includes comprehensive error handling:
- **File I/O Errors**: Falls back to PlayerPrefs
- **JSON Parsing Errors**: Creates new default data
- **Data Validation**: Ensures data integrity on load
- **Null Checks**: Protects against null references

## Performance Considerations

- **Lazy Loading**: Data is only loaded when first accessed
- **Caching**: Data is cached in memory after loading
- **Minimal I/O**: Only saves when necessary
- **Background Saving**: Saves on app focus loss/pause

## Security Notes

- Save files are stored in JSON format (human-readable)
- No encryption is implemented (add if needed for competitive games)
- PlayerPrefs backup provides additional data safety
- Data validation prevents most tampering issues

## Extending the System

### Adding New Data Fields:
1. Add the field to `PlayerData.cs`
2. Update the `ResetToDefaults()` method
3. Add validation in `PlayerDataManager.ValidatePlayerData()`
4. Create convenience methods in `PlayerDataManager` if needed

### Custom Events:
```csharp
// Add custom events to PlayerDataManager
public static event Action<float> OnScoreUpdated;

// Trigger in update methods
OnScoreUpdated?.Invoke(newScore);
```

### Custom Save Locations:
Modify the `SAVE_FILE_NAME` constant in `PlayerDataManager.cs` or create custom save paths for different profiles.
