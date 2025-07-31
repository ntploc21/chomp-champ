using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Class to hold all game session data.
/// This class is used to store the player's progress, stats, and other relevant information during a
/// game session.
/// It can be serialized for saving and loading purposes.
/// </summary>
[Serializable]
public class GameSessionData
{
  [Header("Core Stats")]
  [Tooltip("Number of lives the player has.")]
  public int lives = 3;
  [Tooltip("Current score of the player.")]
  public float score = 0f;
  [Tooltip("Number of enemies eaten by the player.")]
  public int enemiesEaten = 0;
  [Tooltip("Enemy eaten for each level.")]
  public Dictionary<int, int> enemiesEatenPerLevel = new Dictionary<int, int>();

  [Tooltip("Total play time in seconds.")]
  public float playTime = 0f;

  [Header("Size & Growth")]
  [Tooltip("Current size of the player character.")]
  public float currentSize = 1f;
  [Tooltip("Current level of the player character.")]
  public int currentLevel = 1;
  [Tooltip("Current experience points of the player character.")]
  public float currentXP = 0f;
  [Tooltip("Experience points required to reach the next level.")]
  public float xpToNextLevel = 100f;

  [Header("Game State")]
  [Tooltip("Reset XP to zero when the player dies?")]
  public bool resetXPOnDeath = true; // I.e., if the player at level 2 and die, he still at level 2 but currentXP is 0

  [Tooltip("Is the player currently alive?")]
  public bool isAlive = true;
  [Tooltip("Is the player currently invincible?")]
  public bool isInvincible = false;
  [Tooltip("Last known position of the player character.")]
  public Vector2 lastPosition = Vector2.zero;
  public float totalXP = 0f; // Total XP accumulated in the session
  public float maximumXP = 0f; // Maximum XP needed in the level

  // Constructor
  public GameSessionData()
  {
    ResetToDefaults();
  }

  public void ResetToDefaults()
  {
    // Reset all core stats to default values
    lives = 3;
    score = 0f;
    enemiesEaten = 0;
    enemiesEatenPerLevel.Clear();
    playTime = 0f;

    // Reset size and growth stats
    currentSize = 1f;
    currentLevel = 1;
    currentXP = 0f;
    xpToNextLevel = 100f;

    // Reset game state
    isAlive = true;
    isInvincible = false;
    lastPosition = Vector2.zero;
    totalXP = 0f;
    maximumXP = 0f; // Reset maximum XP
  }

  // Create a copy for save/load systems
  public GameSessionData Clone()
  {
    return JsonUtility.FromJson<GameSessionData>(JsonUtility.ToJson(this));
  }
}