using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
  #region Editor Data
  [Header("Components")]
  [SerializeField] private EnemyCore enemyCore = null;
  [SerializeField] private Rigidbody2D rb = null;
  [Header("Movement Settings")]
  [SerializeField] private float wanderCooldown = 2f;
  [SerializeField] private float movementSmoothing = 0.5f;
  [SerializeField] private float noiseIntensity = 0.5f;
  [SerializeField] private float noiseFrequency = 1f;
  [SerializeField] private float speedVariation = 0.2f;

  [Header("Debugs")]
  [SerializeField] private bool showMovementGizmos = false;
  #endregion

  #region Internal Data
  private Vector2 currentTarget = Vector2.zero;
  private Vector2 wanderTarget = Vector2.zero;
  private bool isMoving = false;
  private float wanderTimer = 0f;
  private float currentSpeed = 0f;

  // Realistic movement variables
  private Vector2 velocity = Vector2.zero;
  private Vector2 desiredVelocity = Vector2.zero;
  private float noiseTime = 0f;
  private float speedMultiplierVariation = 1f;
  private float speedVariationTimer = 0f;
  #endregion

  #region Properties
  public Vector2 Position => transform.position;
  public Vector2 CurrentTarget => currentTarget;
  public bool IsMoving => isMoving;
  #endregion

  #region Unity Events
  private void Awake()
  {
    if (enemyCore == null)
    {
      enemyCore = GetComponent<EnemyCore>();
    }
    if (rb == null)
    {
      rb = GetComponent<Rigidbody2D>();
    }
  }

  private void FixedUpdate()
  {
    if (isMoving)
    {
      MoveTowardsTarget();
    }
  }
  #endregion

  #region Public Methods
  /// <summary>
  /// Initialize the movement component with the enemy core
  /// </summary>
  public void Initialize(EnemyCore core)
  {
    enemyCore = core;
    rb = core.Rigidbody;
  }

  /// <summary>
  /// Move towards the current target position
  /// </summary>
  /// <param name="newTarget">The new target position</param>
  public void SetTarget(Vector2 newTarget)
  {
    currentTarget = newTarget;
    isMoving = true;
  }

  /// <summary>
  /// Set movement speed based on enemy data and optional multiplier
  /// </summary>
  public void SetSpeed(float speedMultiplier = 1f)
  {
    if (enemyCore?.Data == null) return;

    float baseSpeed = enemyCore.Data.baseSpeed;
    float newSpeed = baseSpeed * speedMultiplier;

    // Ensure speed is positive
    if (newSpeed < 0f) newSpeed = 0f;

    // Update current speed and Rigidbody velocity
    rb.velocity = rb.velocity.normalized * newSpeed;
    UpdateCurrentSpeed(newSpeed);
  }

  /// <summary>
  /// Update the current speed of the enemy
  /// </summary>
  /// <param name="newSpeed">The new speed to set</param>
  public void UpdateCurrentSpeed(float newSpeed)
  {
    currentSpeed = newSpeed;
    if (currentSpeed <= 0f)
    {
      isMoving = false; // Stop moving if speed is zero or negative
    }
  }

  /// <summary>
  /// Stop the enemy movement and reset target
  /// </summary>
  public void StopMovement()
  {
    isMoving = false;
    rb.velocity = Vector2.zero; // Stop the Rigidbody movement
  }

  /// <summary>
  /// Move in a specific direction with a speed multiplier
  /// </summary>
  /// <param name="direction">Direction to move in</param>
  /// <param name="speedMultiplier">Multiplier for the base speed</param>
  public void MoveInDirection(Vector2 direction, float speedMultiplier = 1f)
  {
    if (enemyCore?.Data != null)
    {
      float speed = enemyCore.Data.baseSpeed * speedMultiplier;
      rb.velocity = direction.normalized * speed;
    }
  }
  /// <summary>
  /// Generate a random wander target within the enemy's wander radius.
  /// This is called periodically based on the wander cooldown.
  /// </summary>
  /// <returns></returns>
  public Vector2 GenerateWanderTarget()
  {
    wanderTimer -= Time.deltaTime;

    if (wanderTimer <= 0f)
    {
      wanderTimer = wanderCooldown;

      if (enemyCore?.Data != null)
      {
        // Generate a random point within wander radius
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float wanderDistance = Random.Range(0.5f, enemyCore.Data.wanderRadius);
        wanderTarget = (Vector2)transform.position + randomDirection * wanderDistance;
      }
    }

    return wanderTarget;
  }

  /// <summary>
  /// Generate a new wander target immediately (for smooth transitions)
  /// </summary>
  public Vector2 GenerateNewWanderTarget()
  {
    if (enemyCore?.Data != null)
    {
      Vector2 randomDirection = Random.insideUnitCircle.normalized;
      float wanderDistance = Random.Range(0.5f, enemyCore.Data.wanderRadius);
      wanderTarget = (Vector2)transform.position + randomDirection * wanderDistance;
      wanderTimer = wanderCooldown; // Reset timer
    }

    return wanderTarget;
  }

  /// <summary>
  /// Get distance to current target
  /// </summary>
  public float GetDistanceToTarget()
  {
    return Vector2.Distance(transform.position, currentTarget);
  }

  /// <summary>
  /// Get distance to a specific position
  /// </summary>
  public float GetDistanceTo(Vector2 position)
  {
    return Vector2.Distance(transform.position, position);
  }

  /// <summary>
  /// Check if we've reached the current target (within a small threshold)
  /// </summary>
  public bool HasReachedTarget(float threshold = 0.1f)
  {
    return GetDistanceToTarget() <= threshold;
  }
  #endregion

  #region Private Methods
  private void MoveTowardsTarget()
  {
    if (rb == null || enemyCore?.Data == null) return;

    // Calculate base direction to target
    Vector2 directionToTarget = (currentTarget - (Vector2)transform.position).normalized;

    // Add movement noise for realistic movement
    Vector2 noiseOffset = GenerateMovementNoise();
    Vector2 combinedDirection = (directionToTarget + noiseOffset).normalized;

    // Apply obstacle avoidance
    Vector2 finalDirection = CalculateObstacleAvoidance(combinedDirection);

    // Use current speed or fall back to base speed with variation
    float baseSpeed = currentSpeed > 0 ? currentSpeed : enemyCore.Data.baseSpeed;
    float finalSpeed = baseSpeed * GetSpeedVariation();

    // Calculate desired velocity
    desiredVelocity = finalDirection * finalSpeed;

    // Apply smooth movement using interpolation
    velocity = Vector2.Lerp(velocity, desiredVelocity, movementSmoothing * Time.fixedDeltaTime * 10f);
    rb.velocity = velocity;

    // Update rotation to face movement direction
    UpdateRotation();

    // Check if we should generate a new wander target before stopping
    if (HasReachedTarget(1f)) // Larger threshold for smoother transitions
    {
      HandleTargetReached();
    }
  }

  private Vector2 GenerateMovementNoise()
  {
    noiseTime += Time.fixedDeltaTime * noiseFrequency;

    // Use Perlin noise for smooth, natural-looking movement variation
    float noiseX = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f * noiseIntensity;
    float noiseY = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f * noiseIntensity;

    return new Vector2(noiseX, noiseY);
  }

  private float GetSpeedVariation()
  {
    speedVariationTimer += Time.fixedDeltaTime;

    // Update speed variation periodically for natural speed changes
    if (speedVariationTimer >= 1f)
    {
      speedVariationTimer = 0f;
      speedMultiplierVariation = 1f + Random.Range(-speedVariation, speedVariation);
    }

    return speedMultiplierVariation;
  }

  private void HandleTargetReached()
  {
    // Don't stop completely - instead prepare for next target
    if (wanderTimer <= 0f)
    {
      Debug.Log("Generating new wander target");

      // Generate new wander target immediately for smooth movement
      GenerateWanderTarget();
      SetTarget(wanderTarget);
    }
  }

  /// <summary>
  /// Apply smooth rotation towards movement direction for more natural looking movement
  /// </summary>
  private void UpdateRotation()
  {
    if (velocity.magnitude > 0.1f)
    {
      float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
      float targetAngle = angle - 90f; // Adjust for sprite orientation

      // Smooth rotation
      float currentAngle = transform.eulerAngles.z;
      float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.fixedDeltaTime * 3f);
      transform.rotation = Quaternion.Euler(0, 0, smoothAngle);
    }
  }

  /// <summary>
  /// Add obstacle avoidance for more realistic pathfinding
  /// </summary>
  private Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
  {
    // Cast rays to detect obstacles
    float avoidanceRadius = 1f;
    Vector2 avoidanceForce = Vector2.zero;

    // Check multiple directions around the enemy
    for (int i = 0; i < 8; i++)
    {
      float angle = i * 45f * Mathf.Deg2Rad;
      Vector2 rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

      RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, avoidanceRadius);
      if (hit.collider != null && hit.collider.gameObject != gameObject)
      {
        // Add force away from obstacle
        Vector2 avoidDirection = ((Vector2)transform.position - hit.point).normalized;
        float distance = hit.distance;
        float force = (avoidanceRadius - distance) / avoidanceRadius;
        avoidanceForce += avoidDirection * force;
      }
    }

    return (desiredDirection + avoidanceForce * 0.5f).normalized;
  }
  #endregion

  #region Debug
  private void OnDrawGizmos()
  {
    if (!showMovementGizmos) return;

    // Draw current target
    if (isMoving)
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(currentTarget, 0.2f);

      // Draw line to target
      Gizmos.color = Color.yellow;
      Gizmos.DrawLine(transform.position, currentTarget);
    }

    // Draw wander target
    Gizmos.color = Color.blue;
    Gizmos.DrawWireSphere(wanderTarget, 0.1f);

    // Draw wander radius
    if (enemyCore?.Data != null)
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, enemyCore.Data.wanderRadius);
    }
  }
  #endregion
}