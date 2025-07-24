using UnityEngine;

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
        if (enemyData == null) return;

        // Set fixed size from size level (no growth)
        currentLevel = enemyData.level;
        currentSize = enemyData.size;

        // Set sprite and animator
        if (enemyData.enemySprite != null)
        {
            // Since we separate logic and visuals, we get the SpriteRenderer from children
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = enemyData.enemySprite;
                spriteRenderer.transform.localScale = Vector3.one * currentSize;
            }
        }

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
}
