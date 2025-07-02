using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  #region Editor Data
  [Header("Movement Settings")]
  [SerializeField] private float baseSpeed = 5f;
  [SerializeField] private float sprintMultiplier = 1.5f;
  [SerializeField] private float dashForce = 10f;
  [SerializeField] private float dashDuration = 0.2f;
  [SerializeField] private float dashCooldown = 1f;

  [Header("Dependencies")]
  [SerializeField] private PlayerCore _playerCore = null;
  [SerializeField] private InputReader _inputReader = null;
  [SerializeField] private Rigidbody2D _rigidbody = null;
  #endregion

  #region Internal Data

  private Vector2 inputVector;
  private Vector2 currentVelocity;
  private Vector2 targetVelocity;
  private bool canMove = true;
  private bool isSprinting = false;
  private bool isDashing = false;
  private float dashTimer;
  private float dashCooldownTimer;
  #endregion

  // Movement state
  public bool IsMoving => currentVelocity.magnitude > 0.1f;
  public bool IsDashing => isDashing;
  public bool CanDash => dashCooldownTimer <= 0f && !isDashing;

  private void Awake()
  {
    if (_rigidbody == null)
    {
      _rigidbody = GetComponent<Rigidbody2D>();
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

  private void Update()
  {
    UpdateTimers();
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

  private void HandleMoveInput(Vector2 moveInput)
  {
    Debug.Log($"Move input received: {moveInput}");
    inputVector = moveInput;
  }

  private void HandleSprintStart()
  {
    Debug.Log("Sprint started");
    if (!CanDash || !canMove) return;

    isSprinting = true;
  }

  private void HandleSprintStop()
  {
    isSprinting = false;
  }

  private void HandleDash()
  {
    if (!CanDash || !canMove) return;

    Vector2 dashDirection = targetVelocity.normalized;
    if (dashDirection == Vector2.zero)
    {
      // Use last movement direction or forward if no input
      dashDirection = currentVelocity.normalized;
      if (dashDirection == Vector2.zero)
        dashDirection = Vector2.up; // Default dash direction
    }

    StartDash(dashDirection);
  }

  private void StartDash(Vector2 direction)
  {
    isDashing = true;
    dashTimer = dashDuration;
    dashCooldownTimer = dashCooldown;

    // Apply dash force
    _rigidbody.velocity = direction * dashForce;

    // Play dash effect
    if (_playerCore?.Effect != null)
    {
      _playerCore.Effect.PlayDashEffect();
    }
  }

  private void HandleNormalMovement()
  {
    // Calculate target velocity based on input
    float currentSpeed = baseSpeed;

    // Apply sprint multiplier if sprinting
    if (isSprinting)
    {
      currentSpeed *= sprintMultiplier;
    }

    // Apply size-based speed modifications (larger = slower)
    if (_playerCore != null)
    {
      float sizeSpeedModifier = Mathf.Lerp(1.2f, 0.8f, (_playerCore.currentSize - 1f) / 4f);
      currentSpeed *= sizeSpeedModifier;
    }

    targetVelocity = inputVector.normalized * currentSpeed;

    _rigidbody.velocity = Vector2.Lerp(_rigidbody.velocity, targetVelocity, Time.fixedDeltaTime * 10f);
  }

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

  private void UpdateTimers()
  {
    if (dashCooldownTimer > 0f)
    {
      dashCooldownTimer -= Time.deltaTime;
    }
  }

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
}
