using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerGrowth : MonoBehaviour
{
  #region Editor Data
  #endregion

  #region Runtime Data
  // Note: Level and XP data is now managed by PlayerDataManager
  // This component now focuses only on visual size management
  #endregion

  #region Events
  public event UnityAction<int, float> OnLevelUp;
  public event UnityAction<float, float> OnXPChanged;
  #endregion

  #region Internal Data
  private LevelData levelData;
  private PlayerCore playerCore;
  private float baseSize = 1f;
  #endregion

  #region Properties
  public int Level => playerCore?.Level ?? 1;
  public float CurrentXP => playerCore?.DataManager?.Data.currentXP ?? 0f;
  public float XPToNextLevel => playerCore?.DataManager?.Data.xpToNextLevel ?? 100f;
  #endregion

  public void Initialize(PlayerCore core)
  {
    playerCore = core;
    baseSize = playerCore.CurrentSize;
    
    // Subscribe to data manager events
    if (playerCore.DataManager != null)
    {
      playerCore.DataManager.OnLevelUp.AddListener(OnLevelUpFromDataManager);
      playerCore.DataManager.OnXPChanged.AddListener(OnXPChangedFromDataManager);
    }
  }

  public void AddXP(float xpAmount)
  {
    // Delegate XP management to PlayerDataManager
    if (playerCore?.DataManager != null)
    {
      playerCore.DataManager.AddXP(xpAmount);
    }
  }

  public void SetSize(float newSize, bool isImmediate = true)
  {
    transform.localScale = new Vector3(newSize, newSize, 1f);
    
    // The size is now stored in PlayerDataManager, so we don't need to set it on PlayerCore
    // The DataManager will handle size updates through its level system
  }

  public void ResetGrowth()
  {
    // Reset through data manager
    if (playerCore?.DataManager != null)
    {
      playerCore.DataManager.ResetPlayerData();
    }
    
    SetSize(baseSize, true);
  }

  private void OnLevelUpFromDataManager(int newLevel)
  {
    // Update visual size based on the new level
    ApplyGrowth();
    
    // Forward the event
    OnLevelUp?.Invoke(newLevel, playerCore.CurrentSize);
  }

  private void OnXPChangedFromDataManager(float currentXP, float xpToNextLevel)
  {
    // Forward the event
    OnXPChanged?.Invoke(currentXP, xpToNextLevel);
  }

  private void ApplyGrowth()
  {
    // Calculate new size based on growth factor and current level from data manager
    if (playerCore?.DataManager?.LevelConfig != null)
    {
      float newSize = playerCore.DataManager.LevelConfig.GetSizeForLevel(Level);
      SetSize(newSize, true);
    }
    
    // Play growth effect
    if (playerCore?.Effect != null)
    {
      playerCore.Effect.PlayGrowthEffect();
    }
  }

  private void OnDestroy()
  {
    // Unsubscribe from events
    if (playerCore?.DataManager != null)
    {
      playerCore.DataManager.OnLevelUp.RemoveListener(OnLevelUpFromDataManager);
      playerCore.DataManager.OnXPChanged.RemoveListener(OnXPChangedFromDataManager);
    }
  }
}
