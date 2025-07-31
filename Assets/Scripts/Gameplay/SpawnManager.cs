using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;  // Add this for scene management
using UnityEngine.Tilemaps;  // Add this for tilemap access
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Advanced enemy spawning system with dynamic level scaling, original game progression,
/// optimized object pooling, and intelligent spawn positioning.
/// Features progression-based enemy levels where lower-level enemies spawn more frequently.
/// </summary>
/// 
[System.Serializable]
public enum ProgressionType
{
  EnemiesDefeated,
  ExperienceGained,
  TimeBased,
  Hybrid
}

[System.Serializable]
public class SpawnPattern
{
  public string patternName;
  public AnimationCurve spawnRateMultiplier;
  public int minEnemies;
  public int maxEnemies;
  public float duration;
  public bool allowOverlap;
}

[System.Serializable]
public class WaveType
{
  public string name;
  public int minSize;
  public int maxSize;
  public float spawnRadius;
  public bool mixedLevels;
  public float levelVariance;
}

[System.Serializable]
public class LevelDistribution
{
  public int level;
  public float weight;
  public int spawnCount;
}

[System.Serializable]
public class ProgressionData
{
  public int currentPlayerLevel = 1;
  public float currentProgress = 0f;
  public int enemiesDefeated = 0;
  public float experienceGained = 0f;
  public float gameTime = 0f;
  public List<LevelDistribution> levelDistribution = new List<LevelDistribution>();
}

public class SpawnManager : MonoBehaviour
{
  #region Editor Data
  [Header("Spawn Configuration")]
  [Tooltip("Whether enemies are currently spawning. Toggle this to start/stop spawning.")]
  [SerializeField] private bool isSpawning = false;
  [Tooltip("Base time between enemy spawns in seconds. Lower values = faster spawning.")]
  [SerializeField] private float baseSpawnRate = 2f;
  [Tooltip("Random variation added to spawn rate (+/- this value in seconds).")]
  [SerializeField] private float spawnRateVariation = 0.5f;
  [Tooltip("Maximum number of enemies that can exist on screen simultaneously.")]
  [SerializeField] private int maxEnemiesOnScreen = 50;
  [Tooltip("Distance from player where enemies will spawn (in world units).")]
  [SerializeField] private float spawnDistance = 15f;

  [Header("Enemy Data")]
  [Tooltip("Array of enemy types that can be spawned. Each has different stats and spawn weights.")]
  [SerializeField] public EnemyData[] enemyTypes;
  [Tooltip("The prefab to instantiate for enemies. Must have EnemyCore component.")]
  [SerializeField] private GameObject enemyPrefab;

  [Header("Level System")]
  [Tooltip("Enable the level-based progression system where lower-level enemies spawn more frequently.")]
  [SerializeField] private bool useFeedingFrenzySystem = true;
  [Tooltip("How many levels above the player level can spawn (higher level enemies).")]
  [SerializeField] private int levelRange = 3; // Levels above player
  [Tooltip("How many levels below the player level can spawn (lower level enemies).")]
  [SerializeField] private int levelRangeBehind = 1; // Levels below player
  [Tooltip("Curve controlling spawn weight distribution across enemy levels. X = normalized level position, Y = weight multiplier.")]
  [SerializeField] private AnimationCurve levelWeightCurve = AnimationCurve.EaseInOut(0, 10, 3, 1);
  [Tooltip("Number of enemies that must be defeated to level up (when using EnemiesDefeated progression).")]
  [SerializeField] private float levelUpThreshold = 10f; // Enemies to defeat for level up
  [Tooltip("Use experience points instead of enemy count for progression (requires ExperienceGained progression type).")]
  [SerializeField] private bool useExperienceProgression = false;
  [Tooltip("Amount of experience needed per level (when using ExperienceGained progression).")]
  [SerializeField] private float experiencePerLevel = 100f;
  [Tooltip("How the player progresses through levels: EnemiesDefeated, ExperienceGained, TimeBased, or Hybrid.")]
  [SerializeField] private ProgressionType progressionType = ProgressionType.EnemiesDefeated;
  [Tooltip("Time in seconds required per level (when using TimeBased or Hybrid progression).")]
  [SerializeField] private float timePerLevel = 30f;
  [Tooltip("Show debug information about level progression and distribution in console.")]
  [SerializeField] private bool showProgressionDebug = true;

  [Header("Advanced Spawn Patterns")]
  [Tooltip("Custom spawn patterns with different timing and enemy counts (not fully implemented).")]
  [SerializeField] private SpawnPattern[] spawnPatterns;
  [Tooltip("Enable adaptive spawning that adjusts difficulty based on player performance.")]
  [SerializeField] private bool enableAdaptiveSpawning = true;
  [Tooltip("How often (in seconds) to check and adjust adaptive spawning parameters.")]
  [SerializeField] private float adaptiveCheckInterval = 5f;
  [Tooltip("Player stress level threshold (0-1) above which spawning will be reduced.")]
  [SerializeField] private float playerStressThreshold = 0.7f;
  [Header("Spawn Areas")]
  [Tooltip("Specific transform points where enemies can spawn. If empty, uses other spawn methods.")]
  [SerializeField] private Transform[] spawnPoints;
  [Tooltip("2D collider defining the spawn area bounds. Used when 'Use Spawn Bounds' is enabled.")]
  [SerializeField] private BoxCollider2D spawnBounds;
  [Tooltip("Use the spawn bounds collider to determine spawn area instead of other methods.")]
  [SerializeField] private bool useSpawnBounds = true;
  [Tooltip("Prefer spawning enemies just outside the camera view for smoother gameplay.")]
  [SerializeField] private bool preferOffScreenSpawning = true;
  [Tooltip("Extra distance beyond camera edge for off-screen spawning (in world units).")]
  [SerializeField] private float offScreenBuffer = 2f;
  [Header("Layer-Based Spawning")]
  [Tooltip("Enable layer-based spawn validation to ensure enemies spawn on valid ground.")]
  [SerializeField] private bool useLayerValidation = true;
  [Tooltip("Use direct tilemap access instead of physics collider checks (more efficient for tile-based games).")]
  [SerializeField] private bool useTilemapDirectAccess = true;
  [Tooltip("Layer mask for valid ground where enemies can spawn (e.g., Ground Layer).")]
  [SerializeField] private LayerMask groundLayer = 1;
  [Tooltip("Layer mask for invalid areas where enemies cannot spawn (e.g., Wall Layer).")]
  [SerializeField] private LayerMask wallLayer = 1 << 8;
  [Tooltip("Radius for layer detection when checking spawn positions.")]
  [SerializeField] private float layerCheckRadius = 0.5f;

  [Header("Difficulty Scaling")]
  [Tooltip("Curve controlling how spawn rate increases over time. X = difficulty (0-1), Y = rate multiplier.")]
  [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Linear(0, 1, 10, 3);
  [Tooltip("Curve controlling how enemy density increases over time. X = difficulty (0-1), Y = density multiplier.")]
  [SerializeField] private AnimationCurve enemyDensityCurve = AnimationCurve.Linear(0, 1, 10, 2);
  [Tooltip("Time in seconds over which difficulty scales from 0 to maximum.")]
  [SerializeField] private float difficultyScaleTime = 60f;

  [Header("Wave Spawning")]
  [Tooltip("Enable spawning groups of enemies together in waves.")]
  [SerializeField] private bool enableWaveSpawning = true;
  [Tooltip("Probability (0-1) that a spawn will be a wave instead of a single enemy.")]
  [SerializeField] private float waveChance = 0.15f;
  [Tooltip("Minimum number of enemies in a wave.")]
  [SerializeField] private int minWaveSize = 3;
  [Tooltip("Maximum number of enemies in a wave.")]
  [SerializeField] private int maxWaveSize = 6;
  [Tooltip("Radius around wave center point where individual enemies will spawn.")]
  [SerializeField] private float waveSpawnRadius = 4f;
  [Tooltip("Different wave configurations with custom sizes and behaviors.")]
  [SerializeField] private WaveType[] waveTypes;

  [Header("Player-Based Spawning")]
  [Tooltip("Prevent enemies from spawning too close to the player.")]
  [SerializeField] private bool avoidPlayerSpawning = true;
  [Tooltip("Minimum distance from player where enemies cannot spawn (in world units).")]
  [SerializeField] private float playerAvoidRadius = 8f;
  [Tooltip("Distance around player to check for enemy density and stress calculation.")]
  [SerializeField] private float playerDetectionRadius = 20f;

  [Header("Object Pooling")]
  [Tooltip("Number of enemies to pre-create in the object pool at startup.")]
  [SerializeField] private int poolInitialSize = 30;
  [Tooltip("Maximum number of enemies the pool can hold. Pool will destroy excess objects.")]
  [SerializeField] private int poolMaxSize = 150;
  [Tooltip("Enable collection checks in object pool (slower but safer for debugging).")]
  [SerializeField] private bool collectionCheck = false;
  [Tooltip("Pre-create and initialize pool objects at startup to reduce hitches during gameplay.")]
  [SerializeField] private bool preWarmPool = true;

  [Header("Performance")]
  [Tooltip("Maximum number of enemies that can spawn in a single frame to prevent hitches.")]
  [SerializeField] private int maxSpawnsPerFrame = 3;
  [Tooltip("How often (in seconds) to perform cleanup operations like removing inactive enemies.")]
  [SerializeField] private float cleanupInterval = 2f;
  [Tooltip("Automatically remove enemies that are too far from the player to improve performance.")]
  [SerializeField] private bool useDistanceCulling = true;
  [Tooltip("Distance from player beyond which enemies will be culled/removed (in world units).")]
  [SerializeField] private float cullDistance = 50f;

  [Header("References")]
  [Tooltip("Reference to the player's core component. Auto-found if not assigned.")]
  [SerializeField] private PlayerCore playerCore;
  [Tooltip("Reference to the game state manager. Auto-found if not assigned.")]
  [SerializeField] public GameState gameState;
  [Tooltip("Camera used for off-screen spawning calculations. Uses Camera.main if not assigned.")]
  [SerializeField] private Camera gameCamera;

  [Header("Debug")]
  [Tooltip("Enable debug logging for spawn events and system status.")]
  [SerializeField] private bool enableDebugLogs = false;
  [Tooltip("Show visual gizmos in scene view for spawn areas, radii, and bounds.")]
  [SerializeField] private bool showSpawnGizmos = false;
  [Tooltip("Display level distribution information in debug logs.")]
  [SerializeField] private bool showLevelDistribution = false;
  #endregion

  #region Internal Data
  private ObjectPool<GameObject> enemyPool;
  private List<GameObject> activeEnemies = new List<GameObject>();
  private Coroutine spawnCoroutine;
  private Coroutine cleanupCoroutine;
  private Coroutine adaptiveCoroutine;

  private float lastSpawnTime;
  private float currentDifficulty = 0f;
  private int spawnsThisFrame = 0;

  // Feeding Frenzy System
  private ProgressionData progressionData = new ProgressionData();
  private Dictionary<int, float> levelWeights = new Dictionary<int, float>();
  private Queue<int> recentSpawnLevels = new Queue<int>();

  // Performance tracking
  private float lastCleanupTime;
  private float lastAdaptiveCheck;
  private float playerStressLevel = 0f;

  // Cached components for performance
  private Transform playerTransform;
  private Transform cameraTransform;
  private Bounds cameraBounds;
  // Scene management for proper enemy assignment
  private Scene levelScene;
  private bool levelSceneFound = false;
  // Layer-based spawning cache
  private Collider2D[] cachedGroundColliders;
  private float lastGroundCacheTime;
  private readonly float groundCacheInterval = 5f; // Refresh cache every 5 seconds
  // Tilemap-based spawning cache
  private Tilemap[] groundTilemaps;
  private Tilemap[] wallTilemaps;
  private float lastTilemapCacheTime;
  private readonly float tilemapCacheInterval = 60f; // Refresh tilemap cache every 60 seconds
  private List<Vector3> spawnableCellOnTilemap = new List<Vector3>();

  // Performance optimization fields
  private Dictionary<Tilemap, int> tilemapHashCache = new Dictionary<Tilemap, int>();
  private Coroutine tilemapRefreshCoroutine;
  private bool isTilemapRefreshing = false;
  private Queue<Vector3> cellProcessingQueue = new Queue<Vector3>();
  private readonly int maxCellsPerFrame = 100; // Process max 100 cells per frame to avoid hitches

  #endregion

  #region Properties
  public bool IsSpawning => isSpawning;
  public int ActiveEnemyCount => activeEnemies.Count;
  public int PooledEnemyCount => enemyPool?.CountInactive ?? 0;
  public float CurrentDifficulty => currentDifficulty;
  public int CurrentPlayerLevel => progressionData.currentPlayerLevel;
  public float CurrentProgress => progressionData.currentProgress;
  public ProgressionData Progression => progressionData;
  #endregion

  #region Unity Lifecycle
  private void Awake()
  {
    InitializeReferences();
    InitializeObjectPool();
    InitializeFeedingFrenzySystem();

    GUIManager.Instance.FindSpawnManagerInScenes();
  }

  private void Start()
  {
    InitializeSpawning();
    SubscribeToEvents();

    if (preWarmPool)
      StartCoroutine(PreWarmPoolCoroutine());
  }

  private void Update()
  {
    if (isSpawning)
    {
      spawnsThisFrame = 0;
      UpdateProgression();
      UpdateDifficulty();
      UpdatePlayerStress();

      if (Time.time - lastCleanupTime > cleanupInterval)
      {
        PerformCleanup();
        lastCleanupTime = Time.time;
      }
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

    if (spawnBounds == null && useSpawnBounds)
      spawnBounds = GetComponent<BoxCollider2D>();

    // Cache transforms for performance
    if (playerCore != null)
      playerTransform = playerCore.transform;

    if (gameCamera != null)
    {
      cameraTransform = gameCamera.transform;
      UpdateCameraBounds();
    }

    // Find and cache the level scene for enemy spawning
    InitializeLevelScene();

    // Cache ground and wall tilemaps if using tilemap spawning
    RefreshTilemapCache();
  }

  /// <summary>
  /// Find and cache the level scene to ensure enemies spawn in the correct scene
  /// </summary>
  private void InitializeLevelScene()
  {
    var sceneName = "";
    if (GUIManager.Instance != null)
    {
      sceneName = GUIManager.Instance.CurrentLevelScene;
    }

    // Look for a scene that is not the Persistent Game Scene and is a level
    for (int i = 0; i < SceneManager.sceneCount; i++)
    {
      Scene scene = SceneManager.GetSceneAt(i);

      // Skip the persistent scene, look for level scenes (CxLy)
      if (scene.name != "Persistent Game State" && (
        scene.name.StartsWith("C") || scene.name.Contains("Level") || scene.name == sceneName
      ))
      {
        levelScene = scene;
        levelSceneFound = true;

        if (enableDebugLogs)
          Debug.Log($"SpawnManager: Found level scene for enemy spawning: {levelScene.name}");
        return;
      }
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

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Object pool initialized with capacity {poolInitialSize}/{poolMaxSize}");
  }

  private void InitializeFeedingFrenzySystem()
  {
    if (!useFeedingFrenzySystem) return;

    progressionData.currentPlayerLevel = 1;
    progressionData.currentProgress = 0f;
    progressionData.levelDistribution.Clear();

    UpdateLevelWeights();

    if (enableDebugLogs)
      Debug.Log("SpawnManager: Feeding Frenzy system initialized");
  }

  private void InitializeSpawning()
  {
    lastSpawnTime = Time.time;
    lastCleanupTime = Time.time;
    lastAdaptiveCheck = Time.time;
    currentDifficulty = 0f;

    if (enemyTypes == null || enemyTypes.Length == 0)
    {
      Debug.LogError("SpawnManager: No enemy types assigned!");
      enabled = false;
      return;
    }

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Initialized with {enemyTypes.Length} enemy types");
  }
  private IEnumerator PreWarmPoolCoroutine()
  {
    var prePopulate = new List<GameObject>();

    for (int i = 0; i < poolInitialSize; i++)
    {
      // Create enemy directly without activating it
      GameObject enemy = enemyPool.Get();

      // Immediately deactivate to prevent coroutines from starting
      enemy.SetActive(false);

      // Remove from active enemies list since we're just pre-warming
      activeEnemies.Remove(enemy);

      prePopulate.Add(enemy);

      // Spread over multiple frames to avoid hitches
      if (i % 5 == 0)
        yield return null;
    }

    // foreach (var enemy in prePopulate)
    // {
    //   enemyPool.Release(enemy);
    // }

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Pre-warmed pool with {poolInitialSize} enemies");
  }

  private void SubscribeToEvents()
  {
    if (gameState != null)
      gameState.OnStateChanged.AddListener(OnGameStateChanged);

    if (playerCore != null)
    {
      // Subscribe to player events for progression tracking
      // These would need to be implemented in PlayerCore
      // playerCore.OnEnemyDefeated += OnEnemyDefeated;
      // playerCore.OnExperienceGained += OnExperienceGained;
    }
  }

  private void UnsubscribeFromEvents()
  {
    if (gameState != null)
      gameState.OnStateChanged.RemoveListener(OnGameStateChanged);

    if (playerCore != null)
    {
      // playerCore.OnEnemyDefeated -= OnEnemyDefeated;
      // playerCore.OnExperienceGained -= OnExperienceGained;
    }
  }
  #endregion

  #region Feeding Frenzy Progression System
  private void UpdateProgression()
  {
    if (!useFeedingFrenzySystem) return;

    progressionData.gameTime += Time.deltaTime;

    float progressNeeded = GetProgressNeeded();
    bool leveledUp = false;

    switch (progressionType)
    {
      case ProgressionType.EnemiesDefeated:
        if (progressionData.enemiesDefeated >= progressNeeded)
        {
          leveledUp = true;
        }
        break;

      case ProgressionType.ExperienceGained:
        if (progressionData.experienceGained >= progressNeeded)
        {
          leveledUp = true;
        }
        break;

      case ProgressionType.TimeBased:
        if (progressionData.gameTime >= progressNeeded)
        {
          leveledUp = true;
        }
        break;

      case ProgressionType.Hybrid:
        float timeProgress = progressionData.gameTime / timePerLevel;
        float combatProgress = progressionData.enemiesDefeated / levelUpThreshold;
        if ((timeProgress + combatProgress) / 2f >= 1f)
        {
          leveledUp = true;
        }
        break;
    }

    if (leveledUp)
    {
      LevelUp();
    }

    UpdateProgressDisplay();
  }

  private float GetProgressNeeded()
  {
    switch (progressionType)
    {
      case ProgressionType.EnemiesDefeated:
        return levelUpThreshold * progressionData.currentPlayerLevel;

      case ProgressionType.ExperienceGained:
        return experiencePerLevel * progressionData.currentPlayerLevel;

      case ProgressionType.TimeBased:
        return timePerLevel * progressionData.currentPlayerLevel;

      case ProgressionType.Hybrid:
        return 1f; // Normalized progress for hybrid

      default:
        return levelUpThreshold;
    }
  }

  private void LevelUp()
  {
    progressionData.currentPlayerLevel++;

    // Reset progress counters
    progressionData.enemiesDefeated = 0;
    progressionData.experienceGained = 0f;
    progressionData.gameTime = 0f;

    UpdateLevelWeights();

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Player leveled up to {progressionData.currentPlayerLevel}!");

    if (showProgressionDebug)
      DebugPrintLevelDistribution();
  }

  private void UpdateLevelWeights()
  {
    levelWeights.Clear();

    int minLevel = Mathf.Max(1, progressionData.currentPlayerLevel - levelRangeBehind);
    int maxLevel = progressionData.currentPlayerLevel + levelRange;

    for (int level = minLevel; level <= maxLevel; level++)
    {
      float normalizedPosition = (float)(level - minLevel) / (maxLevel - minLevel);
      float weight = levelWeightCurve.Evaluate(normalizedPosition);
      levelWeights[level] = weight;
    }

    // Ensure current level and below have higher weights (Feeding Frenzy style)
    for (int level = minLevel; level <= progressionData.currentPlayerLevel; level++)
    {
      if (levelWeights.ContainsKey(level))
      {
        levelWeights[level] *= 2f; // Double weight for current and below levels
      }
    }
  }

  private void UpdateProgressDisplay()
  {
    float currentValue = 0f;
    float targetValue = GetProgressNeeded();

    switch (progressionType)
    {
      case ProgressionType.EnemiesDefeated:
        currentValue = progressionData.enemiesDefeated;
        break;
      case ProgressionType.ExperienceGained:
        currentValue = progressionData.experienceGained;
        break;
      case ProgressionType.TimeBased:
        currentValue = progressionData.gameTime;
        break;
      case ProgressionType.Hybrid:
        float timeProgress = progressionData.gameTime / timePerLevel;
        float combatProgress = progressionData.enemiesDefeated / levelUpThreshold;
        currentValue = (timeProgress + combatProgress) / 2f;
        targetValue = 1f;
        break;
    }

    progressionData.currentProgress = Mathf.Clamp01(currentValue / targetValue);
  }

  public void OnEnemyDefeated(int enemyLevel, float experienceReward)
  {
    if (!useFeedingFrenzySystem) return;

    progressionData.enemiesDefeated++;
    progressionData.experienceGained += experienceReward;

    // Track level distribution
    var levelDist = progressionData.levelDistribution.Find(x => x.level == enemyLevel);
    if (levelDist == null)
    {
      levelDist = new LevelDistribution { level = enemyLevel, weight = 0f, spawnCount = 0 };
      progressionData.levelDistribution.Add(levelDist);
    }
    levelDist.spawnCount++;
  }
  #endregion

  #region Adaptive Spawning System
  private void UpdatePlayerStress()
  {
    if (!enableAdaptiveSpawning) return;

    if (Time.time - lastAdaptiveCheck < adaptiveCheckInterval) return;
    lastAdaptiveCheck = Time.time;

    float enemyDensity = 0f;
    float averageEnemyLevel = 0f;
    int enemiesNearPlayer = 0;

    foreach (var enemy in activeEnemies)
    {
      if (enemy == null || !enemy.activeInHierarchy) continue;

      float distanceToPlayer = Vector3.Distance(enemy.transform.position, playerTransform.position);

      if (distanceToPlayer <= playerDetectionRadius)
      {
        enemiesNearPlayer++;

        EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
        if (enemyCore != null)
        {
          // Use the enemy's actual level from their data
          averageEnemyLevel += enemyCore.Data != null ? enemyCore.Data.level : enemyCore.CurrentLevel;
        }
      }
    }

    if (enemiesNearPlayer > 0)
    {
      averageEnemyLevel /= enemiesNearPlayer;
      enemyDensity = (float)enemiesNearPlayer / maxEnemiesOnScreen;
    }

    // Calculate stress level
    float levelStress = Mathf.Max(0f, (averageEnemyLevel - progressionData.currentPlayerLevel) / levelRange);
    float densityStress = enemyDensity;

    playerStressLevel = (levelStress + densityStress) / 2f;

    // Adjust spawn parameters based on stress
    if (playerStressLevel > playerStressThreshold)
    {
      // Reduce spawn rate when player is stressed
      baseSpawnRate *= 1.2f;
    }
    else if (playerStressLevel < playerStressThreshold * 0.5f)
    {
      // Increase spawn rate for more challenge when player is doing well
      baseSpawnRate *= 0.9f;
    }

    if (enableDebugLogs)
    {
      Debug.Log($"Adaptive System: Stress={playerStressLevel:F2}, Density={enemyDensity:F2}, AvgLevel={averageEnemyLevel:F1}");
    }
  }
  #endregion

  #region Object Pool Management
  private GameObject CreatePooledEnemy()
  {
    GameObject enemy = Instantiate(enemyPrefab);
    enemy.SetActive(false);

    // Move enemy to the level scene instead of Persistent Game Scene
    if (levelSceneFound && levelScene.IsValid())
    {
      SceneManager.MoveGameObjectToScene(enemy, levelScene);
    }
    else
    {
      // Re-initialize level scene if it's invalid (e.g., after scene reload)
      InitializeLevelScene();

      if (levelSceneFound && levelScene.IsValid())
      {
        SceneManager.MoveGameObjectToScene(enemy, levelScene);
      }
      else if (enableDebugLogs)
      {
        Debug.LogWarning("SpawnManager: Could not find valid level scene, enemy will remain in Persistent Game Scene");
      }
    }

    // Initialize with default components if needed
    if (enemy.GetComponent<EnemyCore>() == null)
    {
      Debug.LogWarning("SpawnManager: Enemy prefab missing EnemyCore component!");
    }

    return enemy;
  }

  private void OnGetFromPool(GameObject enemy)
  {
    enemy.SetActive(true);
    activeEnemies.Add(enemy);

    EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
    if (enemyCore != null)
    {
      enemyCore.isActive = true;
      enemyCore.ResetAnimatorAlpha();
    }
  }

  private void OnReturnToPool(GameObject enemy)
  {
    enemy.SetActive(false);
    activeEnemies.Remove(enemy);

    EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
    if (enemyCore != null)
    {
      enemyCore.isActive = false;
      enemyCore.ResetEnemy(); // Reset enemy state
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
    if (isSpawning) return;

    isSpawning = true;

    if (spawnCoroutine != null)
      StopCoroutine(spawnCoroutine);

    spawnCoroutine = StartCoroutine(SpawnCoroutine());

    if (cleanupCoroutine != null)
      StopCoroutine(cleanupCoroutine);

    cleanupCoroutine = StartCoroutine(PerformanceCleanupCoroutine());

    if (enableAdaptiveSpawning)
    {
      if (adaptiveCoroutine != null)
        StopCoroutine(adaptiveCoroutine);

      adaptiveCoroutine = StartCoroutine(AdaptiveSpawningCoroutine());
    }

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

    if (cleanupCoroutine != null)
    {
      StopCoroutine(cleanupCoroutine);
      cleanupCoroutine = null;
    }

    if (adaptiveCoroutine != null)
    {
      StopCoroutine(adaptiveCoroutine);
      adaptiveCoroutine = null;
    }

    if (enableDebugLogs)
      Debug.Log("SpawnManager: Stopped spawning");
  }

  public void ClearAllEnemies()
  {
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
      if (CanSpawn() && spawnsThisFrame < maxSpawnsPerFrame)
      {
        if (enableWaveSpawning && Random.value < waveChance)
        {
          SpawnWave();
        }
        else
        {
          SpawnSingleEnemy();
        }

        lastSpawnTime = Time.time;
        spawnsThisFrame++;
      }

      float spawnDelay = CalculateSpawnDelay();
      yield return new WaitForSeconds(spawnDelay);
    }
  }

  private IEnumerator PerformanceCleanupCoroutine()
  {
    while (isSpawning)
    {
      yield return new WaitForSeconds(cleanupInterval);
      PerformCleanup();
    }
  }

  private IEnumerator AdaptiveSpawningCoroutine()
  {
    while (isSpawning && enableAdaptiveSpawning)
    {
      yield return new WaitForSeconds(adaptiveCheckInterval);
      UpdatePlayerStress();
    }
  }

  private bool CanSpawn()
  {
    if (activeEnemies.Count >= maxEnemiesOnScreen)
      return false;

    if (gameState != null && !gameState.CanEnemiesSpawn())
      return false;

    float spawnDelay = CalculateSpawnDelay();
    if (Time.time - lastSpawnTime < spawnDelay)
      return false;

    return true;
  }

  private float CalculateSpawnDelay()
  {
    float difficultyMultiplier = spawnRateCurve.Evaluate(currentDifficulty);
    float densityMultiplier = enemyDensityCurve.Evaluate(currentDifficulty);

    float baseDelay = baseSpawnRate / (difficultyMultiplier * densityMultiplier);
    float variation = Random.Range(-spawnRateVariation, spawnRateVariation);

    return Mathf.Max(0.1f, baseDelay + variation);
  }
  private void SpawnSingleEnemy()
  {
    Vector3 spawnPosition = GetSpawnPosition();
    if (spawnPosition == Vector3.zero) return; // Failed to find valid position

    EnemyData enemyData = SelectEnemyType();
    int enemyLevel = enemyData.level; // Use the actual level from EnemyData

    SpawnEnemyAtPosition(spawnPosition, enemyData, enemyLevel);
  }
  private void SpawnWave()
  {
    Vector3 centerPosition = GetSpawnPosition();
    if (centerPosition == Vector3.zero) return;

    WaveType waveType = SelectWaveType();
    EnemyData enemyData = SelectWaveEnemyType();

    int waveSize = Random.Range(waveType?.minSize ?? minWaveSize, (waveType?.maxSize ?? maxWaveSize) + 1);
    float radius = waveType?.spawnRadius ?? waveSpawnRadius;

    for (int i = 0; i < waveSize && spawnsThisFrame < maxSpawnsPerFrame; i++)
    {
      Vector3 offset = Random.insideUnitCircle * radius;
      Vector3 spawnPosition = centerPosition + offset;

      int enemyLevel = enemyData.level; // Use the actual level from EnemyData

      // Only apply variance if wave type specifically allows mixed levels
      if (waveType != null && waveType.mixedLevels)
      {
        int variance = Mathf.RoundToInt(waveType.levelVariance);
        enemyLevel += Random.Range(-variance, variance + 1);
        enemyLevel = Mathf.Max(1, enemyLevel);
      }

      SpawnEnemyAtPosition(spawnPosition, enemyData, enemyLevel);
      spawnsThisFrame++;
    }

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Spawned wave of {waveSize} {enemyData.enemyName}s at level {enemyData.level}");
  }

  private void SpawnEnemyAtPosition(Vector3 position, EnemyData enemyData, int level)
  {
    if (enemyPool == null) return;

    GameObject enemy = enemyPool.Get();
    enemy.transform.position = position;

    EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
    if (enemyCore != null)
    {
      enemyCore.SetEnemyData(enemyData);
      enemyCore.SetLevel(level);
      enemyCore.isActive = true;
      enemyCore.ResetAnimatorAlpha();
    }

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Spawned {enemyData.enemyName} at level {level} at {position}. Is in wall layer: {IsPositionOnWallTile(position)}");
  }
  #endregion

  #region Enemy Selection
  private EnemyData SelectEnemyType()
  {
    var availableEnemies = enemyTypes.Where(enemy =>
        enemy != null && CanSpawnEnemyType(enemy)).ToArray();

    if (availableEnemies.Length == 0)
    {
      Debug.LogWarning("SpawnManager: No suitable enemies found, using first available");
      return enemyTypes[0];
    }

    // If Feeding Frenzy system is enabled, filter by level appropriateness
    if (useFeedingFrenzySystem)
    {
      var levelAppropriateEnemies = availableEnemies.Where(enemy =>
          IsEnemyLevelAppropriate(enemy)).ToArray();

      if (levelAppropriateEnemies.Length > 0)
      {
        availableEnemies = levelAppropriateEnemies;
      }
    }

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

    return availableEnemies[Random.Range(0, availableEnemies.Length)];
  }

  private bool IsEnemyLevelAppropriate(EnemyData enemyData)
  {
    if (!useFeedingFrenzySystem) return true;

    int minLevel = Mathf.Max(1, progressionData.currentPlayerLevel - levelRangeBehind);
    int maxLevel = progressionData.currentPlayerLevel + levelRange;

    return enemyData.level >= minLevel && enemyData.level <= maxLevel;
  }
  private EnemyData SelectWaveEnemyType()
  {
    var waveEnemies = enemyTypes.Where(enemy =>
        enemy != null && enemy.canSpawnInWaves && CanSpawnEnemyType(enemy)).ToArray();

    if (waveEnemies.Length == 0)
      return SelectEnemyType();

    // If Feeding Frenzy system is enabled, filter by level appropriateness
    if (useFeedingFrenzySystem)
    {
      var levelAppropriateEnemies = waveEnemies.Where(enemy =>
          IsEnemyLevelAppropriate(enemy)).ToArray();

      if (levelAppropriateEnemies.Length > 0)
      {
        waveEnemies = levelAppropriateEnemies;
      }
    }

    return waveEnemies[Random.Range(0, waveEnemies.Length)];
  }

  private WaveType SelectWaveType()
  {
    if (waveTypes == null || waveTypes.Length == 0)
      return null;

    return waveTypes[Random.Range(0, waveTypes.Length)];
  }

  private bool CanSpawnEnemyType(EnemyData enemyData)
  {
    if (enemyData == null) return false;

    if (enemyData.maxSpawnCount > 0)
    {
      int currentCount = activeEnemies.Count(enemy =>
      {
        var core = enemy.GetComponent<EnemyCore>();
        return core != null && core.Data == enemyData;
      });

      if (currentCount >= enemyData.maxSpawnCount)
        return false;
    }

    return true;
  }
  #endregion

  #region Spawn Position
  private Vector3 GetSpawnPosition()
  {
    Vector3 position = Vector3.zero;
    int attempts = 0;
    int maxAttempts = 15;

    do
    {
      position = GenerateRandomSpawnPosition();
      attempts++;
    }
    while (attempts < maxAttempts && !IsValidSpawnPosition(position));

    if (attempts >= maxAttempts)
    {
      if (enableDebugLogs)
        Debug.LogWarning("SpawnManager: Failed to find valid spawn position after maximum attempts");
      return Vector3.zero;
    }

    return position;
  }
  private Vector3 GenerateRandomSpawnPosition()
  {
    // Prioritize direct tilemap access if enabled
    if (useLayerValidation && useTilemapDirectAccess)
    {
      Vector3 tilemapPosition = GenerateTilemapBasedPosition();

      if (tilemapPosition != Vector3.zero)
        return tilemapPosition;
    }

    if (preferOffScreenSpawning && gameCamera != null)
    {
      return GenerateOffScreenPosition();
    }
    else if (useSpawnBounds && spawnBounds != null)
    {
      return GenerateBoundsPosition();
    }
    else if (spawnPoints != null && spawnPoints.Length > 0)
    {
      return GenerateSpawnPointPosition();
    }
    else
    {
      return GeneratePlayerRadiusPosition();
    }
  }

  private Vector3 GenerateOffScreenPosition()
  {
    UpdateCameraBounds();

    // Generate position just outside camera view
    Vector3 cameraPos = cameraTransform.position;
    float cameraSize = gameCamera.orthographicSize;
    float aspect = gameCamera.aspect;

    // Choose a side (0=top, 1=right, 2=bottom, 3=left)
    int side = Random.Range(0, 4);
    Vector3 position = cameraPos;

    switch (side)
    {
      case 0: // Top
        position.y += cameraSize + offScreenBuffer;
        position.x += Random.Range(-cameraSize * aspect, cameraSize * aspect);
        break;
      case 1: // Right
        position.x += cameraSize * aspect + offScreenBuffer;
        position.y += Random.Range(-cameraSize, cameraSize);
        break;
      case 2: // Bottom
        position.y -= cameraSize + offScreenBuffer;
        position.x += Random.Range(-cameraSize * aspect, cameraSize * aspect);
        break;
      case 3: // Left
        position.x -= cameraSize * aspect + offScreenBuffer;
        position.y += Random.Range(-cameraSize, cameraSize);
        break;
    }

    return position;
  }

  private Vector3 GenerateBoundsPosition()
  {
    Bounds bounds = spawnBounds.bounds;
    float x = Random.Range(bounds.min.x, bounds.max.x);
    float y = Random.Range(bounds.min.y, bounds.max.y);
    return new Vector3(x, y, 0f);
  }

  private Vector3 GenerateSpawnPointPosition()
  {
    Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
    Vector3 offset = Random.insideUnitCircle * 2f;
    return spawnPoint.position + offset;
  }
  private Vector3 GeneratePlayerRadiusPosition()
  {
    if (playerTransform != null)
    {
      Vector2 randomDirection = Random.insideUnitCircle.normalized;
      return playerTransform.position + (Vector3)(randomDirection * spawnDistance);
    }

    return Vector3.zero + (Vector3)(Random.insideUnitCircle * 10f);
  }

  /// <summary>
  /// Generate a random point within a collider's bounds
  /// </summary>
  private Vector3 GeneratePointInCollider(Collider2D collider)
  {
    Bounds bounds = collider.bounds;

    // For more accurate sampling, we could implement different strategies based on collider type
    if (collider is BoxCollider2D)
    {
      // Simple box sampling
      float x = Random.Range(bounds.min.x, bounds.max.x);
      float y = Random.Range(bounds.min.y, bounds.max.y);
      return new Vector3(x, y, 0f);
    }
    else if (collider is CircleCollider2D circle)
    {
      // Sample within circle
      Vector2 randomPoint = Random.insideUnitCircle * circle.radius;
      return (Vector3)((Vector2)circle.transform.position + randomPoint);
    }
    else
    {
      // Generic bounds sampling for other collider types
      float x = Random.Range(bounds.min.x, bounds.max.x);
      float y = Random.Range(bounds.min.y, bounds.max.y);
      return new Vector3(x, y, 0f);
    }
  }
  /// <summary>
  /// Refresh the cache of ground colliders periodically
  /// </summary>
  private void RefreshGroundCollidersCache()
  {
    if (cachedGroundColliders != null && Time.time - lastGroundCacheTime < groundCacheInterval)
      return;

    // Find all colliders on ground layer
    cachedGroundColliders = FindObjectsOfType<Collider2D>()
      .Where(col => ((1 << col.gameObject.layer) & groundLayer) != 0
                   && col.gameObject.activeInHierarchy
                   && col.enabled)
      .ToArray();

    lastGroundCacheTime = Time.time;

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Refreshed ground colliders cache, found {cachedGroundColliders.Length} colliders");
  }

  /// <summary>
  /// Generate a spawn position directly from tilemap tiles
  /// </summary>
  private Vector3 GenerateTilemapBasedPosition()
  {
    RefreshTilemapCache();

    if (groundTilemaps == null || groundTilemaps.Length == 0)
    {
      if (enableDebugLogs)
        Debug.LogWarning("SpawnManager: No ground tilemaps found for tilemap-based spawning");

      return Vector3.zero;
    }

    // Check if we have any valid spawn cells available
    if (spawnableCellOnTilemap == null || spawnableCellOnTilemap.Count == 0)
    {
      if (enableDebugLogs)
        Debug.Log("SpawnManager: No valid spawn cells available, attempting synchronous fallback");

      return Vector3.zero; // No valid spawn cells found

      // Try synchronous fallback for emergency spawning
      // return GenerateFallbackTilemapPosition();
    }

    int maxPositionAttempts = 15;
    for (int posAttempt = 0; posAttempt < maxPositionAttempts; posAttempt++)
    {
      Vector3 cellPos = spawnableCellOnTilemap[Random.Range(0, spawnableCellOnTilemap.Count)];

      // Add small random offset within the cell
      // cellPos += new Vector3(
      //   Random.Range(-0.4f, 0.4f),
      //   Random.Range(-0.4f, 0.4f),
      //   0f
      // );

      // Check if this position doesn't overlap with wall tiles
      if (!IsPositionOnWallTile(cellPos))
      {
        return cellPos;
      }
    }
    
    if (enableDebugLogs)
      Debug.Log("SpawnManager: Failed to find valid tilemap-based position after all attempts");

    return Vector3.zero;
  }

  /// <summary>
  /// Check if a world position overlaps with any wall tiles
  /// </summary>
  private bool IsPositionOnWallTile(Vector3 worldPosition)
  {
    if (wallTilemaps == null || wallTilemaps.Length == 0)
      return false;

    foreach (Tilemap wallTilemap in wallTilemaps)
    {
      if (wallTilemap == null || !wallTilemap.gameObject.activeInHierarchy)
        continue;

      // Convert world position to cell position
      Vector3Int cellPos = wallTilemap.WorldToCell(worldPosition);

      // Check if there's a wall tile at this position
      TileBase wallTile = wallTilemap.GetTile(cellPos);
      if (wallTile != null)
      {
        return true; // Found a wall tile
      }
    }

    return false; // No wall tiles found
  }
  
  /// <summary>
  /// Optimized tilemap cache refresh with change detection and coroutine-based processing
  /// </summary>
  private void RefreshTilemapCache()
  {
    // Avoid refresh if we're already refreshing or too soon since last refresh
    if (isTilemapRefreshing || (groundTilemaps != null && Time.time - lastTilemapCacheTime < tilemapCacheInterval))
      return;

    // Check if tilemaps have actually changed since last cache by comparing hash codes
    if (HasTilemapsChanged())
    {
      if (tilemapRefreshCoroutine != null)
        StopCoroutine(tilemapRefreshCoroutine);

      tilemapRefreshCoroutine = StartCoroutine(RefreshTilemapCacheCoroutine());
    }
  }

  /// <summary>
  /// Check if any tilemaps have changed since last cache by comparing hash codes
  /// </summary>
  private bool HasTilemapsChanged()
  {
    // First time check or no cached tilemaps
    if (groundTilemaps == null || wallTilemaps == null)
      return true;

    // Quick check: verify all cached tilemaps still exist and are active
    foreach (var tilemap in groundTilemaps)
    {
      if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        return true;
    }

    foreach (var tilemap in wallTilemaps)
    {
      if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        return true;
    }

    // Check for hash changes (indicates tilemap content changed)
    foreach (var kvp in tilemapHashCache)
    {
      Tilemap tilemap = kvp.Key;
      int cachedHash = kvp.Value;

      if (tilemap == null) continue;

      // Calculate current hash based on tilemap bounds and tile count
      int currentHash = CalculateTilemapHash(tilemap);
      if (currentHash != cachedHash)
        return true;
    }

    return false;
  }

  /// <summary>
  /// Calculate a hash for tilemap to detect changes
  /// </summary>
  private int CalculateTilemapHash(Tilemap tilemap)
  {
    if (tilemap == null) return 0;

    int hash = tilemap.cellBounds.GetHashCode();
    hash = hash * 31 + tilemap.GetUsedTilesCount();
    hash = hash * 31 + tilemap.gameObject.GetInstanceID();
    return hash;
  }

  /// <summary>
  /// Coroutine-based tilemap cache refresh that spreads work across multiple frames
  /// </summary>
  private IEnumerator RefreshTilemapCacheCoroutine()
  {
    isTilemapRefreshing = true;

    try
    {
      // Step 1: Find and cache tilemap references
      yield return StartCoroutine(CacheTilemapReferences());

      // Step 2: Calculate spawn cells across multiple frames
      yield return StartCoroutine(CalculateSpawnCellsCoroutine());

      lastTilemapCacheTime = Time.time;

      if (enableDebugLogs)
        Debug.Log($"SpawnManager: Optimized tilemap cache refresh completed - Ground: {groundTilemaps?.Length ?? 0}, Wall: {wallTilemaps?.Length ?? 0}, Spawn Cells: {spawnableCellOnTilemap.Count}");
    }
    finally
    {
      isTilemapRefreshing = false;
    }
  }

  /// <summary>
  /// Cache tilemap references and update hash cache
  /// </summary>
  private IEnumerator CacheTilemapReferences()
  {
    // Find all tilemaps on ground layer
    var allTilemaps = FindObjectsOfType<Tilemap>();

    var groundTilemapsList = new List<Tilemap>();
    var wallTilemapsList = new List<Tilemap>();

    foreach (var tilemap in allTilemaps)
    {
      if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        continue;

      int layer = tilemap.gameObject.layer;

      if ((groundLayer & (1 << layer)) != 0)
        groundTilemapsList.Add(tilemap);

      if ((wallLayer & (1 << layer)) != 0)
        wallTilemapsList.Add(tilemap);
    }

    groundTilemaps = groundTilemapsList.ToArray();
    wallTilemaps = wallTilemapsList.ToArray();

    // Update hash cache
    tilemapHashCache.Clear();
    foreach (var tilemap in groundTilemaps)
    {
      if (tilemap != null)
      {
        tilemapHashCache[tilemap] = CalculateTilemapHash(tilemap);
      }
    }
    foreach (var tilemap in wallTilemaps)
    {
      if (tilemap != null)
      {
        tilemapHashCache[tilemap] = CalculateTilemapHash(tilemap);
      }
    }

    yield return null; // Yield after reference caching
  }

  /// <summary>
  /// Calculate spawn cells using optimized coroutine-based approach
  /// </summary>
  private IEnumerator CalculateSpawnCellsCoroutine()
  {
    spawnableCellOnTilemap.Clear();

    if (groundTilemaps == null || groundTilemaps.Length == 0)
      yield break;

    // Step 1: Calculate combined bounds more efficiently
    BoundsInt combinedBounds = CalculateCombinedTilemapBounds();
    if (combinedBounds.size == Vector3Int.zero)
      yield break;

    Debug.Log($"SpawnManager: Step 1: Combined tilemap bounds calculated: {combinedBounds}.");

    // Step 2: Pre-populate cell processing queue
    cellProcessingQueue.Clear();
    for (int x = combinedBounds.xMin; x < combinedBounds.xMax; x++)
    {
      for (int y = combinedBounds.yMin; y < combinedBounds.yMax; y++)
      {
        cellProcessingQueue.Enqueue(new Vector3(x, y, 0));
      }
    }

    Debug.Log($"SpawnManager: Step 2: Calculated {cellProcessingQueue.Count} cells for processing in tilemaps");

    // Step 3: Process cells in batches across frames
    var validCells = new HashSet<Vector3>();
    int processedThisFrame = 0;

    while (cellProcessingQueue.Count > 0)
    {
      Vector3 cellPos = cellProcessingQueue.Dequeue();

      if (IsValidGroundCell(cellPos) && !IsPositionOnWallTile(cellPos))
      {
        validCells.Add(cellPos);
      }

      processedThisFrame++;

      // Yield every maxCellsPerFrame to prevent frame drops
      if (processedThisFrame >= maxCellsPerFrame)
      {
        processedThisFrame = 0;
        yield return null;
      }
    }

    Debug.Log($"SpawnManager: Step 3: Processed {validCells.Count} valid ground cells from tilemaps");

    // Step 4: Filter cells that have sufficient neighbors
    yield return StartCoroutine(FilterCellsByNeighborsCoroutine(validCells));
  }

  /// <summary>
  /// Calculate combined bounds of all ground tilemaps more efficiently
  /// </summary>
  private BoundsInt CalculateCombinedTilemapBounds()
  {
    bool hasBounds = false;
    BoundsInt combinedBounds = new BoundsInt();

    foreach (var tilemap in groundTilemaps)
    {
      if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
        continue;


      BoundsInt cellBounds = tilemap.cellBounds;
      if (cellBounds.size == Vector3Int.zero)
        continue;

      if (!hasBounds)
      {
        combinedBounds = cellBounds;
        hasBounds = true;
      }
      else
      {
        // Efficiently expand bounds
        Vector3Int min = Vector3Int.Min(combinedBounds.min, cellBounds.min);
        Vector3Int max = Vector3Int.Max(combinedBounds.max, cellBounds.max);
        combinedBounds.SetMinMax(min, max);
      }
    }

    return hasBounds ? combinedBounds : new BoundsInt();
  }

  /// <summary>
  /// Check if a cell position is a valid ground cell
  /// </summary>
  private bool IsValidGroundCell(Vector3 cellPos)
  {
    foreach (var groundTilemap in groundTilemaps)
    {
      if (groundTilemap == null || !groundTilemap.gameObject.activeInHierarchy)
        continue;

      // Convert world position to cell position
      Vector3Int cellPosition = groundTilemap.WorldToCell(cellPos);

      if (groundTilemap.GetTile(cellPosition) != null)
        return true;
    }
    return false;
  }

  /// <summary>
  /// Filter cells by neighbor count using coroutine for performance
  /// </summary>
  private IEnumerator FilterCellsByNeighborsCoroutine(HashSet<Vector3> validCells)
  {
    var finalSpawnCells = new List<Vector3>();
    int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
    int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

    int processedThisFrame = 0;

    foreach (var cell in validCells)
    {
      int neighborCount = 0;

      // Count valid neighbors
      for (int k = 0; k < 8; k++)
      {
        Vector3 neighborPos = new Vector3(cell.x + dx[k], cell.y + dy[k], 0);
        if (validCells.Contains(neighborPos))
          neighborCount++;
      }

      // Only add cells with all 8 neighbors (completely surrounded)
      if (neighborCount >= 8)
      {
        finalSpawnCells.Add((Vector3)cell);
      }

      processedThisFrame++;

      // Yield periodically to prevent frame drops
      if (processedThisFrame >= maxCellsPerFrame)
      {
        processedThisFrame = 0;
        yield return null;
      }
    }

    // Update the final spawn cell list
    spawnableCellOnTilemap.Clear();
    spawnableCellOnTilemap.AddRange(finalSpawnCells);

    Debug.Log($"RefreshTilemapCache: Found {spawnableCellOnTilemap.Count} valid spawn cells for spawning enemies");
  }

  private bool IsPositionOnCollider(Vector3 position)
  {
    // Check for any Collider2D at this position
    Collider2D collider = Physics2D.OverlapPoint(position);

    if (collider != null)
    {
      if (enableDebugLogs)
        Debug.Log($"SpawnManager: Position {position} rejected - overlaps {collider.GetType().Name} on {collider.gameObject.name}");

      return true;
    }

    return false;
  }

  private bool IsValidSpawnPosition(Vector3 position)
  {
    // Check distance to player
    if (avoidPlayerSpawning && playerTransform != null)
    {
      float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
      if (distanceToPlayer < playerAvoidRadius)
        return false;
    }

    if (useTilemapDirectAccess && IsPositionOnCollider(position))
    {
      if (enableDebugLogs)
        Debug.Log($"SpawnManager: Position {position} rejected - overlaps TilemapCollider2D");

      return false;
    }

    // Only do layer validation if we're not using tilemap direct access
    // (since direct access already ensures valid layers)
    if (useLayerValidation)
    {
      if (!IsPositionOnValidLayer(position))
        return false;
    }

    // Check if position is reasonable relative to camera
    if (gameCamera != null)
    {
      Vector3 screenPos = gameCamera.WorldToViewportPoint(position);
      float distanceFromCamera = Vector3.Distance(position, cameraTransform.position);

      // Too far from camera
      if (distanceFromCamera > spawnDistance + offScreenBuffer + 5f)
        return false;

      // For off-screen spawning, ensure it's not too far off-screen
      if (preferOffScreenSpawning)
      {
        bool wayOffScreen = screenPos.x < -0.5f || screenPos.x > 1.5f ||
                          screenPos.y < -0.5f || screenPos.y > 1.5f;
        if (wayOffScreen)
          return false;
      }
    }
    return true;
  }
  
  /// <summary>
  /// Check if the position is on a valid layer for spawning (Ground Layer but not Wall Layer)
  /// </summary>
  private bool IsPositionOnValidLayer(Vector3 position)
  {
    // Use direct tilemap access if enabled
    if (useTilemapDirectAccess)
    {
      return IsValidPositionOnTilemaps(position);
    }

    // Fallback to physics-based checking
    // Check if position is on ground layer
    Collider2D groundCollider = Physics2D.OverlapCircle(position, layerCheckRadius, groundLayer);
    bool onGroundLayer = groundCollider != null;

    // Check if position overlaps with wall layer
    Collider2D wallCollider = Physics2D.OverlapCircle(position, layerCheckRadius, wallLayer);
    bool onWallLayer = wallCollider != null;

    // Valid position: on ground layer AND NOT on wall layer
    bool isValid = onGroundLayer && !onWallLayer;

    if (enableDebugLogs && !isValid)
    {
      if (!onGroundLayer)
        Debug.Log($"SpawnManager: Position {position} rejected - not on ground layer");
      if (onWallLayer)
        Debug.Log($"SpawnManager: Position {position} rejected - overlaps wall layer");
    }

    return isValid;
  }

  /// <summary>
  /// Check if position is valid using direct tilemap access
  /// </summary>
  private bool IsValidPositionOnTilemaps(Vector3 position)
  {
    RefreshTilemapCache();

    // Check if position has ground tile
    bool hasGroundTile = false;
    if (groundTilemaps != null)
    {
      foreach (Tilemap groundTilemap in groundTilemaps)
      {
        if (groundTilemap == null || !groundTilemap.gameObject.activeInHierarchy)
          continue;

        Vector3Int cellPos = groundTilemap.WorldToCell(position);
        TileBase tile = groundTilemap.GetTile(cellPos);
        if (tile != null)
        {
          hasGroundTile = true;
          break;
        }
      }
    }

    // Check if position has wall tile
    bool hasWallTile = IsPositionOnWallTile(position);

    // Valid position: has ground tile AND no wall tile
    bool isValid = hasGroundTile && !hasWallTile;

    if (enableDebugLogs && !isValid)
    {
      if (!hasGroundTile)
        Debug.Log($"SpawnManager: Position {position} rejected - no ground tile");
        
      if (hasWallTile)
        Debug.Log($"SpawnManager: Position {position} rejected - overlaps wall tile");
    }

    return isValid;
  }

  private void UpdateCameraBounds()
  {
    if (gameCamera == null || cameraTransform == null) return;

    float height = gameCamera.orthographicSize * 2f;
    float width = height * gameCamera.aspect;
    Vector3 center = cameraTransform.position;

    cameraBounds = new Bounds(center, new Vector3(width, height, 0f));
  }
  #endregion

  #region Difficulty Management
  private void UpdateDifficulty()
  {
    if (gameState != null)
    {
      float timeDifficulty = gameState.GameTimer / difficultyScaleTime;
      float progressionDifficulty = (progressionData.currentPlayerLevel - 1) * 0.1f;

      currentDifficulty = Mathf.Clamp01(timeDifficulty + progressionDifficulty);
    }
  }
  #endregion

  #region Performance and Cleanup
  private void PerformCleanup()
  {
    // Remove null or inactive enemies
    for (int i = activeEnemies.Count - 1; i >= 0; i--)
    {
      GameObject enemy = activeEnemies[i];
      if (enemy == null || !enemy.activeInHierarchy)
      {
        activeEnemies.RemoveAt(i);
        continue;
      }

      // Distance culling
      if (useDistanceCulling && playerTransform != null)
      {
        float distance = Vector3.Distance(enemy.transform.position, playerTransform.position);
        if (distance > cullDistance)
        {
          EnemyCore enemyCore = enemy.GetComponent<EnemyCore>();
          if (enemyCore != null && !enemyCore.isActive)
          {
            enemyPool.Release(enemy);
            continue;
          }
        }
      }

      // Check if enemy should be returned to pool
      EnemyCore core = enemy.GetComponent<EnemyCore>();
      if (core != null && !core.isActive)
      {
        enemyPool.Release(enemy);
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
  public void SetPlayerLevel(int level)
  {
    if (useFeedingFrenzySystem)
    {
      progressionData.currentPlayerLevel = Mathf.Max(1, level);
      UpdateLevelWeights();
    }
  }
  public void SetLayerValidation(bool useValidation, LayerMask ground = default, LayerMask wall = default)
  {
    useLayerValidation = useValidation;
    if (ground != default) groundLayer = ground;
    if (wall != default) wallLayer = wall;

    // Refresh cache when layer settings change
    if (useValidation)
    {
      cachedGroundColliders = null; // Force refresh
      RefreshGroundCollidersCache();
    }

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Layer validation set to {useValidation}. Ground: {groundLayer.value}, Wall: {wallLayer.value}");
  }
  public void RefreshGroundCache()
  {
    cachedGroundColliders = null;
    RefreshGroundCollidersCache();

    if (useTilemapDirectAccess)
    {
      groundTilemaps = null;
      wallTilemaps = null;
      RefreshTilemapCache();
    }
  }

  public void AddProgress(float amount)
  {
    switch (progressionType)
    {
      case ProgressionType.EnemiesDefeated:
        progressionData.enemiesDefeated += Mathf.RoundToInt(amount);
        break;
      case ProgressionType.ExperienceGained:
        progressionData.experienceGained += amount;
        break;
    }
  }
  public void ForceSpawnEnemy(EnemyData enemyData = null, int level = -1)
  {
    // Re-check level scene before forced spawning
    if (!levelSceneFound || !levelScene.IsValid())
    {
      InitializeLevelScene();
    }

    if (enemyData == null)
      enemyData = SelectEnemyType();

    if (level <= 0)
      level = enemyData.level; // Use the actual level from EnemyData

    Vector3 spawnPos = GetSpawnPosition();
    if (spawnPos != Vector3.zero)
    {
      SpawnEnemyAtPosition(spawnPos, enemyData, level);
    }
  }

  public void ForceSpawnWave(EnemyData enemyData = null, int waveSize = -1)
  {
    // Re-check level scene before forced spawning
    if (!levelSceneFound || !levelScene.IsValid())
    {
      InitializeLevelScene();
    }

    if (enemyData == null)
      enemyData = SelectWaveEnemyType();

    if (waveSize <= 0)
      waveSize = Random.Range(minWaveSize, maxWaveSize + 1);

    Vector3 centerPos = GetSpawnPosition();
    if (centerPos == Vector3.zero) return;

    for (int i = 0; i < waveSize; i++)
    {
      Vector3 offset = Random.insideUnitCircle * waveSpawnRadius;
      Vector3 spawnPos = centerPos + offset;
      int level = enemyData.level; // Use the actual level from EnemyData
      SpawnEnemyAtPosition(spawnPos, enemyData, level);
    }
  }

  /// <summary>
  /// Public method to refresh the level scene reference after scene reload
  /// Call this from UIManager after level replay
  /// </summary>
  public void RefreshLevelScene()
  {
    InitializeLevelScene();

    if (enableDebugLogs)
      Debug.Log($"SpawnManager: Level scene reference refreshed to: {(levelSceneFound ? levelScene.name : "None")}");
  }

  /// <summary>
  /// Get the current level scene name (for debugging)
  /// </summary>
  public string GetCurrentLevelScene()
  {
    return levelSceneFound ? levelScene.name : "None";
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
    if (avoidPlayerSpawning && playerTransform != null)
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(playerTransform.position, playerAvoidRadius);
    }

    // Draw spawn distance
    if (playerTransform != null)
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(playerTransform.position, spawnDistance);
    }

    // Draw player detection radius
    if (playerTransform != null)
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(playerTransform.position, playerDetectionRadius);
    }

    // Draw camera bounds
    if (gameCamera != null)
    {
      UpdateCameraBounds();
      Gizmos.color = Color.white;
      Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
    }

    // Draw valid spawn cells
    if (spawnableCellOnTilemap != null && spawnableCellOnTilemap.Count > 0)
    {
      Gizmos.color = Color.red;
      foreach (Vector3 cellPos in spawnableCellOnTilemap)
      {
        Gizmos.DrawSphere(cellPos, 0.4f);
      }
    }

    // Draw all wall cells for comparison
    if (wallTilemaps != null)
    {
      Gizmos.color = Color.magenta;
      foreach (var tilemap in wallTilemaps)
      {
        if (tilemap == null || !tilemap.gameObject.activeInHierarchy) continue;

        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
          for (int y = bounds.yMin; y < bounds.yMax; y++)
          {
            Vector3Int cellPos = new Vector3Int(x, y, 0);
            if (tilemap.GetTile(cellPos) != null)
            {
              Vector3 worldPos = tilemap.CellToWorld(cellPos);
              Gizmos.DrawWireCube(worldPos, Vector3.one * 0.6f);
            }
          }
        }
      }
    }

    // Draw all ground cells for comparison
      if (groundTilemaps != null)
      {
        Gizmos.color = Color.blue;
        foreach (var tilemap in groundTilemaps)
        {
          if (tilemap == null || !tilemap.gameObject.activeInHierarchy) continue;

          BoundsInt bounds = tilemap.cellBounds;
          for (int x = bounds.xMin; x < bounds.xMax; x++)
          {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
              Vector3Int cellPos = new Vector3Int(x, y, 0);
              if (tilemap.GetTile(cellPos) != null)
              {
                Vector3 worldPos = tilemap.CellToWorld(cellPos);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
              }
            }
          }
        }
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
              $"\nPlayer Level: {progressionData.currentPlayerLevel}" +
              $"\nPlayer Progress: {progressionData.currentProgress:F2}" +
              $"\nPlayer Stress: {playerStressLevel:F2}" +
              $"\nSpawn Rate: {baseSpawnRate:F2}s" +
              $"\nLast Spawn: {Time.time - lastSpawnTime:F2}s ago" +
              $"\nLayer Validation: {useLayerValidation}" +
              $"\nTilemap Direct Access: {useTilemapDirectAccess}" +
              $"\nGround Layer: {groundLayer.value}" +
              $"\nWall Layer: {wallLayer.value}" +
              $"\nCached Ground Colliders: {(cachedGroundColliders?.Length ?? 0)}" +
              $"\nGround Tilemaps: {(groundTilemaps?.Length ?? 0)}" +
              $"\nWall Tilemaps: {(wallTilemaps?.Length ?? 0)}");
  }

  [System.Diagnostics.Conditional("UNITY_EDITOR")]
  public void DebugPrintLevelDistribution()
  {
    if (!showLevelDistribution) return;

    string distribution = "Level Distribution:\n";
    foreach (var kvp in levelWeights.OrderBy(x => x.Key))
    {
      distribution += $"Level {kvp.Key}: Weight {kvp.Value:F2}\n";
    }

    if (recentSpawnLevels.Count > 0)
    {
      var recentLevels = recentSpawnLevels.GroupBy(x => x).OrderBy(x => x.Key);
      distribution += "\nRecent Spawns:\n";
      foreach (var group in recentLevels)
      {
        distribution += $"Level {group.Key}: {group.Count()} spawns\n";
      }
    }

    Debug.Log(distribution);
  }
  #endregion

  #region Synchronous fallback for tilemap position generation when cache is not ready
  /// <summary>
  /// Synchronous fallback for tilemap-based position generation when cache is not ready
  /// </summary>
  private Vector3 GenerateFallbackTilemapPosition()
  {
    if (groundTilemaps == null || groundTilemaps.Length == 0)
      return Vector3.zero;

    // Try a simple approach: pick a random tilemap and find a valid cell
    int maxTilemapAttempts = Mathf.Min(3, groundTilemaps.Length);

    for (int tilemapAttempt = 0; tilemapAttempt < maxTilemapAttempts; tilemapAttempt++)
    {
      Tilemap groundTilemap = groundTilemaps[Random.Range(0, groundTilemaps.Length)];

      if (groundTilemap == null || !groundTilemap.gameObject.activeInHierarchy)
        continue;

      // Get tilemap bounds
      BoundsInt tilemapBounds = groundTilemap.cellBounds;

      if (tilemapBounds.size == Vector3Int.zero)
        continue;

      // Try multiple random positions within the tilemap
      int maxPositionAttempts = 10;
      for (int posAttempt = 0; posAttempt < maxPositionAttempts; posAttempt++)
      {
        // Generate random cell position within tilemap bounds
        Vector3Int randomCellPos = new Vector3Int(
          Random.Range(tilemapBounds.xMin, tilemapBounds.xMax),
          Random.Range(tilemapBounds.yMin, tilemapBounds.yMax),
          0
        );

        // Check if there's a tile at this position
        TileBase groundTile = groundTilemap.GetTile(randomCellPos);
        if (groundTile != null)
        {
          // Convert cell position to world position
          Vector3 worldPos = groundTilemap.CellToWorld(randomCellPos);

          // Add small random offset within the cell
          worldPos += new Vector3(
            Random.Range(-0.4f, 0.4f),
            Random.Range(-0.4f, 0.4f),
            0f
          );

          // Check if this position doesn't overlap with wall tiles
          if (!IsPositionOnWallTile(worldPos))
          {
            if (enableDebugLogs)
              Debug.Log($"SpawnManager: Found fallback tilemap position at {worldPos}");
            return worldPos;
          }
        }
      }
    }

    if (enableDebugLogs)
      Debug.Log("SpawnManager: Fallback tilemap position generation failed");

    return Vector3.zero;
  }
  #endregion
}
