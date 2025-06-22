using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Michsky.UI.Reach
{
    public class SceneManager : MonoBehaviour
    {
        // Content
        public List<SceneItem> scenes = new List<SceneItem>();

        // Settings
        public int currentSceneIndex = 0;
        private int newSceneIndex;
        public bool useLoadingScreen = true;
        [SerializeField] private bool initializeButtons = true;
        [SerializeField] private bool useCooldownForHotkeys = false;
        [SerializeField] private bool bypassLoadingOnEnable = false;
        [SerializeField] private UpdateMode updateMode = UpdateMode.UnscaledTime;
        [SerializeField] private SceneMode sceneMode = SceneMode.Single;
        [Range(0.75f, 2)] public float transitionSpeed = 1;

        // Loading Screen
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private string loadingAnimationIn = "LoadingScreen_In";
        [SerializeField] private string loadingAnimationOut = "LoadingScreen_Out";
        private GameObject currentLoadingScreen;

        // Events
        [System.Serializable] public class SceneChangeCallback : UnityEvent<int> { }
        public SceneChangeCallback onSceneChanged;
        public UnityEvent onSceneLoadStart;
        public UnityEvent onSceneLoadComplete;

        // Helpers
        string animSpeedKey = "AnimSpeed";
        bool isInitialized = false;
        public float cachedStateLength = 1;
        [HideInInspector] public int managerIndex;

        public enum SceneMode { Single, Additive }
        public enum UpdateMode { DeltaTime, UnscaledTime }

        [System.Serializable]
        public class SceneItem
        {
            [Tooltip("[Required] This is the variable that you use to call specific scenes.")]
            public string sceneName = "My Scene";
            [Tooltip("[Required] Scene name to load (must be in Build Settings).")]
            public string sceneToLoad = "SceneName";
            [Tooltip("[Optional] If you want the scene manager to have button capability, you can assign a scene button here.")]
            public SceneButton sceneButton;
            [Tooltip("[Optional] Alternate scene button variable that supports standard buttons instead of scene buttons.")]
            public ButtonManager altSceneButton;
            [Tooltip("[Optional] Custom loading screen for this specific scene.")]
            public GameObject customLoadingScreen;
            [Tooltip("[Optional] Scene-specific settings or data to pass.")]
            public SceneData sceneData;
            [HideInInspector] public GameObject latestSelected;
            [HideInInspector] public HotkeyEvent[] hotkeys;
        }

        [System.Serializable]
        public class SceneData
        {
            [Tooltip("Additional data to pass to the scene.")]
            public string additionalData = "";
            [Tooltip("Whether to reset player progress when loading this scene.")]
            public bool resetProgress = false;
            [Tooltip("Custom transition duration for this scene.")]
            public float customTransitionDuration = -1;
        }

        void Awake()
        {
            if (scenes.Count == 0)
                return;

            if (loadingScreenPrefab != null)
            {
                cachedStateLength = ReachUIInternalTools.GetAnimatorClipLength(loadingScreenPrefab.GetComponent<Animator>(), loadingAnimationIn);
            }

            if (ControllerManager.instance != null)
            {
                managerIndex = ControllerManager.instance.sceneManagers.Count;
                ControllerManager.instance.sceneManagers.Add(this);
            }
        }

        void OnEnable()
        {
            if (!isInitialized) { InitializeScenes(); }
            if (ControllerManager.instance != null) { ControllerManager.instance.currentSceneManagerIndex = managerIndex; }
        }

        public void InitializeScenes()
        {
            if (scenes.Count == 0) { return; }

            // Set current scene index based on active scene
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].sceneToLoad == activeSceneName)
                {
                    currentSceneIndex = i;
                    break;
                }
            }

            if (scenes[currentSceneIndex].sceneButton != null)
            {
                scenes[currentSceneIndex].sceneButton.SetSelected(true);
            }

            onSceneChanged.Invoke(currentSceneIndex);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (initializeButtons)
                {
                    string tempName = scenes[i].sceneName;
                    if (scenes[i].sceneButton != null) { scenes[i].sceneButton.onClick.AddListener(() => LoadScene(tempName)); }
                    if (scenes[i].altSceneButton != null) { scenes[i].altSceneButton.onClick.AddListener(() => LoadScene(tempName)); }
                }
            }

            isInitialized = true;
        }

        public void LoadFirstScene()
        { 
            LoadSceneByIndex(0); 
        }

        public void LoadScene(string newScene)
        {
            bool catchedScene = false;

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].sceneName == newScene)
                {
                    newSceneIndex = i;
                    catchedScene = true;
                    break;
                }
            }

            if (!catchedScene)
            {
                Debug.LogWarning("There is no scene named '" + newScene + "' in the scene list.", this);
                return;
            }

            if (newSceneIndex != currentSceneIndex)
            {
                StartCoroutine(LoadSceneCoroutine(newSceneIndex));
            }
        }

        public void LoadSceneByIndex(int sceneIndex)
        {
            if (sceneIndex > scenes.Count || sceneIndex < 0)
            {
                Debug.LogWarning("Index '" + sceneIndex.ToString() + "' doesn't exist.", this);
                return;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].sceneName == scenes[sceneIndex].sceneName)
                {
                    LoadScene(scenes[sceneIndex].sceneName);
                    break;
                }
            }
        }

        public void LoadSceneByName(string sceneName)
        {
            // Direct scene loading by Unity scene name
            StartCoroutine(LoadSceneByNameCoroutine(sceneName));
        }

        public void NextScene()
        {
            if (currentSceneIndex <= scenes.Count - 2)
            {
                LoadSceneByIndex(currentSceneIndex + 1);
            }
        }

        public void PreviousScene()
        {
            if (currentSceneIndex >= 1)
            {
                LoadSceneByIndex(currentSceneIndex - 1);
            }
        }

        public void ReloadCurrentScene()
        {
            StartCoroutine(LoadSceneCoroutine(currentSceneIndex));
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // Add this flag to block input during transitions
        public bool isTransitioning = false;

        IEnumerator LoadSceneCoroutine(int sceneIndex)
        {
            isTransitioning = true;
            onSceneLoadStart.Invoke();

            // Show loading screen if enabled
            if (useLoadingScreen && !bypassLoadingOnEnable)
            {
                GameObject loadingScreenToUse = scenes[sceneIndex].customLoadingScreen != null ? 
                    scenes[sceneIndex].customLoadingScreen : loadingScreenPrefab;

                if (loadingScreenToUse != null)
                {
                    currentLoadingScreen = Instantiate(loadingScreenToUse);
                    Animator loadingAnimator = currentLoadingScreen.GetComponent<Animator>();
                    
                    if (loadingAnimator != null)
                    {
                        loadingAnimator.SetFloat(animSpeedKey, transitionSpeed);
                        loadingAnimator.Play(loadingAnimationIn);
                        
                        float transitionDuration = scenes[sceneIndex].sceneData != null && 
                            scenes[sceneIndex].sceneData.customTransitionDuration > 0 ? 
                            scenes[sceneIndex].sceneData.customTransitionDuration : cachedStateLength * transitionSpeed;

                        if (updateMode == UpdateMode.UnscaledTime) 
                        { 
                            yield return new WaitForSecondsRealtime(transitionDuration); 
                        }
                        else 
                        { 
                            yield return new WaitForSeconds(transitionDuration); 
                        }
                    }
                }
            }

            // Load the scene
            if (sceneMode == SceneMode.Single)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(scenes[sceneIndex].sceneToLoad);
            }
            else if (sceneMode == SceneMode.Additive)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(scenes[sceneIndex].sceneToLoad, LoadSceneMode.Additive);
            }

            // Update current scene index
            currentSceneIndex = sceneIndex;

            // Wait for scene to load
            yield return new WaitForEndOfFrame();

            // Hide loading screen
            if (currentLoadingScreen != null)
            {
                Animator loadingAnimator = currentLoadingScreen.GetComponent<Animator>();
                if (loadingAnimator != null)
                {
                    loadingAnimator.SetFloat(animSpeedKey, transitionSpeed);
                    loadingAnimator.Play(loadingAnimationOut);
                    
                    float transitionDuration = cachedStateLength * transitionSpeed;
                    if (updateMode == UpdateMode.UnscaledTime) 
                    { 
                        yield return new WaitForSecondsRealtime(transitionDuration); 
                    }
                    else 
                    { 
                        yield return new WaitForSeconds(transitionDuration); 
                    }
                }
                
                Destroy(currentLoadingScreen);
                currentLoadingScreen = null;
            }

            onSceneChanged.Invoke(currentSceneIndex);
            onSceneLoadComplete.Invoke();
            isTransitioning = false;
        }

        IEnumerator LoadSceneByNameCoroutine(string sceneName)
        {
            isTransitioning = true;
            onSceneLoadStart.Invoke();

            // Show loading screen if enabled
            if (useLoadingScreen && !bypassLoadingOnEnable && loadingScreenPrefab != null)
            {
                currentLoadingScreen = Instantiate(loadingScreenPrefab);
                Animator loadingAnimator = currentLoadingScreen.GetComponent<Animator>();
                
                if (loadingAnimator != null)
                {
                    loadingAnimator.SetFloat(animSpeedKey, transitionSpeed);
                    loadingAnimator.Play(loadingAnimationIn);
                    
                    float transitionDuration = cachedStateLength * transitionSpeed;
                    if (updateMode == UpdateMode.UnscaledTime) 
                    { 
                        yield return new WaitForSecondsRealtime(transitionDuration); 
                    }
                    else 
                    { 
                        yield return new WaitForSeconds(transitionDuration); 
                    }
                }
            }

            // Load the scene
            if (sceneMode == SceneMode.Single)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            else if (sceneMode == SceneMode.Additive)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            }

            // Wait for scene to load
            yield return new WaitForEndOfFrame();

            // Hide loading screen
            if (currentLoadingScreen != null)
            {
                Animator loadingAnimator = currentLoadingScreen.GetComponent<Animator>();
                if (loadingAnimator != null)
                {
                    loadingAnimator.SetFloat(animSpeedKey, transitionSpeed);
                    loadingAnimator.Play(loadingAnimationOut);
                    
                    float transitionDuration = cachedStateLength * transitionSpeed;
                    if (updateMode == UpdateMode.UnscaledTime) 
                    { 
                        yield return new WaitForSecondsRealtime(transitionDuration); 
                    }
                    else 
                    { 
                        yield return new WaitForSeconds(transitionDuration); 
                    }
                }
                
                Destroy(currentLoadingScreen);
                currentLoadingScreen = null;
            }

            onSceneLoadComplete.Invoke();
            isTransitioning = false;
        }

        public void AddNewItem()
        {
            SceneItem scene = new SceneItem();

            if (scenes.Count != 0)
            {
                int tempIndex = scenes.Count - 1;
                scene.sceneName = "New Scene " + tempIndex.ToString();
                scene.sceneToLoad = "NewScene" + tempIndex.ToString();
                scene.sceneData = new SceneData();
            }

            scenes.Add(scene);
        }

        public SceneItem GetCurrentScene()
        {
            if (scenes.Count > 0 && currentSceneIndex < scenes.Count)
            {
                return scenes[currentSceneIndex];
            }
            return null;
        }

        public SceneItem GetSceneByName(string sceneName)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].sceneName == sceneName)
                {
                    return scenes[i];
                }
            }
            return null;
        }

        public SceneItem GetSceneByIndex(int index)
        {
            if (index >= 0 && index < scenes.Count)
            {
                return scenes[index];
            }
            return null;
        }
    }
}