using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Main Achievement Manager for Feeding Frenzy game
/// Integrates level-specific achievements with the existing Reach.UI achievement system
/// </summary>
public class FFAchievementManager : MonoBehaviour
{
    [Header("Achievement Configuration")]
    [SerializeField] private List<LevelAchievementConfig> levelConfigs = new List<LevelAchievementConfig>();
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool showAchievementNotifications = true;

    [Header("Notification Settings")]
    [SerializeField] private float notificationDuration = 3f;
    [SerializeField] private GameObject achievementNotificationPrefab;
    [SerializeField] private Transform notificationParent;

    [Header("Events")]
    public UnityEvent<string> OnAchievementUnlocked = new UnityEvent<string>();
    public UnityEvent<LevelAchievementConfig.LevelAchievement> OnAchievementComplete = new UnityEvent<LevelAchievementConfig.LevelAchievement>();

    // Cached references
    private GameController gameController;
    private GameDataManager gameDataManager;
    private LevelAchievementConfig currentLevelConfig;
    private float levelStartTime;
    private List<string> newlyUnlockedAchievements = new List<string>();

    // Singleton pattern for easy access
    public static FFAchievementManager Instance { get; private set; }

    #region Unity Events
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cache references
        gameController = FindObjectOfType<GameController>();
        gameDataManager = FindObjectOfType<GameDataManager>();
    }

    private void Start()
    {
        InitializeAchievementSystem();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize the achievement system and subscribe to events
    /// </summary>
    private void InitializeAchievementSystem()
    {
        // Ensure we have all required references
        if (gameController == null)
            gameController = FindObjectOfType<GameController>();

        if (gameDataManager == null)
            gameDataManager = FindObjectOfType<GameDataManager>();

        // Subscribe to game events if available
        if (gameController != null)
        {
            // We'll hook into the level completion in the GameController
            Debug.Log("FeedingFrenzyAchievementManager initialized and ready");
        }
        else
        {
            Debug.LogWarning("GameController not found! Achievement system may not work correctly.");
        }

        // Load current level achievements
        LoadCurrentLevelAchievements();
    }

    /// <summary>
    /// Load achievements for the current level
    /// </summary>
    private void LoadCurrentLevelAchievements()
    {
        if (gameController == null)
            return;

        string currentLevelName = gameController.CurrentLevelName;
        currentLevelConfig = GetLevelConfig(currentLevelName);

        if (currentLevelConfig != null)
        {
            if (enableDebugLogging)
                Debug.Log($"Loaded {currentLevelConfig.Achievements.Count} achievements for level: {currentLevelName}");
        }
        else
        {
            if (enableDebugLogging)
                Debug.LogWarning($"No achievement configuration found for level: {currentLevelName}");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Call this when a level starts to track achievement progress
    /// </summary>
    public void OnLevelStart()
    {
        levelStartTime = Time.time;
        newlyUnlockedAchievements.Clear();
        LoadCurrentLevelAchievements();

        if (enableDebugLogging)
            Debug.Log($"Achievement tracking started for level: {gameController?.CurrentLevelName}");
    }

    /// <summary>
    /// Call this when a level is completed to check for achievement unlocks
    /// </summary>
    public void OnLevelComplete()
    {
        if (currentLevelConfig == null)
        {
            if (enableDebugLogging)
                Debug.LogWarning("No achievement config loaded for current level");
            return;
        }

        float levelCompletionTime = Time.time - levelStartTime;
        GameSessionData sessionData = gameDataManager?.SessionData;

        if (sessionData == null)
        {
            Debug.LogError("GameSessionData is null! Cannot check achievements.");
            return;
        }

        CheckAndUnlockAchievements(sessionData, levelCompletionTime);
    }

    /// <summary>
    /// Manually check a specific achievement
    /// </summary>
    public bool CheckAchievement(string achievementId)
    {
        if (currentLevelConfig == null)
            return false;

        var achievement = currentLevelConfig.GetAchievementById(achievementId);
        if (achievement == null)
            return false;

        GameSessionData sessionData = gameDataManager?.SessionData;
        if (sessionData == null)
            return false;

        float currentTime = Time.time - levelStartTime;
        return achievement.CheckCondition(sessionData, currentTime);
    }

    /// <summary>
    /// Get progress description for a specific achievement
    /// </summary>
    public string GetAchievementProgress(string achievementId)
    {
        if (currentLevelConfig == null)
            return "No level config loaded";

        var achievement = currentLevelConfig.GetAchievementById(achievementId);
        if (achievement == null)
            return "Achievement not found";

        GameSessionData sessionData = gameDataManager?.SessionData;
        if (sessionData == null)
            return "No session data";

        float currentTime = Time.time - levelStartTime;
        return achievement.GetProgressDescription(sessionData, currentTime);
    }

    /// <summary>
    /// Get all achievements for the current level
    /// </summary>
    public List<LevelAchievementConfig.LevelAchievement> GetCurrentLevelAchievements()
    {
        return currentLevelConfig?.Achievements ?? new List<LevelAchievementConfig.LevelAchievement>();
    }

    /// <summary>
    /// Get configuration for a specific level
    /// </summary>
    public LevelAchievementConfig GetLevelConfig(string levelName)
    {
        return levelConfigs.Find(config => config.LevelName == levelName);
    }

    /// <summary>
    /// Add a new level configuration
    /// </summary>
    public void AddLevelConfig(LevelAchievementConfig config)
    {
        if (config != null && !levelConfigs.Contains(config))
        {
            levelConfigs.Add(config);
        }
    }

    /// <summary>
    /// Get all newly unlocked achievements from the last level completion
    /// </summary>
    public List<string> GetNewlyUnlockedAchievements()
    {
        return new List<string>(newlyUnlockedAchievements);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Check all achievements for the current level and unlock any that are completed
    /// </summary>
    private void CheckAndUnlockAchievements(GameSessionData sessionData, float levelTime)
    {
        if (currentLevelConfig == null)
            return;

        var pendingAchievements = currentLevelConfig.GetPendingAchievements();

        if (enableDebugLogging)
            Debug.Log($"Checking {pendingAchievements.Count} pending achievements...");

        foreach (var achievement in pendingAchievements)
        {
            if (achievement.CheckCondition(sessionData, levelTime))
            {
                UnlockAchievement(achievement);
            }
            else if (enableDebugLogging)
            {
                Debug.Log($"Achievement '{achievement.AchievementTitle}' not met: {achievement.GetProgressDescription(sessionData, levelTime)}");
            }
        }

        // Show summary if any achievements were unlocked
        if (newlyUnlockedAchievements.Count > 0)
        {
            ShowAchievementSummary();
        }
    }

    /// <summary>
    /// Unlock a specific achievement
    /// </summary>
    private void UnlockAchievement(LevelAchievementConfig.LevelAchievement achievement)
    {
        // Unlock using Reach.UI system
        achievement.Unlock();

        // Apply rewards
        achievement.ApplyRewards();

        // Track newly unlocked
        newlyUnlockedAchievements.Add(achievement.AchievementTitle);

        // Fire events
        OnAchievementUnlocked.Invoke(achievement.AchievementTitle);
        OnAchievementComplete.Invoke(achievement);

        // Show notification
        if (showAchievementNotifications)
        {
            ShowAchievementNotification(achievement);
        }

        if (enableDebugLogging)
        {
            Debug.Log($"Achievement Unlocked: {achievement.AchievementTitle} " +
                     $"(+{achievement.CurrencyReward} currency, +{achievement.XpReward} XP)");
        }
    }

    /// <summary>
    /// Show a notification for a newly unlocked achievement
    /// </summary>
    private void ShowAchievementNotification(LevelAchievementConfig.LevelAchievement achievement)
    {
        if (achievementNotificationPrefab == null || notificationParent == null)
        {
            Debug.LogWarning("Achievement notification prefab or parent not set!");
            return;
        }

        StartCoroutine(ShowNotificationCoroutine(achievement));
    }

    /// <summary>
    /// Coroutine to handle achievement notification display
    /// </summary>
    private IEnumerator ShowNotificationCoroutine(LevelAchievementConfig.LevelAchievement achievement)
    {
        GameObject notification = Instantiate(achievementNotificationPrefab, notificationParent);

        // Basic notification setup - can be extended with custom components later
        Debug.Log($"Achievement Notification: {achievement.AchievementTitle} - {achievement.AchievementDescription}");

        yield return new WaitForSeconds(notificationDuration);

        if (notification != null)
        {
            Destroy(notification);
        }
    }

    /// <summary>
    /// Show a summary of all achievements unlocked in the session
    /// </summary>
    private void ShowAchievementSummary()
    {
        if (enableDebugLogging)
        {
            Debug.Log($"Level completed! Unlocked {newlyUnlockedAchievements.Count} achievements:");
            foreach (var achievementTitle in newlyUnlockedAchievements)
            {
                Debug.Log($"  - {achievementTitle}");
            }
        }
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Test Current Level Achievements")]
    private void TestCurrentLevelAchievements()
    {
        if (currentLevelConfig == null)
        {
            Debug.LogWarning("No current level config loaded");
            return;
        }

        Debug.Log($"Testing achievements for level: {currentLevelConfig.LevelName}");

        GameSessionData testData = gameDataManager?.SessionData ?? new GameSessionData();
        float testTime = 45f; // Test with 45 seconds

        foreach (var achievement in currentLevelConfig.Achievements)
        {
            bool met = achievement.CheckCondition(testData, testTime);
            string progress = achievement.GetProgressDescription(testData, testTime);
            Debug.Log($"[{(met ? "✓" : "✗")}] {achievement.AchievementTitle}: {progress}");
        }
    }

    [ContextMenu("Force Unlock All Current Level Achievements")]
    private void ForceUnlockAllCurrentAchievements()
    {
        if (currentLevelConfig == null)
        {
            Debug.LogWarning("No current level config loaded");
            return;
        }

        foreach (var achievement in currentLevelConfig.Achievements)
        {
            if (!achievement.IsUnlocked())
            {
                UnlockAchievement(achievement);
            }
        }
    }

    [ContextMenu("Print Achievement Status")]
    private void PrintAchievementStatus()
    {
        Debug.Log($"=== Achievement Manager Status ===");
        Debug.Log($"Current Level: {gameController?.CurrentLevelName ?? "None"}");
        Debug.Log($"Level Config Loaded: {currentLevelConfig != null}");
        Debug.Log($"Total Level Configs: {levelConfigs.Count}");

        if (currentLevelConfig != null)
        {
            Debug.Log($"Current Level Achievements: {currentLevelConfig.Achievements.Count}");
            foreach (var achievement in currentLevelConfig.Achievements)
            {
                string status = achievement.IsUnlocked() ? "UNLOCKED" : "LOCKED";
                Debug.Log($"  - {achievement.AchievementTitle} [{status}]");
            }
        }
    }
    #endregion
}
