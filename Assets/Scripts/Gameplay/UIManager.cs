using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton UI Manager that handles canvas references across multiple scenes
/// Follows Single Responsibility Principle by focusing only on UI management
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }
    #endregion

    #region Canvas References
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject pauseMenuCanvas;
    #endregion

    #region Properties
    public GameObject VictoryCanvas => victoryCanvas;
    public GameObject GameOverCanvas => gameOverCanvas;
    public GameObject PauseMenuCanvas => pauseMenuCanvas;
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
        pauseMenuCanvas = FindCanvasInScenes("Canvas - Pause Menu");
        LogCanvasStatus();
    }
    #endregion

    #region Canvas Control
    /// <summary>
    /// Show Victory Canvas
    /// </summary>
    public void ShowVictoryCanvas()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
            Debug.Log("Victory Canvas shown");
        }
        else
        {
            Debug.LogWarning("Victory Canvas not found - cannot show");
        }
    }

    /// <summary>
    /// Hide Victory Canvas
    /// </summary>
    public void HideVictoryCanvas()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(false);
            Debug.Log("Victory Canvas hidden");
        }
    }

    /// <summary>
    /// Show Game Over Canvas
    /// </summary>
    public void ShowGameOverCanvas()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            Debug.Log("Game Over Canvas shown");
        }
        else
        {
            Debug.LogWarning("Game Over Canvas not found - cannot show");
        }
    }

    /// <summary>
    /// Hide Game Over Canvas
    /// </summary>
    public void HideGameOverCanvas()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
            Debug.Log("Game Over Canvas hidden");
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
            case "pause":
                return pauseMenuCanvas != null;
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
                  $"\nGame Over Canvas: {(gameOverCanvas != null ? "Found" : "Not Found")}" +
                  $"\nPause Menu Canvas: {(pauseMenuCanvas != null ? "Found" : "Not Found")}");
    }
    #endregion
}