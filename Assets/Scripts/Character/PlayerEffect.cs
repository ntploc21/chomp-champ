using UnityEngine;
using System.Collections;
using Michsky.UI.Reach;
using Cinemachine;
using UnityEngine.Rendering;

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
  [SerializeField] private float deathShakeDuration = 0.3f;
  [SerializeField] private float deathShakeIntensity = 0.2f;

  [Header("Advanced Shake Settings")]
  [SerializeField] private AnimationCurve shakeDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
  [SerializeField] private float defaultShakeFrequency = 25f;
  [SerializeField] private bool useAdvancedShake = false;

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
  [SerializeField] private bool enableAdvancedEffects = true;
  [SerializeField] private int maxConcurrentEffects = 3;

  [Header("Debug")]
  [SerializeField] private bool enableDebugLogs = false;
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

  // Caching
  private WaitForSeconds flickerWait;
  private WaitForSeconds flashWait;
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

    // Find camera for screen shake
    playerCamera = Camera.main;
    if (playerCamera == null)
    {
      playerCamera = FindObjectOfType<Camera>();
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

    // Spawn visual effect
    StartCoroutine(SpawnAnimation());
  }
  public void PlayEatEffect()
  {
    if (!effectsEnabled || currentEffectCount >= maxConcurrentEffects) return;

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
    }    // Screen shake
    if (enableScreenShake)
    {
      if (useAdvancedShake)
        PlayAdvancedShake(eatShakeIntensity, ShakeType.Normal);
      else
        StartCoroutine(ScreenShake(eatShakeDuration, eatShakeIntensity));
    }

    // Flash effect
    StartCoroutine(FlashEffect(eatFlashColor, 0.1f));

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

    if (animator != null)
    {
      animator.SetTrigger("Evolve");
    }
  }

  public void PlayDashEffect()
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
      if (useAdvancedShake)
        PlayAdvancedShake(dashShakeIntensity, ShakeType.Impulse);
      else
        StartCoroutine(ScreenShake(dashShakeDuration, dashShakeIntensity));
    }

    // Dash trail effect
    StartCoroutine(DashTrailEffect());

    if (animator != null)
    {
      animator.SetTrigger("Dash");
    }
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
    Color[] colors = { Color.white, Color.yellow, Color.cyan, Color.magenta };

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

    CinemachineBasicMultiChannelPerlin noise = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

    if (noise == null) yield break;

    // Store original values
    float originalAmplitude = noise.m_AmplitudeGain;
    float originalFrequency = noise.m_FrequencyGain;

    // Set shake values
    noise.m_AmplitudeGain = intensity;
    noise.m_FrequencyGain = 5f; // Optional: adjust frequency for different shake feel

    // Wait for the shake duration
    yield return new WaitForSeconds(duration);

    // Restore original values
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

      noise.m_AmplitudeGain = currentIntensity;

      yield return null;
    }

    // Restore original values
    noise.m_AmplitudeGain = originalAmplitude;
    noise.m_FrequencyGain = originalFrequency;
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
      noise.m_AmplitudeGain = currentIntensity;

      yield return null;
    }

    // Restore original values
    noise.m_AmplitudeGain = originalAmplitude;
    noise.m_FrequencyGain = originalFrequency;
  }

  // Public method to trigger advanced screen shake
  public void PlayAdvancedShake(float intensity, ShakeType shakeType = ShakeType.Normal)
  {
    if (!enableScreenShake) return;

    switch (shakeType)
    {
      case ShakeType.Normal:
        StartCoroutine(ScreenShake(0.2f, intensity));
        break;
      case ShakeType.Decay:
        StartCoroutine(ScreenShakeWithDecay(0.5f, intensity));
        break;
      case ShakeType.Impulse:
        StartCoroutine(ImpulseScreenShake(intensity));
        break;
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
      StartCoroutine(ScreenShake(duration, intensity));
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
}
