using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;
using Michsky.UI.Reach;

public enum GameStateType
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory,
    Loading
}

/// <summary>
/// Manages the in-game state for individual scenes (Playing/Paused only)
/// Coordinates with PauseMenuManager from persistent scene and controls spawning
/// </summary>
public class GameState : MonoBehaviour
{
    #region Editor Data
    [Header("Scene Type")]
    [SerializeField] private bool isInGameScene = true; // Set to true for in-game scenes
    [SerializeField] private bool startInPlayingState = true; // Auto-start gameplay

    [Header("Game State")]
    [SerializeField] private GameStateType currentState = GameStateType.Playing;
    [SerializeField] private GameStateType previousState = GameStateType.Playing;

    [Header("Game Configuration")]
    [SerializeField] private float gameTimer = 0f;
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool gameInProgress = false;

    [Header("References")]
    [SerializeField] private PlayerCore playerCore;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private Camera gameCamera;

    [Header("External References (From Persistent Scene)")]
    [SerializeField] private Michsky.UI.Reach.PauseMenuManager pauseMenuManager;

    [Header("Game Settings")]
    [SerializeField] private float gameOverDelay = 2f;
    [SerializeField] private float victoryDelay = 3f;
    [SerializeField] private bool allowPause = true;

    [Header("Victory Conditions")]
    [SerializeField] private int victoryLevel = 10;
    [SerializeField] private float victoryScore = 10000f;
    [SerializeField] private float victoryTime = 300f; // This time is for survival mode, where player must survive for a certain duration
    [SerializeField] private float loseTime = 300f; // This time is for timing out the player, where player must complete the level before this time
    public float VictoryTime => victoryTime;

    [Header("Events")]
    public UnityEvent<GameStateType> OnStateChanged;
    public UnityEvent OnGameStart;
    public UnityEvent OnGamePause;
    public UnityEvent OnGameResume;
    public UnityEvent OnGameOver;
    public UnityEvent OnVictory;
    public UnityEvent<float> OnGameTimerUpdate;
    #endregion

    #region Internal Data
    private GameController gameController; // Reference to GameController for game management
    private float originalMusicVolume = 1f; // Store original music volume for resuming
    #endregion

    // Properties
    public GameStateType CurrentState => currentState;
    public GameStateType PreviousState => previousState;
    public float GameTimer => gameTimer;
    public bool IsPaused => isPaused;
    public bool GameInProgress => gameInProgress;
    public bool IsPlaying => currentState == GameStateType.Playing && !isPaused;

    // Static instance for easy access (only for in-game scenes)
    public static GameState Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        // Only use singleton for in-game scenes to avoid conflicts with persistent scene
        if (isInGameScene)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                // If another instance exists, destroy this one (likely from persistent scene)
                Debug.LogWarning("Multiple GameState instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
        }

        InitializeReferences();
    }
    private void Start()
    {
        InitializeGameReferences_();
        InitializeGameState();
        SubscribeToEvents();

        // Initialize GUIManager when GameState starts
        if (GUIManager.Instance != null)
        {
        }

        if (UIManagerAudio.instance != null)
        {
            // Store original music volume for resuming later
            originalMusicVolume = UIManagerAudio.instance.GetMusicVolume() / 100f;

            string forcedMusicName = playerCore?.DataManager?.LevelConfig?.musicName;
            if (!string.IsNullOrEmpty(forcedMusicName))
            {
                // Play specific music for the level if defined
                UIManagerAudio.instance.PlayMusic(forcedMusicName);
            }
            else
            {
                // Otherwise play default gameplay music
                UIManagerAudio.instance.PlayMusicInCategory(MusicLibrary.MusicCategory.Custom, "Gameplay");
            }
        }
    }

    private void Update()
    {
        if (gameInProgress && !isPaused)
        {
            UpdateGameTimer();
            CheckVictoryConditions();
        }

        HandleInput();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        // Reset volume back to original when GameState is destroyed
        if (UIManagerAudio.instance != null)
        {
            UIManagerAudio.instance.SetMusicVolume(originalMusicVolume);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Initialization
    private void InitializeReferences()
    {
        // Find references if not assigned
        if (playerCore == null)
            playerCore = FindObjectOfType<PlayerCore>();

        if (spawnManager == null)
            spawnManager = FindObjectOfType<SpawnManager>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (pauseMenuManager == null)
            pauseMenuManager = FindObjectOfType<Michsky.UI.Reach.PauseMenuManager>();
    }

    private void InitializeGameState()
    {
        // For in-game scenes, start directly in Playing state
        if (isInGameScene && startInPlayingState)
        {
            // Apply shop effects if available
            if (ShopManager.Instance != null)
            {
                Debug.Log("Applying shop effects at game start");

                int lifeBoost = ShopManager.Instance.GetTotalLifeBoost();
                if (lifeBoost > 0)
                {
                    playerCore?.DataManager?.AddLives(lifeBoost);
                }

                float scoreMultiplier = ShopManager.Instance.GetTotalScoreMultiplier();
                if (scoreMultiplier > 1f)
                {
                    if (playerCore != null)
                    {
                        playerCore.DataManager.scoreBoost = scoreMultiplier;
                    }
                }

                // Add onVictory listeners to GameController
                OnVictory.AddListener(ShopManager.Instance.ResetPurchasedItems);
                // OnGameOver.AddListener(ShopManager.Instance.ResetPurchasedItems);
            }

            gameController?.StartGame();
            ChangeState(GameStateType.Playing);
        }
        else
        {
            // Set initial state for non-game scenes
            ChangeState(GameStateType.MainMenu);
        }
    }

    private void InitializeGameReferences_()
    {
        // Find GameController in the scene
        gameController = FindObjectOfType<GameController>();
        if (gameController == null)
        {
            Debug.LogError("GameState: GameController not found in the scene!");
        }
    }

    private void SubscribeToEvents()
    {
        if (playerCore != null)
        {
            playerCore.OnPlayerDeath.AddListener(OnPlayerDeath);
            playerCore.OnPlayerSpawn.AddListener(OnPlayerSpawn);
        }

        // Subscribe to PauseMenuManager events
        if (pauseMenuManager != null)
        {
            pauseMenuManager.onOpen.AddListener(OnPauseMenuOpened);
            pauseMenuManager.onClose.AddListener(OnPauseMenuClosed);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerCore != null)
        {
            playerCore.OnPlayerDeath.RemoveListener(OnPlayerDeath);
            playerCore.OnPlayerSpawn.RemoveListener(OnPlayerSpawn);
        }

        // Unsubscribe from PauseMenuManager events
        if (pauseMenuManager != null)
        {
            pauseMenuManager.onOpen.RemoveListener(OnPauseMenuOpened);
            pauseMenuManager.onClose.RemoveListener(OnPauseMenuClosed);
        }
    }
    #endregion

    #region State Management
    public void ChangeState(GameStateType newState)
    {
        if (newState == currentState) return;

        previousState = currentState;
        currentState = newState;

        HandleStateChange();
        OnStateChanged?.Invoke(currentState);
    }

    private void HandleStateChange()
    {
        switch (currentState)
        {
            case GameStateType.MainMenu:
                HandleMainMenuState();
                break;

            case GameStateType.Playing:
                HandlePlayingState();
                break;

            case GameStateType.Paused:
                HandlePausedState();
                break;

            case GameStateType.GameOver:
                HandleGameOverState();
                break;

            case GameStateType.Victory:
                HandleVictoryState();
                break;

            case GameStateType.Loading:
                HandleLoadingState();
                break;
        }
    }
    private void HandleMainMenuState()
    {
        gameInProgress = false;
        isPaused = false;
        gameTimer = 0f;

        // Disable game systems
        if (spawnManager != null)
            spawnManager.StopSpawning();

        Time.timeScale = 1f;

        // Hide all UI canvases
        if (GUIManager.Instance != null)
            GUIManager.Instance.HideAllCanvases();
    }

    private void HandlePlayingState()
    {
        gameInProgress = true;
        isPaused = false;

        // Enable game systems
        if (spawnManager != null)
            spawnManager.StartSpawning();

        Time.timeScale = 1f;

        // Hide all UI canvases when playing
        if (GUIManager.Instance != null)
            GUIManager.Instance.HideAllCanvases();

        // Enable music for gameplay
        if (UIManagerAudio.instance != null)
        {
            // Reset music volume to original when starting gameplay
            UIManagerAudio.instance.SetMusicVolume(originalMusicVolume);
        }

        OnGameStart?.Invoke();
    }
    private void HandlePausedState()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // Tone down music when paused
        if (UIManagerAudio.instance != null)
        {
            UIManagerAudio.instance.SetMusicVolume(originalMusicVolume * 0.5f);
        }

        OnGamePause?.Invoke();
    }
    private void HandleGameOverState()
    {
        gameInProgress = false;
        isPaused = false;

        // Disable game systems
        if (spawnManager != null)
            spawnManager.StopSpawning();

        Time.timeScale = 1f;

        // Play "GameOver" UI sound effect
        if (UIManagerAudio.instance != null)
        {
            UIManagerAudio.instance.StopMusic();
            UIManagerAudio.instance.PlayUISFX("GameOver");
        }

        // Show Game Over Canvas with game statistics
        if (GUIManager.Instance != null)
        {
            float score = playerCore != null ? playerCore.Score : 0f;
            string gameTime = GetFormattedGameTime();
            int enemiesEaten = playerCore != null ? playerCore.DataManager.SessionData.enemiesEaten : 0;

            GUIManager.Instance.ShowGameOverCanvas(score, gameTime, enemiesEaten);
        }

        // GameController game over handling
        if (gameController != null)
            gameController.OnPlayerDied(true);

        OnGameOver?.Invoke();
    }
    private void HandleVictoryState()
    {
        gameInProgress = false;
        isPaused = false;

        // Disable game systems
        if (spawnManager != null)
            spawnManager.StopSpawning();

        Time.timeScale = 1f;

        // Play "GameWin" UI sound effect
        if (UIManagerAudio.instance != null)
        {
            UIManagerAudio.instance.StopMusic();
            UIManagerAudio.instance.PlayUISFX("GameWin");
        }

        // Show Victory Canvas with game statistics
        if (GUIManager.Instance != null)
        {
            float score = playerCore != null ? playerCore.Score : 0f;
            string gameTime = GetFormattedGameTime();
            int enemiesEaten = playerCore != null ? playerCore.DataManager.SessionData.enemiesEaten : 0;

            GUIManager.Instance.ShowVictoryCanvas(score, gameTime, enemiesEaten);
        }

        // GameController victory handling
        if (gameController != null)
        {
            gameController.OnLevelCompleted();
        }

        OnVictory?.Invoke();
    }
    private void HandleLoadingState()
    {
        gameInProgress = false;
        isPaused = false;
        Time.timeScale = 1f;

        // Hide all UI canvases during loading
        if (GUIManager.Instance != null)
            GUIManager.Instance.HideAllCanvases();
    }
    #endregion

    #region Game Control
    public void StartGame()
    {
        Debug.Log("Starting game...");

        // Reset game state
        gameTimer = 0f;

        // Reset player
        if (playerCore != null)
        {
            playerCore.ResetPlayer();
        }

        ChangeState(GameStateType.Playing);
    }

    public void PauseGame()
    {
        if (!allowPause || currentState != GameStateType.Playing) return;

        Debug.Log("Pausing game...");
        ChangeState(GameStateType.Paused);
    }

    public void ResumeGame()
    {
        if (currentState != GameStateType.Paused) return;

        Debug.Log("Resuming game...");
        isPaused = false;
        Time.timeScale = 1f;
        ChangeState(GameStateType.Playing);

        OnGameResume?.Invoke();
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game...");

        // Clear all enemies
        if (spawnManager != null)
            spawnManager.ClearAllEnemies();

        StartGame();
    }

    public void GameOver()
    {
        if (currentState == GameStateType.GameOver) return;

        StartCoroutine(GameOverCoroutine());
    }
    public void Victory()
    {
        if (currentState == GameStateType.Victory) return;

        StartCoroutine(VictoryCoroutine());
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region Game Timer
    private void UpdateGameTimer()
    {
        gameTimer += Time.deltaTime;
        OnGameTimerUpdate?.Invoke(gameTimer);
    }

    public string GetFormattedGameTime()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60f);
        int seconds = Mathf.FloorToInt(gameTimer % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion

    #region Victory Conditions
    private void CheckVictoryConditions()
    {
        if (playerCore == null) return;

        // Don't check victory conditions if it in Victory
        if (currentState == GameStateType.Victory || currentState == GameStateType.GameOver)
        {
            return;
        }

        // Check time-based lose condition (timed level), disabled by setting loseTime to 0
        if (loseTime > 0 && gameTimer >= loseTime)
        {
            GameOver();
            return;
        }

        // Check level-based victory, disabled by setting victoryLevel to 0
        if (victoryLevel > 0 && playerCore.CurrentLevel >= victoryLevel)
        {
            Victory();
            return;
        }

        // Check score-based victory, disabled by setting victoryScore to 0
        if (victoryScore > 0 && playerCore.Score >= victoryScore)
        {
            Victory();
            return;
        }

        // Check time-based victory (survival mode), disabled by setting victoryTime to 0
        if (victoryTime > 0 && gameTimer >= victoryTime)
        {
            Victory();
            return;
        }
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        // Let PauseMenuManager handle pause input if it exists
        // Otherwise use fallback ESC handling
        if (pauseMenuManager == null)
        {
            // Fallback: Pause/Resume with Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameStateType.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameStateType.Paused)
                {
                    ResumeGame();
                }
            }
        }

        // Restart with R key (debug/development)
        if (Input.GetKeyDown(KeyCode.R) && (currentState == GameStateType.GameOver || currentState == GameStateType.Victory))
        {
            RestartGame();
        }
    }
    #endregion

    #region Event Handlers
    private void OnPlayerDeath(PlayerCore player)
    {
        Debug.Log("Player died");

        // Check if player has lives left
        if (player.Lives <= 0)
        {
            GameOver();
        }
    }

    private void OnPlayerSpawn(PlayerCore player)
    {
        //  Debug.Log("Player spawned/respawned");
    }

    // Called when PauseMenuManager opens pause menu
    private void OnPauseMenuOpened()
    {
        Debug.Log("Pause menu opened - switching to Paused state");
        if (currentState == GameStateType.Playing)
        {
            ChangeState(GameStateType.Paused);
        }
    }

    // Called when PauseMenuManager closes pause menu
    private void OnPauseMenuClosed()
    {
        Debug.Log("Pause menu closed - switching to Playing state");
        if (currentState == GameStateType.Paused)
        {
            ChangeState(GameStateType.Playing);
        }
    }
    #endregion

    #region Coroutines
    private IEnumerator GameOverCoroutine()
    {
        yield return new WaitForSeconds(gameOverDelay);
        ChangeState(GameStateType.GameOver);
    }

    private IEnumerator VictoryCoroutine()
    {
        yield return new WaitForSeconds(victoryDelay);
        ChangeState(GameStateType.Victory);
    }
    #endregion

    #region Utility Methods
    public bool CanPlayerMove()
    {
        return IsPlaying;
    }

    public bool CanEnemiesSpawn()
    {
        return IsPlaying;
    }

    public bool CanPlayerTakeDamage()
    {
        return IsPlaying;
    }

    public void SetVictoryConditions(int level = -1, float score = -1, float time = -1)
    {
        if (level > 0) victoryLevel = level;
        if (score > 0) victoryScore = score;
        if (time > 0) victoryTime = time;
    }
    #endregion

    #region Debug
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintState()
    {
        Debug.Log($"GameState Debug:" +
                  $"\nCurrent State: {currentState}" +
                  $"\nPrevious State: {previousState}" +
                  $"\nGame Timer: {gameTimer:F2}" +
                  $"\nIs Paused: {isPaused}" +
                  $"\nGame In Progress: {gameInProgress}" +
                  $"\nPlayer Level: {(playerCore != null ? playerCore.CurrentLevel : 0)}" + $"\nPlayer Score: {(playerCore != null ? playerCore.Score : 0)}");
    }
    #endregion
}
