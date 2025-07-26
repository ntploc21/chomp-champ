using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to hold persistent player data.
/// This class is used to store the player's progression, stats, and other relevant information
/// that persists between game sessions
/// </summary>
[Serializable]
public class PlayerData
{
  #region Editor Data
  [Header("Player Info")]
  [Tooltip("Player's display name.")]
  public string playerName = "Player";
  
  [Header("Game Progression")]
  [Tooltip("Player level across gameplay.")]
  public int currentLevel = 1;
  [Tooltip("Total experience points accumulated across all sessions.")]
  public float totalExperienceEarned = 0f;
  [Tooltip("Best (highest) score achieved.")]
  public Dictionary<string, float> bestScore = new Dictionary<string, float>
  {
    { "C0L1", 0f } // Initialize with first level
  };
  [Tooltip("Total play time across all sessions in seconds.")]
  public float totalPlayTime = 0f;
  
  [Header("Game Statistics")]
  [Tooltip("Total number of fish eaten across all sessions.")]
  public int totalFishEaten = 0;
  [Tooltip("Total number of games played.")]
  public int gamesPlayed = 0;
  [Tooltip("Total number of deaths.")]
  public int totalDeaths = 0;
  [Tooltip("Total score across all sessions.")]
  public float totalScore = 0f;
  
  [Header("Level Progression")]
  [Tooltip("Array of level names that have been unlocked.")]
  public string[] unlockedLevels = new string[] { "C0L1" }; // Start with first level unlocked
  [Tooltip("Array of level names that have been completed.")]
  public string[] completedLevels = new string[0];
  
  [Header("Achievements")]
  [Tooltip("List of achievements unlocked by the player.")]
  public List<string> achievements = new List<string>();

  [Header("Shopping")]
  [Tooltip("Player's currency balance.")]
  public int currencyBalance = 0; // Player's currency balance for shopping

  [Header("Settings & Preferences")]
  [Tooltip("Last save timestamp.")]
  public string lastSaveTime = "";
  #endregion

  // Constructor
  public PlayerData()
  {
    ResetToDefaults();
  }
  /// <summary>
  /// Resets the player data to default values.
  /// This method is called when starting a new game or resetting the player data.
  /// </summary>
  public void ResetToDefaults()
  {
    // Reset player info
    playerName = "Player";

    // Reset game progression
    currentLevel = 1;
    totalExperienceEarned = 0f;
    bestScore = new Dictionary<string, float>
    {
      { "C0L1", 0f },
      { "C0L2", 0f },
      { "C0L3", 0f },
      { "C1L1", 0f },
      { "C1L2", 0f },
      { "C1L3", 0f },
      { "C2L1", 0f },
      { "C2L2", 0f },
      { "C2L3", 0f },
      { "C3L1", 0f },
      { "C3L2", 0f },
      { "C3L3", 0f }
    };

    // Reset statistics
    totalPlayTime = 0f;
    totalFishEaten = 0;
    gamesPlayed = 0;
    totalDeaths = 0;

    // Reset level progression
    unlockedLevels = new string[] { "C0L1", "C1L1", "C2L1", "C3L1" }; // Start with first levels of each chapter unlocked
    completedLevels = new string[0];

    // Reset Achievements
    achievements.Clear();

    // Reset shopping
    currencyBalance = 0;

    // Reset settings
    lastSaveTime = System.DateTime.Now.ToString();
  }
  
  /// <summary>
  /// Updates the last save timestamp to current time.
  /// </summary>
  public void UpdateSaveTime()
  {
    lastSaveTime = System.DateTime.Now.ToString();
  }
}