using UnityEngine;

public class PlayerCore : MonoBehaviour
{
  #region Editor Data
  [Header("Components")]
  [SerializeField] private PlayerMovement playerMovement;
  [SerializeField] private PlayerGrowth playerGrowth;
  [SerializeField] private PlayerEffect playerEffect;

  [Header("Player Stats")]
  public float currentSize = 1f;
  public int lives = 3;
  public int maxLives = 5;
  public float score = 0f;

  [Header("State")]
  public bool isAlive = true;
  public bool isInvincible = false;
  public float invincibilityDuration = 2f;
  #endregion

  // Events
  public static System.Action<PlayerCore> OnPlayerDeath;
  public static System.Action<PlayerCore, float> OnPlayerGrowth;
  public static System.Action<PlayerCore, EnemyCore> OnPlayerEatEnemy;

  // Properties
  public PlayerMovement Movement => playerMovement;
  public PlayerGrowth Growth => playerGrowth;
  public PlayerEffect Effect => playerEffect;

  private void Awake()
  {
    InitializeComponents();
  }

  private void Start()
  {
    InitializePlayer();
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
  }

  public void EatEnemy(EnemyCore enemy)
  {
    if (!isAlive || enemy == null) return;

    // Increase score
    score += enemy.Data.sizeLevel * 10f;

    // Trigger growth
    if (playerGrowth != null)
    {
      float growthAmount = enemy.Data.growthRate;
      playerGrowth.Grow(growthAmount);
    }

    // Play eat effect
    if (playerEffect != null)
    {
      playerEffect.PlayEatEffect();
    }

    // Fire events
    OnPlayerEatEnemy?.Invoke(this, enemy);
    OnPlayerGrowth?.Invoke(this, currentSize);

    Debug.Log($"Player ate {enemy.name}! Current size: {currentSize}, Score: {score}");
  }

  public void OnEaten()
  {
    if (!isAlive || isInvincible) return;

    // Player loses a life
    lives--;

    if (lives <= 0)
    {
      Die();
    }
    else
    {
      Respawn();
    }
  }

  private void Die()
  {
    isAlive = false;

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

    Debug.Log("Player died! Game Over.");
  }

  private void Respawn()
  {
    // Make player temporarily invincible
    StartCoroutine(InvincibilityCoroutine());

    // Reset size slightly (optional - you might want to keep size or reduce it)
    if (playerGrowth != null)
    {
      float newSize = Mathf.Max(1f, currentSize * 0.8f); // Reduce size by 20%
      playerGrowth.SetSize(newSize);
    }

    // Play respawn effect
    if (playerEffect != null)
    {
      playerEffect.PlayRespawnEffect();
    }

    Debug.Log($"Player respawned! Lives remaining: {lives}");
  }

  private System.Collections.IEnumerator InvincibilityCoroutine()
  {
    isInvincible = true;

    float elapsed = 0f;
    while (elapsed < invincibilityDuration)
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
  }

  public void AddLife()
  {
    if (lives < maxLives)
    {
      lives++;
      Debug.Log($"Life added! Lives: {lives}");
    }
  }

  public void SetInvincible(float duration)
  {
    if (isInvincible) return;

    StartCoroutine(InvincibilityCoroutine());
  }

  public void UpdateSize(float newSize)
  {
    currentSize = newSize;
    OnPlayerGrowth?.Invoke(this, currentSize);
  }

  private void OnDestroy()
  {
    // Clean up events
    OnPlayerDeath = null;
    OnPlayerGrowth = null;
    OnPlayerEatEnemy = null;
  }
}
