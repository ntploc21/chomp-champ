using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-30)]
public class GameDataManager : MonoBehaviour
{
  #region Editor Data
  [Header("Components")]
  [SerializeField] private GameSessionData gameSessionData = null;
  [SerializeField] private LevelData levelData = null;

  [Header("Events")]
  public UnityEvent<GameSessionData> OnDataChanged = null;
  public UnityEvent<int> OnLevelUp = null;
  public UnityEvent<float> OnScoreChanged = null;
  public UnityEvent<int> OnLivesChanged = null;
  public UnityEvent<float, float> OnXPChanged = null; // XP changed event with current and next level XP
  #endregion

  #region Internal Data
  public float scoreBoost = 1f; // Default score boost multiplier
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
  }

  private void Start()
  {
    GUIManager.Instance.FindGameDataManagerInScenes();
  }

  public float doubleScoreTime = 10f;
  public float doubleScoreStart = -1000000f;

  public void StartDoubleScore()
  {
    doubleScoreStart = Time.time;
    Debug.Log("Double score started.");
  }

  public float ScoreMultiplier()
  {
    float elapsed = Time.time - doubleScoreStart;
    if (elapsed < doubleScoreTime)
    {
      return 2f;
    }
    else
    {
      return 1f;
    }
  }

  private void Update()
  {
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

  }

  private void OnAplicationFocus(bool hasFocus)
  {

  }

#if UNITY_EDITOR
  /// <summary>
  /// Called when Inspector values change - triggers UI updates automatically
  /// Only works in Play mode
  /// </summary>
  private void OnValidate()
  {
    // Only trigger events in Play mode to avoid issues in Edit mode
    if (Application.isPlaying && gameSessionData != null)
    {
      // Invoke all relevant events to update the HUD
      OnDataChanged?.Invoke(gameSessionData);
      OnScoreChanged?.Invoke(gameSessionData.score);
      OnLivesChanged?.Invoke(gameSessionData.lives);
      OnXPChanged?.Invoke(gameSessionData.currentXP, gameSessionData.xpToNextLevel);
    }
  }
#endif
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
    gameSessionData.score += amount * ScoreMultiplier();

    OnScoreChanged?.Invoke(gameSessionData.score);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void AddXP(float amount)
  {
    if (!gameSessionData.isAlive || amount <= 0) return;

    gameSessionData.currentXP += amount;
    gameSessionData.totalXP += amount; // Track total XP gained in the session

    // Check for level up
    while (gameSessionData.currentXP >= gameSessionData.xpToNextLevel && !IsMaxLevel())
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

    if (gameSessionData.resetXPOnDeath)
    {
      gameSessionData.totalXP -= gameSessionData.currentXP; // Deduct current XP from total XP
      gameSessionData.currentXP = 0f; // Reset XP to zero on death
    }

    if (gameSessionData.lives <= 0)
    {
      gameSessionData.isAlive = false; // Player is dead
    }

    OnLivesChanged?.Invoke(gameSessionData.lives);
    OnDataChanged?.Invoke(gameSessionData);
  }

  public void EatEnemy(int enemyLevel, bool isStreak = false)
  {
    gameSessionData.enemiesEaten++;

    // Calculate the XP gained from eating an enemy
    float baseXP = enemyLevel * 3f; // Example: 10 XP per unit level
    float bonusMul = levelData.CalculateXPBonus(gameSessionData.currentSize, enemyLevel, isStreak);
    float totalXP = baseXP * bonusMul;

    // Add XP to the player
    AddXP(totalXP);

    // Calculate score based on enemy size
    float scoreGained = enemyLevel * 100f * bonusMul * scoreBoost;
    AddScore(scoreGained);

    // Track enemies eaten per level
    if (!gameSessionData.enemiesEatenPerLevel.ContainsKey(enemyLevel))
    {
      gameSessionData.enemiesEatenPerLevel[enemyLevel] = 0;
    }
    gameSessionData.enemiesEatenPerLevel[enemyLevel]++;

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

  /// <summary>
  /// Manually trigger all UI update events - useful for Inspector testing
  /// </summary>
  [ContextMenu("Update All UI")]
  public void UpdateAllUI()
  {
    if (gameSessionData != null)
    {
      OnDataChanged?.Invoke(gameSessionData);
      OnScoreChanged?.Invoke(gameSessionData.score);
      OnLivesChanged?.Invoke(gameSessionData.lives);
      OnXPChanged?.Invoke(gameSessionData.currentXP, gameSessionData.xpToNextLevel);
      Debug.Log("All UI events triggered manually");
    }
  }

  /// <summary>
  /// Test method to add score and trigger events
  /// </summary>
  [ContextMenu("Test Add 100 Score")]
  public void TestAddScore()
  {
    AddScore(100f);
  }

  /// <summary>
  /// Test method to add XP and trigger events
  /// </summary>
  [ContextMenu("Test Add 50 XP")]
  public void TestAddXP()
  {
    AddXP(50f);
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