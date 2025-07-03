using UnityEngine;
using System;
using UnityEngine.Events;

public class PlayerCore : MonoBehaviour
{
  #region Editor Data
  [Header("Components")]
  [SerializeField] private PlayerMovement playerMovement;
  [SerializeField] private PlayerGrowth playerGrowth;
  [SerializeField] private PlayerEffect playerEffect;

  [Header("Player Stats")]
  [SerializeField] private float startSize = 1f;
  [SerializeField] private int startLives = 3;
  [SerializeField] private int maxLives = 5;
  [SerializeField] private float invincibilityDuration = 2f;
  
  [Header("Gameplay Settings")]
  [SerializeField] private float sizeReductionOnDeath = 0.2f; // 20% size reduction
  [SerializeField] private bool resetSizeOnDeath = false;
  [SerializeField] private float respawnDelay = 1f;
  
  [Header("Audio")]
  [SerializeField] private AudioClip eatSFX;
  [SerializeField] private AudioClip growthSFX;
  [SerializeField] private AudioClip deathSFX;
  [SerializeField] private AudioClip respawnSFX;

  [Header("Debug")]
  [SerializeField] private bool enableDebugLogs = false;
  #endregion

  #region Runtime Data
  [Header("Runtime Stats")]
  [Space(10)]
  public float currentSize = 1f;
  public int lives = 3;
  public float score = 0f;
  public bool isAlive = true;
  public bool isInvincible = false;
  
  [Space(5)]
  public int enemiesEaten = 0;
  public float totalGrowth = 0f;
  public float playTime = 0f;
  #endregion  // Events
  
  public static UnityAction<PlayerCore> OnPlayerDeath;
  public static UnityAction<PlayerCore, float> OnPlayerGrowth;
  public static UnityAction<PlayerCore, EnemyCore> OnPlayerEatEnemy;

  // Properties for better encapsulation
  public PlayerMovement Movement => playerMovement;
  public PlayerGrowth Growth => playerGrowth;
  public PlayerEffect Effect => playerEffect;
  public bool IsAlive => isAlive;
  public bool IsInvincible => isInvincible;
  public float CurrentSize => currentSize;
  public int Lives => lives;
  public float Score => score;
  
  // Cached Components
  private AudioSource audioSource;
  private Camera playerCamera;
  
  // Performance optimization
  private float lastUpdateTime;
  private const float UPDATE_INTERVAL = 0.1f;
  private void Awake()
  {
    InitializeComponents();
    CacheComponents();
  }

  private void Start()
  {
    InitializePlayer();
  }
  
  private void Update()
  {
    // Update play time
    if (isAlive)
    {
      playTime += Time.deltaTime;
    }
    
    // Periodic updates for performance
    if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
    {
      lastUpdateTime = Time.time;
      PeriodicUpdate();
    }
  }

  private void CacheComponents()
  {
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    playerCamera = Camera.main;
    if (playerCamera == null)
    {
      playerCamera = FindObjectOfType<Camera>();
    }
  }
  
  private void PeriodicUpdate()
  {
    // Any periodic checks that don't need to run every frame
    ValidateComponents();
  }
  
  private void ValidateComponents()
  {
    if (enableDebugLogs)
    {
      if (playerMovement == null) Debug.LogWarning("PlayerMovement component missing!");
      if (playerGrowth == null) Debug.LogWarning("PlayerGrowth component missing!");
      if (playerEffect == null) Debug.LogWarning("PlayerEffect component missing!");
    }
  }

  private void InitializeComponents()
  {
    if (playerMovement == null)
      playerMovement = GetComponent<PlayerMovement>();

    if (playerGrowth == null)
      playerGrowth = GetComponent<PlayerGrowth>();

    if (playerEffect == null)
      playerEffect = GetComponent<PlayerEffect>();

    // Initialize all components with reference to this core
    playerMovement?.Initialize(this);
    playerGrowth?.Initialize(this);
    playerEffect?.Initialize(this);
  }
  private void InitializePlayer()
  {
    // Initialize from start values
    lives = startLives;
    currentSize = startSize;
    score = 0f;
    enemiesEaten = 0;
    totalGrowth = 0f;
    playTime = 0f;
    
    isAlive = true;
    isInvincible = false;

    // Set initial size
    if (playerGrowth != null)
    {
      playerGrowth.SetSize(currentSize);
    }

    // Initialize effects
    if (playerEffect != null)
    {
      playerEffect.PlaySpawnEffect();
    }
    
    if (enableDebugLogs)
    {
      Debug.Log($"Player initialized - Size: {currentSize}, Lives: {lives}");
    }
  }
  
  private void PlaySFX(AudioClip clip)
  {
    if (clip != null && audioSource != null)
    {
      audioSource.PlayOneShot(clip);
    }
  }
  public void EatEnemy(EnemyCore enemy)
  {
    if (!isAlive || enemy == null) return;

    // Calculate score bonus
    float scoreGain = enemy.Data.sizeLevel * 10f;
      // Size-based bonus scoring
    float sizeDifference = currentSize - enemy.Data.sizeLevel;
    if (sizeDifference > 1f) // Bonus for eating much smaller enemies
    {
      scoreGain *= 1.5f;
    }
    else if (sizeDifference < 0.5f) // Bonus for eating similar/larger enemies
    {
      scoreGain *= 2f;
    }

    // Update stats
    score += scoreGain;
    enemiesEaten++;

    // Play audio
    PlaySFX(eatSFX);

    // Trigger growth
    if (playerGrowth != null)
    {
      float oldSize = currentSize;
      float growthAmount = enemy.Data.growthRate;
      playerGrowth.Grow(growthAmount);
      totalGrowth += growthAmount;
      
      // Fire growth event with old and new size info
      OnPlayerGrowth?.Invoke(this, currentSize);
    }

    // Play eat effect
    if (playerEffect != null)
    {
      playerEffect.PlayEatEffect();
    }

    // Fire events
    OnPlayerEatEnemy?.Invoke(this, enemy);

    if (enableDebugLogs)
    {
      Debug.Log($"Player ate {enemy.name}! Score gained: {scoreGain:F1}, Total score: {score:F1}, Current size: {currentSize:F2}");
    }
  }
  public void OnEaten()
  {
    if (!isAlive || isInvincible) return;

    // Player loses a life
    lives--;
    
    if (enableDebugLogs)
    {
      Debug.Log($"Player was eaten! Lives remaining: {lives}");
    }

    if (lives <= 0)
    {
      Die();
    }
    else
    {
      StartCoroutine(RespawnCoroutine());
    }
  }

  private void Die()
  {
    isAlive = false;

    // Play death audio
    PlaySFX(deathSFX);

    // Play death effect
    if (playerEffect != null)
    {
      playerEffect.PlayDeathEffect();
    }

    // Disable movement
    if (playerMovement != null)
    {
      playerMovement.SetCanMove(false);
    }

    // Fire death event
    OnPlayerDeath?.Invoke(this);

    if (enableDebugLogs)
    {
      Debug.Log($"Player died! Final score: {score:F1}, Enemies eaten: {enemiesEaten}, Play time: {playTime:F1}s");
    }
  }

  private System.Collections.IEnumerator RespawnCoroutine()
  {
    // Disable movement temporarily
    if (playerMovement != null)
    {
      playerMovement.SetCanMove(false);
    }
    
    // Wait for respawn delay
    yield return new WaitForSeconds(respawnDelay);
    
    Respawn();
  }

  private void Respawn()
  {
    // Handle size reduction
    if (resetSizeOnDeath)
    {
      currentSize = startSize;
    }
    else
    {
      float newSize = Mathf.Max(startSize, currentSize * (1f - sizeReductionOnDeath));
      currentSize = newSize;
    }
    
    // Apply new size
    if (playerGrowth != null)
    {
      playerGrowth.SetSize(currentSize);
    }

    // Make player temporarily invincible
    StartCoroutine(InvincibilityCoroutine());

    // Re-enable movement
    if (playerMovement != null)
    {
      playerMovement.SetCanMove(true);
    }

    // Play respawn audio
    PlaySFX(respawnSFX);

    // Play respawn effect
    if (playerEffect != null)
    {
      playerEffect.PlayRespawnEffect();
    }

    if (enableDebugLogs)
    {
      Debug.Log($"Player respawned! Lives: {lives}, Size: {currentSize:F2}");
    }
  }
  private System.Collections.IEnumerator InvincibilityCoroutine(float duration = -1f)
  {
    isInvincible = true;
    
    // Use provided duration or default
    float invincibilityTime = duration > 0 ? duration : invincibilityDuration;

    float elapsed = 0f;
    while (elapsed < invincibilityTime)
    {
      elapsed += Time.deltaTime;

      // Flicker effect
      if (playerEffect != null)
      {
        playerEffect.SetFlicker(true);
      }

      yield return null;
    }

    isInvincible = false;

    if (playerEffect != null)
    {
      playerEffect.SetFlicker(false);
    }
    
    if (enableDebugLogs)
    {
      Debug.Log("Invincibility ended");
    }
  }
  public void AddLife()
  {
    if (lives < maxLives)
    {
      lives++;
      PlaySFX(respawnSFX); // Reuse respawn sound for life gain
      
      if (enableDebugLogs)
      {
        Debug.Log($"Life added! Current lives: {lives}");
      }
    }
  }

  public void SetInvincible(float duration)
  {
    if (isInvincible) return;

    StartCoroutine(InvincibilityCoroutine(duration));
  }
  
  public void AddScore(float points)
  {
    score += points;
    
    if (enableDebugLogs)
    {
      Debug.Log($"Score added: {points:F1}, Total: {score:F1}");
    }
  }
  
  public void ForceSize(float newSize)
  {
    float oldSize = currentSize;
    currentSize = Mathf.Max(0.1f, newSize); // Minimum size protection
    
    if (playerGrowth != null)
    {
      playerGrowth.SetSize(currentSize);
    }
    
    OnPlayerGrowth?.Invoke(this, currentSize);
    
    if (enableDebugLogs)
    {
      Debug.Log($"Size forced from {oldSize:F2} to {currentSize:F2}");
    }
  }
  
  public bool CanEat(float enemySize)
  {
    return isAlive && !isInvincible && playerGrowth != null && playerGrowth.CanEat(enemySize);
  }
  
  public bool CanBeEatenBy(float enemySize)
  {
    return isAlive && !isInvincible && playerGrowth != null && playerGrowth.CanBeEatenBy(enemySize);
  }

  public void UpdateSize(float newSize)
  {
    currentSize = newSize;
    OnPlayerGrowth?.Invoke(this, currentSize);
  }
  
  // Method to reset player to initial state (useful for game restart)
  public void ResetPlayer()
  {
    lives = startLives;
    currentSize = startSize;
    score = 0f;
    enemiesEaten = 0;
    totalGrowth = 0f;
    playTime = 0f;
    isAlive = true;
    isInvincible = false;
    
    // Stop any running coroutines
    StopAllCoroutines();
    
    // Reset components
    if (playerGrowth != null)
    {
      playerGrowth.SetSize(currentSize);
    }
    
    if (playerMovement != null)
    {
      playerMovement.SetCanMove(true);
    }
    
    if (playerEffect != null)
    {
      playerEffect.SetFlicker(false);
    }
    
    if (enableDebugLogs)
    {
      Debug.Log("Player reset to initial state");
    }
  }

  private void OnDestroy()
  {
    // Clean up events
    OnPlayerDeath = null;
    OnPlayerGrowth = null;
    OnPlayerEatEnemy = null;
  }
}
