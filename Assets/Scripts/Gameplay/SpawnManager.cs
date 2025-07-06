using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages enemy spawning with object pooling, difficulty scaling, and spawn patterns
/// Coordinates with GameState for spawn control and PlayerCore for difficulty scaling
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private bool isSpawning = false;
    [SerializeField] private float baseSpawnRate = 2f; // Seconds between spawns
    [SerializeField] private float spawnRateVariation = 0.5f; // Random variation
    [SerializeField] private int maxEnemiesOnScreen = 50;
    [SerializeField] private float spawnDistance = 15f; // Distance from player to spawn

    [Header("Enemy Data")]
    [SerializeField] private EnemyData[] enemyTypes;
    [SerializeField] private GameObject enemyPrefab; // Base enemy prefab with EnemyCore

    [Header("Spawn Areas")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BoxCollider2D spawnBounds; // Alternative to spawn points
    [SerializeField] private bool useSpawnBounds = true;

    [Header("Difficulty Scaling")]
    [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 1, 10, 3);
    [SerializeField] private AnimationCurve enemyLevelCurve = AnimationCurve.Linear(0, 1, 10, 5);
    [SerializeField] private float difficultyScaleTime = 60f; // Time to reach max difficulty

    [Header("Wave Spawning")]
    [SerializeField] private bool enableWaveSpawning = true;
    [SerializeField] private float waveChance = 0.2f; // 20% chance for wave spawn
    [SerializeField] private int minWaveSize = 3;
    [SerializeField] private int maxWaveSize = 8;
    [SerializeField] private float waveSpawnRadius = 3f;

    [Header("Player-Based Spawning")]
    [SerializeField] private bool scaleWithPlayerLevel = false; // Disabled - use enemy's natural level
    [SerializeField] private float playerLevelInfluence = 0f; // No influence
    [SerializeField] private bool avoidPlayerSpawning = true;
    [SerializeField] private float playerAvoidRadius = 8f;

    [Header("Object Pooling")]
    [SerializeField] private int poolInitialSize = 20;
    [SerializeField] private int poolMaxSize = 100;
    [SerializeField] private bool collectionCheck = false;

    [Header("References")]
    [SerializeField] private PlayerCore playerCore;
    [SerializeField] private GameState gameState;
    [SerializeField] private Camera gameCamera;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showSpawnGizmos = false;

    // Private variables
    private ObjectPool<GameObject> enemyPool;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private float lastSpawnTime;
    private float currentDifficulty = 0f;

    // Properties
    public bool IsSpawning => isSpawning;
    public int ActiveEnemyCount => activeEnemies.Count;
    public int PooledEnemyCount => enemyPool?.CountInactive ?? 0;
    public float CurrentDifficulty => currentDifficulty;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeReferences();
        InitializeObjectPool();
    }

    private void Start()
    {
        InitializeSpawning();
        SubscribeToEvents();
    }

    private void Update()
    {
        if (isSpawning)
        {
            UpdateDifficulty();
            CleanupInactiveEnemies();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupObjectPool();
    }
    #endregion

    #region Initialization
    private void InitializeReferences()
    {
        if (playerCore == null)
            playerCore = FindObjectOfType<PlayerCore>();

        if (gameState == null)
            gameState = FindObjectOfType<GameState>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        // Initialize spawn bounds if not set
        if (spawnBounds == null && useSpawnBounds)
        {
            spawnBounds = GetComponent<BoxCollider2D>();
        }
    }

    private void InitializeObjectPool()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("SpawnManager: Enemy prefab not assigned!");
            return;
        }

        enemyPool = new ObjectPool<GameObject>(
            createFunc: CreatePooledEnemy,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyPooledEnemy,
            collectionCheck: collectionCheck,
            defaultCapacity: poolInitialSize,
            maxSize: poolMaxSize
        );

        // Pre-populate pool
        var prePopulate = new List<GameObject>();
        for (int i = 0; i < poolInitialSize; i++)
        {
            prePopulate.Add(enemyPool.Get());
        }

        foreach (var enemy in prePopulate)
        {
            enemyPool.Release(enemy);
        }

        if (enableDebugLogs)
            Debug.Log($"SpawnManager: Object pool initialized with {poolInitialSize} enemies");
    }

    private void InitializeSpawning()
    {
        lastSpawnTime = Time.time;
        currentDifficulty = 0f;

        // Validate enemy types
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError("SpawnManager: No enemy types assigned!");
            enabled = false;
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"SpawnManager: Initialized with {enemyTypes.Length} enemy types");
    }

    private void SubscribeToEvents()
    {
        if (gameState != null)
        {
            gameState.OnStateChanged.AddListener(OnGameStateChanged);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (gameState != null)
        {
            gameState.OnStateChanged.RemoveListener(OnGameStateChanged);
        }
    }
    #endregion

    #region Object Pool Management
    private GameObject CreatePooledEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab);
        enemy.SetActive(false);
        return enemy;
    }

    private void OnGetFromPool(GameObject enemy)
    {
        enemy.SetActive(true);
        activeEnemies.Add(enemy);

        // Ensure EnemyCore is properly activated
        EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
        if (enemyCore != null)
        {
            enemyCore.isActive = true;
        }
    }

    private void OnReturnToPool(GameObject enemy)
    {
        enemy.SetActive(false);
        activeEnemies.Remove(enemy);

        // Reset enemy state
        EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
        if (enemyCore != null)
        {
            enemyCore.isActive = false;
        }
    }

    private void OnDestroyPooledEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            activeEnemies.Remove(enemy);
            Destroy(enemy);
        }
    }

    private void CleanupObjectPool()
    {
        if (enemyPool != null)
        {
            enemyPool.Clear();
            enemyPool = null;
        }

        activeEnemies.Clear();
    }
    #endregion

    #region Spawn Control
    public void StartSpawning()
    {
        if (isSpawning)
        {
            if (enableDebugLogs)
                Debug.Log("SpawnManager: Already spawning, ignoring StartSpawning call");
            return;
        }
        isSpawning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnCoroutine());

        if (enableDebugLogs)
            Debug.Log("SpawnManager: Started spawning");
    }

    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (enableDebugLogs)
            Debug.Log("SpawnManager: Stopped spawning");
    }

    public void ClearAllEnemies()
    {
        // Return all active enemies to pool
        var enemiesToClear = new List<GameObject>(activeEnemies);
        foreach (var enemy in enemiesToClear)
        {
            if (enemy != null)
            {
                enemyPool.Release(enemy);
            }
        }

        if (enableDebugLogs)
            Debug.Log($"SpawnManager: Cleared {enemiesToClear.Count} enemies");
    }
    #endregion

    #region Spawn Logic
    private IEnumerator SpawnCoroutine()
    {
        while (isSpawning)
        {
            // Check if we can spawn more enemies
            if (CanSpawn())
            {
                // Decide between single spawn or wave spawn
                if (enableWaveSpawning && Random.value < waveChance)
                {
                    SpawnWave();
                }
                else
                {
                    SpawnSingleEnemy();
                }

                lastSpawnTime = Time.time;
            }

            // Wait for next spawn opportunity
            float spawnDelay = CalculateSpawnDelay();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private bool CanSpawn()
    {
        // Check if we're at max capacity
        if (activeEnemies.Count >= maxEnemiesOnScreen)
            return false;

        // Check if enough time has passed
        float spawnDelay = CalculateSpawnDelay();
        if (Time.time - lastSpawnTime < spawnDelay)
            return false;

        // Check if game allows spawning
        if (gameState != null && !gameState.CanEnemiesSpawn())
            return false;

        return true;
    }

    private float CalculateSpawnDelay()
    {
        float difficultyMultiplier = spawnRateCurve.Evaluate(currentDifficulty);
        float baseDelay = baseSpawnRate / difficultyMultiplier;
        float variation = Random.Range(-spawnRateVariation, spawnRateVariation);
        return Mathf.Max(0.1f, baseDelay + variation);
    }

    private void SpawnSingleEnemy()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        EnemyData enemyData = SelectEnemyType();

        SpawnEnemyAtPosition(spawnPosition, enemyData);
    }

    private void SpawnWave()
    {
        Vector3 centerPosition = GetSpawnPosition();
        EnemyData enemyData = SelectWaveEnemyType();
        int waveSize = Random.Range(minWaveSize, maxWaveSize + 1);

        for (int i = 0; i < waveSize; i++)
        {
            Vector3 offset = Random.insideUnitCircle * waveSpawnRadius;
            Vector3 spawnPosition = centerPosition + offset;

            SpawnEnemyAtPosition(spawnPosition, enemyData);
        }

        if (enableDebugLogs)
            Debug.Log($"SpawnManager: Spawned wave of {waveSize} {enemyData.enemyName}s");
    }

    private void SpawnEnemyAtPosition(Vector3 position, EnemyData enemyData)
    {
        if (enemyPool == null) return;

        GameObject enemy = enemyPool.Get();
        enemy.transform.position = position;

        // Initialize enemy with data
        EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
        if (enemyCore != null)
        {
            enemyCore.SetEnemyData(enemyData);

            // Ensure the enemy is properly activated
            enemyCore.isActive = true;

            // Use the enemy's natural level from EnemyData (no scaling)
            enemyCore.currentLevel = enemyData.sizeLevel;
        }

        if (enableDebugLogs)
            Debug.Log($"SpawnManager: Spawned {enemyData.enemyName} at level {enemyData.sizeLevel} at {position}");
    }
    #endregion

    #region Enemy Selection
    private EnemyData SelectEnemyType()
    {
        // Filter enemies based on availability
        var availableEnemies = enemyTypes.Where(enemy =>
            enemy != null && CanSpawnEnemyType(enemy)).ToArray();

        if (availableEnemies.Length == 0)
        {
            Debug.LogWarning("SpawnManager: No suitable enemies found, using first available");
            return enemyTypes[0];
        }

        // Weight-based selection
        float totalWeight = availableEnemies.Sum(enemy => enemy.spawnWeight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in availableEnemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return enemy;
            }
        }

        // Fallback
        return availableEnemies[Random.Range(0, availableEnemies.Length)];
    }

    private EnemyData SelectWaveEnemyType()
    {
        // For waves, prefer enemies that can spawn in waves
        var waveEnemies = enemyTypes.Where(enemy =>
            enemy != null && enemy.canSpawnInWaves && CanSpawnEnemyType(enemy)).ToArray();

        if (waveEnemies.Length == 0)
            return SelectEnemyType();

        return waveEnemies[Random.Range(0, waveEnemies.Length)];
    }

    private bool CanSpawnEnemyType(EnemyData enemyData)
    {
        if (enemyData == null) return false;

        // Check if max spawn count is reached
        if (enemyData.maxSpawnCount > 0 && activeEnemies.Count(enemy => enemy.GetComponent<EnemyCore>().Data == enemyData) >= enemyData.maxSpawnCount)
            return false;

        return true;
    }
    #endregion

    #region Spawn Position
    private Vector3 GetSpawnPosition()
    {
        Vector3 position;
        int attempts = 0;
        int maxAttempts = 10;

        do
        {
            position = GenerateRandomSpawnPosition();
            attempts++;
        }
        while (attempts < maxAttempts && !IsValidSpawnPosition(position));

        return position;
    }

    private Vector3 GenerateRandomSpawnPosition()
    {
        if (useSpawnBounds && spawnBounds != null)
        {
            // Use spawn bounds
            Bounds bounds = spawnBounds.bounds;
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            return new Vector3(x, y, 0f);
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use spawn points with random offset
            int index = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[index];

            Vector3 offset = Random.insideUnitCircle * 2f;
            return spawnPoint.position + offset;
        }
        else
        {
            // Generate position around player
            if (playerCore != null)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                Vector3 playerPos = playerCore.transform.position;
                return playerPos + (Vector3)(randomDirection * spawnDistance);
            }
            else
            {
                // Fallback to random position
                return Vector3.zero + (Vector3)(Random.insideUnitCircle * 10f);
            }
        }
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if too close to player
        if (avoidPlayerSpawning && playerCore != null)
        {
            float distanceToPlayer = Vector3.Distance(position, playerCore.transform.position);
            if (distanceToPlayer < playerAvoidRadius)
                return false;
        }

        // Check if position is on screen (optional - might want enemies to spawn off-screen)
        if (gameCamera != null)
        {
            Vector3 screenPos = gameCamera.WorldToViewportPoint(position);
            bool onScreen = screenPos.x >= 0 && screenPos.x <= 1 && screenPos.y >= 0 && screenPos.y <= 1;

            Debug.Log($"SpawnManager: Checking position {position} - On Screen: {onScreen}");

            // For off-screen spawning, we want positions just outside the camera view
            if (!onScreen)
            {
                float buffer = 2f; // Units outside camera view
                Vector3 cameraPos = gameCamera.transform.position;
                float distance = Vector3.Distance(position, cameraPos);

                // Too far from camera
                if (distance > spawnDistance + buffer)
                    return false;
            }
        }

        return true;
    }
    #endregion

    #region Difficulty Management
    private void UpdateDifficulty()
    {
        if (gameState != null)
        {
            // Base difficulty on game time only (affects spawn rate, not enemy levels)
            float timeDifficulty = gameState.GameTimer / difficultyScaleTime;

            // No player level influence on difficulty
            currentDifficulty = Mathf.Clamp01(timeDifficulty);
        }
    }
    #endregion

    #region Cleanup
    private void CleanupInactiveEnemies()
    {
        // Remove null or inactive enemies from active list
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
            }
            else
            {
                // Check if enemy should be returned to pool (too far from player, etc.)
                EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
                if (enemyCore != null && !enemyCore.isActive)
                {
                    enemyPool.Release(enemy);
                }
            }
        }
    }
    #endregion

    #region Event Handlers
    private void OnGameStateChanged(GameStateType newState)
    {
        switch (newState)
        {
            case GameStateType.Playing:
                if (!isSpawning)
                    StartSpawning();
                break;

            case GameStateType.Paused:
            case GameStateType.GameOver:
            case GameStateType.Victory:
            case GameStateType.MainMenu:
                if (isSpawning)
                    StopSpawning();
                break;
        }
    }
    #endregion

    #region Public Interface
    public void SetSpawnRate(float newRate)
    {
        baseSpawnRate = Mathf.Max(0.1f, newRate);
    }

    public void SetMaxEnemies(int maxEnemies)
    {
        maxEnemiesOnScreen = Mathf.Max(1, maxEnemies);
    }

    public void ForceSpawnEnemy(EnemyData enemyData = null)
    {
        if (enemyData == null)
            enemyData = SelectEnemyType();

        Vector3 spawnPos = GetSpawnPosition();
        SpawnEnemyAtPosition(spawnPos, enemyData);
    }

    public void ForceSpawnWave(EnemyData enemyData = null, int waveSize = -1)
    {
        if (enemyData == null)
            enemyData = SelectWaveEnemyType();

        if (waveSize <= 0)
            waveSize = Random.Range(minWaveSize, maxWaveSize + 1);

        Vector3 centerPos = GetSpawnPosition();

        for (int i = 0; i < waveSize; i++)
        {
            Vector3 offset = Random.insideUnitCircle * waveSpawnRadius;
            Vector3 spawnPos = centerPos + offset;
            SpawnEnemyAtPosition(spawnPos, enemyData);
        }
    }
    #endregion

    #region Debug and Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!showSpawnGizmos) return;

        // Draw spawn bounds
        if (useSpawnBounds && spawnBounds != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnBounds.bounds.center, spawnBounds.bounds.size);
        }

        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 1f);
                }
            }
        }

        // Draw player avoid radius
        if (avoidPlayerSpawning && playerCore != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerCore.transform.position, playerAvoidRadius);
        }

        // Draw spawn distance
        if (playerCore != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCore.transform.position, spawnDistance);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintStatus()
    {
        Debug.Log($"SpawnManager Status:" +
                  $"\nIs Spawning: {isSpawning}" +
                  $"\nActive Enemies: {ActiveEnemyCount}/{maxEnemiesOnScreen}" +
                  $"\nPooled Enemies: {PooledEnemyCount}" +
                  $"\nCurrent Difficulty: {currentDifficulty:F2}" +
                  $"\nSpawn Rate: {baseSpawnRate:F2}s" +
                  $"\nLast Spawn: {Time.time - lastSpawnTime:F2}s ago");
    }
    #endregion
}
