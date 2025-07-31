using UnityEngine;

/// <summary>
/// AI State definitions for enemy behavior
/// </summary>
public enum AIState
{
    Idle,
    Wandering,
    Chasing,
    Fleeing,
    Investigating
}

/// <summary>
/// Enemy behavior patterns for wandering
/// </summary>
public enum BehaviorPattern
{
    Random,
    Circular,
    Linear,
    Organic
}

/// <summary>
/// New EnemyBehaviour component designed from scratch.
/// This version delegates all movement to EnemyMovement and focuses purely on AI decision making.
/// Clean separation of concerns: AI logic here, movement execution in EnemyMovement.
/// </summary>
public class EnemyBehaviour : MonoBehaviour
{
    #region Core Components
    [Header("Core Dependencies")]
    [SerializeField] private EnemyCore core;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private Transform playerTransform;
    #endregion

    #region AI Configuration
    [Header("AI Settings")]
    [SerializeField] private AIState currentState = AIState.Wandering;
    [SerializeField] private BehaviorPattern wanderPattern = BehaviorPattern.Organic;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 5f;
    [SerializeField] private float hearingRange = 3f;
    [SerializeField] private LayerMask playerLayer = 1 << 7;

    [Header("Behavior Timing")]
    [SerializeField] private float stateTransitionCooldown = 0.5f;
    [SerializeField] private float decisionUpdateRate = 0.2f;
    [SerializeField] private float wanderDirectionChangeInterval = 3f;
    #endregion

    #region State Management
    private AIState previousState;
    private float lastStateChangeTime;
    private float lastDecisionTime;
    private float lastWanderDirectionChange;

    // Cached data for performance
    private Vector2 lastKnownPlayerPosition;
    private float distanceToPlayer;
    private bool playerInSight;
    private bool playerInHearing;
    #endregion

    #region Properties
    public AIState CurrentState => currentState;
    public bool IsActive { get; private set; } = true;
    public Vector2 LastKnownPlayerPosition => lastKnownPlayerPosition;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeBehaviour();
    }

    private void Update()
    {
        if (!IsActive || !core.isActive) return;

        UpdateSensors();
        ProcessAI();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize the behavior system with all required components
    /// </summary>
    public void Initialize(EnemyCore enemyCore)
    {
        core = enemyCore;
        movement = GetComponent<EnemyMovement>();

        if (movement == null)
        {
            Debug.LogError($"EnemyMovement component not found on {gameObject.name}");
            return;
        }

        movement.Initialize(core);
        InitializeBehaviour();
    }

    private void InitializeBehaviour()
    {
        // Cache player reference
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // Set initial state
        TransitionToState(AIState.Wandering);

        // Start wandering immediately
        if (movement != null)
        {
            BeginWandering();
        }
    }
    #endregion

    #region Sensor System
    /// <summary>
    /// Update all sensory information about the environment
    /// </summary>
    private void UpdateSensors()
    {
        if (playerTransform == null) return;

        // Calculate distance and direction to player
        Vector2 toPlayer = playerTransform.position - transform.position;
        distanceToPlayer = toPlayer.magnitude;        // Vision check - requires line of sight
        playerInSight = CanSeePlayer(toPlayer);

        // Hearing check - simpler distance check
        playerInHearing = distanceToPlayer <= hearingRange;

        // Update last known position if player is detected
        if (playerInSight || playerInHearing)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }
    }

    /// <summary>
    /// Check if the enemy can see the player (line of sight)
    /// </summary>
    private bool CanSeePlayer(Vector2 directionToPlayer)
    {
        if (distanceToPlayer > visionRange) return false;

        // Raycast to check for obstacles
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer.normalized,
            distanceToPlayer,
            ~playerLayer // Exclude player layer from obstacles
        );

        // If we hit something before reaching the player, vision is blocked
        return hit.collider == null;
    }
    #endregion

    #region AI Decision Making
    /// <summary>
    /// Main AI processing loop - evaluates conditions and makes decisions
    /// </summary>
    private void ProcessAI()
    {
        // Limit decision frequency for performance
        if (Time.time - lastDecisionTime < decisionUpdateRate) return;
        lastDecisionTime = Time.time;

        // Evaluate state transitions based on enemy type and conditions
        AIState newState = EvaluateDesiredState();

        if (newState != currentState && CanTransitionToState(newState))
        {
            TransitionToState(newState);
        }

        // Execute current state behavior
        ExecuteCurrentState();
    }

    /// <summary>
    /// Determine what state the enemy should be in based on current conditions
    /// </summary>
    private AIState EvaluateDesiredState()
    {
        if (core.Data == null) return AIState.Idle;

        // Check for player detection first
        if (playerInSight || playerInHearing)
        {
            AIState playerResponseState = DeterminePlayerResponseState();
            return playerResponseState;
        }

        // No player detected - default behaviors
        switch (currentState)
        {
            case AIState.Chasing:
            case AIState.Fleeing:
                // Lost the player - investigate last known position
                return AIState.Investigating;

            case AIState.Investigating:
                // After investigating, return to wandering
                if (Vector2.Distance(transform.position, lastKnownPlayerPosition) < 1f)
                {
                    return AIState.Wandering;
                }
                break;
        }

        return AIState.Wandering;
    }

    /// <summary>
    /// Determine how to respond to detected player based on enemy type and level
    /// </summary>
    private AIState DeterminePlayerResponseState()
    {
        if (playerTransform == null) return currentState;

        PlayerCore playerCore = playerTransform.GetComponent<PlayerCore>();
        if (playerCore == null) return currentState;

        // Check player status - don't chase if dead or invincible
        if (!playerCore.IsAlive || playerCore.IsInvincible)
        {
            return AIState.Wandering;
        }

        // Behavior based on enemy type and relative size
        switch (core.Data.behaviorType)
        {
            case EnemyType.Predator:
                // Chase if we're bigger or same size
                return (core.CurrentLevel > playerCore.CurrentLevel) ? AIState.Chasing : AIState.Fleeing;

            case EnemyType.Prey:
                // Always flee from player
                return AIState.Fleeing;

            case EnemyType.Neutral:
            default:
                // Neutral enemies continue wandering unless threatened
                return (core.CurrentLevel < playerCore.CurrentLevel) ? AIState.Fleeing : AIState.Wandering;
        }
    }

    /// <summary>
    /// Check if state transition is allowed based on cooldowns
    /// </summary>
    private bool CanTransitionToState(AIState newState)
    {
        return Time.time - lastStateChangeTime >= stateTransitionCooldown;
    }

    /// <summary>
    /// Execute the transition to a new state
    /// </summary>
    private void TransitionToState(AIState newState)
    {
        previousState = currentState;
        currentState = newState;
        lastStateChangeTime = Time.time;

        // State entry actions
        OnStateEnter(newState);
    }

    /// <summary>
    /// Handle actions when entering a new state
    /// </summary>
    private void OnStateEnter(AIState state)
    {
        switch (state)
        {
            case AIState.Wandering:
                BeginWandering();
                break;

            case AIState.Chasing:
                BeginChasing();
                break;

            case AIState.Fleeing:
                BeginFleeing();
                break;

            case AIState.Investigating:
                BeginInvestigating();
                break;

            case AIState.Idle:
                movement?.StopMovement();
                break;
        }
    }
    #endregion

    #region State Execution
    /// <summary>
    /// Execute behavior for the current state
    /// </summary>
    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Wandering:
                UpdateWandering();
                break;

            case AIState.Chasing:
                UpdateChasing();
                break;

            case AIState.Fleeing:
                UpdateFleeing();
                break;

            case AIState.Investigating:
                UpdateInvestigating();
                break;
        }
    }

    private void BeginWandering()
    {
        if (movement == null) return;

        Vector2 wanderTarget = GenerateWanderTarget();
        movement.SetTarget(wanderTarget);
        movement.SetSpeed(1f); // Normal speed
    }
    private void UpdateWandering()
    {
        if (movement == null) return;

        // Check if we need a new wander target
        if (Time.time - lastWanderDirectionChange >= wanderDirectionChangeInterval ||
            movement.HasReachedTarget(0.5f))
        {
            Vector2 newTarget = GenerateWanderTarget();
            movement.SetTarget(newTarget);
            lastWanderDirectionChange = Time.time;
        }

        // Update animation with current movement
        if (core != null && core.Rigidbody != null)
        {
            Vector2 currentVelocity = core.Rigidbody.velocity;
            if (currentVelocity.magnitude > 0.1f)
            {
                core.SetMovement(currentVelocity.normalized);
            }
        }
    }

    private void BeginChasing()
    {
        if (movement == null || playerTransform == null) return;

        movement.SetTarget(playerTransform.position);
        movement.SetSpeed(core.Data.chaseSpeedMultiplier);
    }
    private void UpdateChasing()
    {
        if (movement == null || playerTransform == null) return;

        // Continuously update target to player position
        movement.SetTarget(playerTransform.position);

        // Update animation with current movement
        if (core != null && core.Rigidbody != null)
        {
            Vector2 currentVelocity = core.Rigidbody.velocity;
            if (currentVelocity.magnitude > 0.1f)
            {
                core.SetMovement(currentVelocity.normalized);
            }
        }
    }

    private void BeginFleeing()
    {
        if (movement == null || playerTransform == null) return;

        Vector2 fleeDirection = GetFleeDirection();
        Vector2 fleeTarget = (Vector2)transform.position + fleeDirection * core.Data.wanderRadius;

        movement.SetTarget(fleeTarget);
        movement.SetSpeed(core.Data.fleeSpeedMultiplier);
    }
    private void UpdateFleeing()
    {
        if (movement == null || playerTransform == null) return;

        // Keep fleeing away from player
        Vector2 fleeDirection = GetFleeDirection();
        Vector2 fleeTarget = (Vector2)transform.position + fleeDirection * core.Data.wanderRadius;

        movement.SetTarget(fleeTarget);

        // Update animation with current movement
        if (core != null && core.Rigidbody != null)
        {
            Vector2 currentVelocity = core.Rigidbody.velocity;
            if (currentVelocity.magnitude > 0.1f)
            {
                core.SetMovement(currentVelocity.normalized);
            }
        }
    }

    private void BeginInvestigating()
    {
        if (movement == null) return;

        movement.SetTarget(lastKnownPlayerPosition);
        movement.SetSpeed(0.8f); // Slower, cautious movement
    }

    private void UpdateInvestigating()
    {
        if (movement == null) return;

        // Update animation with current movement
        if (core != null && core.Rigidbody != null)
        {
            Vector2 currentVelocity = core.Rigidbody.velocity;
            if (currentVelocity.magnitude > 0.1f)
            {
                core.SetMovement(currentVelocity.normalized);
            }
        }

        // Investigation continues until we reach the target or detect player again
        // State transition logic will handle moving to other states
    }
    #endregion

    #region Movement Helpers
    /// <summary>
    /// Generate a new wander target based on the current behavior pattern
    /// </summary>
    private Vector2 GenerateWanderTarget()
    {
        Vector2 currentPos = transform.position;

        switch (wanderPattern)
        {
            case BehaviorPattern.Circular:
                return GenerateCircularWanderTarget(currentPos);

            case BehaviorPattern.Linear:
                return GenerateLinearWanderTarget(currentPos);

            case BehaviorPattern.Organic:
                return GenerateOrganicWanderTarget(currentPos);

            case BehaviorPattern.Random:
            default:
                return GenerateRandomWanderTarget(currentPos);
        }
    }

    private Vector2 GenerateRandomWanderTarget(Vector2 currentPos)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(1f, core.Data.wanderRadius);

        return currentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private Vector2 GenerateCircularWanderTarget(Vector2 currentPos)
    {
        // Move in a rough circular pattern
        float time = Time.time * 0.5f;
        float angle = time * Mathf.PI * 2f;
        float radius = core.Data.wanderRadius * 0.7f;

        return currentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private Vector2 GenerateLinearWanderTarget(Vector2 currentPos)
    {
        // Move back and forth in a line
        Vector2 direction = (Time.time % 10f < 5f) ? Vector2.right : Vector2.left;
        return currentPos + direction * core.Data.wanderRadius;
    }

    private Vector2 GenerateOrganicWanderTarget(Vector2 currentPos)
    {
        // Use Perlin noise for smooth, organic movement
        float time = Time.time * 0.3f;
        float noiseX = Mathf.PerlinNoise(time, 0f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0f, time) - 0.5f;

        Vector2 direction = new Vector2(noiseX, noiseY).normalized;
        float distance = Random.Range(core.Data.wanderRadius * 0.5f, core.Data.wanderRadius);

        return currentPos + direction * distance;
    }

    private Vector2 GetFleeDirection()
    {
        if (playerTransform == null) return Random.insideUnitCircle.normalized;

        // Flee directly away from player
        Vector2 awayFromPlayer = (transform.position - playerTransform.position).normalized;

        // Add some randomness to avoid predictable movement
        float randomAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(randomAngle);
        float sin = Mathf.Sin(randomAngle);

        return new Vector2(
            awayFromPlayer.x * cos - awayFromPlayer.y * sin,
            awayFromPlayer.x * sin + awayFromPlayer.y * cos
        );
    }
    #endregion

    #region Public Interface
    /// <summary>
    /// Force the enemy to enter a specific state
    /// </summary>
    public void ForceState(AIState state)
    {
        TransitionToState(state);
    }

    /// <summary>
    /// Activate or deactivate the AI behavior
    /// </summary>
    public void SetActive(bool active)
    {
        IsActive = active;
        if (!active && movement != null)
        {
            movement.StopMovement();
        }
    }

    /// <summary>
    /// Get information about the current AI state
    /// </summary>
    public string GetStateInfo()
    {
        return $"State: {currentState}, Player Detected: {playerInSight || playerInHearing}, Distance: {distanceToPlayer:F1}";
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        if (core?.Data == null) return;

        // Draw vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Draw hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Draw wander radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, core.Data.wanderRadius);

        // Draw line to player if detected
        if (playerTransform != null && (playerInSight || playerInHearing))
        {
            Gizmos.color = playerInSight ? Color.red : Color.blue;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        // Draw last known player position
        if (lastKnownPlayerPosition != Vector2.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.3f);
        }

        // Draw current target
        if (movement != null && movement.IsMoving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(movement.CurrentTarget, 0.2f);
            Gizmos.DrawLine(transform.position, movement.CurrentTarget);
        }
    }
    #endregion
}
