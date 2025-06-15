using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapManager : MonoBehaviour
{
  [Header("Loading Settings")]
  public float minimumLoadTime = 2f;
  public string nextSceneName = "MainMenu";

  [Header("UI References")]
  public GameObject loadingPanel;
  public UnityEngine.UI.Slider progressBar;
  public TMPro.TextMeshProUGUI loadingText;

  private float loadStartTime;
  private bool managersInitialized = false;

  void Start()
  {
    loadStartTime = Time.time;
    loadingPanel.SetActive(true);
    StartCoroutine(InitializeGame());
  }

  IEnumerator InitializeGame()
  {
    // Phase 1: Initialize Core Managers
    UpdateLoadingUI("Initializing Game Systems...", 0.1f);
    yield return StartCoroutine(InitializeManagers());

    // Phase 2: Load Settings and Data
    UpdateLoadingUI("Loading Settings...", 0.3f);
    yield return StartCoroutine(LoadGameData());

    // Phase 3: Initialize Audio System
    UpdateLoadingUI("Setting up Audio...", 0.5f);
    yield return StartCoroutine(InitializeAudio());

    // Phase 4: Preload Essential Assets
    UpdateLoadingUI("Loading Assets...", 0.7f);
    yield return StartCoroutine(PreloadAssets());

    // Phase 5: Final Setup
    UpdateLoadingUI("Finalizing...", 0.9f);
    yield return StartCoroutine(FinalizeSetup());

    // Ensure minimum load time
    float elapsedTime = Time.time - loadStartTime;
    if (elapsedTime < minimumLoadTime)
    {
      yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
    }

    // Complete loading
    UpdateLoadingUI("Complete!", 1.0f);
    yield return new WaitForSeconds(0.5f);

    // Transition to main menu
    SceneLoader.Instance.LoadScene(nextSceneName);
  }

  IEnumerator InitializeManagers()
  {
    // GameManager should initialize first
    if (GameManager.Instance == null)
    {
      Debug.LogError("GameManager not found in Bootstrap scene!");
    }

    // Wait for managers to be ready
    while (!GameManager.Instance.IsInitialized)
    {
      yield return null;
    }

    managersInitialized = true;
  }

  IEnumerator LoadGameData()
  {
    // Load settings, save data, player progress
    GameManager.Instance.LoadAllData();
    yield return new WaitForSeconds(0.2f); // Simulate load time
  }

  IEnumerator InitializeAudio()
  {
    AudioManager.Instance.Initialize();
    yield return new WaitForSeconds(0.3f);
  }

  IEnumerator PreloadAssets()
  {
    // Preload critical assets, UI sprites, etc.
    yield return new WaitForSeconds(0.4f);
  }

  IEnumerator FinalizeSetup()
  {
    // Apply loaded settings
    GameManager.Instance.ApplySettings();
    yield return new WaitForSeconds(0.2f);
  }

  void UpdateLoadingUI(string text, float progress)
  {
    if (loadingText != null) loadingText.text = text;
    if (progressBar != null) progressBar.value = progress;
  }
}
