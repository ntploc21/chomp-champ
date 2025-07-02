using UnityEngine;
using System.Collections;
using Michsky.UI.Reach;

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

  [Header("Screen Effects")]
  [SerializeField] private bool enableScreenShake = true;
  [SerializeField] private float eatShakeDuration = 0.1f;
  [SerializeField] private float eatShakeIntensity = 0.1f;
  [SerializeField] private float dashShakeDuration = 0.2f;
  [SerializeField] private float dashShakeIntensity = 0.15f;
  #endregion

  #region Internal Data
  private PlayerCore playerCore;
  private Camera playerCamera;
  private Color originalSpriteColor;

  // Flicker effect variables
  private bool isFlickering = false;
  private Coroutine flickerCoroutine;
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
      StartCoroutine(ScreenShake(eatShakeDuration, eatShakeIntensity));
    }

    // Flash effect
    StartCoroutine(FlashEffect(Color.green, 0.1f));

    // Animation trigger
    if (animator != null)
    {
      animator.SetTrigger("Eat");
    }
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
      UIManagerAudio.instance.PlaySFX("DashSound");
    }

    // Play dash particles
    if (dashParticles != null)
    {
      dashParticles.Play();
    }

    // Screen shake
    if (enableScreenShake)
    {
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

    Vector3 originalPosition = playerCamera.transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;

      float x = Random.Range(-1f, 1f) * intensity;
      float y = Random.Range(-1f, 1f) * intensity;

      playerCamera.transform.position = originalPosition + new Vector3(x, y, 0);

      yield return null;
    }

    playerCamera.transform.position = originalPosition;
  }
}
