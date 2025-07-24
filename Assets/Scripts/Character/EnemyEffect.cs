using UnityEngine;
using Michsky.UI.Reach;

public class EnemyEffect : MonoBehaviour
{
  #region Editor Data
  private EnemyCore enemyCore;
  private EnemyData enemyData;

  [Header("Visual Effects")]
  private SpriteRenderer spriteRenderer;
  private Animator animator;

  [Header("Particle Effects")]
  private ParticleSystem deathParticles;
  private ParticleSystem spawnParticles;

  // Cached references and values for performance
  private Color originalColor;
  private Vector3 originalScale;
  private static readonly WaitForSeconds flashWait = new WaitForSeconds(0.1f);
  private UIManagerAudio audioManager;
  #endregion
  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    animator = GetComponent<Animator>();

    // Cache audio manager reference to avoid repeated singleton calls
    audioManager = UIManagerAudio.instance;
  }
  public void Initialize(EnemyCore core)
  {
    enemyCore = core;
    enemyData = core.Data;

    if (enemyData != null)
    {
      // Set sprite
      if (spriteRenderer != null && enemyData.enemySprite != null)
      {
        spriteRenderer.sprite = enemyData.enemySprite;
        // Cache original color to avoid repeated access
        originalColor = spriteRenderer.color;
      }

      // Set animator
      if (animator != null && enemyData.animatorController != null)
      {
        animator.runtimeAnimatorController = enemyData.animatorController;
      }

      // Cache original scale
      originalScale = transform.localScale;

      // Initialize particle effects only if they don't exist
      if (deathParticles == null || spawnParticles == null)
      {
        InitializeParticleEffects();
      }
    }

    // Play spawn effect
    PlaySpawnEffect();
  }

  private void InitializeParticleEffects()
  {
    // Create death particle effect if prefab exists
    if (enemyData.deathEffectPrefab != null)
    {
      GameObject deathEffectObj = Instantiate(enemyData.deathEffectPrefab, transform);
      deathParticles = deathEffectObj.GetComponent<ParticleSystem>();
      if (deathParticles != null)
      {
        var main = deathParticles.main;
        main.playOnAwake = false;
      }
    }

    // Create spawn particle effect if prefab exists
    if (enemyData.spawnEffectPrefab != null)
    {
      GameObject spawnEffectObj = Instantiate(enemyData.spawnEffectPrefab, transform);
      spawnParticles = spawnEffectObj.GetComponent<ParticleSystem>();
      if (spawnParticles != null)
      {
        var main = spawnParticles.main;
        main.playOnAwake = false;
      }
    }
  }
  public void PlaySpawnEffect()
  {
    // Play spawn sound with cached reference
    if (audioManager != null)
    {
      audioManager.PlaySFX("SpawnSound");
    }

    // Play spawn particles
    if (spawnParticles != null)
    {
      spawnParticles.Play();
    }

    // Spawn animation (fade in or scale up)
    StartCoroutine(SpawnAnimation());
  }

  public void PlayDeathEffect()
  {
    // Play death sound with cached reference
    if (audioManager != null)
    {
      audioManager.PlaySFX("DeathSound");
    }

    // Play death particles
    if (deathParticles != null)
    {
      deathParticles.Play();
    }

    // Death animation
    StartCoroutine(DeathAnimation());
  }
  public void PlayEatEffect()
  {
    // Play eat sound with cached reference
    if (audioManager != null)
    {
      audioManager.PlaySFX("EatSound");
    }
  }
  private System.Collections.IEnumerator SpawnAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Use cached original color instead of accessing it repeatedly
    Color startColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    spriteRenderer.color = startColor;

    // Fade in over 0.5 seconds
    float duration = 0.5f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float alpha = Mathf.Lerp(0f, originalColor.a, elapsed / duration);
      spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
      yield return null;
    }

    spriteRenderer.color = originalColor;
  }
  private System.Collections.IEnumerator DeathAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Flash effect - use cached wait time and color
    for (int i = 0; i < 3; i++)
    {
      spriteRenderer.color = Color.white;
      yield return flashWait;
      spriteRenderer.color = originalColor;
      yield return flashWait;
    }

    // Fade out
    float duration = 0.3f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / duration);
      spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
      yield return null;
    }
  }
}
