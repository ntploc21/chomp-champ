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
    private float wanderCooldown = 2f;

    [Header("Detection")]
    private float playerDistance;
    private bool playerDetected = false;
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
    }

    private void Start()
    {
        // Find player - you might want to use a GameManager or PlayerManager for this
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
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

    private void UpdateAI()
    {
        if (player != null)
        {
            playerDistance = Vector2.Distance(visualTransform.position, player.position);
            playerDetected = playerDistance <= enemyData.detectionRange;
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
                if (player != null)
                {
                    targetDirection = (player.position - visualTransform.position).normalized;
                    moveSpeed *= enemyData.chaseSpeedMultiplier;
                }
                break;

            case EnemyAIState.Fleeing:
                if (player != null)
                {
                    targetDirection = (visualTransform.position - player.position).normalized;
                    moveSpeed *= enemyData.fleeSpeedMultiplier;
                }
                break;
        }

        // Apply movement
        if (targetDirection != Vector2.zero && rb != null)
        {
            Debug.Log($"Moving in direction: {targetDirection} with speed: {moveSpeed}");
            rb.velocity = targetDirection * moveSpeed;

            // Rotate to face movement direction (optional)
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            visualTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private Vector2 HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        // Check if we reached the wander target or timer expired
        if (wanderTimer <= 0f || Vector2.Distance(visualTransform.position, wanderTarget) < 0.5f)
        {
            SetNewWanderTarget();
        }

        return (wanderTarget - (Vector2)visualTransform.position).normalized;
    }

    private void SetNewWanderTarget()
    {
        // Set a random target within wander radius
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        wanderTarget = (Vector2)visualTransform.position + randomDirection * enemyData.wanderRadius;

        wanderTimer = wanderCooldown;

        Debug.Log($"New wander target set: {wanderTarget}");
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData != null)
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
