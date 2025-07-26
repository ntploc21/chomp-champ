using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MonoBehaviour helper class for PlayerDataManager integration.
/// Provides Unity Events and inspector-friendly methods for designers.
/// This class can be placed on GameObjects to easily integrate with the static PlayerDataManager.
/// </summary>
public class PlayerDataHelper : MonoBehaviour
{
  #region Inspector Events
  [Header("Player Data Events")]
  [Tooltip("Called when player data is loaded.")]
  public UnityEvent<PlayerData> OnPlayerDataLoaded = new UnityEvent<PlayerData>();

  [Tooltip("Called when player data is saved.")]
  public UnityEvent<PlayerData> OnPlayerDataSaved = new UnityEvent<PlayerData>();

  [Tooltip("Called when player data is deleted.")]
  public UnityEvent OnPlayerDataDeleted = new UnityEvent();

  [Tooltip("Called when player data changes.")]
  public UnityEvent<PlayerData> OnPlayerDataChanged = new UnityEvent<PlayerData>();

  [Header("Settings")]
  [Tooltip("Auto-initialize PlayerDataManager on Awake.")]
  [SerializeField] private bool autoInitialize = true;

  [Tooltip("Auto-save when the application loses focus or pauses.")]
  [SerializeField] private bool autoSaveOnFocusLoss = true;

  [Tooltip("Enable debug logging for player data operations.")]
  [SerializeField] private bool enableDebugLogging = false;
  #endregion

  #region Properties
  /// <summary>
  /// Quick access to current player data.
  /// </summary>
  public PlayerData CurrentPlayerData => PlayerDataManager.CurrentPlayerData;

  /// <summary>
  /// Quick access to whether save data exists.
  /// </summary>
  public bool HasSaveData => PlayerDataManager.HasSaveData;
  #endregion

  #region Unity Events
  private void Awake()
  {
    if (autoInitialize)
    {
      InitializePlayerData();
    }
  }

  private void OnEnable()
  {
    // Subscribe to PlayerDataManager events
    PlayerDataManager.OnPlayerDataLoaded += HandlePlayerDataLoaded;
    PlayerDataManager.OnPlayerDataSaved += HandlePlayerDataSaved;
    PlayerDataManager.OnPlayerDataDeleted += HandlePlayerDataDeleted;
    PlayerDataManager.OnPlayerDataChanged += HandlePlayerDataChanged;
  }

  private void OnDisable()
  {
    // Unsubscribe from PlayerDataManager events
    PlayerDataManager.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
    PlayerDataManager.OnPlayerDataSaved -= HandlePlayerDataSaved;
    PlayerDataManager.OnPlayerDataDeleted -= HandlePlayerDataDeleted;
    PlayerDataManager.OnPlayerDataChanged -= HandlePlayerDataChanged;
  }

  private void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus && autoSaveOnFocusLoss)
    {
      SavePlayerData();
    }
  }

  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus && autoSaveOnFocusLoss)
    {
      SavePlayerData();
    }
  }

  private void OnDestroy()
  {
    // Save data when this object is destroyed (e.g., scene change)
    if (autoSaveOnFocusLoss)
    {
      SavePlayerData();
    }
  }
  #endregion

  #region Event Handlers
  private void HandlePlayerDataLoaded(PlayerData playerData)
  {
    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Player data loaded for {playerData.playerName}");

    OnPlayerDataLoaded?.Invoke(playerData);
  }

  private void HandlePlayerDataSaved(PlayerData playerData)
  {
    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Player data saved for {playerData.playerName}");

    OnPlayerDataSaved?.Invoke(playerData);
  }

  private void HandlePlayerDataDeleted()
  {
    if (enableDebugLogging)
      Debug.Log("PlayerDataHelper: Player data deleted");

    OnPlayerDataDeleted?.Invoke();
  }

  private void HandlePlayerDataChanged(PlayerData playerData)
  {
    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Player data changed for {playerData.playerName}");

    OnPlayerDataChanged?.Invoke(playerData);
  }
  #endregion

  #region Public Methods - Basic Operations
  /// <summary>
  /// Initializes the player data system.
  /// </summary>
  [ContextMenu("Initialize Player Data")]
  public void InitializePlayerData()
  {
    PlayerDataManager.Initialize();

    if (enableDebugLogging)
      Debug.Log("PlayerDataHelper: Initialized player data system");
  }

  /// <summary>
  /// Saves the current player data.
  /// </summary>
  [ContextMenu("Save Player Data")]
  public void SavePlayerData()
  {
    bool success = PlayerDataManager.SavePlayerData();

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Save operation {(success ? "successful" : "failed")}");
  }

  /// <summary>
  /// Loads player data from storage.
  /// </summary>
  [ContextMenu("Load Player Data")]
  public void LoadPlayerData()
  {
    bool loadedFromSave = PlayerDataManager.LoadPlayerData();

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Data loaded from {(loadedFromSave ? "save file" : "defaults")}");
  }

  /// <summary>
  /// Deletes all player data and resets to defaults.
  /// </summary>
  [ContextMenu("Delete Player Data")]
  public void DeletePlayerData()
  {
    bool success = PlayerDataManager.DeletePlayerData();

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Delete operation {(success ? "successful" : "failed")}");
  }

  /// <summary>
  /// Creates new default player data.
  /// </summary>
  [ContextMenu("Create New Player Data")]
  public void CreateNewPlayerData()
  {
    PlayerDataManager.CreateNewPlayerData();

    if (enableDebugLogging)
      Debug.Log("PlayerDataHelper: Created new player data");
  }
  #endregion

  #region Public Methods - Game Integration
  /// <summary>
  /// Called when a game session starts.
  /// </summary>
  public void OnGameStart()
  {
    PlayerDataManager.IncrementGamesPlayed();

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Game started. Total games: {CurrentPlayerData.gamesPlayed}");
  }

  /// <summary>
  /// Called when a game session ends with score and stats.
  /// </summary>
  public void OnGameEnd(string levelName, float score, int fishEaten, float playTime, float maxSize, int level)
  {
    PlayerDataManager.UpdateBestScore(levelName, score);
    PlayerDataManager.AddFishEaten(fishEaten);
    PlayerDataManager.AddPlayTime(playTime);
    PlayerDataManager.UpdateHighestLevel(level);

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Game ended. Score: {score}, Fish: {fishEaten}, Time: {playTime:F1}s");
  }

  /// <summary>
  /// Called when player dies.
  /// </summary>
  public void OnPlayerDeath()
  {
    PlayerDataManager.IncrementDeaths();

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Player died. Total deaths: {CurrentPlayerData.totalDeaths}");
  }

  /// <summary>
  /// Called when a level is completed.
  /// </summary>
  public void OnLevelComplete(string levelName)
  {
    PlayerDataManager.CompleteLevel(levelName);

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Level completed: {levelName}");
  }

  /// <summary>
  /// Called when a new level is unlocked.
  /// </summary>
  public void OnLevelUnlock(string levelName)
  {
    PlayerDataManager.UnlockLevel(levelName);

    if (enableDebugLogging)
      Debug.Log($"PlayerDataHelper: Level unlocked: {levelName}");
  }
  #endregion

  #region Public Methods - Data Queries
  /// <summary>
  /// Checks if a level is unlocked.
  /// </summary>
  public bool IsLevelUnlocked(string levelName)
  {
    return PlayerDataManager.IsLevelUnlocked(levelName);
  }

  /// <summary>
  /// Checks if a level is completed.
  /// </summary>
  public bool IsLevelCompleted(string levelName)
  {
    return PlayerDataManager.IsLevelCompleted(levelName);
  }

  /// <summary>
  /// Gets the completion percentage (0-1).
  /// </summary>
  public float GetCompletionPercentage()
  {
    return PlayerDataManager.GetCompletionPercentage();
  }

  /// <summary>
  /// Gets formatted play time string.
  /// </summary>
  public string GetFormattedPlayTime()
  {
    float totalSeconds = CurrentPlayerData.totalPlayTime;
    int hours = (int)(totalSeconds / 3600);
    int minutes = (int)((totalSeconds % 3600) / 60);
    int seconds = (int)(totalSeconds % 60);

    if (hours > 0)
      return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    else
      return $"{minutes:D2}:{seconds:D2}";
  }

  /// <summary>
  /// Gets formatted best score string.
  /// </summary>
  public string GetFormattedBestScore(string levelName)
  {
    if (CurrentPlayerData.bestScore.TryGetValue(levelName, out float score))
    {
      return score.ToString("N0");
    }
    return "0";
  }
  #endregion

  #region Debug Methods
  /// <summary>
  /// Prints debug information about player data.
  /// </summary>
  [ContextMenu("Print Debug Info")]
  public void PrintDebugInfo()
  {
    Debug.Log(PlayerDataManager.GetDebugInfo());
  }

  /// <summary>
  /// Logs current player statistics.
  /// </summary>
  [ContextMenu("Log Player Stats")]
  public void LogPlayerStats()
  {
    var data = CurrentPlayerData;
    Debug.Log($"Player Stats:\n" +
             $"Name: {data.playerName}\n" +
             $"Highest Level: {data.highestLevelReached}\n" +
             $"Best Score: {data.bestScore:N0}\n" +
             $"Total XP: {data.totalExperienceEarned:N0}\n" +
             $"Play Time: {GetFormattedPlayTime()}\n" +
             $"Fish Eaten: {data.totalFishEaten:N0}\n" +
             $"Games Played: {data.gamesPlayed}\n" +
             $"Deaths: {data.totalDeaths}\n" +
             $"Completion: {GetCompletionPercentage():P1}");
  }
  #endregion

  #region Utility Methods
  /// <summary>
  /// Sets the player name with validation.
  /// </summary>
  public void SetPlayerName(string playerName)
  {
    PlayerDataManager.SetPlayerName(playerName);
  }

  /// <summary>
  /// Adds experience points to the player's total.
  /// </summary>
  public void AddExperience(float amount)
  {
    PlayerDataManager.AddExperience(amount);
  }

  /// <summary>
  /// Updates player data with a custom action.
  /// </summary>
  public void UpdatePlayerData(System.Action<PlayerData> updateAction)
  {
    PlayerDataManager.UpdatePlayerData(updateAction, autoSaveOnFocusLoss);
  }
  #endregion
}
