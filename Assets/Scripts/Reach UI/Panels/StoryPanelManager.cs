using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Michsky.UI.Reach
{
  public class StoryPanelManager : MonoBehaviour
  {
    #region Editor Data
    [Header("References")]
    [SerializeField] private PanelManager panelManager = null;
    [SerializeField] private ChapterManager chapterManager = null;
    [SerializeField] private List<ChapterManager> levelManagers = new List<ChapterManager>();
    #endregion

    #region Internal Data
    private int currentChapterIndex = 0;
    private int currentLevelIndex = 0;
    private bool fromGamePlay = false;
    private List<string> unlockedLevels = new List<string>();
    #endregion

    #region Unity Events
    private void Awake()
    {
      if (panelManager == null)
      {
        panelManager = GetComponent<PanelManager>();
      }

      if (levelManagers.Count == 0)
      {
        levelManagers.AddRange(GetComponentsInChildren<ChapterManager>());
      }

      // Get the unlocked chapters and levels
      PlayerDataManager.Initialize(); // This won't be call again if already initialized
      unlockedLevels = PlayerDataManager.GetUnlockedLevels(); // List of Format "CXLY" where X is chapter and Y is level

      Cursor.visible = true;
      Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
      Cursor.visible = true;
      Cursor.lockState = CursorLockMode.None;

      // Initialize levels based on unlocked chapters
      InitializeLevels();

      // Open the respective chapter/level panel after gameplay if applicable
      OpenChapterPanel();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Initializes the levels based on the unlocked chapters.
    /// This method sets up the chapter managers and their respective levels.
    /// </summary>
    private void InitializeLevels()
    {
      // Loop through every chapter panel and lock all chapters
      for (int i = 1; i < chapterManager.chapters.Count; ++i) {
        string chapterID = chapterManager.chapters[i].chapterID;
        ChapterManager.SetLocked(chapterID); // Lock all chapters initially
      }

      // Loop through every chapter/levels panel and lock all levels
      foreach (var levelManager in levelManagers)
      {
        // Loop through all levels in the chapter manager and lock them
        for (int i = 0; i < levelManager.chapters.Count; i++)
        {
          string levelID = levelManager.chapters[i].chapterID;
          ChapterManager.SetLocked(levelID); // Lock all levels initially
        }
      }

      // Loop through the unlocked levels and set the chapter index unlocked
      foreach (var unlockedLevel in unlockedLevels)
      {
        // Extract chapter and level indices from the unlocked level string
        int chapterIndex = int.Parse(unlockedLevel.Substring(1, 1)); // "C0L1" -> 0
        int levelIndex = int.Parse(unlockedLevel.Substring(3, 1)); // "C0L1" -> 1

        // Set the chapter index unlocked
        string chapterID = $"Chapter {chapterIndex}";
        string levelID = $"Chapter {chapterIndex}-{levelIndex}";
        ChapterManager.SetUnlocked(chapterID);
        ChapterManager.SetUnlocked(levelID); // Unlock the specific level

        Debug.Log($"Unlocked Level: {unlockedLevel} (Chapter: {chapterIndex}, Level: {levelIndex})");
      }

      // Initialize the chapter managers with the unlocked levels
      chapterManager.InitializeChapters();
      foreach (var levelManager in levelManagers)
      {
        levelManager.InitializeChapters();
      }
    }

    /// <summary>
    /// Opens the chapter panel based on the current chapter and level indices.
    /// This method initializes the chapter managers and sets the current chapter index.
    /// </summary>
    private void OpenChapterPanel()
    {
      // Get the current chapter and level from PlayerPrefs
      // PlayerPrefs.SetInt("FromGamePlay", 1);
      // PlayerPrefs.SetInt("ReturnChapter", chapter);
      // PlayerPrefs.SetInt("ReturnLevel", level);

      // Load the saved chapter and level
      currentChapterIndex = PlayerPrefs.GetInt("ReturnChapter", 0);
      currentLevelIndex = PlayerPrefs.GetInt("ReturnLevel", 0);
      fromGamePlay = PlayerPrefs.GetInt("FromGamePlay", 1) == 1;
      PlayerPrefs.SetInt("FromGamePlay", 0); // Reset the flag after reading
      PlayerPrefs.Save();

      if (!fromGamePlay)
      {
        return; // If not returning from gameplay, do not initialize
      }

      Debug.Log($"Returning to Chapter: {currentChapterIndex}, Level: {currentLevelIndex}");

      // Initialize the chapter managers
      panelManager.currentPanelIndex = currentChapterIndex;

      if (currentLevelIndex > 0)
      {
        foreach (var levelManager in levelManagers)
        {
          // Get the chapter index from the chapter manager via getting a gameObject name
          int chapterIndex = int.Parse(levelManager.gameObject.name.Replace("Chapter", ""));
          if (chapterIndex == currentChapterIndex)
          {
            levelManager.currentChapterIndex = currentLevelIndex - 1; // Adjust for zero-based index
          }
        }
      }

      // Open the chapter panel
      if (currentChapterIndex >= 0 && currentLevelIndex > 0)
      {
        panelManager.OpenPanel($"Chapter {currentChapterIndex}");
      }
    }
    #endregion
  }
}