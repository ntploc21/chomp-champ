using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "New Level Data", menuName = "Game/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    #region Editor Data
    [Header("Basic Growth Settings")]
    public float growthFactor = 1.1f; // Growth factor per level
    public float baseSize = 1f; // Starting size
    public float maxSize = 10f; // Maximum achievable size

    [Header("XP Settings")]
    public float initialXPToNextLevel = 100f; // Initial XP required for level 2
    public float xpGrowthFactor = 1.2f; // XP growth factor per level
    public AnimationCurve xpCurve = AnimationCurve.EaseInOut(0, 100, 10, 1000); // XP curve for levels

    [Header("Level Rewards")]
    public List<LevelReward> levelRewards = new List<LevelReward>();

    [Header("Special Levels")]
    public List<EvolutionLevel> evolutionLevels = new List<EvolutionLevel>();

    [Header("Progression Settings")]
    public int maxLevel = 50;
    public bool useExponentialGrowth = true;
    public float exponentialBase = 1.15f;

    [Header("Bonus Multipliers")]
    public float streakMultiplier = 1.5f; // Bonus for eating enemies in succession
    public float speedKillBonus = 2.0f; // Bonus for quick kills

    [Header("Debug")]
    public bool showDebugInfo = false;
    #endregion

    /// <summary>
    /// Calculate XP required for a specific level
    /// </summary>
    public float GetXPForLevel(int level)
    {
        if (level <= 1) return 0f;

        if (useExponentialGrowth)
        {
            return initialXPToNextLevel * Mathf.Pow(exponentialBase, level - 2);
        }
        else
        {
            return xpCurve.Evaluate(level);
        }
    }

    /// <summary>
    /// Calculate total XP needed to reach a specific level
    /// </summary>
    public float GetTotalXPForLevel(int level)
    {
        float total = 0f;
        for (int i = 2; i <= level; i++)
        {
            total += GetXPForLevel(i);
        }
        return total;
    }

    /// <summary>
    /// Get size for a specific level
    /// </summary>
    public float GetSizeForLevel(int level)
    {
        float size = baseSize * Mathf.Pow(growthFactor, level - 1);
        return Mathf.Min(size, maxSize);
    }

    /// <summary>
    /// Get level rewards for a specific level
    /// </summary>
    public LevelReward GetRewardForLevel(int level)
    {
        return levelRewards.Find(reward => reward.level == level);
    }

    /// <summary>
    /// Check if level is an evolution level
    /// </summary>
    public EvolutionLevel GetEvolutionForLevel(int level)
    {
        return evolutionLevels.Find(evo => evo.level == level);
    }

    /// <summary>
    /// Calculate XP bonus based on player size and enemy size
    /// </summary>
    public float CalculateXPBonus(float playerSize, float enemySize, bool isStreak = false, bool isSpeedKill = false)
    {
        float bonus = 1.0f;

        // Size reduction: if player is larger than enemy, reduce XP gain
        if (playerSize > enemySize)
        {
            float sizeDifference = playerSize - enemySize;
            bonus = Mathf.Max(0.1f, 1.0f - (sizeDifference / playerSize));
        }

        // Streak bonus
        if (isStreak)
            bonus *= streakMultiplier;

        // Speed kill bonus
        if (isSpeedKill)
            bonus *= speedKillBonus;

        return bonus;
    }
}

[System.Serializable]
public class LevelReward
{
    public int level;
    public string rewardName;
    public RewardType type;
    public float value;
    public Sprite icon;
    public string description;

    public enum RewardType
    {
        ExtraLife,
        SpeedBoost,
        SizeBonus,
        XPMultiplier,
        SpecialAbility,
        Achievement
    }
}

[System.Serializable]
public class EvolutionLevel
{
    public int level;
    public string evolutionName;
    public Sprite evolutionSprite;
    public string description;
    public Color evolutionColor = Color.white;
    public ParticleSystem evolutionEffect;
    public AudioClip evolutionSound;
    public float sizeMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public bool unlocksDashAbility = false;
    public bool unlocksNewMovement = false;
}
