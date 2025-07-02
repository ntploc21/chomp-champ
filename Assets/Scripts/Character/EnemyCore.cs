using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    #region Editor Data
    [Header("Components")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyBehaviour enemyBehaviour;
    [SerializeField] private EnemyEffect enemyEffect;

    [Header("Runtime Properties")]
    [SerializeField] public float currentSize = 1f;
    [SerializeField] public bool isActive = true;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D _collider;
    #endregion

    public EnemyData Data => enemyData;
    public EnemyBehaviour Behaviour => enemyBehaviour;
    public EnemyEffect Effects => enemyEffect;
    public Rigidbody2D Rigidbody => _rigidbody;
    public Collider2D Collider => _collider;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
        if (_collider == null)
            _collider = GetComponent<Collider2D>();

        InitializeComponents();
    }

    private void Start()
    {
        if (enemyData != null)
        {
            InitializeFromData();
        }
    }

    private void InitializeComponents()
    {
        if (enemyBehaviour == null)
            enemyBehaviour = GetComponent<EnemyBehaviour>();

        if (enemyEffect == null)
            enemyEffect = GetComponent<EnemyEffect>();
    }

    public void InitializeFromData()
    {
        if (enemyData == null) return;

        // Set initial size and scale
        currentSize = enemyData.sizeLevel;
        transform.localScale = Vector3.one * currentSize;

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

        // Set physics properties
        if (_rigidbody != null)
        {
            _rigidbody.gravityScale = 0f; // Top-down game
        }

        // Set collider size based on enemy data
        if (_collider != null && enemyData.hitboxRadius > 0)
        {
            if (_collider is CircleCollider2D circleCol)
            {
                circleCol.radius = enemyData.hitboxRadius;
            }
            else if (_collider is BoxCollider2D boxCol)
            {
                boxCol.size = Vector2.one * (enemyData.hitboxRadius * 2f);
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

    public float GetSizeComparison(float otherSize)
    {
        return currentSize - otherSize;
    }

    public bool IsLargerThan(float otherSize)
    {
        return currentSize > otherSize;
    }

    public bool IsSmallerThan(float otherSize)
    {
        return currentSize < otherSize;
    }
}
