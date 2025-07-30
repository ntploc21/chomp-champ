using UnityEngine;
using System.Collections.Generic;
using Michsky.UI.Reach;

/// <summary>
/// ScriptableObject that defines achievements available for a specific level
/// Integrates with the existing Reach.UI achievement system
/// </summary>
[CreateAssetMenu(fileName = "New Level Achievement Config", menuName = "Game/Level Achievement Config")]
public class LevelAchievementConfig : ScriptableObject
{
  [Header("Level Information")]
  [SerializeField] private string levelName = "C0L1";
  [SerializeField] private string levelDisplayName = "Ocean Depths - Level 1";
  [TextArea(2, 4)]
  [SerializeField] private string levelDescription = "The first level of your feeding frenzy adventure";

  [Header("Achievement Definitions")]
  [SerializeField] private List<LevelAchievement> achievements = new List<LevelAchievement>();

  public string LevelName => levelName;
  public List<LevelAchievement> Achievements => achievements;

  /// <summary>
  /// Get all achievements for this level that match a specific condition type
  /// </summary>
  public List<LevelAchievement> GetAchievementsByConditionType<T>() where T : AchievementCondition
  {
    List<LevelAchievement> result = new List<LevelAchievement>();
    foreach (var achievement in achievements)
    {
      if (achievement.Condition is T)
        result.Add(achievement);
    }
    return result;
  }

  /// <summary>
  /// Get achievement by its unique ID
  /// </summary>
  public LevelAchievement GetAchievementById(string achievementId)
  {
    return achievements.Find(a => a.AchievementId == achievementId);
  }

  /// <summary>
  /// Get all achievements that should be checked (not already unlocked)
  /// </summary>
  public List<LevelAchievement> GetPendingAchievements()
  {
    List<LevelAchievement> pending = new List<LevelAchievement>();
    foreach (var achievement in achievements)
    {
      if (!achievement.IsUnlocked())
        pending.Add(achievement);
    }
    return pending;
  }
  
  [System.Serializable]
  public class LevelAchievement
  {
    [Header("Achievement Identity")]
    public string achievementId = "unique_achievement_id";
    public string achievementTitle = "Achievement Title";
    [TextArea(2, 4)]
    public string achievementDescription = "Complete this task to unlock the achievement";

    [Header("Achievement Properties")]
    public AchievementLibrary.AchievementType achievementType = AchievementLibrary.AchievementType.Common;
    public Sprite achievementIcon;
    public bool isHidden = false;

    [Header("Achievement Condition")]
    [SerializeReference] public AchievementCondition condition;

    [Header("Rewards (Optional)")]
    public int currencyReward = 100;
    public float xpReward = 50f;        // Properties
    public string AchievementId => achievementId;
    public string AchievementTitle => achievementTitle;
    public string AchievementDescription => achievementDescription;
    public AchievementLibrary.AchievementType AchievementType => achievementType;
    public Sprite AchievementIcon => achievementIcon;
    public bool IsHidden => isHidden;
    public AchievementCondition Condition => condition;
    public int CurrencyReward => currencyReward;
    public float XpReward => xpReward;

    /// <summary>
    /// Check if this achievement meets its condition
    /// </summary>
    public bool CheckCondition(GameSessionData sessionData, float gameTime)
    {
      return condition != null && condition.EvaluateCondition(sessionData, gameTime);
    }

    /// <summary>
    /// Check if this achievement is already unlocked
    /// </summary>
    public bool IsUnlocked()
    {
      return PlayerPrefs.GetString("ACH_" + achievementTitle) == "true";
    }

    /// <summary>
    /// Unlock this achievement using the Reach.UI system
    /// </summary>
    public void Unlock()
    {
      AchievementManager.SetAchievement(achievementTitle, true);
      Debug.Log($"Achievement Unlocked: {achievementTitle}");
    }

    /// <summary>
    /// Get progress description for this achievement
    /// </summary>
    public string GetProgressDescription(GameSessionData sessionData, float gameTime)
    {
      if (condition == null)
        return "No condition set";

      return condition.GetProgressDescription(sessionData, gameTime);
    }

    /// <summary>
    /// Apply the rewards for this achievement to the player
    /// </summary>
    public void ApplyRewards()
    {
      if (currencyReward > 0)
      {
        PlayerDataManager.UpdatePlayerData(data =>
        {
          data.currencyBalance += currencyReward;
        }, true);
      }

      if (xpReward > 0)
      {
        PlayerDataManager.UpdatePlayerData(data =>
        {
          data.totalExperienceEarned += xpReward;
        }, true);
      }
    }

    /// <summary>
    /// Validate that this achievement is properly configured
    /// </summary>
    public bool IsValid()
    {
      return !string.IsNullOrEmpty(achievementId) &&
             !string.IsNullOrEmpty(achievementTitle) &&
             condition != null;
    }
  }

  /// <summary>
  /// Validate that this level achievement config is properly set up
  /// </summary>
  public bool IsValid()
  {
    if (string.IsNullOrEmpty(levelName))
      return false;

    foreach (var achievement in achievements)
    {
      if (!achievement.IsValid())
        return false;
    }

    return true;
  }

  /// <summary>
  /// Get total possible currency rewards for this level
  /// </summary>
  public int GetTotalCurrencyRewards()
  {
    int total = 0;
    foreach (var achievement in achievements)
    {
      total += achievement.CurrencyReward;
    }
    return total;
  }

  /// <summary>
  /// Get total possible XP rewards for this level
  /// </summary>
  public float GetTotalXpRewards()
  {
    float total = 0f;
    foreach (var achievement in achievements)
    {
      total += achievement.XpReward;
    }
    return total;
  }

#if UNITY_EDITOR
  [ContextMenu("Create Sample Achievements")]
  private void CreateSampleAchievements()
  {
    achievements.Clear();

    // Speed Runner achievement
    var speedRunner = new LevelAchievement();
    speedRunner.achievementId = levelName + "_speed_runner";
    speedRunner.achievementTitle = "Speed Runner";
    speedRunner.achievementDescription = "Complete the level in under 60 seconds";
    speedRunner.achievementType = AchievementLibrary.AchievementType.Rare;
    speedRunner.condition = new TimeCondition();
    speedRunner.currencyReward = 200;
    speedRunner.xpReward = 100f;

    // Glutton achievement
    var glutton = new LevelAchievement();
    glutton.achievementId = levelName + "_glutton";
    glutton.achievementTitle = "Glutton";
    glutton.achievementDescription = "Eat 25 enemies in a single level";
    glutton.achievementType = AchievementLibrary.AchievementType.Common;
    glutton.condition = new EnemiesEatenCondition();
    glutton.currencyReward = 150;
    glutton.xpReward = 75f;

    achievements.Add(speedRunner);
    achievements.Add(glutton);

    UnityEditor.EditorUtility.SetDirty(this);
  }
#endif
}
