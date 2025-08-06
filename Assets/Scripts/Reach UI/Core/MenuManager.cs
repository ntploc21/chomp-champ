using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Michsky.UI.Reach
{
    [DisallowMultipleComponent]
    public class MenuManager : MonoBehaviour
    {
        // Resources
        public UIManager UIManagerAsset;
        public Animator splashScreen;
        [SerializeField] private GameObject mainContent;
        [SerializeField] private ImageFading initPanel;

        // Helpers
        float splashInTime;
        float splashOutTime;

        void Awake()
        {
            Time.timeScale = 1;

            if (initPanel != null) { initPanel.gameObject.SetActive(true); }
            if (splashScreen != null) { splashScreen.gameObject.SetActive(false); }
        }

        void Start()
        {
            StartCoroutine("StartInitialize");
        }

        public void DisableSplashScreen()
        {
            StopCoroutine("DisableSplashScreenAnimator");
            StartCoroutine("FinalizeSplashScreen");

            splashScreen.enabled = true;
            splashScreen.Play("Out");
        }

        void Initialize()
        {
            // Initialize the Player Save Data
            PlayerDataManager.Initialize();

            if (UIManagerAsset == null || mainContent == null)
            {
                Debug.LogError("<b>[Reach UI]</b> Cannot initialize the resources due to missing resources.", this);
                return;
            }

            mainContent.gameObject.SetActive(false);
            bool enableSplashAfter = false;

            if (UIManagerAsset.enableSplashScreen && UIManagerAsset.showSplashScreenOnce && GameObject.Find("[Reach UI - Splash Screen Helper]") != null)
            {
                UIManagerAsset.enableSplashScreen = false;
                enableSplashAfter = true;
            }

            if (UIManagerAsset.enableSplashScreen && splashScreen != null && UIManagerAsset.showSplashScreenOnce)
            {
                if (splashScreen == null)
                {
                    Debug.LogError("<b>[Reach UI]</b> Splash Screen is enabled but its resource is missing. Please assign the correct variable for 'Splash Screen'.", this);
                    return;
                }

                // Getting in and out animation length
                AnimationClip[] clips = splashScreen.runtimeAnimatorController.animationClips;
                splashInTime = clips[0].length;
                splashOutTime = clips[1].length;

                splashScreen.enabled = true;
                splashScreen.gameObject.SetActive(true);
                StartCoroutine("DisableSplashScreenAnimator");

                if (UIManagerAsset.showSplashScreenOnce)
                {
                    GameObject tempHelper = new GameObject();
                    tempHelper.name = "[Reach UI - Splash Screen Helper]";
                    DontDestroyOnLoad(tempHelper);
                }
            }

            else
            {
                if (mainContent == null)
                {
                    Debug.LogError("<b>[Reach UI]</b> 'Main Panels' is missing. Please assign the correct variable for 'Main Panels'.", this);
                    return;
                }

                if (splashScreen != null) { splashScreen.gameObject.SetActive(false); }
                mainContent.gameObject.SetActive(false);
                StartCoroutine("FinalizeSplashScreen");
            }

            if (enableSplashAfter && UIManagerAsset.showSplashScreenOnce)
            {
                UIManagerAsset.enableSplashScreen = true;
            }
        }

        IEnumerator StartInitialize()
        {
            yield return new WaitForSeconds(0.5f);
            if (initPanel != null) { initPanel.FadeOut(); }
            Initialize();
        }

        IEnumerator DisableSplashScreenAnimator()
        {
            yield return new WaitForSeconds(splashInTime + 0.1f);
            splashScreen.enabled = false;
        }

        IEnumerator FinalizeSplashScreen()
        {
            yield return new WaitForSeconds(splashOutTime + 0.1f);

            if (UIManagerAsset != null && UIManagerAsset.enableSplashScreen)
            {
                // If splash screen is enabled, we will disable it after the animation
                if (splashScreen == null || splashScreen.gameObject == null)
                {

                }
                else
                {
                    splashScreen.gameObject.SetActive(false);
                }
            }

            mainContent.gameObject.SetActive(true);

            if (ControllerManager.instance != null
             && ControllerManager.instance.gamepadEnabled
             && ControllerManager.instance.firstSelected != null
             && ControllerManager.instance.firstSelected.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(ControllerManager.instance.firstSelected);
            }
        }

        #region Public Methods
        /// <summary>
        /// Reset the player data to default values.
        /// This method is called when starting a new game or resetting the player data.
        /// </summary>
        public void ResetPlayerData()
        {
            // Reset the player data
            PlayerDataManager.DeletePlayerData();

            // Reset the achievements
            AchievementManager.ResetAchievements();

            // Load the Main Menu scene as Refreshing the player data
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }
        #endregion
    }
}