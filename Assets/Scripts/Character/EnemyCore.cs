using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;

public class EnemyCore : MonoBehaviour
{
    #region Editor Data
    [Header("Components")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyBehaviour enemyBehaviour;
    [SerializeField] private EnemyEffect enemyEffect;

    [Header("Runtime Properties")]
    [SerializeField] public bool isActive = true;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D _collider;
    #endregion

    #region Internal Data
    private int currentLevel = 1; // Default level, can be set from data
    private float currentSize = 1f; // Default size, can be modified based on level or other factors
    
    // Animation system
    private Animator _animator;
    private SpriteLibrary _spriteLibrary;
    private SpriteResolver _spriteResolver;
    
    // Cached animation parameter hashes for performance
    private int hitHash;
    private int deathHash;
    private int invincibleHash;
    private int moveXHash;
    private int moveYHash;
    private int isMovingHash;
    #endregion

    #region Properties
    public EnemyData Data => enemyData;
    public EnemyBehaviour Behaviour => enemyBehaviour;
    public EnemyEffect Effects => enemyEffect;
    public Rigidbody2D Rigidbody => _rigidbody;
    public Collider2D Collider => _collider;

    public int CurrentLevel => currentLevel;
    public float CurrentSize => currentSize;
    #endregion

    #region Unity Events
    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
        if (_collider == null)
            _collider = GetComponent<Collider2D>();
        if (enemyData != null)
            SetEnemyData(enemyData);

        InitializeComponents();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        // Cache components only once to avoid repeated GetComponent calls
        if (enemyBehaviour == null)
            enemyBehaviour = GetComponent<EnemyBehaviour>();

        if (enemyEffect == null)
            enemyEffect = GetComponent<EnemyEffect>();
    }
    public void InitializeFromData()
    {
        if (enemyData == null) return;        // Set fixed size from size level (no growth)
        currentLevel = enemyData.level;
        currentSize = enemyData.size;

        // Initialize sprite library system
        InitializeSpriteLibrarySystem();

        // Initialize behavior
        if (enemyBehaviour != null)
        {
            enemyBehaviour.Initialize(this);
        }

        // Initialize effects
        if (enemyEffect != null)
        {
            enemyEffect.Initialize(this);
        }

        // Set physics properties once
        if (_rigidbody != null)
        {
            _rigidbody.gravityScale = 0f; // Top-down game
            _rigidbody.mass = currentSize; // Set mass based on size, can be adjusted further
        }

        // Optimize collider setup - avoid repeated type checking
        if (_collider != null && enemyData.hitboxRadius > 0)
        {
            // Use pattern matching for better performance
            switch (_collider)
            {
                case CircleCollider2D circleCol:
                    circleCol.radius = enemyData.hitboxRadius;
                    break;
                case BoxCollider2D boxCol:
                    boxCol.size = Vector2.one * (enemyData.hitboxRadius * 2f);
                    break;
                case CapsuleCollider2D capsuleCol:
                    capsuleCol.size = new Vector2(enemyData.hitboxRadius * 2f, enemyData.hitboxRadius * 2f);
                    break;
            }
        }

        isActive = true;
    }

    /// <summary>
    /// Set the enemy data and initialize the enemy core.
    /// </summary>
    /// <param name="data"></param>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        InitializeFromData();
    }

    /// <summary>
    /// Initialize the sprite library system for this enemy
    /// </summary>
    private void InitializeSpriteLibrarySystem()
    {
        if (enemyData == null) return;

        // Get or create sprite renderer (usually on child GameObject)
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"EnemyCore: No SpriteRenderer found on {gameObject.name} or its children!");
            return;
        }
        GameObject spriteObject = spriteRenderer.gameObject;

        // Set up SpriteLibrary component
        _spriteLibrary = spriteObject.GetComponent<SpriteLibrary>();
        if (_spriteLibrary == null)
            _spriteLibrary = spriteObject.AddComponent<SpriteLibrary>();

        // Set up SpriteResolver component
        _spriteResolver = spriteObject.GetComponent<SpriteResolver>();
        if (_spriteResolver == null)
            _spriteResolver = spriteObject.AddComponent<SpriteResolver>();

        // Set up Animator component
        _animator = spriteObject.GetComponent<Animator>();
        if (_animator == null && enemyData.animatorController != null)
            _animator = spriteObject.AddComponent<Animator>();

        // Configure sprite library
        if (enemyData.spriteLibrary != null)
        {
            _spriteLibrary.spriteLibraryAsset = enemyData.spriteLibrary;
            
            // Set default category and label
            if (!string.IsNullOrEmpty(enemyData.defaultSpriteCategory) && 
                !string.IsNullOrEmpty(enemyData.defaultSpriteLabel))
            {
                _spriteResolver.SetCategoryAndLabel(enemyData.defaultSpriteCategory, enemyData.defaultSpriteLabel);
            }
        }
        else if (enemyData.fallbackSprite != null)
        {
            // Use fallback sprite for backward compatibility
            spriteRenderer.sprite = enemyData.fallbackSprite;
        }

        // Set up animator controller
        if (enemyData.animatorController != null)
        {
            _animator.runtimeAnimatorController = enemyData.animatorController;
        }
        
        // Cache animation parameter hashes for performance
        CacheAnimationParameters();

        // Apply size scaling
        spriteRenderer.transform.localScale = Vector3.one * currentSize;
    }

    /// <summary>
    /// Cache animation parameter hashes for better performance
    /// </summary>
    private void CacheAnimationParameters()
    {
        if (enemyData == null || _animator == null) return;

        hitHash = Animator.StringToHash(enemyData.hitAnimationParameter);
        deathHash = Animator.StringToHash(enemyData.deathAnimationParameter);
        invincibleHash = Animator.StringToHash(enemyData.invincibleAnimationParameter);
        isMovingHash = Animator.StringToHash(enemyData.isMovingAnimationParameter);
    }
    #endregion

    #region Animation Control
    /// <summary>
    /// Trigger hit animation
    /// </summary>
    public void TriggerHit()
    {
        if (_animator != null && hitHash != 0)
            _animator.SetTrigger(hitHash);
    }

    /// <summary>
    /// Trigger death animation
    /// </summary>
    public void TriggerDeath()
    {
        if (_animator != null && deathHash != 0)
            _animator.SetTrigger(deathHash);
    }

    /// <summary>
    /// Set invincible state
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        if (_animator != null && invincibleHash != 0)
            _animator.SetBool(invincibleHash, invincible);
    }

    /// <summary>
    /// Set movement parameters for animation
    /// </summary>
    public void SetMovement(Vector2 movement)
    {
        if (_animator == null) return;

        if (isMovingHash != 0)
        {
            _animator.SetBool(isMovingHash, movement.magnitude > 0.1f);

            // Set movement direction parameters
            if (movement.x != 0)
            {
                enemyEffect.SpriteRenderer.flipX = movement.x < 0; // Flip sprite based on movement direction
            }
        }
    }

    /// <summary>
    /// Change sprite category at runtime (useful for special states)
    /// </summary>
    public void SetSpriteCategory(string category)
    {
        if (_spriteResolver != null && !string.IsNullOrEmpty(category))
        {
            _spriteResolver.SetCategoryAndLabel(category, enemyData.defaultSpriteLabel);
        }
    }

    /// <summary>
    /// Change sprite variant at runtime
    /// </summary>
    public void SetSpriteVariant(string label)
    {
        if (_spriteResolver != null && !string.IsNullOrEmpty(label))
        {
            _spriteResolver.SetCategoryAndLabel(enemyData.defaultSpriteCategory, label);
        }
    }

    /// <summary>
    /// Get current sprite category
    /// </summary>
    public string GetCurrentSpriteCategory()
    {
        return _spriteResolver?.GetCategory();
    }

    /// <summary>
    /// Get current sprite label
    /// </summary>
    public string GetCurrentSpriteLabel()
    {
        return _spriteResolver?.GetLabel();
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Returns the enemy to the pool or disables it.
    /// This method should be called when the enemy is no longer needed,
    /// such as when it is eaten or defeated.
    /// </summary>
    private void ReturnToPool()
    {
        // This will be handled by ObjectPool system
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the enemy is eaten by the player.
    /// This method handles the enemy's death effects and disables it.
    /// </summary>
    public void OnEaten()
    {
        isActive = false;

        // Play death effects
        if (enemyEffect != null)
        {
            enemyEffect.PlayDeathEffect();
        }

        // Disable or return to pool
        ReturnToPool();
    }

    public int GetLevelComparison(int otherLevel)
    {
        return currentLevel - otherLevel;
    }

    public bool IsLargerThan(int otherLevel)
    {
        return currentLevel > otherLevel;
    }

    public bool IsSmallerThan(int otherLevel)
    {
        return currentLevel < otherLevel;
    }
    #endregion

    #region Public Methods
    public void SetLevel(int level)
    {
        if (level < 1) level = 1; // Ensure minimum level is 1
        currentLevel = level;
    }

    public void ResetEnemy()
    {
        currentLevel = 1; // Reset to default level
        currentSize = 1f; // Reset size to default
        InitializeFromData(); // Reinitialize with current data
    }
    #endregion

    #region Alpha Fix for Animator Override
    /// <summary>
    /// Fix animator alpha override by toggling animator state
    /// This is more efficient than continuous LateUpdate checking
    /// </summary>
    private void FixAnimatorAlphaOverride(SpriteRenderer spriteRenderer)
    {
        if (_animator != null && spriteRenderer != null)
        {
            // Disable animator to prevent override
            _animator.enabled = false;

            // Set desired alpha while animator is disabled
            Color color = Color.white;
            color.a = 1f;
            spriteRenderer.color = color;
            
            // Re-enable animator
            _animator.enabled = true;
        }
    }
    
    /// <summary>
    /// Public method to manually fix alpha if needed at runtime
    /// </summary>
    public void ResetAnimatorAlpha()
    {
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            FixAnimatorAlphaOverride(spriteRenderer);
        }
    }
    #endregion
}
