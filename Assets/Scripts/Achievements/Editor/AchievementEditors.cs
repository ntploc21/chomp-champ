#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Michsky.UI.Reach;

/// <summary>
/// Custom editor for LevelAchievementConfig to make it easier to set up achievements
/// </summary>
[CustomEditor(typeof(LevelAchievementConfig))]
public class LevelAchievementConfigEditor : Editor
{
  private SerializedProperty levelNameProp;
  private SerializedProperty levelDisplayNameProp;
  private SerializedProperty levelDescriptionProp;
  private SerializedProperty achievementsProp;

  private void OnEnable()
  {
    levelNameProp = serializedObject.FindProperty("levelName");
    levelDisplayNameProp = serializedObject.FindProperty("levelDisplayName");
    levelDescriptionProp = serializedObject.FindProperty("levelDescription");
    achievementsProp = serializedObject.FindProperty("achievements");
  }

  public override void OnInspectorGUI()
  {
    serializedObject.Update();

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Level Achievement Configuration", EditorStyles.boldLabel);
    EditorGUILayout.Space();

    // Level Info
    EditorGUILayout.PropertyField(levelNameProp);
    EditorGUILayout.PropertyField(levelDisplayNameProp);
    EditorGUILayout.PropertyField(levelDescriptionProp);

    EditorGUILayout.Space();

    // Achievements list
    EditorGUILayout.PropertyField(achievementsProp);

    EditorGUILayout.Space();

    // Utility buttons
    EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Add Basic Achievement"))
    {
      AddBasicAchievement();
    }
    if (GUILayout.Button("Add Speed Achievement"))
    {
      AddSpeedAchievement();
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Add Score Achievement"))
    {
      AddScoreAchievement();
    }
    if (GUILayout.Button("Add Perfect Run"))
    {
      AddPerfectRunAchievement();
    }
    EditorGUILayout.EndHorizontal();

    if (GUILayout.Button("Clear All Achievements"))
    {
      if (EditorUtility.DisplayDialog("Clear Achievements", "Are you sure you want to clear all achievements?", "Yes", "No"))
      {
        ClearAchievements();
      }
    }

    EditorGUILayout.Space();

    // Validation
    var config = target as LevelAchievementConfig;
    if (config != null)
    {
      if (!config.IsValid())
      {
        EditorGUILayout.HelpBox("Configuration is not valid. Check that all achievements have IDs, titles, and conditions.", MessageType.Warning);
      }
      else
      {
        EditorGUILayout.HelpBox($"Configuration is valid. {config.Achievements.Count} achievements configured.", MessageType.Info);
      }

      // Show reward totals
      EditorGUILayout.LabelField($"Total Currency Rewards: {config.GetTotalCurrencyRewards()}");
      EditorGUILayout.LabelField($"Total XP Rewards: {config.GetTotalXpRewards()}");
    }

    serializedObject.ApplyModifiedProperties();
  }

  private void AddBasicAchievement()
  {
    var config = target as LevelAchievementConfig;
    if (config == null) return;

    var achievement = new LevelAchievementConfig.LevelAchievement();
    // Set up basic completion achievement
    SetPrivateField(achievement, "achievementId", $"{config.LevelName}_completion");
    SetPrivateField(achievement, "achievementTitle", "Level Complete");
    SetPrivateField(achievement, "achievementDescription", "Complete the level");
    SetPrivateField(achievement, "achievementType", AchievementLibrary.AchievementType.Common);
    SetPrivateField(achievement, "condition", new ScoreCondition());
    SetPrivateField(achievement, "currencyReward", 50);
    SetPrivateField(achievement, "xpReward", 25f);

    config.Achievements.Add(achievement);
    EditorUtility.SetDirty(config);
  }

  private void AddSpeedAchievement()
  {
    var config = target as LevelAchievementConfig;
    if (config == null) return;

    var achievement = new LevelAchievementConfig.LevelAchievement();
    var timeCondition = new TimeCondition();
    SetPrivateField(timeCondition, "maxTimeSeconds", 45f);

    SetPrivateField(achievement, "achievementId", $"{config.LevelName}_speed");
    SetPrivateField(achievement, "achievementTitle", "Speed Runner");
    SetPrivateField(achievement, "achievementDescription", "Complete the level quickly");
    SetPrivateField(achievement, "achievementType", AchievementLibrary.AchievementType.Rare);
    SetPrivateField(achievement, "condition", timeCondition);
    SetPrivateField(achievement, "currencyReward", 150);
    SetPrivateField(achievement, "xpReward", 75f);

    config.Achievements.Add(achievement);
    EditorUtility.SetDirty(config);
  }

  private void AddScoreAchievement()
  {
    var config = target as LevelAchievementConfig;
    if (config == null) return;

    var achievement = new LevelAchievementConfig.LevelAchievement();
    var scoreCondition = new ScoreCondition();
    SetPrivateField(scoreCondition, "minimumScore", 2000f);

    SetPrivateField(achievement, "achievementId", $"{config.LevelName}_high_score");
    SetPrivateField(achievement, "achievementTitle", "High Scorer");
    SetPrivateField(achievement, "achievementDescription", "Achieve a high score");
    SetPrivateField(achievement, "achievementType", AchievementLibrary.AchievementType.Common);
    SetPrivateField(achievement, "condition", scoreCondition);
    SetPrivateField(achievement, "currencyReward", 100);
    SetPrivateField(achievement, "xpReward", 50f);

    config.Achievements.Add(achievement);
    EditorUtility.SetDirty(config);
  }

  private void AddPerfectRunAchievement()
  {
    var config = target as LevelAchievementConfig;
    if (config == null) return;

    var achievement = new LevelAchievementConfig.LevelAchievement();
    var noDeathCondition = new NoDeathCondition();
    SetPrivateField(noDeathCondition, "maxLivesLost", 0);
    SetPrivateField(noDeathCondition, "startingLives", 3);

    SetPrivateField(achievement, "achievementId", $"{config.LevelName}_perfect");
    SetPrivateField(achievement, "achievementTitle", "Perfect Run");
    SetPrivateField(achievement, "achievementDescription", "Complete without losing any lives");
    SetPrivateField(achievement, "achievementType", AchievementLibrary.AchievementType.Legendary);
    SetPrivateField(achievement, "condition", noDeathCondition);
    SetPrivateField(achievement, "currencyReward", 300);
    SetPrivateField(achievement, "xpReward", 150f);

    config.Achievements.Add(achievement);
    EditorUtility.SetDirty(config);
  }

  private void ClearAchievements()
  {
    var config = target as LevelAchievementConfig;
    if (config == null) return;

    config.Achievements.Clear();
    EditorUtility.SetDirty(config);
  }

  private void SetPrivateField(object obj, string fieldName, object value)
  {
    var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (field != null)
    {
      field.SetValue(obj, value);
    }
  }
}

/// <summary>
/// Custom editor for FeedingFrenzyAchievementManager
/// </summary>
[CustomEditor(typeof(FFAchievementManager))]
public class FeedingFrenzyAchievementManagerEditor : Editor
{
  public override void OnInspectorGUI()
  {
    DrawDefaultInspector();

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

    var manager = target as FFAchievementManager;
    if (manager == null) return;

    if (Application.isPlaying)
    {
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Test Achievements"))
      {
        manager.SendMessage("TestCurrentLevelAchievements", SendMessageOptions.DontRequireReceiver);
      }
      if (GUILayout.Button("Force Unlock All"))
      {
        manager.SendMessage("ForceUnlockAllCurrentAchievements", SendMessageOptions.DontRequireReceiver);
      }
      EditorGUILayout.EndHorizontal();

      if (GUILayout.Button("Print Status"))
      {
        manager.SendMessage("PrintAchievementStatus", SendMessageOptions.DontRequireReceiver);
      }

      // Show current achievements
      var currentAchievements = manager.GetCurrentLevelAchievements();
      if (currentAchievements.Count > 0)
      {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Level Achievements:", EditorStyles.boldLabel);
        foreach (var achievement in currentAchievements)
        {
          string status = achievement.IsUnlocked() ? "[UNLOCKED]" : "[LOCKED]";
          EditorGUILayout.LabelField($"{status} {achievement.AchievementTitle}");
        }
      }
    }
    else
    {
      EditorGUILayout.HelpBox("Debug tools are available during play mode.", MessageType.Info);
    }
  }
}
#endif
