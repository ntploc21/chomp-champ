using UnityEngine;

/// <summary>
/// Example integration class showing how to connect GameSessionData with PlayerData.
/// This class handles the flow of data from temporary session data to persistent player data.
/// </summary>
public class GameSessionToPlayerDataBridge : MonoBehaviour
{
  #region Inspector Fields
  [Header("Components")]
  [Tooltip("Reference to the GameDataManager for session data.")]
  [SerializeField] private GameDataManager gameDataManager;

  [Tooltip("Reference to the PlayerDataHelper for persistent data.")]
  [SerializeField] private PlayerDataHelper playerDataHelper;

  [Header("Settings")]
  [Tooltip("Automatically save player data when session ends.")]
  [SerializeField] private bool autoSaveOnSessionEnd = true;

  [Tooltip("Update player data during gameplay (real-time).")]
  [SerializeField] private bool updateDuringGameplay = false;

  [Tooltip("Minimum time between real-time updates (seconds).")]
  [SerializeField] private float updateInterval = 10f;
  #endregion

  #region Private Fields
  private float lastUpdateTime = 0f;
  private bool sessionStarted = false;
  private float sessionStartTime = 0f;
  private int sessionStartFishCount = 0;
  #endregion

  #region Unity Events
  private void Awake()
  {
    // Auto-find components if not assigned
    if (gameDataManager == null)
      gameDataManager = FindObjectOfType<GameDataManager>();

    if (playerDataHelper == null)
      playerDataHelper = FindObjectOfType<PlayerDataHelper>();

    // Validate components
    if (gameDataManager == null)
      Debug.LogError("GameSessionToPlayerDataBridge: GameDataManager not found!");

    if (playerDataHelper == null)
      Debug.LogError("GameSessionToPlayerDataBridge: PlayerDataHelper not found!");
  }

  private void OnEnable()
  {
    // Subscribe to game session events
    if (gameDataManager != null)
    {
      gameDataManager.OnDataChanged.AddListener(OnSessionDataChanged);
      gameDataManager.OnLevelUp.AddListener(OnPlayerLevelUp);
      gameDataManager.OnScoreChanged.AddListener(OnScoreChanged);
    }
  }

  private void OnDisable()
  {
    // Unsubscribe from game session events
    if (gameDataManager != null)
    {
      gameDataManager.OnDataChanged.RemoveListener(OnSessionDataChanged);
      gameDataManager.OnLevelUp.RemoveListener(OnPlayerLevelUp);
      gameDataManager.OnScoreChanged.RemoveListener(OnScoreChanged);
    }
  }

  private void Update()
  {
    // Real-time updates during gameplay
    if (updateDuringGameplay && sessionStarted && Time.time - lastUpdateTime >= updateInterval)
    {
      UpdatePlayerDataFromSession(false); // Don't end session, just update
      lastUpdateTime = Time.time;
    }
  }
  #endregion

  #region Event Handlers
  private void OnSessionDataChanged(GameSessionData sessionData)
  {
    if (!sessionStarted && sessionData.isAlive)
    {
      StartNewSession();
    }
    else if (sessionStarted && !sessionData.isAlive)
    {
      EndCurrentSession();
    }
  }

  private void OnPlayerLevelUp(int newLevel)
  {
    // Update highest level reached in player data
    PlayerDataManager.UpdateHighestLevel(newLevel);

    Debug.Log($"Player reached level {newLevel}. Updating persistent data.");
  }

  private void OnScoreChanged(float newScore)
  {
    var levelName = gameDataManager.LevelConfig.levelName ?? "";

    if (levelName == "")
    {
      Debug.LogWarning("Level name is empty, cannot update player data.");
      return;
    }

    // Continuously update best score if it's improving
    if (newScore > PlayerDataManager.CurrentPlayerData.bestScore[levelName])
    {
      PlayerDataManager.UpdateBestScore(levelName, newScore);
    }
  }
  #endregion

  #region Session Management
  /// <summary>
  /// Starts tracking a new game session.
  /// </summary>
  public void StartNewSession()
  {
    if (sessionStarted) return;

    sessionStarted = true;
    sessionStartTime = Time.time;
    sessionStartFishCount = PlayerDataManager.CurrentPlayerData.totalFishEaten;
    lastUpdateTime = Time.time;

    // Notify PlayerDataHelper that a game has started
    if (playerDataHelper != null)
    {
      playerDataHelper.OnGameStart();
    }

    Debug.Log("Game session started. Tracking player data changes.");
  }

  /// <summary>
  /// Ends the current game session and updates player data.
  /// </summary>
  public void EndCurrentSession()
  {
    if (!sessionStarted) return;

    UpdatePlayerDataFromSession(true);
    sessionStarted = false;

    Debug.Log("Game session ended. Player data updated and saved.");
  }

  /// <summary>
  /// Manually end the session (can be called from UI or other systems).
  /// </summary>
  [ContextMenu("End Session")]
  public void ManualEndSession()
  {
    EndCurrentSession();
  }

  /// <summary>
  /// Forces an update of player data from current session data.
  /// </summary>
  [ContextMenu("Update Player Data")]
  public void ForceUpdatePlayerData()
  {
    UpdatePlayerDataFromSession(false);
  }
  #endregion

  #region Data Transfer
  /// <summary>
  /// Updates persistent player data from current session data.
  /// </summary>
  /// <param name="isSessionEnd">Whether this is called at session end.</param>
  private void UpdatePlayerDataFromSession(bool isSessionEnd)
  {
    if (gameDataManager?.SessionData == null) return;

    var levelName = gameDataManager.LevelConfig.levelName ?? "";
    var sessionData = gameDataManager.SessionData;
    float sessionPlayTime = Time.time - sessionStartTime;

    if (levelName == "")
    {
      Debug.LogWarning("Level name is empty, cannot update player data.");
      return;
    }

    // Update player data through the manager
    PlayerDataManager.UpdatePlayerData(playerData =>
    {
      // Update progression data
      if (sessionData.currentLevel > playerData.highestLevelReached)
        playerData.highestLevelReached = sessionData.currentLevel;

      if (sessionData.score > playerData.bestScore[levelName])
        playerData.bestScore[levelName] = sessionData.score;

      // Add session XP to total (if you want to track total XP across sessions)
      playerData.totalExperienceEarned += sessionData.totalXP;

      // Add session play time
      playerData.totalPlayTime += sessionPlayTime;

      // Add fish eaten this session
      int fishEatenThisSession = sessionData.enemiesEaten - (sessionStartFishCount - PlayerDataManager.CurrentPlayerData.totalFishEaten);
      if (fishEatenThisSession > 0)
        playerData.totalFishEaten += fishEatenThisSession;

    }, autoSaveOnSessionEnd);

    // Call PlayerDataHelper methods for session end
    if (isSessionEnd && playerDataHelper != null)
    {
      playerDataHelper.OnGameEnd(
          levelName,
          sessionData.score,
          sessionData.enemiesEaten,
          sessionPlayTime,
          sessionData.currentSize,
          sessionData.currentLevel
      );

      // If player died (not alive), increment death count
      if (!sessionData.isAlive)
      {
        playerDataHelper.OnPlayerDeath();
      }
    }
  }
  #endregion

  #region Level Progression Integration
  /// <summary>
  /// Called when a level is completed successfully.
  /// </summary>
  /// <param name="levelName">Name of the completed level.</param>
  public void OnLevelCompleted(string levelName)
  {
    PlayerDataManager.CompleteLevel(levelName);

    if (playerDataHelper != null)
    {
      playerDataHelper.OnLevelComplete(levelName);
    }

    Debug.Log($"Level '{levelName}' marked as completed in player data.");
  }

  /// <summary>
  /// Called when a new level should be unlocked.
  /// </summary>
  /// <param name="levelName">Name of the level to unlock.</param>
  public void OnLevelUnlocked(string levelName)
  {
    PlayerDataManager.UnlockLevel(levelName);

    if (playerDataHelper != null)
    {
      playerDataHelper.OnLevelUnlock(levelName);
    }

    Debug.Log($"Level '{levelName}' unlocked in player data.");
  }

  /// <summary>
  /// Unlocks the next level in sequence based on current progress.
  /// </summary>
  /// <param name="levelPrefix">Prefix for level names (e.g., "Level").</param>
  public void UnlockNextLevel(string levelPrefix = "Level")
  {
    int nextLevelNumber = PlayerDataManager.CurrentPlayerData.highestLevelReached + 1;
    string nextLevelName = $"{levelPrefix}{nextLevelNumber}";
    OnLevelUnlocked(nextLevelName);
  }
  #endregion

  #region Debug & Utility
  /// <summary>
  /// Gets debug information about the current session.
  /// </summary>
  public string GetSessionDebugInfo()
  {
    if (!sessionStarted || gameDataManager?.SessionData == null)
      return "No active session";

    var sessionData = gameDataManager.SessionData;
    float sessionTime = Time.time - sessionStartTime;

    return $"Active Session:\n" +
           $"Duration: {sessionTime:F1}s\n" +
           $"Level: {sessionData.currentLevel}\n" +
           $"Score: {sessionData.score:N0}\n" +
           $"Fish Eaten: {sessionData.enemiesEaten}\n" +
           $"Size: {sessionData.currentSize:F2}\n" +
           $"XP: {sessionData.currentXP:F0}/{sessionData.xpToNextLevel:F0}\n" +
           $"Lives: {sessionData.lives}\n" +
           $"Alive: {sessionData.isAlive}";
  }

  /// <summary>
  /// Logs current session and player data for debugging.
  /// </summary>
  [ContextMenu("Log Session Debug Info")]
  public void LogSessionDebugInfo()
  {
    Debug.Log(GetSessionDebugInfo());

    if (playerDataHelper != null)
    {
      playerDataHelper.LogPlayerStats();
    }
  }
  #endregion
}
