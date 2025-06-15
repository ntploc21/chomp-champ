using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
  [Header("Tab Buttons")]
  public Button audioTabButton;
  public Button videoTabButton;
  public Button keybindingsTabButton;

  [Header("Content Panels")]
  public GameObject audioPanel;
  public GameObject videoPanel;
  public GameObject keybindingsPanel;

  [Header("Navigation")]
  public Button backButton;
  public Button creditsButton;
  public GameObject creditsOverlay;

  // private AudioSettingsController audioController;
  // private VideoSettingsController videoController;
  // private KeybindingsController keybindingsController;

  void Start()
  {
    // Get references to controllers
    // audioController = FindObjectOfType<AudioSettingsController>();
    // videoController = FindObjectOfType<VideoSettingsController>();
    // keybindingsController = FindObjectOfType<KeybindingsController>();

    // Setup button events
    audioTabButton.onClick.AddListener(() => ShowTab("Audio"));
    videoTabButton.onClick.AddListener(() => ShowTab("Video"));
    keybindingsTabButton.onClick.AddListener(() => ShowTab("Keybindings"));

    backButton.onClick.AddListener(OnBackClicked);
    creditsButton.onClick.AddListener(OnCreditsClicked);

    // Load all settings
    LoadAllSettings();

    // Show audio tab by default
    ShowTab("Audio");
  }

  void ShowTab(string tabName)
  {
    // Hide all panels
    audioPanel.SetActive(false);
    videoPanel.SetActive(false);
    keybindingsPanel.SetActive(false);

    // Show selected panel
    switch (tabName)
    {
      case "Audio":
        audioPanel.SetActive(true);
        break;
      case "Video":
        videoPanel.SetActive(true);
        break;
      case "Keybindings":
        keybindingsPanel.SetActive(true);
        break;
    }
  }

  void LoadAllSettings()
  {
    // if (audioController != null) audioController.LoadSettings();
    // if (videoController != null) videoController.LoadSettings();
    // if (keybindingsController != null) keybindingsController.LoadSettings();
  }

  void OnBackClicked()
  {
    // Save all settings before leaving
    SaveAllSettings();
    SceneManager.LoadScene("MainMenu");
  }

  void OnCreditsClicked()
  {
    creditsOverlay.SetActive(!creditsOverlay.activeSelf);
  }

  void SaveAllSettings()
  {
    // if (audioController != null) audioController.SaveSettings();
    // if (videoController != null) videoController.SaveSettings();
    // if (keybindingsController != null) keybindingsController.SaveSettings();
  }
}
