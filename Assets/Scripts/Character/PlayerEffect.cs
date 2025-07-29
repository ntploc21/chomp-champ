using UnityEngine;
using System.Collections;
using System.Linq;
using Michsky.UI.Reach;
using Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

public class PlayerEffect : MonoBehaviour
{
  #region Editor Data
  [Header("Visual Effects")]
  [SerializeField] private SpriteRenderer spriteRenderer;
  [SerializeField] private Animator animator;

  [Header("Particle Effects")]
  [SerializeField] private ParticleSystem eatParticles;
  [SerializeField] private ParticleSystem growthParticles;
  [SerializeField] private ParticleSystem deathParticles;
  [SerializeField] private ParticleSystem dashParticles;
  [SerializeField] private ParticleSystem evolutionParticles;
  [SerializeField] private ParticleSystem spawnParticles;
  [Header("Screen Effects")]
  [SerializeField] private bool enableScreenShake = true;
  [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;
  [SerializeField] private float eatShakeDuration = 0.1f;
  [SerializeField] private float eatShakeIntensity = 0.1f;
  [SerializeField] private float dashShakeDuration = 0.2f;
  [SerializeField] private float dashShakeIntensity = 0.15f;

  [Header("Flash Effects")]
  [SerializeField] private Color eatFlashColor = Color.green;
  [SerializeField] private Color growthFlashColor = Color.yellow;
  [SerializeField] private Color evolutionFlashColor = Color.cyan;
  [SerializeField] private Color deathFlashColor = Color.red;
  [SerializeField] private Color dashFlashColor = Color.white;

  [Header("Animation Settings")]
  // [SerializeField] private float spawnDuration = 0.5f;
  // [SerializeField] private float growthPulseDuration = 0.3f;
  // [SerializeField] private float deathFadeDuration = 1f;
  [SerializeField] private float flickerRate = 0.1f;

  [Header("Performance")]
  [SerializeField] private bool enableParticlePooling = true;
  [SerializeField] private int maxConcurrentEffects = 5;

  [Header("Debug")]
  [SerializeField] private bool enableDebugLogs = false;

  [Header("Dash Effect Enhancement")]
  [SerializeField] private LayerMask groundLayerMask = 1;
  [SerializeField] private LayerMask wallLayerMask = 1 << 8;
  [SerializeField] private float groundDetectionRadius = 0.5f;
  [SerializeField] private bool enableGroundColorDetection = true;
  [SerializeField] private float colorDarkeningFactor = 0.4f;
  [SerializeField] private Color fallbackGroundColor = new Color(0.4f, 0.3f, 0.2f, 1f); // Default brown dirt color
  #endregion

  #region Properties
  public SpriteRenderer SpriteRenderer => spriteRenderer;
  public Animator Animator => animator;
  #endregion

  #region Internal Data
  private PlayerCore playerCore;
  private Camera playerCamera;
  private Color originalSpriteColor;

  // Flicker effect variables
  private bool isFlickering = false;
  private Coroutine flickerCoroutine;
  // Performance tracking
  private int currentEffectCount = 0;
  private bool effectsEnabled = true;

  // Screen shake management
  private Coroutine currentScreenShakeCoroutine;
  private float lastShakeTime = 0f;
  private float shakeMinInterval = 0.1f; // Minimum time between shakes

  // Caching
  private WaitForSeconds flickerWait;
  private WaitForSeconds flashWait;

  // Ground detection cache
  private Tilemap[] cachedGroundTilemaps;
  private float lastGroundCacheTime;
  private readonly float groundCacheInterval = 2f;
  #endregion
  private void Awake()
  {
    if (spriteRenderer == null)
      spriteRenderer = GetComponent<SpriteRenderer>();

    if (animator == null)
      animator = GetComponent<Animator>();

    // Store original color
    if (spriteRenderer != null)
    {
      originalSpriteColor = spriteRenderer.color;
    }
    else
    {
      if (enableDebugLogs)
        Debug.LogWarning("PlayerEffect: SpriteRenderer not found! Visual effects may not work.", this);
    }

    // Find camera for screen shake
    playerCamera = Camera.main;
    if (playerCamera == null)
    {
      playerCamera = FindObjectOfType<Camera>();
    }

    // Validate Cinemachine camera
    if (cinemachineVirtualCamera == null)
    {
      cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
      if (cinemachineVirtualCamera == null && enableDebugLogs)
      {
        Debug.LogWarning("PlayerEffect: CinemachineVirtualCamera not assigned! Screen shake effects will not work.", this);
      }
    }

    // Cache wait times for performance
    flickerWait = new WaitForSeconds(flickerRate);
    flashWait = new WaitForSeconds(0.1f);
  }

  public void Initialize(PlayerCore core)
  {
    playerCore = core;
  }

  public void PlaySpawnEffect()
  {
    // Play spawn animation
    if (animator != null)
    {
      animator.SetTrigger("Spawn");
    }

    // Play spawn particles
    if (spawnParticles != null)
    {
      spawnParticles.Play();
    }

    // Spawn visual effect
    StartCoroutine(SpawnAnimation());
  }
  public void PlayEatEffect()
  {
    if (!effectsEnabled)
    {
      if (enableDebugLogs)
        Debug.LogWarning("PlayerEffect: Effects are disabled!", this);
      return;
    }

    if (currentEffectCount >= maxConcurrentEffects)
    {
      if (enableDebugLogs)
        Debug.LogWarning($"PlayerEffect: Max concurrent effects reached ({currentEffectCount}/{maxConcurrentEffects})", this);
      return;
    }

    StartCoroutine(PlayEatEffectCoroutine());
  }

  private IEnumerator PlayEatEffectCoroutine()
  {
    currentEffectCount++;

    // Play eat sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("EatSound");
    }

    // Play eat particles
    if (eatParticles != null)
    {
      eatParticles.Play();
    }

    // Screen shake
    if (enableScreenShake)
    {
      currentScreenShakeCoroutine = StartCoroutine(ScreenShake(eatShakeDuration, eatShakeIntensity));
    }

    // Flash effect
    StartCoroutine(FlashEffect(eatFlashColor, 10f));

    // Animation trigger
    if (animator != null)
    {
      animator.SetTrigger("Eat");
    }

    if (enableDebugLogs)
    {
      Debug.Log("Eat effect played");
    }

    yield return new WaitForSeconds(0.5f); // Effect duration
    currentEffectCount--;
  }

  public void PlayGrowthEffect()
  {
    // Play growth sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("GrowthSound");
    }

    // Play growth particles
    if (growthParticles != null)
    {
      growthParticles.Play();
    }

    // Growth pulse animation
    StartCoroutine(GrowthPulseEffect());

    if (animator != null)
    {
      animator.SetTrigger("Grow");
    }
  }

  public void PlayEvolutionEffect()
  {
    // Play evolution sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("EvolutionSound");
    }

    // Play evolution particles
    if (evolutionParticles != null)
    {
      evolutionParticles.Play();
    }

    // Special evolution animation
    StartCoroutine(EvolutionAnimation());
  }
  public void PlayDashEffect()
  {
    // Use enhanced dash effect if ground detection is enabled
    if (enableGroundColorDetection)
    {
      PlayEnhancedDashEffect();
    }
    else
    {
      PlayBasicDashEffect();
    }
  }

  /// <summary>
  /// Basic dash effect (original behavior)
  /// </summary>
  public void PlayBasicDashEffect()
  {
    // Play dash sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFXWithSettings("DashSound");
    }

    // Play dash particles
    if (dashParticles != null)
    {
      dashParticles.Play();
    }

    // Screen shake
    if (enableScreenShake)
    {
      currentScreenShakeCoroutine = StartCoroutine(ScreenShake(dashShakeDuration, dashShakeIntensity));
    }

    // Dash trail effect
    StartCoroutine(DashTrailEffect());
  }

  public void PlayDeathEffect()
  {
    // Play death sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("DeathSound");
    }

    // Play death particles
    if (deathParticles != null)
    {
      deathParticles.Play();
    }

    // Death animation
    StartCoroutine(DeathAnimation());

    if (animator != null)
    {
      animator.SetTrigger("Death");
    }
  }

  public void PlayRespawnEffect()
  {
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("SpawnSound");
    }
    // Similar to spawn but with different timing
    StartCoroutine(RespawnAnimation());

    if (animator != null)
    {
      animator.SetTrigger("Respawn");
    }
  }

  public void SetFlicker(bool flicker)
  {
    isFlickering = flicker;

    if (flickerCoroutine != null)
    {
      StopCoroutine(flickerCoroutine);
    }

    if (flicker)
    {
      flickerCoroutine = StartCoroutine(FlickerEffect());
    }
    else
    {
      // Restore original color
      if (spriteRenderer != null)
      {
        spriteRenderer.color = originalSpriteColor;
      }
    }
  }

  private IEnumerator SpawnAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Start with transparent
    Color transparent = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, 0f);
    spriteRenderer.color = transparent;

    // Scale effect
    Vector3 originalScale = transform.localScale;
    transform.localScale = originalScale * 0.1f;

    // Animate scale and alpha
    float duration = 0.5f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / duration;

      // Scale
      transform.localScale = Vector3.Lerp(originalScale * 0.1f, originalScale, progress);

      // Alpha
      float alpha = Mathf.Lerp(0f, originalSpriteColor.a, progress);
      spriteRenderer.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, alpha);

      yield return null;
    }

    transform.localScale = originalScale;
    spriteRenderer.color = originalSpriteColor;
  }

  private IEnumerator FlashEffect(Color flashColor, float duration)
  {
    if (spriteRenderer == null) yield break;

    Debug.LogWarning($"Flashing effect with color: {flashColor} for duration: {duration}", this);

    Color original = spriteRenderer.color;
    spriteRenderer.color = flashColor;

    yield return new WaitForSeconds(duration);

    spriteRenderer.color = original;
  }

  private IEnumerator GrowthPulseEffect()
  {
    Vector3 originalScale = transform.localScale;
    Vector3 pulseScale = originalScale * 1.2f;

    float duration = 0.3f;
    float elapsed = 0f;

    // Scale up
    while (elapsed < duration / 2f)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / (duration / 2f);
      transform.localScale = Vector3.Lerp(originalScale, pulseScale, progress);
      yield return null;
    }

    elapsed = 0f;
    // Scale back down
    while (elapsed < duration / 2f)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / (duration / 2f);
      transform.localScale = Vector3.Lerp(pulseScale, originalScale, progress);
      yield return null;
    }

    transform.localScale = originalScale;
  }

  private IEnumerator EvolutionAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Multi-color flash sequence
    Color[] colors = { Color.white, Color.yellow, Color.cyan, Color.magenta, Color.green, Color.blue, Color.red };

    for (int i = 0; i < colors.Length; i++)
    {
      spriteRenderer.color = colors[i];
      yield return new WaitForSeconds(0.1f);
    }

    spriteRenderer.color = originalSpriteColor;

    // Combined with growth pulse
    yield return StartCoroutine(GrowthPulseEffect());
  }

  private IEnumerator DashTrailEffect()
  {
    // Create trail effect by temporarily reducing alpha
    if (spriteRenderer == null) yield break;

    Color trailColor = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, 0.7f);
    spriteRenderer.color = trailColor;

    yield return new WaitForSeconds(0.2f);

    spriteRenderer.color = originalSpriteColor;
  }

  private IEnumerator DeathAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Flash red then fade out
    spriteRenderer.color = Color.red;
    yield return new WaitForSeconds(0.1f);

    Color fadeColor = originalSpriteColor;
    float duration = 1f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float alpha = Mathf.Lerp(originalSpriteColor.a, 0f, elapsed / duration);
      spriteRenderer.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
      yield return null;
    }
  }

  private IEnumerator RespawnAnimation()
  {
    yield return StartCoroutine(SpawnAnimation());
  }

  private IEnumerator FlickerEffect()
  {
    while (isFlickering)
    {
      if (spriteRenderer != null)
      {
        spriteRenderer.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, 0.3f);
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalSpriteColor;
        yield return new WaitForSeconds(0.1f);
      }
      else
      {
        yield return null;
      }
    }
  }

  private IEnumerator ScreenShake(float duration, float intensity)
  {
    if (playerCamera == null) yield break;
    if (cinemachineVirtualCamera == null)
    {
      yield break;
    }

    // Prevent multiple screen shakes running at the same time
    float currentTime = Time.time;
    //if (currentTime - lastShakeTime > shakeMinInterval)
    //{
      // Stop any current screen shake
      if (currentScreenShakeCoroutine != null)
      {
        //StopCoroutine(currentScreenShakeCoroutine);
        yield break;
      }

      lastShakeTime = currentTime; 
    //}

    CinemachineBasicMultiChannelPerlin noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

    if (noise == null) yield break;

    // Store original values
    float originalAmplitude = noise.m_AmplitudeGain;
    float originalFrequency = noise.m_FrequencyGain;

    // Set shake values
    noise.m_AmplitudeGain = intensity;
    noise.m_FrequencyGain = 5f; // Optional: adjust frequency for different shake feel

    // Wait for the shake duration
    yield return new WaitForSeconds(duration);    // Restore original values
    noise.m_AmplitudeGain = originalAmplitude;
    noise.m_FrequencyGain = originalFrequency;
    if (enableDebugLogs)
    {
      Debug.Log($"Screen shake completed - Duration: {duration}s, Intensity: {intensity}");
    }
  }

  // Advanced screen shake with decay
  private IEnumerator ScreenShakeWithDecay(float duration, float startIntensity, AnimationCurve decayCurve = null)
  {
    if (playerCamera == null) yield break;
    if (cinemachineVirtualCamera == null) yield break;

    CinemachineBasicMultiChannelPerlin noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    if (noise == null) yield break;

    // Store original values
    float originalAmplitude = noise.m_AmplitudeGain;
    float originalFrequency = noise.m_FrequencyGain;

    // Use default decay curve if none provided
    if (decayCurve == null)
    {
      decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }

    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / duration;
      float currentIntensity = startIntensity * decayCurve.Evaluate(progress);

      noise.m_AmplitudeGain = currentIntensity; yield return null;
    }

    // Restore original values
    noise.m_AmplitudeGain = originalAmplitude;
    noise.m_FrequencyGain = originalFrequency;

    // Clear the coroutine reference when done
    currentScreenShakeCoroutine = null;
  }

  // Impulse-based screen shake for more realistic feel
  private IEnumerator ImpulseScreenShake(float intensity, float frequency = 25f)
  {
    if (playerCamera == null) yield break;
    if (cinemachineVirtualCamera == null) yield break;

    CinemachineBasicMultiChannelPerlin noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    if (noise == null) yield break;

    // Store original values
    float originalAmplitude = noise.m_AmplitudeGain;
    float originalFrequency = noise.m_FrequencyGain;

    // Apply impulse
    noise.m_AmplitudeGain = intensity;
    noise.m_FrequencyGain = frequency;

    // Decay the shake over time
    float duration = intensity * 0.5f; // Duration based on intensity
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float progress = elapsed / duration;

      // Exponential decay
      float currentIntensity = intensity * Mathf.Exp(-progress * 5f);
      noise.m_AmplitudeGain = currentIntensity; yield return null;
    }

    // Restore original values
    noise.m_AmplitudeGain = originalAmplitude;
    noise.m_FrequencyGain = originalFrequency;

    // Clear the coroutine reference when done
    currentScreenShakeCoroutine = null;
  }
  // Public method to trigger advanced screen shake
  public void PlayAdvancedShake(float intensity, ShakeType shakeType = ShakeType.Normal)
  {
    if (!enableScreenShake) return;

    // Prevent multiple screen shakes running at the same time
    float currentTime = Time.time;
    if (currentTime - lastShakeTime > shakeMinInterval)
    {
      // Stop any current screen shake
      if (currentScreenShakeCoroutine != null)
      {
        StopCoroutine(currentScreenShakeCoroutine);
      }

      lastShakeTime = currentTime;

      switch (shakeType)
      {
        case ShakeType.Normal:
          currentScreenShakeCoroutine = StartCoroutine(ScreenShake(0.2f, intensity));
          break;
        case ShakeType.Decay:
          currentScreenShakeCoroutine = StartCoroutine(ScreenShakeWithDecay(0.5f, intensity));
          break;
        case ShakeType.Impulse:
          currentScreenShakeCoroutine = StartCoroutine(ImpulseScreenShake(intensity));
          break;
      }
    }
  }

  // Enum for different shake types
  public enum ShakeType
  {
    Normal,
    Decay,
    Impulse
  }

  // Utility Methods
  public void SetEffectsEnabled(bool enabled)
  {
    effectsEnabled = enabled;

    if (enableDebugLogs)
    {
      Debug.Log($"Effects enabled: {enabled}");
    }
  }
  public void StopAllEffects()
  {
    StopAllCoroutines();
    currentEffectCount = 0;

    // Clear screen shake coroutine reference
    currentScreenShakeCoroutine = null;
    lastShakeTime = 0f;

    // Reset screen shake to original state
    if (cinemachineVirtualCamera != null)
    {
      CinemachineBasicMultiChannelPerlin noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
      if (noise != null)
      {
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
      }
    }

    // Reset visual state
    if (spriteRenderer != null)
    {
      spriteRenderer.color = originalSpriteColor;
    }

    SetFlicker(false);

    if (enableDebugLogs)
    {
      Debug.Log("All effects stopped");
    }
  }

  public void PlayCustomFlash(Color color, float duration)
  {
    StartCoroutine(FlashEffect(color, duration));
  }
  public void PlayCustomShake(float duration, float intensity)
  {
    if (enableScreenShake)
    {
      currentScreenShakeCoroutine = StartCoroutine(ScreenShake(duration, intensity));
    }
  }

  public void PlayCustomShake(float intensity, ShakeType shakeType)
  {
    if (enableScreenShake)
    {
      PlayAdvancedShake(intensity, shakeType);
    }
  }

  public bool IsEffectPlaying()
  {
    return currentEffectCount > 0;
  }

  public int GetCurrentEffectCount()
  {
    return currentEffectCount;
  }

  public void ResetToOriginalState()
  {
    StopAllEffects();

    if (spriteRenderer != null)
    {
      spriteRenderer.color = originalSpriteColor;
    }

    transform.localScale = Vector3.one;
  }

  // Performance optimization for particle systems
  private void OptimizeParticleSystem(ParticleSystem particles)
  {
    if (particles == null) return;

    var main = particles.main;
    main.maxParticles = Mathf.Min(main.maxParticles, 50); // Limit particles for performance
  }

  private void OnValidate()
  {
    // Validate settings in editor
    maxConcurrentEffects = Mathf.Max(1, maxConcurrentEffects);
    flickerRate = Mathf.Max(0.01f, flickerRate);

    // Optimize particle systems
    if (enableParticlePooling)
    {
      OptimizeParticleSystem(eatParticles);
      OptimizeParticleSystem(growthParticles);
      OptimizeParticleSystem(deathParticles);
      OptimizeParticleSystem(dashParticles);
      OptimizeParticleSystem(evolutionParticles);
      OptimizeParticleSystem(spawnParticles);
    }
  }

  /// <summary>
  /// Validates that all necessary components are properly set up.
  /// Call this method to debug PlayerEffect setup issues.
  /// </summary>
  public void ValidateSetup()
  {
    bool isValid = true;

    if (spriteRenderer == null)
    {
      Debug.LogError("PlayerEffect: SpriteRenderer is missing! Visual effects will not work.", this);
      isValid = false;
    }

    if (animator == null)
    {
      Debug.LogWarning("PlayerEffect: Animator is missing! Animation triggers will not work.", this);
      isValid = false;
    }

    if (cinemachineVirtualCamera == null)
    {
      Debug.LogError("PlayerEffect: CinemachineVirtualCamera is missing! Screen shake effects will not work.", this);
      isValid = false;
    }
    else
    {
      var noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
      if (noise == null)
      {
        Debug.LogError("PlayerEffect: CinemachineBasicMultiChannelPerlin component is missing from virtual camera! Screen shake will not work.", this);
        isValid = false;
      }
    }

    if (UIManagerAudio.instance == null)
    {
      Debug.LogWarning("PlayerEffect: UIManagerAudio.instance is null! Sound effects will not play.", this);
    }

    // Check particle systems
    if (eatParticles == null) Debug.LogWarning("PlayerEffect: Eat particles not assigned.", this);
    if (growthParticles == null) Debug.LogWarning("PlayerEffect: Growth particles not assigned.", this);
    if (deathParticles == null) Debug.LogWarning("PlayerEffect: Death particles not assigned.", this);
    if (dashParticles == null) Debug.LogWarning("PlayerEffect: Dash particles not assigned.", this);
    if (evolutionParticles == null) Debug.LogWarning("PlayerEffect: Evolution particles not assigned.", this);
    if (spawnParticles == null) Debug.LogWarning("PlayerEffect: Spawn particles not assigned.", this);

    if (!effectsEnabled)
    {
      Debug.LogWarning("PlayerEffect: Effects are currently disabled!", this);
    }

    if (isValid)
    {
      Debug.Log("PlayerEffect: Validation passed! All critical components are properly set up.", this);
    }
    else
    {
      Debug.LogError("PlayerEffect: Validation failed! Some critical components are missing.", this);
    }
  }

  private void Update()
  {
    // Update ground detection cache
    if (Time.time - lastGroundCacheTime > groundCacheInterval)
    {
      UpdateGroundCache();
    }
  }

  private void UpdateGroundCache()
  {
    // Get all Tilemaps in the scene
    Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();

    // Filter and cache ground Tilemaps
    cachedGroundTilemaps = System.Array.FindAll(allTilemaps, tilemap => (groundLayerMask & (1 << tilemap.gameObject.layer)) != 0);

    lastGroundCacheTime = Time.time;

    if (enableDebugLogs)
    {
      Debug.Log($"Ground cache updated. Cached {cachedGroundTilemaps.Length} Tilemaps.", this);
    }
  }

  /// <summary>
  /// Get the player's current movement direction for dash particles
  /// </summary>
  private Vector2 GetDashDirection()
  {
    // Try to get movement direction from PlayerCore or PlayerMovement
    if (playerCore != null)
    {
      // Check if PlayerCore has a movement component
      PlayerMovement playerMovement = playerCore.GetComponent<PlayerMovement>();
      if (playerMovement != null)
      {
        return -playerMovement.MovementDirection; // Negative for particles going backward
      }

      // Fallback: try to get from rigidbody velocity
      Rigidbody2D rb = playerCore.GetComponent<Rigidbody2D>();
      if (rb != null && rb.velocity.magnitude > 0.1f)
      {
        return -rb.velocity.normalized; // Negative for particles going backward
      }
    }

    // Default to left if no movement detected
    return Vector2.left;
  }

  /// <summary>
  /// Detect ground color beneath the player using tilemap sampling
  /// </summary>
  private Color GetGroundColor(Vector3 position)
  {
    if (!enableGroundColorDetection)
      return fallbackGroundColor;

    RefreshGroundTilemapCache();

    if (cachedGroundTilemaps == null || cachedGroundTilemaps.Length == 0)
      return fallbackGroundColor;

    // Sample multiple points around the player position for better color detection
    Vector3[] samplePoints = {
      position,
      position + Vector3.left * 0.2f,
      position + Vector3.right * 0.2f,
      position + Vector3.down * 0.2f,
      position + Vector3.up * 0.2f
    };

    Color averageColor = Color.clear;
    int validSamples = 0;

    foreach (Vector3 samplePoint in samplePoints)
    {
      Color tileColor = SampleTileColor(samplePoint);
      if (tileColor != Color.clear)
      {
        averageColor += tileColor;
        validSamples++;
      }
    }

    if (validSamples > 0)
    {
      averageColor /= validSamples;
      return DarkenColor(averageColor, colorDarkeningFactor);
    }

    return fallbackGroundColor;
  }

  /// <summary>
  /// Sample tile color at a specific world position
  /// </summary>
  private Color SampleTileColor(Vector3 worldPosition)
  {
    foreach (Tilemap tilemap in cachedGroundTilemaps)
    {
      if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        continue;

      Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
      TileBase tile = tilemap.GetTile(cellPosition);

      if (tile != null)
      {
        // Try to get sprite from tile
        Sprite tileSprite = tilemap.GetSprite(cellPosition);
        if (tileSprite != null && tileSprite.texture != null)
        {
          return SampleSpriteColor(tileSprite);
        }

        // Fallback: use tilemap color
        Color tilemapColor = tilemap.GetColor(cellPosition);
        if (tilemapColor != Color.white) // Only use if it's not the default color
        {
          return tilemapColor;
        }
      }
    }

    return Color.clear;
  }

  /// <summary>
  /// Sample dominant color from a sprite
  /// </summary>
  private Color SampleSpriteColor(Sprite sprite)
  {
    if (sprite == null || sprite.texture == null)
      return Color.clear;

    try
    {
      Texture2D texture = sprite.texture;
      Rect spriteRect = sprite.textureRect;

      // Sample from the center of the sprite
      int centerX = Mathf.RoundToInt(spriteRect.center.x);
      int centerY = Mathf.RoundToInt(spriteRect.center.y);

      // Make sure coordinates are within bounds
      centerX = Mathf.Clamp(centerX, 0, texture.width - 1);
      centerY = Mathf.Clamp(centerY, 0, texture.height - 1);

      return texture.GetPixel(centerX, centerY);
    }
    catch (System.Exception)
    {
      // If texture is not readable, return clear
      return Color.clear;
    }
  }

  /// <summary>
  /// Darken a color by a specified factor
  /// </summary>
  private Color DarkenColor(Color originalColor, float darkeningFactor)
  {
    float factor = 1f - Mathf.Clamp01(darkeningFactor);
    return new Color(
      originalColor.r * factor,
      originalColor.g * factor,
      originalColor.b * factor,
      originalColor.a
    );
  }

  /// <summary>
  /// Configure dash particles with direction and ground color
  /// </summary>
  private void ConfigureDashParticles(Vector2 direction, Color groundColor)
  {
    if (dashParticles == null) return;

    var main = dashParticles.main;
    var shape = dashParticles.shape;
    var velocityOverLifetime = dashParticles.velocityOverLifetime;
    var colorOverLifetime = dashParticles.colorOverLifetime;

    // Configure particle color
    main.startColor = groundColor;

    // Configure emission direction
    shape.rotation = new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

    // Configure velocity to emit particles backward from movement
    velocityOverLifetime.enabled = true;
    velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

    // Set base velocity in the dash direction
    var velocity = velocityOverLifetime.radial;
    velocity.mode = ParticleSystemCurveMode.Constant;
    velocity.constant = 3f; // Adjust particle spread speed

    // Configure color over lifetime for fade effect
    colorOverLifetime.enabled = true;
    Gradient gradient = new Gradient();
    GradientColorKey[] colorKeys = new GradientColorKey[2];
    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

    colorKeys[0] = new GradientColorKey(groundColor, 0f);
    colorKeys[1] = new GradientColorKey(groundColor * 0.7f, 1f);

    alphaKeys[0] = new GradientAlphaKey(1f, 0f);
    alphaKeys[1] = new GradientAlphaKey(0.8f, 0.5f);
    alphaKeys[2] = new GradientAlphaKey(0f, 1f);

    gradient.SetKeys(colorKeys, alphaKeys);
    colorOverLifetime.color = gradient;

    // Configure size and lifetime for ground debris effect
    main.startLifetime = 0.5f;
    main.startSpeed = 2f;
    main.startSize = Random.Range(0.1f, 0.3f);

    // Add some randomness to make it look more natural
    main.startSpeedMultiplier = Random.Range(0.8f, 1.2f);
  }

  /// <summary>
  /// Enhanced dash effect with ground detection and dynamic particle configuration
  /// </summary>
  public void PlayEnhancedDashEffect()
  {
    Vector2 dashDirection = GetDashDirection();
    Color groundColor = GetGroundColor(transform.position);

    PlayEnhancedDashEffect(dashDirection, groundColor);
  }

  /// <summary>
  /// Enhanced dash effect with specified direction and color
  /// </summary>
  public void PlayEnhancedDashEffect(Vector2 dashDirection, Color groundColor)
  {
    // Play dash sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFXWithSettings("DashSound");
    }

    // Configure and play enhanced dash particles
    if (dashParticles != null)
    {
      ConfigureDashParticles(dashDirection, groundColor);
      dashParticles.Play();
    }

    // Screen shake
    if (enableScreenShake)
    {
      currentScreenShakeCoroutine = StartCoroutine(ScreenShake(dashShakeDuration, dashShakeIntensity));
    }

    // Enhanced dash trail effect with ground color
    StartCoroutine(EnhancedDashTrailEffect(groundColor));
  }

  /// <summary>
  /// Enhanced dash trail effect with ground color
  /// </summary>
  private IEnumerator EnhancedDashTrailEffect(Color groundColor)
  {
    if (spriteRenderer == null) yield break;

    // Create trail color that matches ground but with transparency
    Color trailColor = new Color(groundColor.r, groundColor.g, groundColor.b, 0.3f);
    Color originalColor = spriteRenderer.color;

    // Apply trail effect
    spriteRenderer.color = Color.Lerp(originalColor, trailColor, 0.6f);

    yield return new WaitForSeconds(0.15f);

    // Fade back to original color
    float fadeTime = 0.1f;
    float elapsed = 0f;

    while (elapsed < fadeTime)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / fadeTime;
      spriteRenderer.color = Color.Lerp(trailColor, originalColor, t);
      yield return null;
    }

    spriteRenderer.color = originalColor;
  }

  /// <summary>
  /// Refresh the cache of ground tilemaps
  /// </summary>
  private void RefreshGroundTilemapCache()
  {
    if (cachedGroundTilemaps != null && Time.time - lastGroundCacheTime < groundCacheInterval)
      return;

    cachedGroundTilemaps = FindObjectsOfType<Tilemap>()
      .Where(tilemap => ((1 << tilemap.gameObject.layer) & groundLayerMask) != 0
                       && tilemap.gameObject.activeInHierarchy)
      .ToArray();

    lastGroundCacheTime = Time.time;

    if (enableDebugLogs)
      Debug.Log($"PlayerEffect: Refreshed ground tilemap cache, found {cachedGroundTilemaps.Length} tilemaps");
  }
}
