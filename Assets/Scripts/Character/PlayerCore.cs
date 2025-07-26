using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D.Animation;

/// <summary>
/// Updated PlayerCore that uses the separated PlayerDataManager
/// This focuses on game logic while data management is handled separately
/// </summary>
public class PlayerCore : MonoBehaviour
{
    #region Editor Data
    [Header("Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGrowth playerGrowth;
    [SerializeField] private PlayerEffect playerEffect;

    [Header("Data Management")]
    [SerializeField] private GameDataManager dataManager;

    [Header("Gameplay Settings")]
    [Tooltip("Duration of invincibility after respawn or eaten by an enemy.")]
    [SerializeField] private float invincibilityDuration = 2f; [Tooltip("Delay before respawning after death.")]
    [SerializeField] private float respawnDelay = 2f;

    [Header("Events")]
    public UnityEvent<PlayerCore> OnPlayerSpawn = null;
    public UnityEvent<PlayerCore> OnPlayerDeath = null;
    public UnityEvent<PlayerCore, EnemyCore> OnPlayerEatEnemy = null;

    [Header("Player Animation System")]
    [Tooltip("Reference to the player body game object for animations.")]
    [SerializeField] private GameObject playerBody;

    [Tooltip("Sprite Library Asset for player animations.")]
    [SerializeField] private SpriteLibraryAsset playerSpriteLibrary;
    [Tooltip("Player animator controller.")]
    [SerializeField] private RuntimeAnimatorController playerAnimatorController;
    [Tooltip("Default sprite category for player.")]
    [SerializeField] private string defaultSpriteCategory = "Idle";
    [Tooltip("Default sprite label for player.")]
    [SerializeField] private string defaultSpriteLabel = "Player";

    [Header("Animation Parameters")]
    [SerializeField] private string hitAnimationParameter = "hit";
    [SerializeField] private string deathAnimationParameter = "death";
    [SerializeField] private string invincibleAnimationParameter = "invincible";
    [SerializeField] private string isMovingAnimationParameter = "isMoving";
    #endregion

    #region Properties
    // Properties that delegate to data manager
    public bool IsAlive => dataManager.SessionData.isAlive;
    public bool IsInvincible => dataManager.SessionData.isInvincible;
    public float CurrentSize => dataManager.SessionData.currentSize;
    public int CurrentLevel => dataManager.SessionData.currentLevel;
    public int Lives => dataManager.SessionData.lives;
    public float Score => dataManager.SessionData.score;
    public int Level => dataManager.SessionData.currentLevel;
    public Vector2 LastPosition => dataManager.SessionData.lastPosition;

    // Component references    public PlayerMovement Movement => playerMovement;
    public PlayerGrowth Growth => playerGrowth;
    public PlayerEffect Effect => playerEffect;
    public GameDataManager DataManager => dataManager;
    #endregion

    #region Internal Data
    // Coroutine for invincibility effect
    private Coroutine invincibilityCoroutine;

    // Animation system components
    private Animator _animator;
    private SpriteLibrary _spriteLibrary;
    private SpriteResolver _spriteResolver;

    // Cached animation parameter hashes for performance
    private int hitHash;
    private int deathHash;
    private int invincibleHash;
    private int isMovingHash;
    #endregion

    #region Unity Events
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializePlayer();
        SubscribeToDataEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDataEvents();
    }
    #endregion

    #region Tick
    private void Update()
    {
        // Update position in data manager
        dataManager.UpdatePosition(transform.position);

        // Handle any real-time logic here
        HandleMovementBasedOnSize();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerGrowth == null)
            playerGrowth = GetComponent<PlayerGrowth>();

        if (playerEffect == null)
            playerEffect = GetComponent<PlayerEffect>();
        if (dataManager == null)
            dataManager = GetComponent<GameDataManager>();

        // Initialize sprite library system
        InitializePlayerSpriteLibrarySystem();

        // Initialize components with this core reference
        playerMovement?.Initialize(this);
        playerGrowth?.Initialize(this);
        playerEffect?.Initialize(this);
    }

    private void InitializePlayer()
    {
        // Apply initial data to visual components
        ApplyDataToVisuals();

        // Play spawn effect
        if (playerEffect != null)
            playerEffect.PlaySpawnEffect();

        OnPlayerSpawn?.Invoke(this);
    }

    private void SubscribeToDataEvents()
    {
        if (dataManager != null)
        {
            dataManager.OnLevelUp.AddListener(OnLevelUpHandler);
            dataManager.OnLivesChanged.AddListener(OnLivesChangedHandler);
            dataManager.OnDataChanged.AddListener(OnDataChangedHandler);
        }
    }

    private void UnsubscribeFromDataEvents()
    {
        if (dataManager != null)
        {
            dataManager.OnLevelUp.RemoveListener(OnLevelUpHandler);
            dataManager.OnLivesChanged.RemoveListener(OnLivesChangedHandler);
            dataManager.OnDataChanged.RemoveListener(OnDataChangedHandler);
        }
    }
    #endregion

    #region Core Gameplay
    /// <summary>
    /// Handles the logic when the player eats an enemy.
    /// This method updates the player's data, plays effects, and fires events.
    /// </summary>
    /// <param name="enemy"></param>
    public void EatEnemy(EnemyCore enemy)
    {
        if (!IsAlive || enemy == null) return;

        // Determine if this is a streak or speed kill
        bool isStreak = false; // Logic for streak detection could be added

        // Handle data changes through data manager
        dataManager.EatEnemy(enemy.Data.level, isStreak);

        // Play effects
        if (playerEffect != null)
            playerEffect.PlayEatEffect();

        // Fire events
        OnPlayerEatEnemy?.Invoke(this, enemy);
    }    /// <summary>
         /// Handles the logic when the player is eaten by an enemy.
         /// This method reduces the player's lives, handles respawn, and plays death effects.
         /// </summary>
    public void OnEaten()
    {
        if (!IsAlive || IsInvincible) return;

        // Trigger hit animation
        TriggerHit();

        // Handle life loss through data manager
        dataManager.LoseLife();

        if (Lives <= 0)
        {
            OnDeath();
        }
        else
        {
            StartCoroutine(RespawnCoroutine(respawnDelay));
        }
    }    /// <summary>
         /// Handles the player's death logic.
         /// This method disables movement, plays death effects, and fires the death event.
         /// </summary>
    private void OnDeath()
    {
        // Trigger death animation
        TriggerDeath();

        // Disable movement
        if (playerMovement != null)
            playerMovement.SetCanMove(false);

        // Play death effect
        if (playerEffect != null)
            playerEffect.PlayDeathEffect();

        // Fire death event
        OnPlayerDeath?.Invoke(this);
    }

    /// <summary>
    /// Respawns the player after death.
    /// This method makes the player temporarily invincible, re-enables movement,
    /// </summary>
    private void Respawn()
    {
        // Make player temporarily invincible
        SetInvincible(invincibilityDuration);

        // Re-enable movement
        if (playerMovement != null)
            playerMovement.SetCanMove(true);

        // Play respawn effect
        if (playerEffect != null)
            playerEffect.PlayRespawnEffect();

        // Set isAlive to true
        dataManager.SessionData.isAlive = true;

        Debug.Log($"Player respawned! Lives: {Lives}, Size: {CurrentSize:F2}");
    }

    private System.Collections.IEnumerator RespawnCoroutine(float delay = 2f)
    {
        // Disable movement temporarily
        if (playerMovement != null)
            playerMovement.SetCanMove(false);

        dataManager.SessionData.isAlive = false;

        yield return new WaitForSeconds(delay);

        Respawn();
    }

    public void SetInvincible(float duration)
    {
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine = StartCoroutine(InvincibilityCoroutine(duration));
    }
    private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
    {
        dataManager.SetInvincible(true);

        // Set invincible animation state
        SetInvincibleAnimation(true);

        // Visual feedback for invincibility
        if (playerEffect != null)
            playerEffect.SetFlicker(true);

        yield return new WaitForSeconds(duration);

        dataManager.SetInvincible(false);

        // Clear invincible animation state
        SetInvincibleAnimation(false);

        if (playerEffect != null)
            playerEffect.SetFlicker(false);
    }
    #endregion

    #region Player Animation Control
    /// <summary>
    /// Triggers the hit animation for the player
    /// </summary>
    public void TriggerHit()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(hitHash);
        }
    }

    /// <summary>
    /// Triggers the death animation for the player
    /// </summary>
    public void TriggerDeath()
    {
        if (_animator != null)
        {
            _animator.SetTrigger(deathHash);
        }
    }

    /// <summary>
    /// Sets the invincible state in the animator
    /// </summary>
    /// <param name="isInvincible">Whether the player is invincible</param>
    public void SetInvincibleAnimation(bool isInvincible)
    {
        if (_animator != null)
        {
            _animator.SetBool(invincibleHash, isInvincible);
        }
    }

    /// <summary>
    /// Updates movement-related animation parameters
    /// </summary>
    /// <param name="movement">Movement vector</param>
    public void SetMovementAnimation(Vector2 movement)
    {
        if (_animator != null)
        {
            _animator.SetBool("isMoving", movement.magnitude > 0.1f);

            // Check if player is moving left/right to flip sprite
            if (movement.x != 0f)
            {
                playerEffect.SpriteRenderer.flipX = movement.x < 0f;
            }
        }
    }

    /// <summary>
    /// Sets a specific sprite category for the player
    /// </summary>
    /// <param name="category">The sprite category to use</param>
    public void SetSpriteCategory(string category)
    {
        if (_spriteResolver != null && !string.IsNullOrEmpty(category))
        {
            string currentLabel = _spriteResolver.GetLabel();
            _spriteResolver.SetCategoryAndLabel(category, currentLabel);
        }
    }

    /// <summary>
    /// Sets a specific sprite variant for the player
    /// </summary>
    /// <param name="label">The sprite label/variant to use</param>
    public void SetSpriteVariant(string label)
    {
        if (_spriteResolver != null && !string.IsNullOrEmpty(label))
        {
            string currentCategory = _spriteResolver.GetCategory();
            _spriteResolver.SetCategoryAndLabel(currentCategory, label);
        }
    }

    /// <summary>
    /// Sets both sprite category and variant at once
    /// </summary>
    /// <param name="category">The sprite category</param>
    /// <param name="label">The sprite label/variant</param>
    public void SetSprite(string category, string label)
    {
        if (_spriteResolver != null && !string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(label))
        {
            _spriteResolver.SetCategoryAndLabel(category, label);
        }
    }
    #endregion

    #region Data Event Handlers
    private void OnLevelUpHandler(int newLevel)
    {
        // Handle visual/audio feedback for level up
        if (playerEffect != null)
            playerEffect.PlayGrowthEffect();

        // Update visual size
        ApplyDataToVisuals();

        Debug.Log($"Level up! New level: {newLevel}");
    }

    private void OnLivesChangedHandler(int newLives)
    {
        // Handle UI updates or other life-related effects
        Debug.Log($"Lives changed: {newLives}");
    }

    private void OnDataChangedHandler(GameSessionData data)
    {
        // Apply any data changes to visual components
        ApplyDataToVisuals();
    }
    #endregion

    #region Visual Updates
    private void ApplyDataToVisuals()
    {
        // Update size
        if (playerGrowth != null)
            playerGrowth.SetSize(CurrentSize, false);

        // Update any other visual elements based on data
        UpdateMovementBasedOnSize();
    }

    private void HandleMovementBasedOnSize()
    {
        // This could adjust movement speed based on size
        // Implementation depends on your game design
    }

    private void UpdateMovementBasedOnSize()
    {
        // Adjust movement parameters based on current size
        // This is just an example - adjust based on your needs
        if (playerMovement != null)
        {
            float sizeModifier = Mathf.Lerp(1.2f, 0.8f, (CurrentSize - 1f) / 9f); // Smaller = faster
            // Apply size modifier to movement (this would need to be implemented in PlayerMovement)
        }
    }
    #endregion

    #region Public Methods
    public void ResetPlayer()
    {
        // Reset through data manager
        dataManager.ResetPlayerData();

        // Reset visual state
        ApplyDataToVisuals();

        // Re-enable movement
        if (playerMovement != null)
            playerMovement.SetCanMove(true);

        // Stop any running coroutines
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            invincibilityCoroutine = null;
        }

        Debug.Log("Player reset to initial state");
    }

    public void AddLives()
    {
        dataManager.AddLives();
    }

    public void AddScore(float points)
    {
        dataManager.AddScore(points);
    }

    public void DebugPlayerState()
    {
        dataManager.PrintDebugData();
    }
    #endregion

    #region Sprite Library System
    /// <summary>
    /// Initializes the player sprite library system by setting up required components
    /// </summary>
    private void InitializePlayerSpriteLibrarySystem()
    {
        // Get or add Animator component
        _animator = playerBody.GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = playerBody.AddComponent<Animator>();
        }

        // Set animator controller if specified
        if (playerAnimatorController != null)
        {
            _animator.runtimeAnimatorController = playerAnimatorController;
        }

        // Get or add SpriteLibrary component
        _spriteLibrary = playerBody.GetComponent<SpriteLibrary>();
        if (_spriteLibrary == null)
        {
            _spriteLibrary = playerBody.AddComponent<SpriteLibrary>();
        }

        // Set sprite library asset if specified
        if (playerSpriteLibrary != null)
        {
            _spriteLibrary.spriteLibraryAsset = playerSpriteLibrary;
        }

        // Get or add SpriteResolver component
        _spriteResolver = playerBody.GetComponent<SpriteResolver>();
        if (_spriteResolver == null)
        {
            _spriteResolver = playerBody.AddComponent<SpriteResolver>();
        }

        // Set default sprite if specified
        if (!string.IsNullOrEmpty(defaultSpriteCategory) && !string.IsNullOrEmpty(defaultSpriteLabel))
        {
            _spriteResolver.SetCategoryAndLabel(defaultSpriteCategory, defaultSpriteLabel);
        }

        // Cache animation parameters for performance
        CacheAnimationParameters();

        Debug.Log("Player Sprite Library System initialized successfully.");
    }

    /// <summary>
    /// Caches animation parameter hashes for performance optimization
    /// </summary>
    private void CacheAnimationParameters()
    {
        if (_animator == null) return;

        hitHash = Animator.StringToHash(hitAnimationParameter);
        deathHash = Animator.StringToHash(deathAnimationParameter);
        invincibleHash = Animator.StringToHash(invincibleAnimationParameter);
        isMovingHash = Animator.StringToHash(isMovingAnimationParameter);
    }
    #endregion
}
