using UnityEngine;
using System;

/// <summary>
/// Custom achievement condition that allows for complex logic via delegate functions
/// This enables custom achievement conditions that can access full game session data
/// </summary>
[System.Serializable]
public class CustomFunctionCondition : AchievementCondition
{
  [Header("Custom Function Settings")]
  [SerializeField] private string functionName = "CustomCheck";
  [TextArea(3, 6)]
  [SerializeField] private string functionDescription = "Custom condition logic";

  // Delegate for custom condition checking
  public Func<GameSessionData, float, bool> CustomConditionCheck { get; set; }

  // Delegate for custom progress description
  public Func<GameSessionData, float, string> CustomProgressDescription { get; set; }

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    if (CustomConditionCheck != null)
      return CustomConditionCheck(sessionData, gameTime);

    // Default fallback - always return false if no custom function is set
    return false;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    if (CustomProgressDescription != null)
      return CustomProgressDescription(sessionData, gameTime);

    return base.GetProgressDescription(sessionData, gameTime);
  }

  /// <summary>
  /// Set a custom condition checking function
  /// </summary>
  public void SetCustomCondition(Func<GameSessionData, float, bool> conditionFunc)
  {
    CustomConditionCheck = conditionFunc;
  }

  /// <summary>
  /// Set a custom progress description function
  /// </summary>
  public void SetCustomProgressDescription(Func<GameSessionData, float, string> progressFunc)
  {
    CustomProgressDescription = progressFunc;
  }
}

/// <summary>
/// Static helper class for common custom achievement conditions
/// These can be used as examples or directly assigned to CustomFunctionCondition
/// </summary>
public static class CommonCustomConditions
{
  /// <summary>
  /// Example: Complete level while maintaining a minimum size throughout
  /// </summary>
  public static bool MaintainMinimumSize(GameSessionData sessionData, float gameTime, float minimumSize = 1.5f)
  {
    // This would require tracking minimum size throughout the game
    // For now, we'll check current size as an example
    return sessionData.currentSize >= minimumSize;
  }

  /// <summary>
  /// Example: Complete level within time limit AND with minimum score
  /// </summary>
  public static bool SpeedAndScore(GameSessionData sessionData, float gameTime, float maxTime = 45f, float minScore = 2000f)
  {
    return gameTime <= maxTime && sessionData.score >= minScore;
  }

  /// <summary>
  /// Example: Eat at least X enemies of a specific level (would need enemy tracking)
  /// </summary>
  public static bool EatSpecificLevelEnemies(GameSessionData sessionData, float gameTime, int minCount = 5, int enemyLevel = 1)
  {
    // This would require more detailed enemy tracking in GameSessionData
    // For now, we'll use a simplified version
    return sessionData.enemiesEaten >= minCount;
  }

  /// <summary>
  /// Example: Perfect run - no damage taken and high score
  /// </summary>
  public static bool PerfectRun(GameSessionData sessionData, float gameTime, int startingLives = 3, float minScore = 1500f)
  {
    bool noDamageTaken = sessionData.lives >= startingLives;
    bool highScore = sessionData.score >= minScore;
    return noDamageTaken && highScore;
  }

  /// <summary>
  /// Example: Level completion with specific XP gain
  /// </summary>
  public static bool XPGainer(GameSessionData sessionData, float gameTime, float minXP = 100f)
  {
    return sessionData.totalXP >= minXP;
  }

  /// <summary>
  /// Example: Score within a specific range (for precision challenges)
  /// </summary>
  public static bool PrecisionScore(GameSessionData sessionData, float gameTime, float minScore = 1000f, float maxScore = 1100f)
  {
    return sessionData.score >= minScore && sessionData.score <= maxScore;
  }

  /// <summary>
  /// Example: Complete level after reaching a certain player level
  /// </summary>
  public static bool LevelUpChallenge(GameSessionData sessionData, float gameTime, int requiredPlayerLevel = 5)
  {
    return sessionData.currentLevel >= requiredPlayerLevel;
  }

  /// <summary>
  /// Get progress description for MaintainMinimumSize condition
  /// </summary>
  public static string MaintainMinimumSizeProgress(GameSessionData sessionData, float gameTime, float minimumSize = 1.5f)
  {
    return $"Maintain Size: {sessionData.currentSize:F2} / {minimumSize:F2} (Required)";
  }

  /// <summary>
  /// Get progress description for SpeedAndScore condition
  /// </summary>
  public static string SpeedAndScoreProgress(GameSessionData sessionData, float gameTime, float maxTime = 45f, float minScore = 2000f)
  {
    bool timeOk = gameTime <= maxTime;
    bool scoreOk = sessionData.score >= minScore;
    return $"Time: {gameTime:F1}s/{maxTime:F1}s {(timeOk ? "✓" : "✗")}, Score: {sessionData.score:N0}/{minScore:N0} {(scoreOk ? "✓" : "✗")}";
  }

  /// <summary>
  /// Get progress description for PerfectRun condition
  /// </summary>
  public static string PerfectRunProgress(GameSessionData sessionData, float gameTime, int startingLives = 3, float minScore = 1500f)
  {
    bool noDamage = sessionData.lives >= startingLives;
    bool highScore = sessionData.score >= minScore;
    return $"No Damage: {(noDamage ? "✓" : "✗")}, High Score: {sessionData.score:N0}/{minScore:N0} {(highScore ? "✓" : "✗")}";
  }
}
