using UnityEngine;
using UnityEngine.Events;
using System;

public class PlayerDataManager : MonoBehaviour
{
    #region Editor Data
    [Header("Data")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private LevelData levelData;

    [Header("Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 30f;
    [SerializeField] private bool enablePersistence = true;

    [Header("Events")]
    public UnityEvent<PlayerData> OnDataChanged;
    public UnityEvent<int> OnLevelUp;
    public UnityEvent<float> OnScoreChanged;
    public UnityEvent<int> OnLivesChanged;
    public UnityEvent<float, float> OnXPChanged; // current, required for next level
    #endregion

    #region Internal Data
    private float lastAutoSaveTime;
    private string saveKey = "PlayerData";
    #endregion

    // Properties for easy access
    public PlayerData Data => playerData;
    public LevelData LevelConfig => levelData;

    #region Unity Lifecycle
    private void Awake()
    {
        if (playerData == null)
            playerData = new PlayerData();

        if (enablePersistence)
            LoadData();
    }

    private void Start()
    {
        lastAutoSaveTime = Time.time;
    }

    private void Update()
    {
        // Auto-save functionality
        if (autoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveData();
            lastAutoSaveTime = Time.time;
        }

        // Update play time
        if (playerData.isAlive)
        {
            playerData.playTime += Time.deltaTime;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enablePersistence)
            SaveData();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && enablePersistence)
            SaveData();
    }
    #endregion

    #region Data Management
    public void ResetPlayerData()
    {
        playerData.ResetToDefaults();
        OnDataChanged?.Invoke(playerData);
        NotifyAllChanges();
    }

    public void AddScore(float points)
    {
        float oldScore = playerData.score;
        playerData.score += points;

        OnScoreChanged?.Invoke(playerData.score);
        OnDataChanged?.Invoke(playerData);
    }

    public void AddXP(float xp)
    {
        if (!playerData.isAlive || xp <= 0) return;

        playerData.currentXP += xp;

        // Check for level up
        while (playerData.currentXP >= playerData.xpToNextLevel && playerData.currentLevel < levelData.maxLevel)
        {
            LevelUp();
        }

        OnXPChanged?.Invoke(playerData.currentXP, playerData.xpToNextLevel);
        OnDataChanged?.Invoke(playerData);
    }

    public void AddLife()
    {
        playerData.lives++;
        OnLivesChanged?.Invoke(playerData.lives);
        OnDataChanged?.Invoke(playerData);
    }

    public void LoseLife()
    {
        playerData.lives = Mathf.Max(0, playerData.lives - 1);

        if (playerData.lives <= 0)
        {
            playerData.isAlive = false;
            EndSession();
        }

        OnLivesChanged?.Invoke(playerData.lives);
        OnDataChanged?.Invoke(playerData);
    }

    public void EatEnemy(float enemySize, bool isStreak = false, bool isSpeedKill = false)
    {
        playerData.enemiesEaten++;

        // Calculate XP gain with bonuses
        float baseXP = enemySize * 10f; // Base XP calculation
        float xpBonus = levelData.CalculateXPBonus(playerData.currentSize, enemySize, isStreak, isSpeedKill);
        float totalXP = baseXP * xpBonus;

        AddXP(totalXP);

        // Calculate score
        float scoreGain = enemySize * 15f * xpBonus;
        AddScore(scoreGain);

        OnDataChanged?.Invoke(playerData);
    }

    private void LevelUp()
    {
        // Subtract XP used for level up
        playerData.currentXP -= playerData.xpToNextLevel;
        playerData.currentLevel++;

        // Calculate new XP requirement and size
        playerData.xpToNextLevel = levelData.GetXPForLevel(playerData.currentLevel + 1);
        playerData.currentSize = levelData.GetSizeForLevel(playerData.currentLevel);

        // Check for level rewards
        var reward = levelData.GetRewardForLevel(playerData.currentLevel);
        if (reward != null)
        {
            ApplyLevelReward(reward);
        }

        // Check for evolution
        var evolution = levelData.GetEvolutionForLevel(playerData.currentLevel);
        if (evolution != null)
        {
            ApplyEvolution(evolution);
        }

        OnLevelUp?.Invoke(playerData.currentLevel);
        OnDataChanged?.Invoke(playerData);
    }

    private void ApplyLevelReward(LevelReward reward)
    {
        switch (reward.type)
        {
            case LevelReward.RewardType.ExtraLife:
                AddLife();
                break;
            case LevelReward.RewardType.SizeBonus:
                playerData.currentSize += reward.value;
                break;
                // Add more reward types as needed
        }
    }

    private void ApplyEvolution(EvolutionLevel evolution)
    {
        if (evolution.sizeMultiplier != 1.0f)
            playerData.currentSize *= evolution.sizeMultiplier;

        // Additional evolution effects can be applied here
        // This could trigger visual/audio effects in other systems
    }

    public void SetInvincible(bool invincible)
    {
        playerData.isInvincible = invincible;
        OnDataChanged?.Invoke(playerData);
    }

    public void UpdatePosition(Vector2 position)
    {
        playerData.lastPosition = position;
    }

    private void EndSession()
    {
        if (enablePersistence)
            SaveData();
    }
    #endregion

    #region Save/Load System
    public void SaveData()
    {
        if (!enablePersistence) return;

        try
        {
            string jsonData = JsonUtility.ToJson(playerData, true);
            PlayerPrefs.SetString(saveKey, jsonData);
            PlayerPrefs.Save();

            Debug.Log("Player data saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data: {e.Message}");
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
                playerData = JsonUtility.FromJson<PlayerData>(jsonData);

                // Validate loaded data
                ValidateData();

                Debug.Log("Player data loaded successfully");
                OnDataChanged?.Invoke(playerData);
                NotifyAllChanges();
            }
            else
            {
                Debug.Log("No saved data found, using defaults");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player data: {e.Message}");
            playerData = new PlayerData(); // Fallback to defaults
        }
    }

    public void DeleteSaveData()
    {
        PlayerPrefs.DeleteKey(saveKey);
        playerData = new PlayerData();
        OnDataChanged?.Invoke(playerData);
        NotifyAllChanges();

        Debug.Log("Save data deleted");
    }

    private void ValidateData()
    {
        // Ensure data integrity
        playerData.lives = Mathf.Max(0, playerData.lives);
        playerData.currentLevel = Mathf.Max(1, playerData.currentLevel);
        playerData.currentSize = Mathf.Max(levelData.baseSize, playerData.currentSize);

        // Recalculate XP requirement if needed
        if (playerData.xpToNextLevel <= 0)
        {
            playerData.xpToNextLevel = levelData.GetXPForLevel(playerData.currentLevel + 1);
        }
    }
    #endregion

    #region Utility Methods
    private void NotifyAllChanges()
    {
        OnScoreChanged?.Invoke(playerData.score);
        OnLivesChanged?.Invoke(playerData.lives);
        OnXPChanged?.Invoke(playerData.currentXP, playerData.xpToNextLevel);
        OnLevelUp?.Invoke(playerData.currentLevel);
    }

    public float GetProgressToNextLevel()
    {
        if (playerData.xpToNextLevel <= 0) return 1f;
        return playerData.currentXP / playerData.xpToNextLevel;
    }

    public bool IsMaxLevel()
    {
        return playerData.currentLevel >= levelData.maxLevel;
    }

    public void DebugPrintData()
    {
        Debug.Log($"Player Stats - Level: {playerData.currentLevel}, XP: {playerData.currentXP}/{playerData.xpToNextLevel}, " +
                 $"Score: {playerData.score}, Lives: {playerData.lives}, Size: {playerData.currentSize:F2}");
    }
    #endregion
}
