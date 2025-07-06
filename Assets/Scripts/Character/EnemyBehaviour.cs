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

public enum WanderPattern
{
    Random,          // Current behavior
    Momentum         // Prefer current direction with slight adjustments
}

public class EnemyBehaviour : MonoBehaviour
{
    #region Editor Data
    private EnemyCore enemyCore;
    private EnemyData enemyData;
    private Transform player;
    private Rigidbody2D rb;

    [Header("AI State")]
    public EnemyAIState currentState = EnemyAIState.Wandering;

    [Header("Wander Settings")]
    [SerializeField] private WanderPattern wanderPattern = WanderPattern.Momentum;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float wanderCooldown = 2f;

    [Header("Collision Avoidance")]
    [SerializeField] private float avoidanceDistance = 2f;
    [SerializeField] private float avoidanceForce = 5f;
    [SerializeField] private LayerMask obstacleLayer = -1; // What layers to avoid
    [SerializeField] private LayerMask enemyLayer = 1 << 6; // Enemy layer for avoiding other enemies

    [Header("Momentum Settings")]
    [SerializeField] private float directionChangeChance = 0.3f; // Chance to change direction each update
    [SerializeField] private float maxTurnAngle = 45f; // Maximum degrees to turn per direction change

    [Header("Detection")]
    private float playerDistance;
    private float playerDistanceSqr; // Cache squared distance for performance
    private bool playerDetected = false;
    #endregion

    #region Internal Data
    private Vector2 currentWanderDirection = Vector2.zero;
    private float wanderDirectionChangeTimer;
    private float wanderDirectionChangeCooldown = 5f; // How often to change direction while wandering

    // New momentum-based movement
    private Vector2 lastMovementDirection = Vector2.right;
    private Vector2 avoidanceDirection = Vector2.zero;
    #endregion

    public EnemyAIState CurrentState => currentState;

    #region Unity Lifecycle
    private void Awake()
    {
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
    #endregion

    #region Tick
    private void Update()
    {
        if (!enemyCore.isActive || enemyData == null)
        {
            Debug.LogWarning($"EnemyCore is inactive or EnemyData is null for {gameObject.name}");
            return;
        }

        UpdateAI();
    }
    #endregion

    public void Initialize(EnemyCore core)
    {
        enemyCore = core;
        enemyData = core.Data;

        rb = enemyCore.Rigidbody;
    }

    private void UpdateAI()
    {
        if (player != null && transform != null)
        {
            // Use squared distance to avoid expensive sqrt calculation
            Vector2 toPlayer = player.position - transform.position;
            playerDistanceSqr = toPlayer.sqrMagnitude;
            playerDistance = Mathf.Sqrt(playerDistanceSqr); // Only calculate when needed

            float detectionRangeSqr = enemyData.detectionRange * enemyData.detectionRange;
            playerDetected = playerDistanceSqr <= detectionRangeSqr;
        }

        // Calculate avoidance direction
        CalculateAvoidance();

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

    private void CalculateAvoidance()
    {
        Vector2 avoidance = Vector2.zero;
        Vector2 currentPos = transform.position;

        // Avoid obstacles
        RaycastHit2D[] obstacles = Physics2D.CircleCastAll(currentPos, avoidanceDistance, Vector2.zero, 0f, obstacleLayer);
        foreach (RaycastHit2D hit in obstacles)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                Vector2 directionAway = (currentPos - (Vector2)hit.collider.transform.position).normalized;
                float distance = Vector2.Distance(currentPos, hit.collider.transform.position);
                float avoidanceStrength = Mathf.Clamp01(1f - (distance / avoidanceDistance));
                avoidance += directionAway * avoidanceStrength;
            }
        }

        // Avoid other enemies
        RaycastHit2D[] enemies = Physics2D.CircleCastAll(currentPos, avoidanceDistance, Vector2.zero, 0f, enemyLayer);
        foreach (RaycastHit2D hit in enemies)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                Vector2 directionAway = (currentPos - (Vector2)hit.collider.transform.position).normalized;
                float distance = Vector2.Distance(currentPos, hit.collider.transform.position);
                float avoidanceStrength = Mathf.Clamp01(1f - (distance / avoidanceDistance));
                avoidance += directionAway * avoidanceStrength * 0.5f; // Weaker avoidance for enemies
            }
        }

        // Also use raycasting in movement direction for better obstacle detection
        if (lastMovementDirection != Vector2.zero)
        {
            RaycastHit2D forwardHit = Physics2D.Raycast(currentPos, lastMovementDirection, avoidanceDistance, obstacleLayer);
            if (forwardHit.collider != null)
            {
                Vector2 perpendicularDirection = Vector2.Perpendicular(lastMovementDirection).normalized;
                avoidance += perpendicularDirection * 2f; // Strong avoidance in perpendicular direction
            }
        }

        avoidanceDirection = avoidance.normalized;
    }

    private void HandlePreyBehavior()
    {
        if (playerDetected && player != null)
        {
            // Check if player is larger (dangerous)
            PlayerCore playerCore = player.GetComponent<PlayerCore>();
            if (playerCore != null && playerCore.CurrentLevel >= enemyCore.currentLevel)
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
            PlayerCore playerCore = player.GetComponent<PlayerCore>();

            // If the player is dead or invincible, stop chasing
            if (playerCore != null && (!playerCore.IsAlive || playerCore.IsInvincible))
            {
                currentState = EnemyAIState.Wandering;
                return;
            }

            // Check if player is smaller (prey)
            if (playerCore != null && playerCore.CurrentLevel < enemyCore.currentLevel)
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
                if (player != null && transform != null)
                {
                    targetDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    moveSpeed *= enemyData.chaseSpeedMultiplier;
                }
                break;

            case EnemyAIState.Fleeing:
                if (player != null && transform != null)
                {
                    targetDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
                    moveSpeed *= enemyData.fleeSpeedMultiplier;
                }
                break;
        }

        // Blend target direction with avoidance
        if (avoidanceDirection != Vector2.zero)
        {
            targetDirection = Vector2.Lerp(targetDirection, avoidanceDirection, avoidanceForce * Time.deltaTime).normalized;
        }

        // Apply movement
        if (targetDirection != Vector2.zero && rb != null)
        {
            rb.velocity = targetDirection * moveSpeed;

            // Optimize rotation calculation
            if (targetDirection.sqrMagnitude > 0.01f) // Only rotate if significant movement
            {
                float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        // Store movement direction for momentum system
        if (targetDirection != Vector2.zero)
        {
            lastMovementDirection = targetDirection;
        }
    }

    private Vector2 HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        // Use squared distance for performance
        Vector2 toTarget = wanderTarget - (Vector2)transform.position;
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
        if (enemyData == null || transform == null)
        {
            Debug.LogWarning($"EnemyData: {enemyData} or Transform: {transform} is null in {gameObject.name}");
            return;
        }

        Vector2 currentPos = (Vector2)transform.position;

        switch (wanderPattern)
        {
            case WanderPattern.Momentum:
                SetMomentumTarget(currentPos);
                break;

            case WanderPattern.Random:
            default:
                SetRandomTarget(currentPos);
                break;
        }

        wanderTimer = wanderCooldown;
    }

    private void SetMomentumTarget(Vector2 currentPos)
    {
        // Continue in roughly the same direction with slight adjustments
        if (currentWanderDirection == Vector2.zero)
        {
            currentWanderDirection = lastMovementDirection;
        }

        // Small chance to make a turn
        if (Random.value < directionChangeChance)
        {
            float turnAngle = Random.Range(-maxTurnAngle, maxTurnAngle) * Mathf.Deg2Rad;
            currentWanderDirection = new Vector2(
                currentWanderDirection.x * Mathf.Cos(turnAngle) - currentWanderDirection.y * Mathf.Sin(turnAngle),
                currentWanderDirection.x * Mathf.Sin(turnAngle) + currentWanderDirection.y * Mathf.Cos(turnAngle)
            );
        }

        // Set target ahead in current direction
        float distance = enemyData.wanderRadius * 0.6f;
        wanderTarget = currentPos + currentWanderDirection * distance;

        // Check if target would lead to collision
        RaycastHit2D hit = Physics2D.Raycast(currentPos, currentWanderDirection, distance, obstacleLayer);
        if (hit.collider != null)
        {
            // Find a new direction that avoids the obstacle
            Vector2 perpendicular = Vector2.Perpendicular(currentWanderDirection);
            if (Random.value > 0.5f) perpendicular = -perpendicular;
            currentWanderDirection = perpendicular;
            wanderTarget = currentPos + currentWanderDirection * distance;
        }

        lastMovementDirection = currentWanderDirection;
    }

    private void SetRandomTarget(Vector2 currentPos)
    {
        // Original random behavior but improved
        wanderDirectionChangeTimer -= Time.deltaTime;
        if (wanderDirectionChangeTimer <= 0f)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            currentWanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            wanderDirectionChangeTimer = wanderDirectionChangeCooldown;
        }

        float distance = Random.Range(enemyData.wanderRadius * 0.3f, enemyData.wanderRadius);
        wanderTarget = currentPos + currentWanderDirection * distance;

        // Check if target would lead to collision
        RaycastHit2D hit = Physics2D.Raycast(currentPos, currentWanderDirection, distance, obstacleLayer);
        if (hit.collider != null)
        {
            // Find a new random direction that avoids the obstacle
            for (int attempts = 0; attempts < 5; attempts++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 newDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                RaycastHit2D testHit = Physics2D.Raycast(currentPos, newDirection, distance, obstacleLayer);
                if (testHit.collider == null)
                {
                    currentWanderDirection = newDirection;
                    wanderTarget = currentPos + currentWanderDirection * distance;
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData != null && transform != null)
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

            // Draw wander radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, enemyData.wanderRadius);

            // Draw current wander target
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wanderTarget, 0.2f);

            // Draw avoidance radius
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, avoidanceDistance);

            // Draw current movement direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, lastMovementDirection * 2f);

            // Draw avoidance direction if active
            if (avoidanceDirection != Vector2.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, avoidanceDirection * 2f);
            }
        }
    }
}
