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
  #endregion

  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    animator = GetComponent<Animator>();
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
      }

      // Set animator
      if (animator != null && enemyData.animatorController != null)
      {
        animator.runtimeAnimatorController = enemyData.animatorController;
      }

      // Initialize particle effects
      InitializeParticleEffects();
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
    // Play spawn sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("SpawnSound");
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
  }

  public void PlayGrowthEffect()
  {
    // Simple scale animation for growth
    StartCoroutine(GrowthAnimation());
  }

  public void PlayEatEffect()
  {
    // Play eat sound
    if (UIManagerAudio.instance != null)
    {
      UIManagerAudio.instance.PlaySFX("EatSound");
    }
  }

  private System.Collections.IEnumerator SpawnAnimation()
  {
    if (spriteRenderer == null) yield break;

    // Start invisible
    Color originalColor = spriteRenderer.color;
    spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

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

    Color originalColor = spriteRenderer.color;

    // Flash effect
    for (int i = 0; i < 3; i++)
    {
      spriteRenderer.color = Color.white;
      yield return new WaitForSeconds(0.1f);
      spriteRenderer.color = originalColor;
      yield return new WaitForSeconds(0.1f);
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

  private System.Collections.IEnumerator GrowthAnimation()
  {
    Vector3 originalScale = transform.localScale;
    Vector3 targetScale = originalScale * 1.2f; // Slightly bigger for effect

    // Scale up
    float duration = 0.2f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
      yield return null;
    }

    // Scale back down
    elapsed = 0f;
    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
      yield return null;
    }

    transform.localScale = originalScale;
  }
}
