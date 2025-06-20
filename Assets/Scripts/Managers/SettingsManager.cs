using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
  private DefaultInputActions _inputActions;
  public Button lastSelectedButton;

  [Header("Tab Buttons")]
  public Button audioTabButton;
  public Button videoTabButton;
  public Button keybindingsTabButton;

  [Header("Content Panels")]
  public GameObject settingsCanvas;
  public GameObject audioCanvas;
  public GameObject videoCanvas;
  public GameObject keybindingsCanvas;

  [Header("Navigation")]
  public Button backButton;
  public Button creditsButton;
  public GameObject creditsOverlay;

  private AudioSettingsController audioController;
  // private VideoSettingsController videoController;
  // private KeybindingsController keybindingsController;

  void Awake()
  {
    // Initialize Input Actions
    _inputActions = new DefaultInputActions();
  }

  void Start()
  {
    // Play Music
    if (AudioManager.Instance != null)
    {
      AudioManager.Instance.PlayMusic("Settings");
    }
    else
    {
      Debug.LogWarning("AudioManager instance is not available.");
    }

    // Get references to controllers
    audioController = FindObjectOfType<AudioSettingsController>();
    // videoController = FindObjectOfType<VideoSettingsController>();
    // keybindingsController = FindObjectOfType<KeybindingsController>();

    // Initialize Canvases
    CloseAllMenus();
    settingsCanvas.SetActive(true);
    EventSystem.current.SetSelectedGameObject(keybindingsTabButton.gameObject);

    // Load all settings
    LoadAllSettings();
  }

  void OnEnable()
  {
    _inputActions.UI.Enable();
    _inputActions.UI.Cancel.performed += OnCancelPerformed;
  }

  void OnDisable()
  {
    _inputActions.UI.Disable();
    _inputActions.UI.Cancel.performed -= OnCancelPerformed;
  }

  void OnCancelPerformed(InputAction.CallbackContext context)
  {
    OnBackClicked();
  }

  void ShowTab(string tabName)
  {
    // Hide all panels
    CloseAllMenus();

    // Show selected panel
    switch (tabName)
    {
      case "Audio":
        audioCanvas.SetActive(true);
        break;
      case "Video":
        videoCanvas.SetActive(true);
        break;
      case "Keybindings":
        keybindingsCanvas.SetActive(true);
        break;
      case "Settings":
        settingsCanvas.SetActive(true);
        break;
      case "Credits":
        creditsOverlay.SetActive(true);
        break;
      default:
        Debug.LogWarning("Unknown tab name: " + tabName);
        break;
    }
  }


  #region Settings Management

  void LoadAllSettings()
  {
    // if (audioController != null) audioController.LoadSettings();
    // if (videoController != null) videoController.LoadSettings();
    // if (keybindingsController != null) keybindingsController.LoadSettings();
  }

  void SaveAllSettings()
  {
    // if (audioController != null) audioController.SaveSettings();
    // if (videoController != null) videoController.SaveSettings();
    // if (keybindingsController != null) keybindingsController.SaveSettings();
  }

  #endregion

  #region Canvas Activations

  void CloseAllMenus()
  {
    settingsCanvas.SetActive(false);
    audioCanvas.SetActive(false);
    videoCanvas.SetActive(false);
    keybindingsCanvas.SetActive(false);
    creditsOverlay.SetActive(false);
  }

  public void OnAudioTabClicked()
  {
    ShowTab("Audio");
  }

  public void OnVideoTabClicked()
  {
    ShowTab("Video");
  }

  public void OnKeybindingsTabClicked()
  {
    ShowTab("Keybindings");
  }

  public void OnCreditsClicked()
  {
    ShowTab("Credits");
  }

  public void OnBackClicked()
  {
    // Check if on settings canvas then return to main menu
    if (settingsCanvas.activeSelf)
    {
      SceneManager.LoadScene("MainMenu");
    }
    else
    {
      CloseAllMenus();
      settingsCanvas.SetActive(true);

      // Save settings when going back
      SaveAllSettings();
      Debug.Log("Settings saved on back click.");
    }
  }


  #endregion
}
