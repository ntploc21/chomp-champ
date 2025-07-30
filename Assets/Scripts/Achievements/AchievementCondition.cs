using UnityEngine;
using System;

/// <summary>
/// Base class for achievement conditions that can be evaluated against game session data
/// </summary>
[System.Serializable]
public abstract class AchievementCondition
{
  [Header("Condition Settings")]
  [SerializeField] protected string conditionName = "New Condition";
  [SerializeField] protected string description = "Condition description";

  public string ConditionName => conditionName;
  public string Description => description;

  /// <summary>
  /// Evaluates if this condition is met based on the current game session data
  /// </summary>
  /// <param name="sessionData">Current game session data</param>
  /// <param name="gameTime">Total time spent in the level</param>
  /// <returns>True if condition is satisfied</returns>
  public abstract bool EvaluateCondition(GameSessionData sessionData, float gameTime);

  /// <summary>
  /// Gets a human-readable description of this condition's current progress
  /// </summary>
  /// <param name="sessionData">Current game session data</param>
  /// <param name="gameTime">Total time spent in the level</param>
  /// <returns>Progress description string</returns>
  public virtual string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    return EvaluateCondition(sessionData, gameTime) ? "✓ Completed" : "✗ Not completed";
  }
}

/// <summary>
/// Achievement condition based on minimum score requirement
/// </summary>
[System.Serializable]
public class ScoreCondition : AchievementCondition
{
  [Header("Score Requirements")]
  [SerializeField] private float minimumScore = 1000f;

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    return sessionData.score >= minimumScore;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    return $"Score: {sessionData.score:N0} / {minimumScore:N0}";
  }
}

/// <summary>
/// Achievement condition based on completion time
/// </summary>
[System.Serializable]
public class TimeCondition : AchievementCondition
{
  [Header("Time Requirements")]
  [SerializeField] private float maxTimeSeconds = 60f;
  [SerializeField] private bool useGameTime = true; // If false, uses real-time

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    float timeToCheck = useGameTime ? gameTime : sessionData.playTime;
    return timeToCheck <= maxTimeSeconds;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    float timeToCheck = useGameTime ? gameTime : sessionData.playTime;
    return $"Time: {timeToCheck:F1}s / {maxTimeSeconds:F1}s (Max)";
  }
}

/// <summary>
/// Achievement condition based on number of enemies eaten
/// </summary>
[System.Serializable]
public class EnemiesEatenCondition : AchievementCondition
{
  [Header("Enemy Requirements")]
  [SerializeField] private int minimumEnemies = 10;
  [SerializeField] private int level = -1; // Optional enemy level requirement, -1 means no specific level

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    if (level >= 0 && !sessionData.enemiesEatenPerLevel.ContainsKey(level))
      return false; // No data for this level

    int enemiesEaten = level >= 0 ? sessionData.enemiesEatenPerLevel[level] : sessionData.enemiesEaten;
    return enemiesEaten >= minimumEnemies;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    int enemiesEaten = level >= 0 ? sessionData.enemiesEatenPerLevel[level] : sessionData.enemiesEaten;
    return $"Enemies Eaten: {enemiesEaten} / {minimumEnemies}";
  }
}

/// <summary>
/// Achievement condition based on never dying (no lives lost)
/// </summary>
[System.Serializable]
public class NoDeathCondition : AchievementCondition
{
  [Header("Death Requirements")]
  [SerializeField] private int maxLivesLost = 0;
  [SerializeField] private int startingLives = 3; // Configure based on your game's starting lives

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    int livesLost = startingLives - sessionData.lives;
    return livesLost <= maxLivesLost;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    int livesLost = startingLives - sessionData.lives;
    return $"Lives Lost: {livesLost} / {maxLivesLost} (Max)";
  }
}

/// <summary>
/// Composite achievement condition that requires ALL sub-conditions to be met
/// </summary>
[System.Serializable]
public class CompositeAndCondition : AchievementCondition
{
  [Header("Sub-Conditions")]
  [SerializeReference] private AchievementCondition[] subConditions = new AchievementCondition[0];

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    if (subConditions == null || subConditions.Length == 0)
      return false;

    foreach (var condition in subConditions)
    {
      if (condition == null || !condition.EvaluateCondition(sessionData, gameTime))
        return false;
    }
    return true;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    if (subConditions == null || subConditions.Length == 0)
      return "No conditions set";

    int completed = 0;
    foreach (var condition in subConditions)
    {
      if (condition != null && condition.EvaluateCondition(sessionData, gameTime))
        completed++;
    }
    return $"Conditions Met: {completed} / {subConditions.Length}";
  }
}

/// <summary>
/// Composite achievement condition that requires ANY sub-condition to be met
/// </summary>
[System.Serializable]
public class CompositeOrCondition : AchievementCondition
{
  [Header("Sub-Conditions")]
  [SerializeReference] private AchievementCondition[] subConditions = new AchievementCondition[0];

  public override bool EvaluateCondition(GameSessionData sessionData, float gameTime)
  {
    if (subConditions == null || subConditions.Length == 0)
      return false;

    foreach (var condition in subConditions)
    {
      if (condition != null && condition.EvaluateCondition(sessionData, gameTime))
        return true;
    }
    return false;
  }

  public override string GetProgressDescription(GameSessionData sessionData, float gameTime)
  {
    if (subConditions == null || subConditions.Length == 0)
      return "No conditions set";

    foreach (var condition in subConditions)
    {
      if (condition != null && condition.EvaluateCondition(sessionData, gameTime))
        return "✓ At least one condition met";
    }
    return "✗ No conditions met";
  }
}
