using UnityEngine;

[System.Serializable]
public class GrowthThreshold
{
  public float sizeThreshold;
  public string unlockName;
  public Sprite newSprite;
  public RuntimeAnimatorController newAnimator;
  public float speedModifier = 1f;
  public bool unlocked = false;
}

public class PlayerGrowth : MonoBehaviour
{
  [Header("Growth Settings")]
  [SerializeField] private float minSize = 0.5f;
  [SerializeField] private float maxSize = 10f;
  [SerializeField] private float baseGrowthRate = 0.1f;

  [Header("Growth Thresholds")]
  [SerializeField] private GrowthThreshold[] growthThresholds;

  [Header("Visual Settings")]
  [SerializeField] private bool scaleSprite = true;
  [SerializeField] private bool scaleCollider = true;
  [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

  private PlayerCore playerCore;
  private SpriteRenderer spriteRenderer;
  private Collider2D playerCollider;
  private Animator animator;

  private float targetSize;
  private float currentGrowthAnimation;
  private bool isGrowing = false;

  // Events
  public System.Action<float> OnSizeChanged;
  public System.Action<GrowthThreshold> OnThresholdUnlocked;

  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    playerCollider = GetComponent<Collider2D>();
    animator = GetComponent<Animator>();
  }

  private void Update()
  {
    if (isGrowing)
    {
      UpdateGrowthAnimation();
    }
  }

  public void Initialize(PlayerCore core)
  {
    playerCore = core;
    targetSize = playerCore.currentSize;

    // Initialize visual scale
    UpdateVisualScale(playerCore.currentSize);
  }

  public void Grow(float growthAmount)
  {
    float newSize = playerCore.currentSize + (growthAmount * baseGrowthRate);
    newSize = Mathf.Clamp(newSize, minSize, maxSize);

    SetSize(newSize);
  }

  public void SetSize(float newSize)
  {
    newSize = Mathf.Clamp(newSize, minSize, maxSize);

    if (Mathf.Approximately(newSize, playerCore.currentSize)) return;

    float previousSize = playerCore.currentSize;
    targetSize = newSize;
    playerCore.currentSize = newSize;

    // Start growth animation
    StartGrowthAnimation();

    // Check for threshold unlocks
    CheckGrowthThresholds(previousSize, newSize);

    // Update player core
    playerCore.UpdateSize(newSize);

    // Fire events
    OnSizeChanged?.Invoke(newSize);
  }

  private void StartGrowthAnimation()
  {
    if (!isGrowing)
    {
      isGrowing = true;
      currentGrowthAnimation = 0f;
    }
  }

  private void UpdateGrowthAnimation()
  {
    const float animationSpeed = 3f;
    currentGrowthAnimation += Time.deltaTime * animationSpeed;

    if (currentGrowthAnimation >= 1f)
    {
      currentGrowthAnimation = 1f;
      isGrowing = false;
    }

    // Apply curve to animation
    float curveValue = growthCurve.Evaluate(currentGrowthAnimation);
    float currentVisualSize = Mathf.Lerp(transform.localScale.x, targetSize, curveValue);

    UpdateVisualScale(currentVisualSize);
  }

  private void UpdateVisualScale(float size)
  {
    if (scaleSprite)
    {
      transform.localScale = Vector3.one * size;
    }

    if (scaleCollider && playerCollider != null)
    {
      if (playerCollider is CircleCollider2D circleCol)
      {
        circleCol.radius = 0.5f * size;
      }
      else if (playerCollider is BoxCollider2D boxCol)
      {
        boxCol.size = Vector2.one * size;
      }
    }
  }

  private void CheckGrowthThresholds(float previousSize, float newSize)
  {
    if (growthThresholds == null) return;

    foreach (var threshold in growthThresholds)
    {
      if (!threshold.unlocked &&
          previousSize < threshold.sizeThreshold &&
          newSize >= threshold.sizeThreshold)
      {
        UnlockThreshold(threshold);
      }
    }
  }

  private void UnlockThreshold(GrowthThreshold threshold)
  {
    threshold.unlocked = true;

    // Apply visual changes
    if (threshold.newSprite != null && spriteRenderer != null)
    {
      spriteRenderer.sprite = threshold.newSprite;
    }

    if (threshold.newAnimator != null && animator != null)
    {
      animator.runtimeAnimatorController = threshold.newAnimator;
    }

    // Play unlock effect
    if (playerCore?.Effect != null)
    {
      playerCore.Effect.PlayEvolutionEffect();
    }

    // Fire event
    OnThresholdUnlocked?.Invoke(threshold);

    Debug.Log($"Player evolved! Unlocked: {threshold.unlockName} at size {threshold.sizeThreshold}");
  }

  public void Shrink(float shrinkAmount)
  {
    float newSize = playerCore.currentSize - (shrinkAmount * baseGrowthRate);
    SetSize(newSize);
  }

  public float GetSizeRatio()
  {
    return (playerCore.currentSize - minSize) / (maxSize - minSize);
  }

  public bool CanEat(float otherSize)
  {
    return playerCore.currentSize > otherSize + 0.1f; // Small tolerance
  }

  public bool CanBeEatenBy(float otherSize)
  {
    return otherSize > playerCore.currentSize + 0.1f; // Small tolerance
  }

  public GrowthThreshold GetCurrentThreshold()
  {
    if (growthThresholds == null) return null;

    GrowthThreshold currentThreshold = null;

    foreach (var threshold in growthThresholds)
    {
      if (threshold.unlocked &&
          (currentThreshold == null || threshold.sizeThreshold > currentThreshold.sizeThreshold))
      {
        currentThreshold = threshold;
      }
    }

    return currentThreshold;
  }

  public GrowthThreshold GetNextThreshold()
  {
    if (growthThresholds == null) return null;

    foreach (var threshold in growthThresholds)
    {
      if (!threshold.unlocked && playerCore.currentSize < threshold.sizeThreshold)
      {
        return threshold;
      }
    }

    return null; // All thresholds unlocked
  }
}
