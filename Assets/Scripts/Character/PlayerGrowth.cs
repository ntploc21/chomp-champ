using UnityEngine;
using System.Collections;

[System.Serializable]
public class GrowthThreshold
{
  public float sizeThreshold;
  public string unlockName;
  public Sprite newSprite;
  public RuntimeAnimatorController newAnimator;
  [Range(0.5f, 2f)] public float speedModifier = 1f;
  [Range(0.5f, 2f)] public float dashPowerModifier = 1f;
  public bool unlocked = false;
  
  [Header("Visual Effects")]
  public Color evolutionColor = Color.white;
  public ParticleSystem evolutionParticles;
  public AudioClip evolutionSFX;
}

public class PlayerGrowth : MonoBehaviour
{
  [Header("Growth Settings")]
  [SerializeField] private float minSize = 0.5f;
  [SerializeField] private float maxSize = 10f;
  [SerializeField] private float baseGrowthRate = 0.1f;
  [SerializeField] private AnimationCurve growthRateBySize = AnimationCurve.Linear(0, 1, 1, 0.5f);

  [Header("Growth Thresholds")]
  [SerializeField] private GrowthThreshold[] growthThresholds;

  [Header("Visual Settings")]
  [SerializeField] private bool scaleSprite = true;
  [SerializeField] private bool scaleCollider = true;
  [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  [SerializeField] private float animationSpeed = 3f;
    [Header("Performance")]
  [SerializeField] private bool enableSmoothGrowth = true;
  [SerializeField] private float growthBuffer = 0.1f; // Accumulate small growth changes
  [SerializeField] private int maxGrowthUpdatesPerFrame = 3;
  
  [Header("Debug")]
  [SerializeField] private bool enableDebugLogs = false;
  [SerializeField] private bool showGrowthGizmos = false;

  private PlayerCore playerCore;
  private SpriteRenderer spriteRenderer;
  private Collider2D playerCollider;
  private Animator animator;
  private PlayerEffect playerEffect;

  private float targetSize;
  private float currentGrowthAnimation;
  private bool isGrowing = false;
  private float accumulatedGrowth = 0f;
  private Coroutine growthCoroutine;

  // Caching for performance
  private GrowthThreshold cachedCurrentThreshold;
  private GrowthThreshold cachedNextThreshold;
  private bool thresholdCacheDirty = true;

  // Events
  public System.Action<float> OnSizeChanged;
  public System.Action<GrowthThreshold> OnThresholdUnlocked;
  public System.Action<float> OnGrowthStarted;
  public System.Action OnGrowthCompleted;
  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    playerCollider = GetComponent<Collider2D>();
    animator = GetComponent<Animator>();
    playerEffect = GetComponent<PlayerEffect>();
  }

  private void Update()
  {
    if (isGrowing)
    {
      UpdateGrowthAnimation();
    }

    // Handle accumulated growth for performance
    if (enableSmoothGrowth && accumulatedGrowth > growthBuffer)
    {
      ProcessAccumulatedGrowth();
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
    if (enableSmoothGrowth)
    {
      accumulatedGrowth += growthAmount;
    }
    else
    {
      ApplyGrowth(growthAmount);
    }
  }

  private void ProcessAccumulatedGrowth()
  {
    if (accumulatedGrowth > 0f)
    {
      ApplyGrowth(accumulatedGrowth);
      accumulatedGrowth = 0f;
    }
  }

  private void ApplyGrowth(float growthAmount)
  {
    float currentGrowthRate = baseGrowthRate * growthRateBySize.Evaluate(GetSizeRatio());
    float newSize = playerCore.currentSize + (growthAmount * currentGrowthRate);
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

    // Mark cache as dirty
    thresholdCacheDirty = true;

    // Start growth animation
    if (growthCoroutine != null)
    {
      StopCoroutine(growthCoroutine);
    }
    growthCoroutine = StartCoroutine(AnimateGrowth(previousSize, newSize));

    // Check for threshold unlocks
    CheckGrowthThresholds(previousSize, newSize);

    // Update player core
    playerCore.UpdateSize(newSize);

    // Fire events
    OnSizeChanged?.Invoke(newSize);
    OnGrowthStarted?.Invoke(newSize - previousSize);

    if (enableDebugLogs)
    {
      Debug.Log($"Player size changed from {previousSize:F2} to {newSize:F2}");
    }
  }

  private IEnumerator AnimateGrowth(float fromSize, float toSize)
  {
    isGrowing = true;
    float elapsedTime = 0f;
    float duration = 1f / animationSpeed;

    Vector3 startScale = transform.localScale;
    Vector3 targetScale = Vector3.one * toSize;

    while (elapsedTime < duration)
    {
      elapsedTime += Time.deltaTime;
      float progress = elapsedTime / duration;
      float curveValue = growthCurve.Evaluate(progress);
      
      float currentVisualSize = Mathf.Lerp(fromSize, toSize, curveValue);
      UpdateVisualScale(currentVisualSize);

      yield return null;
    }

    // Ensure final size is exact
    UpdateVisualScale(toSize);
    isGrowing = false;
    OnGrowthCompleted?.Invoke();
    growthCoroutine = null;
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
    thresholdCacheDirty = true;

    // Apply visual changes
    if (threshold.newSprite != null && spriteRenderer != null)
    {
      spriteRenderer.sprite = threshold.newSprite;
    }

    if (threshold.newAnimator != null && animator != null)
    {
      animator.runtimeAnimatorController = threshold.newAnimator;
    }

    // Play unlock effects
    StartCoroutine(PlayEvolutionEffect(threshold));

    // Fire event
    OnThresholdUnlocked?.Invoke(threshold);

    if (enableDebugLogs)
    {
      Debug.Log($"Player evolved! Unlocked: {threshold.unlockName} at size {threshold.sizeThreshold}");
    }
  }
  private IEnumerator PlayEvolutionEffect(GrowthThreshold threshold)
  {
    // Play particles
    if (threshold.evolutionParticles != null)
    {
      var particles = Instantiate(threshold.evolutionParticles, transform.position, Quaternion.identity);
      particles.Play();
      Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
    }

    // Play sound through AudioSource if available
    if (threshold.evolutionSFX != null)
    {
      AudioSource audioSource = GetComponent<AudioSource>();
      if (audioSource == null)
      {
        audioSource = gameObject.AddComponent<AudioSource>();
      }
      audioSource.PlayOneShot(threshold.evolutionSFX);
    }

    // Color flash effect
    if (spriteRenderer != null)
    {
      Color originalColor = spriteRenderer.color;
      float flashDuration = 0.5f;
      float elapsedTime = 0f;

      while (elapsedTime < flashDuration)
      {
        elapsedTime += Time.deltaTime;
        float alpha = Mathf.PingPong(elapsedTime * 10f, 1f);
        spriteRenderer.color = Color.Lerp(originalColor, threshold.evolutionColor, alpha * 0.5f);
        yield return null;
      }

      spriteRenderer.color = originalColor;
    }

    // Trigger player effect if available
    if (playerEffect != null)
    {
      playerEffect.PlayEvolutionEffect();
    }
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

  // Utility Methods
  public void ForceEvolution(int thresholdIndex)
  {
    if (growthThresholds != null && thresholdIndex >= 0 && thresholdIndex < growthThresholds.Length)
    {
      UnlockThreshold(growthThresholds[thresholdIndex]);
    }
  }

  public void ResetEvolutions()
  {
    if (growthThresholds == null) return;

    foreach (var threshold in growthThresholds)
    {
      threshold.unlocked = false;
    }
    thresholdCacheDirty = true;

    if (enableDebugLogs)
    {
      Debug.Log("All evolutions reset");
    }
  }

  public float GetProgressToNextThreshold()
  {
    var nextThreshold = GetNextThreshold();
    if (nextThreshold == null) return 1f; // All unlocked

    var currentThreshold = GetCurrentThreshold();
    float baseSize = currentThreshold?.sizeThreshold ?? minSize;
    
    return Mathf.Clamp01((playerCore.currentSize - baseSize) / (nextThreshold.sizeThreshold - baseSize));
  }

  public bool IsMaxSize()
  {
    return Mathf.Approximately(playerCore.currentSize, maxSize);
  }

  public bool IsMinSize()
  {
    return Mathf.Approximately(playerCore.currentSize, minSize);
  }

  public float GetGrowthRate()
  {
    float sizeRatio = GetSizeRatio();
    return baseGrowthRate * growthRateBySize.Evaluate(sizeRatio);
  }

  public void SetMaxSize(float newMaxSize)
  {
    maxSize = Mathf.Max(minSize, newMaxSize);
    SetSize(Mathf.Min(playerCore.currentSize, maxSize));
  }

  public void SetMinSize(float newMinSize)
  {
    minSize = Mathf.Max(0.1f, newMinSize);
    SetSize(Mathf.Max(playerCore.currentSize, minSize));
  }

  // Debug Methods
  public void DebugGrowth(float amount)
  {
    if (enableDebugLogs)
    {
      Debug.Log($"Debug growth: {amount}, Current size: {playerCore.currentSize:F2}");
    }
    Grow(amount);
  }

  public void DebugSetSize(float size)
  {
    if (enableDebugLogs)
    {
      Debug.Log($"Debug set size: {size}");
    }
    SetSize(size);
  }

  private void OnDrawGizmosSelected()
  {
    if (!showGrowthGizmos) return;

    // Draw size bounds
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, minSize);
    
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, maxSize);

    // Draw current size
    Gizmos.color = Color.yellow;
    float currentDisplaySize = playerCore != null ? playerCore.currentSize : transform.localScale.x;
    Gizmos.DrawWireSphere(transform.position, currentDisplaySize);

    // Draw threshold sizes
    if (growthThresholds != null)
    {
      foreach (var threshold in growthThresholds)
      {
        Gizmos.color = threshold.unlocked ? Color.cyan : Color.white;
        Gizmos.DrawWireSphere(transform.position, threshold.sizeThreshold);
      }
    }
  }
}
