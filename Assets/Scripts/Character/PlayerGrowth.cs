using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerGrowth : MonoBehaviour
{
  #region Editor Data
  [Header("Growth Settings")]
  [SerializeField] private float growthFactor = 1.1f; // Growth factor per level

  [Header("XP Settings")]
  [SerializeField] private float initialXPToNextLevel = 100f; // Initial XP required for level 2
  [SerializeField] private float xpGrowthFactor = 1.2f; // XP growth factor per level
  #endregion

  #region Runtime Data
  [Header("Runtime Stats")]
  [Space(10)]
  public int currentLevel = 1; // Current level of the player
  public float currentXP = 0f; // Current XP of the player
  public float xpToNextLevel = 100f; // XP required for the next level
  #endregion

  #region Events
  public event UnityAction<int, float> OnLevelUp;
  public event UnityAction<float, float> OnXPChanged;
  #endregion

  #region Internal Data
  private PlayerCore playerCore;
  private float baseSize = 1f;
  #endregion

  #region Properties
  public int Level => currentLevel;
  public float CurrentXP => currentXP;
  public float XPToNextLevel => xpToNextLevel;
  #endregion

  public void Initialize(PlayerCore core)
  {
    playerCore = core;
    baseSize = playerCore.CurrentSize;
  }

  public void AddXP(float xpAmount)
  {
    if (!playerCore.IsAlive || xpAmount <= 0)
      return;

    currentXP += xpAmount;

    // Use a while loop to handle multiple level-ups in one call
    while (currentXP >= xpToNextLevel)
    {
      LevelUp();
    }

    // Notify listeners about the XP change
    OnXPChanged?.Invoke(currentXP, xpToNextLevel);
  }

  public void SetSize(float newSize, bool isImmediate = true)
  {
    transform.localScale = new Vector3(newSize, newSize, 1f);

    if (playerCore != null)
    {
      playerCore.currentSize = newSize;
    }
  }

  public void ResetGrowth()
  {
    currentLevel = 1;
    currentXP = 0f;
    xpToNextLevel = initialXPToNextLevel; // Reset to initial value
    SetSize(baseSize, true);

    // Notify listeners about reset
    OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    OnLevelUp?.Invoke(currentLevel, baseSize);
  }

  private void LevelUp()
  {
    // Subtract the XP needed for the next level
    currentXP -= xpToNextLevel;
    currentLevel++;

    // Calculate new XP requirement (example: increase by 20% each level)
    xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthFactor);

    // Apply the visual and logical growth
    ApplyGrowth();

    // Notify listeners about the level-up
    OnLevelUp?.Invoke(currentLevel, playerCore.CurrentSize);
  }

  private void ApplyGrowth()
  {
    // Calculate new size based on growth factor and current level
    float newSize = baseSize * Mathf.Pow(growthFactor, currentLevel - 1);
    SetSize(newSize, true);

    // Update player's current size
    if (playerCore != null)
    {
      playerCore.currentSize = newSize;
      playerCore.Effect?.PlayGrowthEffect();
    }
  }
}