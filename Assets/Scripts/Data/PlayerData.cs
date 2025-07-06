using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    [Header("Core Stats")]
    public int lives = 3;
    public float score = 0f;
    public int enemiesEaten = 0;
    public float playTime = 0f;

    [Header("Size & Growth")]
    public float currentSize = 1f;
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;

    [Header("Game State")]
    public bool isAlive = true;
    public bool isInvincible = false;
    public Vector2 lastPosition = Vector2.zero;

    // Constructor
    public PlayerData()
    {
        ResetToDefaults();
    }
    public void ResetToDefaults()
    {
        lives = 3;
        score = 0f;
        enemiesEaten = 0;
        playTime = 0f;
        currentSize = 1f;
        currentLevel = 1;
        currentXP = 0f;
        xpToNextLevel = 100f;
        isAlive = true;
        isInvincible = false;
        lastPosition = Vector2.zero;
    }

    // Create a copy for save/load systems
    public PlayerData Clone()
    {
        return JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(this));
    }
}
