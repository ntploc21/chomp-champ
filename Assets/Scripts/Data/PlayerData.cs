using System;
using UnityEngine;

/// <summary>
/// Class to hold player data.
/// This class is used to store the player's name, level, experience points, achievements, and other relevant information.
/// /// It can be serialized for saving and loading purposes.
/// </summary>
[Serializable]
public class PlayerData
{
  #region Editor Data
  [Header("Player Info")]
  [Tooltip("Unique identifier for the player.")]
  public string playerName = "Player"; // Default name
  [Tooltip("Current level of the player.")]
  public int currentLevel = 1; // Default level
  [Tooltip("Current experience points of the player.")]
  public float currentXP = 0f; // Default XP
  [Tooltip("Total play time in seconds.")]
  public float totalPlayTime = 0f; // Total play time in seconds

  [Header("Level Progression")]
  public string[] levelsCleared = new string[0]; // List of cleared levels

  [Header("Achievements")]
  [Tooltip("List of achievements unlocked by the player.")]
  public string[] achievements = new string[0]; // List of achievements
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
    // Reset player info to default values
    currentLevel = 1;
    currentXP = 0f;
    totalPlayTime = 0f;

    // Reset level progression
    levelsCleared = new string[0];

    // Reset achievements
    achievements = new string[0];
  }
}