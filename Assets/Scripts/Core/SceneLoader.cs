// Scripts/Core/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;
    public static SceneLoader Instance
    {
        get
        {
            Debug.Log("SceneLoader Instance accessed");
            if (instance == null)
            {
                Debug.Log("Creating new SceneLoader instance");
                instance = FindObjectOfType<SceneLoader>();
                if (instance == null)
                {
                    Debug.Log("No existing SceneLoader found, creating a new one");
                    GameObject obj = new GameObject("SceneLoader");
                    instance = obj.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    [Header("Loading Settings")]
    public GameObject loadingScreen;
    public UnityEngine.UI.Slider progressBar;
    public TMPro.TextMeshProUGUI loadingText;

    private bool isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
    }

    public void LoadSceneWithDelay(string sceneName, float delay)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneWithDelayCoroutine(sceneName, delay));
        }
    }

    IEnumerator LoadSceneWithDelayCoroutine(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName, bool showLoadingScreen = false)
    {
        isLoading = true;

        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
    
        // Start loading the scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Update progress
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.Round(progress * 100)}%";
            }

            // Scene is ready, activate it
            if (operation.progress >= 0.9f)
            {
                // Optional: Add minimum loading time here if desired
                // yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }

        isLoading = false;
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Save data before quitting
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveAllData();
        }

        // Quit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
#else
        Application.Quit(); // Quit the application
#endif
    }
}
