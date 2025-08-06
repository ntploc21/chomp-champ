using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Static manager for handling persistent player data operations.
/// Provides save/load/delete functionality and data management utilities.
/// This manager handles player progression data that persists between game sessions.
/// </summary>
public static class PlayerDataManager
{
  #region Constants & Settings
  private const string SAVE_FILE_NAME = "PlayerSave.json";
  private const string SAVE_KEY = "FeedingFrenzyPlayerData";
  private const int SAVE_VERSION = 1;
  #endregion

  #region Static Data
  private static PlayerData _cachedPlayerData;
  private static bool _isInitialized = false;
  private static string _saveFilePath;
  #endregion

  #region Events
  public static event Action<PlayerData> OnPlayerDataLoaded;
  public static event Action<PlayerData> OnPlayerDataSaved;
  public static event Action OnPlayerDataDeleted;
  public static event Action<PlayerData> OnPlayerDataChanged;
  #endregion

  #region Properties
  /// <summary>
  /// Gets the current player data. Initializes if not already loaded.
  /// </summary>
  public static PlayerData CurrentPlayerData
  {
    get
    {
      if (!_isInitialized)
      {
        Initialize();
      }
      return _cachedPlayerData;
    }
  }

  /// <summary>
  /// Gets whether player data exists (either cached or saved).
  /// </summary>
  public static bool HasSaveData
  {
    get
    {
      Initialize();
      return _cachedPlayerData != null && (HasSaveFile() || HasPlayerPrefsData());
    }
  }

  /// <summary>
  /// Gets the save file path.
  /// </summary>
  public static string SaveFilePath => _saveFilePath;
  #endregion

  #region Initialization
  /// <summary>
  /// Initializes the PlayerDataManager. Called automatically when accessing CurrentPlayerData.
  /// </summary>
  public static void Initialize()
  {
    if (_isInitialized) return;

    _saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    LoadPlayerData();
    _isInitialized = true;

    Debug.Log($"PlayerDataManager initialized. Save path: {_saveFilePath}");
  }

  /// <summary>
  /// Forces reinitialization of the manager.
  /// </summary>
  public static void Reinitialize()
  {
    _isInitialized = false;
    _cachedPlayerData = null;
    Initialize();
  }
  #endregion

  #region Save/Load Operations
  /// <summary>
  /// Saves the current player data to both file and PlayerPrefs.
  /// </summary>
  /// <param name="playerData">Player data to save. If null, uses current cached data.</param>
  /// <returns>True if save was successful, false otherwise.</returns>
  public static bool SavePlayerData(PlayerData playerData = null)
  {
    try
    {
      if (playerData != null)
      {
        _cachedPlayerData = playerData;
      }

      if (_cachedPlayerData == null)
      {
        Debug.LogError("No player data to save!");
        return false;
      }

      // Update save timestamp
      _cachedPlayerData.UpdateSaveTime();

      // Create save wrapper with version info
      SaveDataWrapper saveWrapper = new SaveDataWrapper
      {
        version = SAVE_VERSION,
        playerData = _cachedPlayerData,
        saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
      };

      string jsonData = JsonConvert.SerializeObject(saveWrapper, Formatting.Indented);

      // Save to file
      File.WriteAllText(_saveFilePath, jsonData);

      // Save to PlayerPrefs as backup
      PlayerPrefs.SetString(SAVE_KEY, jsonData);
      PlayerPrefs.Save();

      OnPlayerDataSaved?.Invoke(_cachedPlayerData);
      Debug.Log($"Player data saved successfully to {_saveFilePath}");
      return true;
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to save player data: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Loads player data from file or PlayerPrefs, with fallback to default values.
  /// </summary>
  /// <returns>True if data was loaded from save, false if using default values.</returns>
  public static bool LoadPlayerData()
  {
    try
    {
      string jsonData = null;

      // Try to load from file first
      if (HasSaveFile())
      {
        jsonData = File.ReadAllText(_saveFilePath);
        Debug.Log("Loading player data from file...");
      }
      // Fallback to PlayerPrefs (optional, but not recommended)
      // else if (HasPlayerPrefsData())
      // {
      //   jsonData = PlayerPrefs.GetString(SAVE_KEY);
      //   Debug.Log("Loading player data from PlayerPrefs...");
      // }

      // If we have data to load
      if (!string.IsNullOrEmpty(jsonData))
      {
        SaveDataWrapper saveWrapper = JsonConvert.DeserializeObject<SaveDataWrapper>(jsonData);

        if (saveWrapper != null && saveWrapper.playerData != null)
        {
          _cachedPlayerData = saveWrapper.playerData;
          ValidatePlayerData();

          OnPlayerDataLoaded?.Invoke(_cachedPlayerData);
          Debug.Log($"Player data loaded successfully. Version: {saveWrapper.version}, Last save: {saveWrapper.saveTimestamp}");
          return true;
        }
      }

      // No valid save data found, create new
      CreateNewPlayerData();
      return false;
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to load player data: {ex.Message}. Creating new save data.");
      CreateNewPlayerData();
      return false;
    }
  }

  /// <summary>
  /// Creates new default player data.
  /// </summary>
  public static void CreateNewPlayerData()
  {
    _cachedPlayerData = new PlayerData();
    OnPlayerDataLoaded?.Invoke(_cachedPlayerData);

    // Save the new player data immediately
    SavePlayerData();
    Debug.Log("Created new player data with default values.");
  }

  /// <summary>
  /// Deletes all saved player data and resets to defaults.
  /// </summary>
  /// <returns>True if deletion was successful.</returns>
  public static bool DeletePlayerData()
  {
    try
    {
      // Delete save file
      if (HasSaveFile())
      {
        File.Delete(_saveFilePath);
      }

      // Delete PlayerPrefs data
      if (HasPlayerPrefsData())
      {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
      }

      // Reset cached data
      CreateNewPlayerData();
      

      OnPlayerDataDeleted?.Invoke();
      Debug.Log("Player data deleted successfully.");
      return true;
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to delete player data: {ex.Message}");
      return false;
    }
  }
  #endregion

  #region Data Management Utilities
  /// <summary>
  /// Updates a specific field in the player data and triggers save.
  /// </summary>
  /// <param name="updateAction">Action to perform on the player data.</param>
  /// <param name="autoSave">Whether to automatically save after update.</param>
  public static void UpdatePlayerData(Action<PlayerData> updateAction, bool autoSave = true)
  {
    if (_cachedPlayerData == null)
    {
      Debug.LogError("No player data loaded!");
      return;
    }

    updateAction?.Invoke(_cachedPlayerData);

    OnPlayerDataChanged?.Invoke(_cachedPlayerData);

    if (autoSave)
    {
      SavePlayerData();
    }
  }

  /// <summary>
  /// Adds experience points to the player's total.
  /// </summary>
  public static void AddExperience(float amount)
  {
    UpdatePlayerData(data => data.totalExperienceEarned += amount);
  }

  /// <summary>
  /// Updates the best score if the new score is higher.
  /// </summary>
  public static void UpdateBestScore(string levelName, float score)
  {
    UpdatePlayerData(data =>
    {
      if (!data.bestScore.ContainsKey(levelName))
      {
        data.bestScore[levelName] = 0f; // Initialize if not exists
      }

      if (score > data.bestScore[levelName])
      {
        data.bestScore[levelName] = score;
      }
    });
  }

  /// <summary>
  /// Increments the current level.
  /// </summary>
  public static void IncrementCurrentLevel()
  {
    UpdatePlayerData(data => data.currentLevel++);
  }

  /// <summary>
  /// Adds play time to the total.
  /// </summary>
  public static void AddPlayTime(float seconds)
  {
    UpdatePlayerData(data => data.totalPlayTime += seconds);
  }

  /// <summary>
  /// Increments the number of fish eaten.
  /// </summary>
  public static void AddFishEaten(int count = 1)
  {
    UpdatePlayerData(data => data.totalFishEaten += count);
  }

  /// <summary>
  /// Increments the number of games played.
  /// </summary>
  public static void IncrementGamesPlayed()
  {
    UpdatePlayerData(data => data.gamesPlayed++);
  }

  /// <summary>
  /// Increments the number of deaths.
  /// </summary>
  public static void IncrementDeaths()
  {
    UpdatePlayerData(data => data.totalDeaths++);
  }

  /// <summary>
  /// Unlocks a new level if not already unlocked.
  /// </summary>
  public static void UnlockLevel(string levelName)
  {
    UpdatePlayerData(data =>
    {
      if (!data.unlockedLevels.Contains(levelName))
      {
        var levelsList = data.unlockedLevels.ToList();
        levelsList.Add(levelName);
        data.unlockedLevels = levelsList.ToArray();
      }
    });
  }

  /// <summary>
  /// Marks a level as completed if not already completed.
  /// </summary>
  public static void CompleteLevel(string levelName)
  {
    UpdatePlayerData(data =>
    {
      if (!data.completedLevels.Contains(levelName))
      {
        var levelsList = data.completedLevels.ToList();
        levelsList.Add(levelName);
        data.completedLevels = levelsList.ToArray();
      }
    });
  }

  /// <summary>
  /// Sets the player's name.
  /// </summary>
  public static void SetPlayerName(string name)
  {
    UpdatePlayerData(data => data.playerName = string.IsNullOrEmpty(name) ? "Player" : name);
  }

  /// <summary>
  /// Add the currency balance to the player data.
  /// </summary>
  public static void AddCurrencyBalance(int amount)
  {
    UpdatePlayerData(data => data.currencyBalance += amount);
  }
  #endregion

  #region Query Methods
  /// <summary>
  /// Checks if a level is unlocked.
  /// </summary>
  public static bool IsLevelUnlocked(string levelName)
  {
    return _cachedPlayerData?.unlockedLevels?.Contains(levelName) ?? false;
  }

  /// <summary>
  /// Checks if a level is completed.
  /// </summary>
  public static bool IsLevelCompleted(string levelName)
  {
    return _cachedPlayerData?.completedLevels?.Contains(levelName) ?? false;
  }

  /// <summary>
  /// Gets the completion percentage (0-1) based on completed levels vs unlocked levels.
  /// </summary>
  public static float GetCompletionPercentage()
  {
    if (_cachedPlayerData?.unlockedLevels == null || _cachedPlayerData.unlockedLevels.Length == 0)
      return 0f;

    int completedCount = _cachedPlayerData.completedLevels?.Length ?? 0;
    return (float)completedCount / _cachedPlayerData.unlockedLevels.Length;
  }

  /// <summary>
  /// Gets the list of unlocked levels.
  /// </summary>
  public static List<string> GetUnlockedLevels()
  {
    return _cachedPlayerData?.unlockedLevels?.ToList() ?? new List<string>();
  }

  /// <summary>
  /// Gets the total number of unlocked levels.
  /// </summary>
  public static int GetUnlockedLevelCount()
  {
    return _cachedPlayerData?.unlockedLevels?.Length ?? 0;
  }

  /// <summary>
  /// Gets the total number of completed levels.
  /// </summary>
  public static int GetCompletedLevelCount()
  {
    return _cachedPlayerData?.completedLevels?.Length ?? 0;
  }

  /// <summary>
  /// Get the player's achievements.
  /// </summary>
  public static List<string> GetAchievements()
  {
    return _cachedPlayerData?.achievements ?? new List<string>();
  }
  #endregion

  #region Validation & Utilities
  /// <summary>
  /// Validates and fixes any invalid data in the player data.
  /// </summary>
  private static void ValidatePlayerData()
  {
    if (_cachedPlayerData == null) return;

    // Ensure values are non-negative
    _cachedPlayerData.currentLevel = Mathf.Max(1, _cachedPlayerData.currentLevel);
    _cachedPlayerData.totalExperienceEarned = Mathf.Max(0f, _cachedPlayerData.totalExperienceEarned);
    _cachedPlayerData.totalPlayTime = Mathf.Max(0f, _cachedPlayerData.totalPlayTime);
    _cachedPlayerData.totalFishEaten = Mathf.Max(0, _cachedPlayerData.totalFishEaten);
    _cachedPlayerData.gamesPlayed = Mathf.Max(0, _cachedPlayerData.gamesPlayed);
    _cachedPlayerData.totalDeaths = Mathf.Max(0, _cachedPlayerData.totalDeaths);

    // Ensure arrays are not null
    if (_cachedPlayerData.unlockedLevels == null)
      _cachedPlayerData.unlockedLevels = new string[] { "C0L1" };
    if (_cachedPlayerData.completedLevels == null)
      _cachedPlayerData.completedLevels = new string[0];

    // Ensure player name is not empty
    if (string.IsNullOrEmpty(_cachedPlayerData.playerName))
      _cachedPlayerData.playerName = "Player";
  }

  /// <summary>
  /// Checks if a save file exists.
  /// </summary>
  private static bool HasSaveFile()
  {
    return !string.IsNullOrEmpty(_saveFilePath) && File.Exists(_saveFilePath);
  }

  /// <summary>
  /// Checks if PlayerPrefs contains save data.
  /// </summary>
  private static bool HasPlayerPrefsData()
  {
    return PlayerPrefs.HasKey(SAVE_KEY);
  }

  /// <summary>
  /// Gets debug information about the current save state.
  /// </summary>
  public static string GetDebugInfo()
  {
    return $"PlayerDataManager Debug Info:\n" +
           $"Initialized: {_isInitialized}\n" +
           $"Has Save File: {HasSaveFile()}\n" +
           $"Has PlayerPrefs Data: {HasPlayerPrefsData()}\n" +
           $"Save Path: {_saveFilePath}\n" +
           $"Player Name: {_cachedPlayerData?.playerName}\n" +
           $"Current Level: {_cachedPlayerData?.currentLevel}\n" +
           $"Best Score: {_cachedPlayerData?.bestScore}\n" +
           $"Total Play Time: {_cachedPlayerData?.totalPlayTime:F1}s";
  }
  #endregion

  #region Save Data Wrapper
  [Serializable]
  private class SaveDataWrapper
  {
    public int version;
    public string saveTimestamp;
    public PlayerData playerData;
  }
  #endregion
}
