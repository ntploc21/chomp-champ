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
    [SerializeField] private List<ChapterManager> chapterManagers = new List<ChapterManager>();
    #endregion

    #region Internal Data
    private int currentChapterIndex = 0;
    private int currentLevelIndex = 0;
    private bool fromGamePlay = false;
    #endregion

    #region Unity Events
    private void Awake()
    {
      if (panelManager == null)
      {
        panelManager = GetComponent<PanelManager>();
      }

      if (chapterManagers.Count == 0)
      {
        chapterManagers.AddRange(GetComponentsInChildren<ChapterManager>());
      }

      Cursor.visible = true;
      Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
      Cursor.visible = true;
      Cursor.lockState = CursorLockMode.None;

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
        foreach (var chapterManager in chapterManagers)
        {
          // Get the chapter index from the chapter manager via getting a gameObject name
          int chapterIndex = int.Parse(chapterManager.gameObject.name.Replace("Chapter", ""));
          if (chapterIndex == currentChapterIndex)
          {
            chapterManager.currentChapterIndex = currentLevelIndex - 1; // Adjust for zero-based index
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