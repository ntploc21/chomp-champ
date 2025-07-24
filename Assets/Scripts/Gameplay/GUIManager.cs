using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Singleton UI Manager that handles canvas references across multiple scenes
/// Follows Single Responsibility Principle by focusing only on UI management
/// </summary>
public class GUIManager : MonoBehaviour
{
    #region Singleton
    public static GUIManager Instance { get; private set; }
    #endregion

    #region Canvas References
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private GameObject gameOverCanvas;

    [SerializeField] private Michsky.UI.Reach.HUDManager hudManager; // Reference to HUDManager for updating UI elements

    // Current level tracking for replay functionality
    private string currentLevelScene = "";
    #endregion

    #region Internal Data
    private GameDataManager gameDataManager;
    #endregion

    #region Properties
    public GameObject VictoryCanvas => victoryCanvas;
    public GameObject GameOverCanvas => gameOverCanvas;
    public GameDataManager GameDataManager => gameDataManager;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCanvasReferences();
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple UIManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindGameDataManagerInScenes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initialize UI Manager and find all canvas references
    /// Call this when scenes are loaded
    /// </summary>
    public void InitializeCanvasReferences()
    {
        RefreshCanvasReferences();
        Debug.Log("UIManager initialized and canvas references refreshed");
    }

    /// <summary>
    /// Refresh all canvas references - useful when scenes are loaded/unloaded
    /// </summary>
    public void RefreshCanvasReferences()
    {
        victoryCanvas = FindCanvasInScenes("Canvas - Victory");
        gameOverCanvas = FindCanvasInScenes("Canvas - Game Over");
        LogCanvasStatus();
    }

    /// <summary>
    /// Find GameDataManager in any loaded scene
    /// This is used to subscribe to game data changes
    /// </summary>
    public void FindGameDataManagerInScenes()
    {
        gameDataManager = FindObjectOfType<GameDataManager>();
        if (gameDataManager != null)
        {
            Debug.Log("GameDataManager found in scene");
            hudManager.SubscribeToEvents();
        }
        else
        {
            Debug.LogWarning("GameDataManager not found in any loaded scene");
        }
    }
    #endregion

    #region Canvas Control
    /// <summary>
    /// Show Victory Canvas with game statistics
    /// </summary>
    /// <param name="score">Final score achieved</param>
    /// <param name="gameTime">Formatted game time string</param>
    /// <param name="enemiesEaten">Number of enemies eaten</param>
    public void ShowVictoryCanvas(float score = 0, string gameTime = "00:00", int enemiesEaten = 0)
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);

            // Freeze the game (like pause menu does)
            Time.timeScale = 0f;

            // Enable cursor for UI interaction (like pause menu does)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Track current level for replay functionality
            TrackCurrentLevel();

            // Update text elements with game statistics
            UpdateCanvasText(victoryCanvas, score, gameTime, enemiesEaten);

            // Debug.Log($"Victory Canvas shown with stats - Score: {score}, Time: {gameTime}, Enemies: {enemiesEaten} (Game Frozen)");
        }
        else
        {
            Debug.LogWarning("Victory Canvas not found - cannot show");
        }
    }

    /// <summary>
    /// Show Victory Canvas (backward compatibility - no statistics)
    /// </summary>
    public void ShowVictoryCanvas()
    {
        ShowVictoryCanvas(0f, "00:00", 0);
    }

    /// <summary>
    /// Hide Victory Canvas
    /// </summary>
    public void HideVictoryCanvas()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);

            // Resume the game (like pause menu does)
            Time.timeScale = 1f;

            // Restore game cursor state (locked and hidden for gameplay)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Victory Canvas hidden (Game Resumed)");
        }
    }
    /// <summary>
    /// Show Game Over Canvas with game statistics
    /// </summary>
    /// <param name="score">Final score achieved</param>
    /// <param name="gameTime">Formatted game time string</param>
    /// <param name="enemiesEaten">Number of enemies eaten</param>
    public void ShowGameOverCanvas(float score = 0, string gameTime = "00:00", int enemiesEaten = 0)
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);

            // Freeze the game (like pause menu does)
            Time.timeScale = 0f;

            // Enable cursor for UI interaction (like pause menu does)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Track current level for replay functionality
            TrackCurrentLevel();

            // Update text elements with game statistics
            UpdateCanvasText(gameOverCanvas, score, gameTime, enemiesEaten);

            Debug.Log($"Game Over Canvas shown with stats - Score: {score}, Time: {gameTime}, Enemies: {enemiesEaten} (Game Frozen)");
        }
        else
        {
            Debug.LogWarning("Game Over Canvas not found - cannot show");
        }
    }

    /// <summary>
    /// Show Game Over Canvas (backward compatibility - no statistics)
    /// </summary>
    public void ShowGameOverCanvas()
    {
        ShowGameOverCanvas(0f, "00:00", 0);
    }
    /// <summary>
    /// Hide Game Over Canvas
    /// </summary>
    public void HideGameOverCanvas()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);

            // Resume the game (like pause menu does)
            Time.timeScale = 1f;

            // Restore game cursor state (locked and hidden for gameplay)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Game Over Canvas hidden (Game Resumed)");
        }
    }

    /// <summary>
    /// Hide all UI canvases
    /// </summary>
    public void HideAllCanvases()
    {
        HideVictoryCanvas();
        HideGameOverCanvas();
    }

    /// <summary>
    /// Replay/Restart the current level
    /// Unloads current level scene and reloads it additively to Persistent Game Scene
    /// </summary>
    public void ReplayCurrentLevel()
    {
        if (string.IsNullOrEmpty(currentLevelScene))
        {
            Debug.LogWarning("No current level tracked for replay. Make sure to call TrackCurrentLevel() first.");
            return;
        }

        Debug.Log($"Replaying current level: {currentLevelScene}");

        // Send reset data first
        gameDataManager.ResetSessionData();

        // Hide all UI canvases first
        HideAllCanvases();

        // Reset time scale before scene transition
        Time.timeScale = 1f;

        // Try multiple scene loading approaches based on what's available
        if (TryReplayWithReachUISceneManager())
        {
            Debug.Log("Replay initiated using Reach UI Scene Manager");
        }
        else if (TryReplayWithLSSManager())
        {
            Debug.Log("Replay initiated using Loading Screen Studio Manager");
        }
        else
        {
            // Fallback to direct Unity scene management
            ReplayWithUnitySceneManager();
            Debug.Log("Replay initiated using Unity Scene Manager fallback");
        }

        // Reset the game data manager reference after replay
        gameDataManager = null;
    }
    #endregion

    #region Canvas Discovery
    /// <summary>
    /// Find a canvas by name across all loaded scenes
    /// </summary>
    /// <param name="canvasName">Name of the canvas to find</param>
    /// <returns>GameObject if found, null otherwise</returns>
    private GameObject FindCanvasInScenes(string canvasName)
    {
        // Check all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            GameObject canvas = FindGameObjectInScene(canvasName, scene);
            if (canvas != null)
            {
                return canvas;
            }
        }

        Debug.LogWarning($"Canvas '{canvasName}' not found in any loaded scene");
        return null;
    }

    /// <summary>
    /// Find a GameObject by name in a specific scene
    /// </summary>
    /// <param name="objectName">Name of the object to find</param>
    /// <param name="scene">Scene to search in</param>
    /// <returns>GameObject if found, null otherwise</returns>
    private GameObject FindGameObjectInScene(string objectName, Scene scene)
    {
        if (!scene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            GameObject found = FindInChildren(rootObject.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Recursively search for a GameObject by name in children
    /// </summary>
    /// <param name="parent">Parent transform to search in</param>
    /// <param name="name">Name to search for</param>
    /// <returns>GameObject if found, null otherwise</returns>
    private GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.name == name)
            return parent.gameObject;

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found = FindInChildren(parent.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
    #endregion

    #region Utility
    /// <summary>
    /// Check if a specific canvas is available
    /// </summary>
    /// <param name="canvasType">Type of canvas to check</param>
    /// <returns>True if canvas is available</returns>
    public bool IsCanvasAvailable(string canvasType)
    {
        switch (canvasType.ToLower())
        {
            case "victory":
                return victoryCanvas != null;
            case "gameover":
                return gameOverCanvas != null;
            default:
                return false;
        }
    }

    /// <summary>
    /// Log the status of all canvas references for debugging
    /// </summary>
    private void LogCanvasStatus()
    {
        Debug.Log($"UIManager Canvas Status:" +
                  $"\nVictory Canvas: {(victoryCanvas != null ? "Found" : "Not Found")}" +
                  $"\nGame Over Canvas: {(gameOverCanvas != null ? "Found" : "Not Found")}");
    }

    /// <summary>
    /// Update text elements in a canvas with game statistics
    /// </summary>
    /// <param name="canvas">The canvas GameObject to update</param>
    /// <param name="score">Final score achieved</param>
    /// <param name="gameTime">Formatted game time string</param>
    /// <param name="enemiesEaten">Number of enemies eaten</param>
    private void UpdateCanvasText(GameObject canvas, float score, string gameTime, int enemiesEaten)
    {
        if (canvas == null) return;

        // Get all TextMeshProUGUI components in the canvas
        TextMeshProUGUI[] textComponents = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI textComp in textComponents)
        {
            if (textComp == null) continue;

            string objectName = textComp.gameObject.name.ToLower();

            // Update score text elements
            if (objectName.Contains("score") && objectName.Contains("text"))
            {
                textComp.text = Mathf.RoundToInt(score).ToString();
                // Debug.Log($"Updated {textComp.gameObject.name} with score: {score}");
            }
            // Update timer/time text elements
            else if ((objectName.Contains("timer") || objectName.Contains("time")) && objectName.Contains("text"))
            {
                textComp.text = gameTime;
                // Debug.Log($"Updated {textComp.gameObject.name} with time: {gameTime}");
            }
            // Update enemy text elements
            else if (objectName.Contains("enemy") && objectName.Contains("text"))
            {
                textComp.text = enemiesEaten.ToString();
                // Debug.Log($"Updated {textComp.gameObject.name} with enemies eaten: {enemiesEaten}");
            }
            // Backup: Try to find text elements with common patterns
            else if (objectName.Contains("final") && objectName.Contains("score"))
            {
                textComp.text = Mathf.RoundToInt(score).ToString();
                // Debug.Log($"Updated {textComp.gameObject.name} with final score: {score}");
            }
            else if (objectName.Contains("play") && objectName.Contains("time"))
            {
                textComp.text = gameTime;
                // Debug.Log($"Updated {textComp.gameObject.name} with play time: {gameTime}");
            }
            else if (objectName.Contains("kills") || objectName.Contains("defeated"))
            {
                textComp.text = enemiesEaten.ToString();
                // Debug.Log($"Updated {textComp.gameObject.name} with enemies defeated: {enemiesEaten}");
            }
        }

        // If no specific text elements were found, log available components for debugging
        // if (textComponents.Length > 0)
        // {
        //     Debug.Log($"Available text components in {canvas.name}:");
        //     foreach (TextMeshProUGUI textComp in textComponents)
        //     {
        //         Debug.Log($"  - {textComp.gameObject.name}: '{textComp.text}'");
        //     }
        // }
    }
    #endregion

    #region Replay System
    /// <summary>
    /// Track the current level scene for replay functionality
    /// Call this when showing Victory/Game Over canvases
    /// </summary>
    private void TrackCurrentLevel()
    {
        // Get all loaded scenes to find the level scene
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            // Skip the persistent scene, look for level scenes
            if (scene.name != "Persistent Game State" && scene.name.Contains("Level"))
            {
                currentLevelScene = scene.name;
                Debug.Log($"Tracked current level for replay: {currentLevelScene}");
                return;
            }
        }

        // Fallback: use active scene if no specific level scene found
        currentLevelScene = SceneManager.GetActiveScene().name;
        Debug.Log($"Fallback: Tracked active scene for replay: {currentLevelScene}");
    }

    /// <summary>
    /// Try to replay using Reach UI Scene Manager
    /// </summary>
    private bool TryReplayWithReachUISceneManager()
    {
        // Look for Reach UI Scene Manager
        Michsky.UI.Reach.SceneManager reachSceneManager = FindObjectOfType<Michsky.UI.Reach.SceneManager>();

        if (reachSceneManager != null)
        {            // Try to find the current level in the scene manager's list
            for (int i = 0; i < reachSceneManager.scenes.Count; i++)
            {
                if (reachSceneManager.scenes[i].sceneToLoad == currentLevelScene)
                {
                    Debug.Log($"Found level in Reach UI Scene Manager, reloading index {i}");
                    reachSceneManager.LoadSceneByIndex(i);

                    // Delay refresh of SpawnManager to allow scene to load
                    StartCoroutine(DelayedSpawnManagerRefresh());

                    return true;
                }
            }

            // If not found in list, try direct scene name loading
            Debug.Log($"Level not found in Reach UI Scene Manager list, trying direct load");
            reachSceneManager.LoadSceneByName(currentLevelScene);

            // Delay refresh of SpawnManager to allow scene to load
            StartCoroutine(DelayedSpawnManagerRefresh());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to replay using Loading Screen Studio Manager
    /// </summary>
    private bool TryReplayWithLSSManager()
    {
        // Look for LSS Manager
        Michsky.LSS.LSS_Manager lssManager = FindObjectOfType<Michsky.LSS.LSS_Manager>();

        if (lssManager != null)
        {
            Debug.Log($"Found LSS Manager, reloading level: {currentLevelScene}");

            // For additive loading (maintaining Persistent Game Scene)
            // First unload the current level, then load it again
            StartCoroutine(ReplayWithLSSCoroutine(lssManager));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Coroutine to handle LSS replay with proper scene unloading/loading
    /// </summary>
    private System.Collections.IEnumerator ReplayWithLSSCoroutine(Michsky.LSS.LSS_Manager lssManager)
    {
        // Unload current level scene
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentLevelScene);
        yield return unloadOp;

        // Small delay to ensure clean unload
        yield return new WaitForSecondsRealtime(0.5f);

        // Load the level scene additively
        lssManager.LoadSceneAdditive(currentLevelScene);

        // Wait a frame for scene to fully load
        yield return new WaitForEndOfFrame();

        // Refresh SpawnManager's level scene reference
        RefreshSpawnManagerLevelScene();

        // Also refresh GameDataManager after loading
        FindGameDataManagerInScenes();
    }

    /// <summary>
    /// Fallback replay using Unity's SceneManager directly
    /// </summary>
    private void ReplayWithUnitySceneManager()
    {
        StartCoroutine(ReplayWithUnityCoroutine());
    }

    /// <summary>
    /// Coroutine to handle Unity SceneManager replay
    /// </summary>
    private System.Collections.IEnumerator ReplayWithUnityCoroutine()
    {
        Debug.Log($"Unloading current level: {currentLevelScene}");

        // Unload current level scene
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentLevelScene);
        yield return unloadOp;

        // Small delay to ensure clean unload
        yield return new WaitForSecondsRealtime(0.1f);

        Debug.Log($"Reloading level additively: {currentLevelScene}");

        // Load the level scene additively to maintain Persistent Game Scene
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(currentLevelScene, LoadSceneMode.Additive);
        yield return loadOp;

        // Wait a frame for scene to fully load
        yield return new WaitForEndOfFrame();

        Debug.Log($"Level {currentLevelScene} reloaded successfully");

        // Refresh SpawnManager's level scene reference
        StartCoroutine(DelayedSpawnManagerRefresh());
    }

    /// <summary>
    /// Get the current level scene name (for external use)
    /// </summary>
    public string GetCurrentLevelScene()
    {
        return currentLevelScene;
    }

    /// <summary>
    /// Manually set the current level scene (for external use)
    /// </summary>
    public void SetCurrentLevelScene(string sceneName)
    {
        currentLevelScene = sceneName;
        Debug.Log($"Manually set current level scene: {currentLevelScene}");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Helper method to refresh SpawnManager's level scene reference
    /// </summary>
    private void RefreshSpawnManagerLevelScene()
    {
        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.RefreshLevelScene();
            Debug.Log($"UIManager: Refreshed SpawnManager level scene to: {spawnManager.GetCurrentLevelScene()}");
        }
        else
        {
            Debug.LogWarning("UIManager: Could not find SpawnManager to refresh level scene");
        }
    }
    #endregion

    #region Delayed Refresh
    /// <summary>
    /// Coroutine to delay SpawnManager refresh after Reach UI scene loading
    /// </summary>
    private System.Collections.IEnumerator DelayedSpawnManagerRefresh()
    {
        var tries = 0;
        var maxTries = 5;

        while (tries < maxTries)
        {
            // Try to find SpawnManager in the scene
            RefreshSpawnManagerLevelScene();

            tries++;
            yield return new WaitForSecondsRealtime(2f);
        }
    }
    #endregion
}