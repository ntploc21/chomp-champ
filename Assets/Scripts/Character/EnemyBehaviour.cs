using Michsky.UI.Reach;
using UnityEngine;
using UnityEngine.UIElements;

public enum EnemyAIState
{
    Wandering,
    Chasing,
    Fleeing,
    Idle
}

public class EnemyBehaviour : MonoBehaviour
{
    #region Editor Data
    private EnemyCore enemyCore;
    private EnemyData enemyData;
    private Transform player;
    private Transform visualTransform;
    private Rigidbody2D rb;

    [Header("AI State")]
    public EnemyAIState currentState = EnemyAIState.Wandering;

    [Header("Wander Settings")]
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float wanderCooldown = 2f; [Header("Detection")]
    private float playerDistance;
    private float playerDistanceSqr; // Cache squared distance for performance
    private bool playerDetected = false;

    [Header("World Boundaries")]
    private static Camera mainCamera; // Cache main camera reference
    private static Vector2 worldBounds; // Cache world boundaries
    private static bool boundsInitialized = false;
    #endregion

    public EnemyAIState CurrentState => currentState;

    private void Awake()
    {
        // Find visual transform if it exists
        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visuals");
            if (visualTransform == null)
            {
                Debug.LogWarning("Visual Transform not found! Please assign it in the inspector.");
            }
        }

        // Initialize world boundaries once for all enemies
        InitializeWorldBounds();
    }
    private void Start()
    {
        // Find player more efficiently using singleton pattern or cache reference
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        SetNewWanderTarget();
    }

    private void Update()
    {
        if (!enemyCore.isActive || enemyData == null) return;

        UpdateAI();
    }

    public void Initialize(EnemyCore core)
    {
        enemyCore = core;
        enemyData = core.Data;

        rb = enemyCore.Rigidbody;
    }

    private static void InitializeWorldBounds()
    {
        if (!boundsInitialized)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Calculate world bounds based on camera view
                float height = mainCamera.orthographicSize * 2f;
                float width = height * mainCamera.aspect;

                // Add some padding to keep enemies slightly within bounds
                worldBounds = new Vector2(width * 0.9f, height * 0.9f);
                boundsInitialized = true;
            }
        }
    }
    private void UpdateAI()
    {
        if (player != null && visualTransform != null)
        {
            // Use squared distance to avoid expensive sqrt calculation
            Vector2 toPlayer = player.position - visualTransform.position;
            playerDistanceSqr = toPlayer.sqrMagnitude;
            playerDistance = Mathf.Sqrt(playerDistanceSqr); // Only calculate when needed

            float detectionRangeSqr = enemyData.detectionRange * enemyData.detectionRange;
            playerDetected = playerDistanceSqr <= detectionRangeSqr;
        }

        // Determine AI state based on enemy type and player detection
        switch (enemyData.enemyType)
        {
            case EnemyType.Prey:
                HandlePreyBehavior();
                break;
            case EnemyType.Predator:
                HandlePredatorBehavior();
                break;
            case EnemyType.Neutral:
                HandleNeutralBehavior();
                break;
        }

        // Execute movement based on current state
        ExecuteMovement();
    }

    private void HandlePreyBehavior()
    {
        if (playerDetected && player != null)
        {
            // Check if player is larger (dangerous)
            PlayerCore playerCore = player.GetComponent<PlayerCore>();
            if (playerCore != null && playerCore.currentSize >= enemyCore.currentSize)
            {
                currentState = EnemyAIState.Fleeing;
                return;
            }
        }

        currentState = EnemyAIState.Wandering;
    }

    private void HandlePredatorBehavior()
    {
        if (playerDetected && player != null)
        {
            // Check if player is smaller (prey)
            PlayerCore playerCore = player.GetComponent<PlayerCore>();
            if (playerCore != null && playerCore.currentSize < enemyCore.currentSize)
            {
                currentState = EnemyAIState.Chasing;
                return;
            }
        }

        currentState = EnemyAIState.Wandering;
    }

    private void HandleNeutralBehavior()
    {
        currentState = EnemyAIState.Wandering;
    }

    private void ExecuteMovement()
    {
        Vector2 targetDirection = Vector2.zero;
        float moveSpeed = enemyData.baseSpeed;

        switch (currentState)
        {
            case EnemyAIState.Wandering:
                targetDirection = HandleWandering();
                break;

            case EnemyAIState.Chasing:
                if (player != null && visualTransform != null)
                {
                    targetDirection = ((Vector2)player.position - (Vector2)visualTransform.position).normalized;
                    moveSpeed *= enemyData.chaseSpeedMultiplier;
                }
                break;

            case EnemyAIState.Fleeing:
                if (player != null && visualTransform != null)
                {
                    targetDirection = ((Vector2)visualTransform.position - (Vector2)player.position).normalized;
                    moveSpeed *= enemyData.fleeSpeedMultiplier;
                }
                break;
        }

        // Apply movement
        if (targetDirection != Vector2.zero && rb != null)
        {
            rb.velocity = targetDirection * moveSpeed;

            // Optimize rotation calculation
            if (targetDirection.sqrMagnitude > 0.01f) // Only rotate if significant movement
            {
                float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                visualTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
    private Vector2 HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        // Use squared distance for performance
        Vector2 toTarget = wanderTarget - (Vector2)visualTransform.position;
        float distanceToTargetSqr = toTarget.sqrMagnitude;

        // Check if we reached the wander target or timer expired
        if (wanderTimer <= 0f || distanceToTargetSqr < 0.25f) // 0.25f = 0.5f squared
        {
            SetNewWanderTarget();
        }

        return toTarget.normalized;
    }

    private void SetNewWanderTarget()
    {
        Vector2 currentPos = (Vector2)visualTransform.position;
        Vector2 newTarget;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            // Improved wander target selection with better distribution
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(enemyData.wanderRadius * 0.3f, enemyData.wanderRadius);

            Vector2 randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            newTarget = currentPos + randomDirection * distance;

            attempts++;
        }
        // Keep trying until we find a target within world bounds or reach max attempts
        while (attempts < maxAttempts && boundsInitialized &&
               (Mathf.Abs(newTarget.x) > worldBounds.x * 0.5f || Mathf.Abs(newTarget.y) > worldBounds.y * 0.5f));

        // If we couldn't find a good target within bounds, move towards center
        if (boundsInitialized && attempts >= maxAttempts)
        {
            Vector2 toCenter = Vector2.zero - currentPos;
            newTarget = currentPos + toCenter.normalized * (enemyData.wanderRadius * 0.5f);
        }

        wanderTarget = newTarget;
        wanderTimer = wanderCooldown;
    }
    private void OnDrawGizmosSelected()
    {
        if (enemyData != null && visualTransform != null)
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(visualTransform.position, enemyData.detectionRange);

            // Draw wander radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(visualTransform.position, enemyData.wanderRadius);

            // Draw current wander target
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wanderTarget, 0.2f);
        }
    }
}
