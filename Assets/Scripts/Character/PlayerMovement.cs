using UnityEngine;

/// <summary>
/// PlayerMovement handles player character movement, including walking, sprinting, and dashing.
/// It manages input, applies forces, and updates the player's position and rotation.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
  #region Editor Data
  [Header("Movement Settings")]
  [Tooltip("Base speed of the player character.")]
  [SerializeField] private float baseSpeed = 5f;
  [Tooltip("Speed multiplier when the player is sprinting.")]
  [SerializeField] private float sprintMultiplier = 1.5f;
  [SerializeField] private float acceleration = 20f;
  [SerializeField] private float deceleration = 10f;
  [Tooltip("Rotation speed of the player character.")]
  [SerializeField] private float rotationSpeed = 10f;

  [Header("Dash Settings")]
  [Tooltip("Force applied during the dash.")]
  [SerializeField] private float dashForce = 10f;
  [Tooltip("Duration of the dash in seconds.")]
  [SerializeField] private float dashDuration = 0.2f;
  [Tooltip("Cooldown time between dashes in seconds.")]
  [SerializeField] private float dashCooldown = 1f;
  [Tooltip("Time during which the player is invulnerable after dashing.")]
  [SerializeField] private float dashInvulnerabilityTime = 0.1f;

  [Header("Size-based Modifiers")]
  [SerializeField] private float minSizeSpeedMultiplier = 1.5f;
  [SerializeField] private float maxSizeSpeedMultiplier = 0.7f;
  [SerializeField] private float sizeReferenceMin = 0.5f;
  [SerializeField] private float sizeReferenceMax = 5f;

  [Header("Dependencies")]
  [SerializeField] private PlayerCore _playerCore = null;
  [SerializeField] private InputReader _inputReader = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;

  [Header("Debug")]
  [Tooltip("Enable debug logs for movement actions.")]
  [SerializeField] private bool enableDebugLogs = false;
  [Tooltip("Show movement gizmos in the editor for debugging.")]
  [SerializeField] private bool showMovementGizmos = false;
  #endregion

  #region Internal Data
  private Vector2 inputVector;
  private Vector2 currentVelocity;
  private Vector2 targetVelocity;
  private Vector2 lastMovementDirection = Vector2.right; // Default to right for dash direction

  private bool canMove = true;
  private bool isSprinting = false;
  private bool isDashing = false;
  private bool isDashInvulnerable = false;

  private float dashTimer = 0f;
  private float dashCooldownTimer = 0f;
  private float dashInvulnerabilityTimer = 0f;

  // Cached values for performance
  private float cachedSizeSpeedModifier = 1f;
  private float lastSizeCheck = 0f;
  private const float SIZE_CHECK_INTERVAL = 0.2f;
  #endregion

  #region Properties
  public bool IsMoving => currentVelocity.magnitude > 0.1f;
  public bool IsDashing => isDashing;
  public bool IsDashInvulnerable => isDashInvulnerable;
  public bool CanDash => dashCooldownTimer <= 0f && !isDashing && canMove;
  public bool IsSprinting => isSprinting && IsMoving;
  public Vector2 MovementDirection => lastMovementDirection;
  public float CurrentSpeed => currentVelocity.magnitude;
  public float MaxSpeed => CalculateCurrentMaxSpeed();
  #endregion

  #region Unity Events
  private void Awake()
  {
    if (_rigidbody == null)
    {
      _rigidbody = GetComponent<Rigidbody2D>();
    }
  }
  public void Initialize(PlayerCore core)
  {
    _playerCore = core;

    // Set up physics
    if (_rigidbody != null)
    {
      _rigidbody.gravityScale = 0f; // Top-down game
      _rigidbody.drag = 0f; // We'll handle deceleration manually
    }
  }

  private void OnEnable()
  {
    _inputReader.OnMoveEvent += HandleMoveInput;
    _inputReader.OnSprintStartedEvent += HandleSprintStart;
    _inputReader.OnSprintStoppedEvent += HandleSprintStop;
    _inputReader.OnDashEvent += HandleDash;
  }

  private void OnDisable()
  {
    _inputReader.OnMoveEvent -= HandleMoveInput;
    _inputReader.OnSprintStartedEvent -= HandleSprintStart;
    _inputReader.OnSprintStoppedEvent -= HandleSprintStop;
    _inputReader.OnDashEvent -= HandleDash;
  }
  #endregion

  #region Tick
  private void Update()
  {
    UpdateTimers();
    UpdateCachedValues();
  }

  private void FixedUpdate()
  {
    if (!canMove) return;

    if (isDashing)
    {
      HandleDashMovement();
    }
    else
    {
      HandleNormalMovement();
    }
  }
  #endregion

  #region Utility Methods
  /// <summary>
  /// Calculates the current maximum speed based on base speed, sprint multiplier, and size-based speed modifier.
  /// This method is called to determine the effective speed of the player character during movement.
  /// </summary>
  /// <returns></returns>
  private float CalculateCurrentMaxSpeed()
  {
    float speed = baseSpeed;
    if (isSprinting) speed *= sprintMultiplier;
    return speed * cachedSizeSpeedModifier;
  }

  private void UpdateCachedValues()
  {
    // Update size-based speed modifier periodically for performance
    if (Time.time - lastSizeCheck >= SIZE_CHECK_INTERVAL)
    {
      lastSizeCheck = Time.time;
      UpdateSizeSpeedModifier();
    }
  }

  private void UpdateSizeSpeedModifier()
  {
    if (_playerCore != null)
    {
      float normalizedSize = Mathf.InverseLerp(sizeReferenceMin, sizeReferenceMax, _playerCore.CurrentSize);
      cachedSizeSpeedModifier = Mathf.Lerp(minSizeSpeedMultiplier, maxSizeSpeedMultiplier, normalizedSize);
    }
  }

  /// <summary>
  /// Updates the timers for dash cooldown and invulnerability.
  /// This method is called every frame to manage the state of dashing and invulnerability.
  /// </summary>
  private void UpdateTimers()
  {
    if (dashCooldownTimer > 0f)
    {
      dashCooldownTimer -= Time.deltaTime;
    }

    if (dashInvulnerabilityTimer > 0f)
    {
      dashInvulnerabilityTimer -= Time.deltaTime;
      isDashInvulnerable = dashInvulnerabilityTimer > 0f;
    }
  }
  #endregion

  #region Input Handlers
  private void HandleMoveInput(Vector2 moveInput)
  {
    inputVector = moveInput;

    // Store last movement direction for dash
    if (moveInput.magnitude > 0.1f)
    {
      lastMovementDirection = moveInput.normalized;
    }
  }

  private void HandleSprintStart()
  {
    if (!canMove) return;

    isSprinting = true;

    if (enableDebugLogs)
    {
      Debug.Log("Sprint started");
    }
  }

  private void HandleSprintStop()
  {
    isSprinting = false;

    if (enableDebugLogs)
    {
      Debug.Log("Sprint stopped");
    }
  }

  private void HandleDash()
  {
    if (!CanDash || !canMove) return;

    Vector2 dashDirection = inputVector.magnitude > 0.1f ? inputVector.normalized : lastMovementDirection;

    // Fallback to forward if no movement history
    if (dashDirection == Vector2.zero)
    {
      dashDirection = transform.up; // Or any default direction
    }

    StartDash(dashDirection);
  }
  private void StartDash(Vector2 direction)
  {
    isDashing = true;
    dashTimer = dashDuration;
    dashCooldownTimer = dashCooldown;

    // Set dash invulnerability
    isDashInvulnerable = true;
    dashInvulnerabilityTimer = dashInvulnerabilityTime;

    // Apply dash force with size consideration
    float dashPower = dashForce * (1f / Mathf.Sqrt(cachedSizeSpeedModifier)); // Larger = less dash power
    _rigidbody.velocity = direction * dashPower;

    // Play dash effect
    if (_playerCore?.Effect != null)
    {
      _playerCore.Effect.PlayDashEffect();
    }

    // Notify player core of dash invulnerability
    if (_playerCore != null)
    {
      _playerCore.SetInvincible(dashInvulnerabilityTime);
    }

    if (enableDebugLogs)
    {
      Debug.Log($"Dash started! Direction: {direction}, Power: {dashPower:F1}");
    }
  }
  #endregion

  #region Movement Handlers
  /// <summary>
  /// Handles normal movement logic, applying input-based velocity and smoothing acceleration/deceleration.
  /// This method is called when the player is not dashing and can move normally.
  /// </summary>
  private void HandleNormalMovement()
  {
    // Calculate target velocity based on input
    float currentSpeed = baseSpeed;

    // Apply sprint multiplier if sprinting
    if (isSprinting)
    {
      currentSpeed *= sprintMultiplier;
    }

    // Apply cached size-based speed modifications
    currentSpeed *= cachedSizeSpeedModifier;
    targetVelocity = inputVector.normalized * currentSpeed;

    // Smooth acceleration/deceleration
    float lerpSpeed = inputVector.magnitude > 0.1f ? acceleration : deceleration;
    currentVelocity = Vector2.Lerp(_rigidbody.velocity, targetVelocity, lerpSpeed * Time.fixedDeltaTime); _rigidbody.velocity = currentVelocity;

    // Update player animation with current movement
    if (_playerCore != null)
    {
      _playerCore.SetMovementAnimation(currentVelocity.normalized);
    }

    // Optional: Rotate the player towards the movement direction
    if (false && rotationSpeed > 0f && currentVelocity.magnitude > 0.1f)
    {
      float targetAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg - 90f;
      float currentAngle = transform.eulerAngles.z;
      float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.fixedDeltaTime);
      transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    // // flip sprite to left or right based on movement direction
    // if (currentVelocity.x < 0)
    // {
    //   transform.localScale = new Vector3(
    //     transform.localScale.x * (transform.localScale.x < 0 ? 1 : -1),
    //     transform.localScale.y,
    //     transform.localScale.z);
    //   //transform.localScale = new Vector3(-1, 1, 1); // Flip to left
    // }
    // else if (currentVelocity.x > 0)
    // {
    //   transform.localScale = new Vector3(
    //     transform.localScale.x * (transform.localScale.x < 0 ? -1 : 1),
    //     transform.localScale.y,
    //     transform.localScale.z);
    //   // transform.localScale = new Vector3(1, 1, 1); // Flip to right
    // }
  }

  /// <summary>
  /// Handles the dash movement logic, applying a burst of speed in the specified direction.
  /// Smoothly transitions back to normal movement after the dash duration.
  /// </summary>
  private void HandleDashMovement()
  {
    dashTimer -= Time.fixedDeltaTime;

    if (dashTimer <= 0f)
    {
      isDashing = false;
      // Smoothly transition back to normal movement
      currentVelocity = _rigidbody.velocity * 0.5f; // Reduce velocity after dash
    }
  }
  #endregion


  #region Public Methods
  public void SetCanMove(bool canMove)
  {
    this.canMove = canMove;

    if (!canMove)
    {
      _rigidbody.velocity = Vector2.zero;
      currentVelocity = Vector2.zero;
      targetVelocity = Vector2.zero;
    }
  }

  public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
  {
    if (_rigidbody != null)
    {
      _rigidbody.AddForce(force, mode);
    }
  }

  public void SetVelocity(Vector2 velocity)
  {
    if (_rigidbody != null)
    {
      _rigidbody.velocity = velocity;
      currentVelocity = velocity;
    }
  }
  public Vector2 GetVelocity()
  {
    return _rigidbody != null ? _rigidbody.velocity : Vector2.zero;
  }

  public void Knockback(Vector2 force)
  {
    if (_rigidbody != null && canMove)
    {
      _rigidbody.AddForce(force, ForceMode2D.Impulse);

      if (enableDebugLogs)
      {
        Debug.Log($"Knockback applied: {force}");
      }
    }
  }

  public void Stop()
  {
    if (_rigidbody != null)
    {
      _rigidbody.velocity = Vector2.zero;
      currentVelocity = Vector2.zero;
      targetVelocity = Vector2.zero;
    }
  }

  public void SetPosition(Vector2 position)
  {
    transform.position = position;
    if (_rigidbody != null)
    {
      _rigidbody.position = position;
    }
  }

  public void SetCanCollide(bool canCollide)
  {
    if (_rigidbody != null)
    {
      _rigidbody.simulated = canCollide;
    }
  }
  #endregion

  #region Debugging
  private void OnDrawGizmos()
  {
    if (!showMovementGizmos) return;

    // Draw movement direction
    if (lastMovementDirection != Vector2.zero)
    {
      Gizmos.color = Color.blue;
      Gizmos.DrawLine(transform.position, transform.position + (Vector3)lastMovementDirection * 2f);
    }

    // Draw velocity
    if (_rigidbody != null && _rigidbody.velocity.magnitude > 0.1f)
    {
      Gizmos.color = Color.green;
      Gizmos.DrawLine(transform.position, transform.position + (Vector3)_rigidbody.velocity);
    }

    // Draw dash cooldown indicator
    if (dashCooldownTimer > 0f)
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, 1f);
    }
    else if (CanDash)
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
  }
  #endregion
}
