using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Updated PlayerCore that uses the separated PlayerDataManager
/// This focuses on game logic while data management is handled separately
/// </summary>
public class PlayerCore : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGrowth playerGrowth;
    [SerializeField] private PlayerEffect playerEffect;
    
    [Header("Data Management")]
    [SerializeField] private PlayerDataManager dataManager;
    
    [Header("Gameplay Settings")]
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float respawnDelay = 1f;
    
    [Header("Events")]
    public UnityEvent<PlayerCore> OnPlayerDeath;
    public UnityEvent<PlayerCore, EnemyCore> OnPlayerEatEnemy;
    public UnityEvent<PlayerCore> OnPlayerSpawn;
    
    // Properties that delegate to data manager
    public bool IsAlive => dataManager.Data.isAlive;
    public bool IsInvincible => dataManager.Data.isInvincible;
    public float CurrentSize => dataManager.Data.currentSize;
    public int CurrentLevel => dataManager.Data.currentLevel;
    public int Lives => dataManager.Data.lives;
    public float Score => dataManager.Data.score;
    public int Level => dataManager.Data.currentLevel;
    public Vector2 LastPosition => dataManager.Data.lastPosition;
    
    // Component references
    public PlayerMovement Movement => playerMovement;
    public PlayerGrowth Growth => playerGrowth;
    public PlayerEffect Effect => playerEffect;
    public PlayerDataManager DataManager => dataManager;
    
    private Coroutine invincibilityCoroutine;
    
    #region Unity Lifecycle
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
            dataManager = GetComponent<PlayerDataManager>();
        
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
    public void EatEnemy(EnemyCore enemy)
    {
        if (!IsAlive || enemy == null) return;
        
        // Determine if this is a streak or speed kill
        bool isStreak = false; // Logic for streak detection could be added
        bool isSpeedKill = false; // Logic for speed kill detection could be added
        
        // Handle data changes through data manager
        dataManager.EatEnemy(enemy.Data.sizeLevel, isStreak, isSpeedKill);
        
        // Play effects
        if (playerEffect != null)
            playerEffect.PlayEatEffect();
        
        // Fire events
        OnPlayerEatEnemy?.Invoke(this, enemy);
    }
    
    public void OnEaten()
    {
        if (!IsAlive || IsInvincible) return;
        
        // Handle life loss through data manager
        dataManager.LoseLife();
        
        if (Lives <= 0)
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
        // Disable movement
        if (playerMovement != null)
            playerMovement.SetCanMove(false);
        
        // Play death effect
        if (playerEffect != null)
            playerEffect.PlayDeathEffect();
        
        // Fire death event
        OnPlayerDeath?.Invoke(this);
        
        Debug.Log($"Player died! Final Score: {Score:F1}, Level: {Level}");
    }
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

        dataManager.Data.isAlive = true;
        
        Debug.Log($"Player respawned! Lives: {Lives}, Size: {CurrentSize:F2}");
    }
    
    private System.Collections.IEnumerator RespawnCoroutine()
    {
        // Disable movement temporarily
        if (playerMovement != null)
            playerMovement.SetCanMove(false);

        DataManager.Data.isAlive = false;

        yield return new WaitForSeconds(respawnDelay);
        
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
        
        // Visual feedback for invincibility
        if (playerEffect != null)
            playerEffect.SetFlicker(true);
        
        yield return new WaitForSeconds(duration);
        
        dataManager.SetInvincible(false);
        
        if (playerEffect != null)
            playerEffect.SetFlicker(false);
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
    
    private void OnDataChangedHandler(PlayerData data)
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
    
    #region Utility Methods
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
    
    public void AddLife()
    {
        dataManager.AddLife();
    }
    
    public void AddScore(float points)
    {
        dataManager.AddScore(points);
    }
    
    public void DebugPlayerState()
    {
        dataManager.DebugPrintData();
    }
    #endregion
}
