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
  public float playTime = 0f;
  #endregion  // Events

  [Header("Events")]
  public UnityAction<PlayerCore> OnPlayerDeath;
  public UnityAction<PlayerCore, float> OnPlayerGrowth;
  public UnityAction<PlayerCore, EnemyCore> OnPlayerEatEnemy;

  #region Properties
  public PlayerMovement Movement => playerMovement;
  public PlayerGrowth Growth => playerGrowth;
  public PlayerEffect Effect => playerEffect;
  public bool IsAlive => isAlive;
  public bool IsInvincible => isInvincible;
  public float CurrentSize => currentSize;
  public int Lives => lives;
  public float Score => score;
  #endregion

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
    playTime = 0f;

    isAlive = true;
    isInvincible = false;

    // Set initial size
    if (playerGrowth != null)
    {
      playerGrowth.ResetGrowth();
      playerGrowth.SetSize(currentSize, true);
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
    // Use UIManagerAudio

    // Trigger growth
    if (playerGrowth != null)
    {
      float xpGain = enemy.Data.sizeLevel * 5f; // Example XP gain
      playerGrowth.AddXP(xpGain);
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
      // Player has no lives left, trigger death
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
    // Use UIManagerAudio

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
    if (isAlive || lives <= 0) return;

    // Make player temporarily invincible
    StartCoroutine(InvincibilityCoroutine());

    // Re-enable movement
    if (playerMovement != null)
    {
      playerMovement.SetCanMove(true);
    }

    // Play respawn audio
    // Use UIManagerAudio

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
      // Use UIManagerAudio

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

  // Method to reset player to initial state (useful for game restart)
  public void ResetPlayer()
  {
    lives = startLives;
    currentSize = startSize;
    score = 0f;
    enemiesEaten = 0;
    playTime = 0f;
    isAlive = true;
    isInvincible = false;

    // Stop any running coroutines
    StopAllCoroutines();

    // Reset components
    if (playerGrowth != null)
    {
      playerGrowth.ResetGrowth();
      playerGrowth.SetSize(currentSize, true);
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
}
