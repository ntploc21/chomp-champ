using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.Lumin;
using UnityEditor.Experimental.RestService;
using System.Xml.XPath;
using Unity.VisualScripting;

public class GameDataManager : MonoBehaviour
{
  #region Editor Data
  [Header("Components")]
  [SerializeField] private GameSessionData gameSessionData = null;
  [SerializeField] private LevelData levelData = null;

  [Header("Settings")]
  [Tooltip("Enable auto-saving of game data.")]
  [SerializeField] private bool autoSaveEnabled = true;
  [Tooltip("Interval for auto-saving game data.")]
  [SerializeField] private float autoSaveInterval = 60f; // Auto-save every 60 seconds
  [Tooltip("Enable data persistence between sessions. This allows the game to save and load data even after the game is closed.")]
  [SerializeField] private bool enablePersistence = true;

  [Header("Events")]
  public UnityEvent<GameSessionData> OnDataChanged = null;
  public UnityEvent<int> OnLevelUp = null;
  public UnityEvent<float> OnScoreChanged = null;
  public UnityEvent<int> OnLivesChanged = null;
  public UnityEvent<float, float> OnXPChanged = null; // XP changed event with current and next level XP
  #endregion

  #region Internal Data
  private float lastAutoSaveTime = 0f;
  private string saveKey = "GameSessionData";
  #endregion

  #region Properties
  public GameSessionData SessionData => gameSessionData;
  public LevelData LevelConfig => levelData;
  #endregion
  #region Unity Events
  private void Awake()
  {
    if (gameSessionData == null)
    {
      gameSessionData = new GameSessionData();
    }

    // Initialize session data from level data at game start
    InitializeSessionFromLevelData();

    if (enablePersistence)
    {
      LoadData();
    }
  }

  private void Start()
  {
    lastAutoSaveTime = Time.time;
  }

  private void Update()
  {
    // Auto save functionality
    if (autoSaveEnabled && (Time.time - lastAutoSaveTime) >= autoSaveInterval)
    {
      SaveData();
      lastAutoSaveTime = Time.time;
    }

    // Update play time
    if (gameSessionData.isAlive)
    {
      gameSessionData.playTime += Time.deltaTime;
    }
  }
  #endregion

  #region Data Event Handlers
  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus && enablePersistence)
    {
      SaveData();
    }
  }

  private void OnAplicationFocus(bool hasFocus)
  {
    if (!hasFocus && enablePersistence)
    {
      SaveData();
    }
  }
  #endregion
  #region Data Management
  public void ResetPlayerData()
  {
    // Reset to defaults and reinitialize from LevelData
    ResetSessionData();
  }

  public void AddScore(float amount)
  {
    float oldScore = gameSessionData.score;
    gameSessionData.score += amount;

    OnScoreChanged?.Invoke(gameSessionData.score);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void AddXP(float amount)
  {
    if (!gameSessionData.isAlive || amount <= 0) return;

    gameSessionData.currentXP += amount;
    gameSessionData.totalXP += amount; // Track total XP gained in the session

    // Check for level up
    if (gameSessionData.currentXP >= gameSessionData.xpToNextLevel && !IsMaxLevel())
    {
      LevelUp();
    }

    OnXPChanged?.Invoke(gameSessionData.currentXP, gameSessionData.xpToNextLevel);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void AddLives(int amount = 1)
  {
    if (amount <= 0) return;

    gameSessionData.lives += amount;
    gameSessionData.isAlive = true; // Ensure player is alive when lives are added

    OnLivesChanged?.Invoke(gameSessionData.lives);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void LoseLife()
  {
    gameSessionData.lives = Mathf.Max(0, gameSessionData.lives - 1);

    if (gameSessionData.lives <= 0)
    {
      gameSessionData.isAlive = false; // Player is dead
      EndSession();
    }

    OnLivesChanged?.Invoke(gameSessionData.lives);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void EatEnemy(float enemySize, bool isStreak = false)
  {
    gameSessionData.enemiesEaten++;

    // Calculate the XP gained from eating an enemy
    float baseXP = enemySize * 10f; // Example: 10 XP per unit size
    float bonusMul = levelData.CalculateXPBonus(gameSessionData.currentSize, enemySize, isStreak);
    float totalXP = baseXP * bonusMul;

    // Add XP to the player
    AddXP(totalXP);

    // Calculate score based on enemy size
    float scoreGained = enemySize * 100f * bonusMul;
    AddScore(scoreGained);

    OnDataChanged?.Invoke(gameSessionData);
  }

  private void LevelUp()
  {
    if (gameSessionData.currentXP < gameSessionData.xpToNextLevel)
    {
      Debug.LogWarning("Not enough XP to level up.");
      return;
    }

    // Subtract the XP required for the next level
    gameSessionData.currentXP -= gameSessionData.xpToNextLevel;
    gameSessionData.currentLevel++;

    // Update level data
    gameSessionData.xpToNextLevel = levelData.GetXPForLevel(gameSessionData.currentLevel + 1);
    gameSessionData.currentSize = levelData.GetSizeForLevel(gameSessionData.currentLevel);

    OnLevelUp?.Invoke(gameSessionData.currentLevel);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void SetInvincible(bool invincible)
  {
    gameSessionData.isInvincible = invincible;

    // Notify listeners about the invincibility change
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void UpdatePosition(Vector2 position)
  {
    gameSessionData.lastPosition = position;
  }

  private void EndSession()
  {
    if (enablePersistence)
    {
      SaveData();
    }
  }
  #endregion

  #region Save/Load System
  public void SaveData()
  {
    if (!enablePersistence) return;

    try
    {
      string jsonData = JsonUtility.ToJson(gameSessionData);
      PlayerPrefs.SetString(saveKey, jsonData);
      PlayerPrefs.Save();

      Debug.Log("Game data saved successfully.");
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to save game data: {ex.Message}");
    }
  }

  public void LoadData()
  {
    if (!enablePersistence) return;

    try
    {
      if (PlayerPrefs.HasKey(saveKey))
      {
        string jsonData = PlayerPrefs.GetString(saveKey);
        gameSessionData = JsonUtility.FromJson<GameSessionData>(jsonData);

        // Validate loaded data
        ValidateData();

        OnDataChanged?.Invoke(gameSessionData);
        NotifyAllChanges();

        Debug.Log("Game data loaded successfully.");
      }
      else
      {
        Debug.LogWarning("No saved game data found. Initializing with default values.");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to load game data: {ex.Message}");
    }
  }

  public void DeleteSaveData()
  {
    PlayerPrefs.DeleteKey(saveKey);
    gameSessionData = new GameSessionData(); // Reset to default values

    OnDataChanged?.Invoke(gameSessionData);
    NotifyAllChanges();

    Debug.Log("Game data deleted successfully.");
  }
  private void ValidateData()
  {
    // Ensure data integrity
    gameSessionData.lives = Mathf.Max(0, gameSessionData.lives);
    gameSessionData.currentLevel = Mathf.Max(1, gameSessionData.currentLevel);
    gameSessionData.currentSize = Mathf.Max(levelData.baseSize, gameSessionData.currentSize);

    // Always sync XP requirements and size with LevelData (single source of truth)
    gameSessionData.xpToNextLevel = levelData.GetXPForLevel(gameSessionData.currentLevel);
    gameSessionData.currentSize = levelData.GetSizeForLevel(gameSessionData.currentLevel);

    Debug.Log($"Validated data synced with LevelData: Level {gameSessionData.currentLevel}, " +
              $"Size {gameSessionData.currentSize:F2}, XP to next: {gameSessionData.xpToNextLevel}");
  }
  #endregion

  #region Utility Methods
  private void NotifyAllChanges()
  {
    OnLevelUp?.Invoke(gameSessionData.currentLevel);
    OnScoreChanged?.Invoke(gameSessionData.score);
    OnLivesChanged?.Invoke(gameSessionData.lives);
    OnXPChanged?.Invoke(gameSessionData.currentXP, gameSessionData.xpToNextLevel);
  }

  public float GetProgressToNextLevel()
  {
    if (gameSessionData.xpToNextLevel <= 0)
    {
      return 0f;
    }

    return Mathf.Clamp01(gameSessionData.currentXP / gameSessionData.xpToNextLevel);
  }

  public bool IsMaxLevel()
  {
    return gameSessionData.currentLevel >= levelData.maxLevel;
  }

  public void PrintDebugData()
  {
    Debug.Log($"Player Stats - Level: {gameSessionData.currentLevel}, XP: {gameSessionData.currentXP}/{gameSessionData.xpToNextLevel}, " +
             $"Score: {gameSessionData.score}, Lives: {gameSessionData.lives}, Size: {gameSessionData.currentSize:F2}");
  }
  #endregion

  #region Initialization
  /// <summary>
  /// Initialize GameSessionData from LevelData configuration
  /// This makes LevelData the single source of truth for level progression
  /// </summary>
  private void InitializeSessionFromLevelData()
  {
    if (levelData == null)
    {
      Debug.LogWarning("LevelData is not assigned! Using default values.");
      return;
    }

    // Initialize XP requirements from LevelData
    gameSessionData.xpToNextLevel = levelData.GetXPForLevel(gameSessionData.currentLevel);

    // Initialize size from LevelData
    gameSessionData.currentSize = levelData.GetSizeForLevel(gameSessionData.currentLevel);

    // Set maximum XP based on LevelData
    gameSessionData.maximumXP = levelData.GetTotalXPForLevel(levelData.maxLevel - 1);
  }

  /// <summary>
  /// Reset session data and reinitialize from LevelData
  /// </summary>
  public void ResetSessionData()
  {
    gameSessionData.ResetToDefaults();
    InitializeSessionFromLevelData();
    OnDataChanged?.Invoke(gameSessionData);
    NotifyAllChanges();
  }
  #endregion
}