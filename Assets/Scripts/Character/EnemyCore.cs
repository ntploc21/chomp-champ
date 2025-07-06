using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    #region Editor Data
    [Header("Components")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyBehaviour enemyBehaviour;
    [SerializeField] private EnemyEffect enemyEffect;

    [Header("Runtime Properties")]
    [SerializeField] public int currentLevel = 1;
    [SerializeField][Range(0.1f, 5f)] public float size = 1f; // Fixed size (no growth)
    [SerializeField] public bool isActive = true;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D _collider;
    #endregion

    #region Properties
    public EnemyData Data => enemyData;
    public EnemyBehaviour Behaviour => enemyBehaviour;
    public EnemyEffect Effects => enemyEffect;
    public Rigidbody2D Rigidbody => _rigidbody;
    public Collider2D Collider => _collider;
    #endregion

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
        currentLevel = enemyData.sizeLevel;
        size = enemyData.size;

        // Set sprite and animator
        if (enemyData.enemySprite != null)
        {
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = enemyData.enemySprite;
                spriteRenderer.transform.localScale = Vector3.one * size;
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
            }
        }

        isActive = true;
    }
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        InitializeFromData();
    }

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

    private void ReturnToPool()
    {
        // This will be handled by ObjectPool system
        gameObject.SetActive(false);
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
}
