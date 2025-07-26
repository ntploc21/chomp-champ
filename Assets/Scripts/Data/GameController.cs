using UnityEngine;

/// <summary>
/// Example game controller showing how to integrate the PlayerData save/load system
/// with your Feeding Frenzy game flow. This demonstrates the typical usage patterns.
/// </summary>
public class GameController : MonoBehaviour
{
  #region Inspector Fields
  [Header("Game References")]
  [SerializeField] private GameDataManager gameDataManager;
  [SerializeField] private PlayerDataHelper playerDataHelper;

  [Header("Game Settings")]
  [SerializeField] private bool autoSaveEnabled = true;
  [SerializeField] private float autoSaveInterval = 30f; // Save every 30 seconds during gameplay
  #endregion

  #region Private Fields
  private float gameStartTime = 0f;
  private float lastAutoSave = 0f;
  private int sessionStartFishCount = 0;
  #endregion

  #region Properties
  public string CurrentLevelName => gameDataManager?.LevelConfig?.levelName ?? "";
  #endregion


  #region Unity Events
  private void Awake()
  {
    // Ensure PlayerDataManager is initialized early
    PlayerDataManager.Initialize();

    // Find components if not assigned
    if (gameDataManager == null)
      gameDataManager = FindObjectOfType<GameDataManager>();
    if (playerDataHelper == null)
      playerDataHelper = FindObjectOfType<PlayerDataHelper>();
  }
  private void Start()
  {
    // Find components if not assigned again
    if (gameDataManager == null)
      gameDataManager = FindObjectOfType<GameDataManager>();
    if (playerDataHelper == null)
      playerDataHelper = FindObjectOfType<PlayerDataHelper>();

    // Subscribe to player data events
    PlayerDataManager.OnPlayerDataLoaded += OnPlayerDataLoaded;

    // Check if current level is unlocked
    if (!PlayerDataManager.IsLevelUnlocked(CurrentLevelName))
    {
      Debug.LogWarning($"Level {CurrentLevelName} is not unlocked. Falling back to Level1.");
      return;
    }
  }

  private void Update()
  {
    // Auto-save during gameplay
    if (autoSaveEnabled && Time.time - lastAutoSave >= autoSaveInterval)
    {
      UpdateProgressivePlayerData();
      lastAutoSave = Time.time;
    }
  }

  private void OnDestroy()
  {
    // Unsubscribe from events
    PlayerDataManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;

    // Save on destroy
    // EndGame(false); // End without completing level
  }

  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus)
    {
      UpdateProgressivePlayerData();
    }
  }

  private void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus)
    {
      UpdateProgressivePlayerData();
    }
  }
  #endregion

  #region Game Flow Methods
  /// <summary>
  /// Starts a new game session.
  /// </summary>
  public void StartGame()
  {
    gameStartTime = Time.time;
    lastAutoSave = Time.time;
    sessionStartFishCount = PlayerDataManager.CurrentPlayerData.totalFishEaten;

    // Increment games played counter
    PlayerDataManager.IncrementGamesPlayed();

    // Notify other systems
    if (playerDataHelper != null)
    {
      playerDataHelper.OnGameStart();
    }

    Debug.Log($"Game started. Level: {CurrentLevelName}, Games played: {PlayerDataManager.CurrentPlayerData.gamesPlayed}");
  }

  /// <summary>
  /// Ends the current game session.
  /// </summary>
  /// <param name="levelCompleted">Whether the level was completed successfully.</param>
  public void EndGame(bool levelCompleted = false)
  {
    float sessionDuration = Time.time - gameStartTime;

    // Update the player data progressive last time to ensure all changes are saved
    UpdateProgressivePlayerData();

    // Update player data with session results
    UpdateFinalPlayerData(sessionDuration, levelCompleted);

    Debug.Log($"Game ended. Duration: {sessionDuration:F1}s, Level completed: {levelCompleted}");
  }

  /// <summary>
  /// Called when the player completes the current level.
  /// </summary>
  public void OnLevelCompleted()
  {
    // Mark level as completed
    PlayerDataManager.CompleteLevel(CurrentLevelName);

    // Unlock next level
    string nextLevelName = GetNextLevelName(CurrentLevelName);
    if (!string.IsNullOrEmpty(nextLevelName))
    {
      PlayerDataManager.UnlockLevel(nextLevelName);
      Debug.Log($"Level {nextLevelName} unlocked!");
    }

    // Notify PlayerDataHelper
    if (playerDataHelper != null)
    {
      playerDataHelper.OnLevelComplete(CurrentLevelName);
      if (!string.IsNullOrEmpty(nextLevelName))
      {
        playerDataHelper.OnLevelUnlock(nextLevelName);
      }
    }

    // End the game as completed
    EndGame(true);
  }

  /// <summary>
  /// Called when the player dies.
  /// </summary>
  public void OnPlayerDied(bool isGameOver = false)
  {
    PlayerDataManager.IncrementDeaths();

    if (playerDataHelper != null)
    {
      playerDataHelper.OnPlayerDeath();
    }

    Debug.Log($"Player died. Total deaths: {PlayerDataManager.CurrentPlayerData.totalDeaths}");

    // End the game if the player has no lives left
    if (gameDataManager.SessionData.lives <= 0 || isGameOver)
      EndGame(false);
  }

  /// <summary>
  /// Called when the player's score changes.
  /// </summary>
  /// <param name="newScore">The new score.</param>
  public void OnScoreChanged(float newScore)
  {
    var levelName = gameDataManager?.LevelConfig?.levelName ?? "";
    if (levelName == "")
    {
      Debug.LogWarning("Level name is empty, cannot update player data.");
      return;
    }

    PlayerDataManager.UpdateBestScore(levelName, newScore);
  }

  /// <summary>
  /// Called when the player eats fish.
  /// </summary>
  /// <param name="fishCount">Number of fish eaten.</param>
  public void OnFishEaten(int fishCount = 1)
  {
    PlayerDataManager.AddFishEaten(fishCount);
  }
  #endregion

  #region Data Management
  /// <summary>
  /// Updates player data progressively during gameplay.
  /// </summary>
  private void UpdateProgressivePlayerData()
  {
    if (gameDataManager?.SessionData == null) return;

    var sessionData = gameDataManager.SessionData;
    float sessionTime = Time.time - gameStartTime;

    // Update progressive stats
    PlayerDataManager.UpdatePlayerData(data =>
    {
      // Update continuous stats
      data.totalPlayTime += sessionTime;

      // Update session-based stats
      if (!data.bestScore.ContainsKey(CurrentLevelName))
      {
        data.bestScore[CurrentLevelName] = 0f; // Initialize if not exists
      }

      if (sessionData.score > data.bestScore[CurrentLevelName])
        data.bestScore[CurrentLevelName] = sessionData.score;

      // Update fish count
      int sessionFish = sessionData.enemiesEaten;
      int totalFishThisSession = sessionFish - (sessionStartFishCount - data.totalFishEaten);
      if (totalFishThisSession > 0)
      {
        data.totalFishEaten = sessionStartFishCount + sessionFish;
      }
    }, false); // Don't auto-save, just update

    // Reset the timer
    gameStartTime = Time.time;
  }

  /// <summary>
  /// Updates player data with final session results.
  /// </summary>
  private void UpdateFinalPlayerData(float sessionDuration, bool levelCompleted)
  {
    if (gameDataManager?.SessionData == null) return;

    var sessionData = gameDataManager.SessionData;

    // Final data update
    PlayerDataManager.UpdatePlayerData(data =>
    {
      // Add remaining session time
      data.totalPlayTime += sessionDuration;

      // Add any remaining XP if tracking cross-session XP
      data.totalExperienceEarned += sessionData.totalXP;

      // Add total score during this session
      data.totalScore += sessionData.score;

      // Add currency balance
      if (levelCompleted)
      {
        int currencyEarned = (int)sessionData.score / 100;
        data.currencyBalance += currencyEarned;
      }
    });

    // Notify PlayerDataHelper with final stats
    if (playerDataHelper != null)
    {
      Debug.Log($"Ending game session for level: {CurrentLevelName}, Score: {sessionData.score}, " +
                $"Enemies Eaten: {sessionData.enemiesEaten}, Duration: {sessionDuration:F1}s, " +
                $"Size: {sessionData.currentSize}, Level: {sessionData.currentLevel}");

      playerDataHelper.OnGameEnd(
          CurrentLevelName,
          sessionData.score,
          sessionData.enemiesEaten,
          sessionDuration,
          sessionData.currentSize,
          sessionData.currentLevel
      );
    }

    // Save the data
    PlayerDataManager.SavePlayerData();
  }
  #endregion

  #region Level Management
  /// <summary>
  /// Gets the next level name in sequence.
  /// </summary>
  private string GetNextLevelName(string currentLevel)
  {
    // Extract level number and increment
    // Level format: "C0L1", "C1L1", etc., each chapter has its own sequence
    int[] levelPerChapter = { 3, 3, 3, 3, 3, 3 }; // Example: C0 has 1 level, C1 has 2 levels, etc.

    if (currentLevel.StartsWith("C") && currentLevel.Length > 3)
    {
      string chapterPart = currentLevel.Substring(0, 3); // "C0L", "C1L", etc.
      string levelPart = currentLevel.Substring(3); // "1", "2", etc.

      if (int.TryParse(levelPart, out int levelNumber))
      {
        if (levelNumber < levelPerChapter[chapterPart[1] - '0'])
        {
          return $"{chapterPart}{levelNumber + 1}"; // Increment level number
        }
        else if (chapterPart[1] - '0' < levelPerChapter.Length - 1)
        {
          // Move to next chapter
          return $"C{chapterPart[1] - '0' + 1}L1"; // Reset level to 1 in next chapter
        }
      }
    }

    return null; // No next level or invalid format
  }

  /// <summary>
  /// Gets available (unlocked) levels.
  /// </summary>
  public string[] GetAvailableLevels()
  {
    return PlayerDataManager.CurrentPlayerData.unlockedLevels;
  }

  /// <summary>
  /// Gets completed levels.
  /// </summary>
  public string[] GetCompletedLevels()
  {
    return PlayerDataManager.CurrentPlayerData.completedLevels;
  }
  #endregion

  #region Event Handlers
  private void OnPlayerDataLoaded(PlayerData playerData)
  {
    Debug.Log($"Player data loaded for: {playerData.playerName}");
  }
  #endregion

  #region Public Interface
  /// <summary>
  /// Sets the player name.
  /// </summary>
  public void SetPlayerName(string newName)
  {
    PlayerDataManager.SetPlayerName(newName);
    Debug.Log($"Player name changed to: {newName}");
  }

  /// <summary>
  /// Manually saves player data.
  /// </summary>
  public void SaveGame()
  {
    UpdateProgressivePlayerData();
    PlayerDataManager.SavePlayerData();
    Debug.Log("Game saved manually");
  }

  /// <summary>
  /// Resets all player data.
  /// </summary>
  public void ResetPlayerData()
  {
    PlayerDataManager.DeletePlayerData();
    Debug.Log("Player data reset");
  }

  /// <summary>
  /// Gets current player statistics for UI display.
  /// </summary>
  public PlayerData GetPlayerStats()
  {
    return PlayerDataManager.CurrentPlayerData;
  }
  #endregion

  #region Debug Methods
  [ContextMenu("Complete Current Level")]
  private void DebugCompleteLevel()
  {
    OnLevelCompleted();
  }

  [ContextMenu("Print Player Stats")]
  private void DebugPrintStats()
  {
    var data = PlayerDataManager.CurrentPlayerData;
    Debug.Log($"Player: {data.playerName}, Level: {data.currentLevel}, Score: {data.bestScore:N0}, " +
             $"Fish: {data.totalFishEaten}, Games: {data.gamesPlayed}, Deaths: {data.totalDeaths}");
  }
  #endregion
}
