using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    // Header: 5 Buttons
    [Header("Buttons")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newAdventureButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button achievementButton;
    [SerializeField] private Button quitButton;

    void Start()
    {
        // Initialize the main menu buttons
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        // Set the continue button active if there is a saved game
        if (!PlayerPrefs.HasKey("SavedGame"))
        {
            // If no saved game exists, hide the continue button
            continueButton.gameObject.SetActive(false);

            // And make selection panel move up
            selectionPanel.transform.localPosition += new Vector3(0, 80, 0);
        }
        else
        {
            continueButton.gameObject.SetActive(true);
        }

        // Set the new adventure button active
        newAdventureButton.gameObject.SetActive(true);

        // Set the options button active
        optionsButton.gameObject.SetActive(true);

        // Set the achievement button active
        achievementButton.gameObject.SetActive(true);

        // Set the quit button active
        quitButton.gameObject.SetActive(true);
    }

    public void OnContinueButtonClicked()
    {
        // Load the saved game
        if (PlayerPrefs.HasKey("SavedGame"))
        {
            // Load the saved game scene or data
            Debug.Log("Loading saved game...");
            // Add your loading logic here
        }
        else
        {
            Debug.Log("No saved game found.");
        }
    }

    public void OnNewAdventureButtonClicked()
    {
        // Start a new adventure
        Debug.Log("Starting a new adventure...");
        // Add your logic to start a new game here
    }

    public void OnOptionsButtonClicked()
    {
        // Open the options menu
        Debug.Log("Opening options menu...");
        // Add your logic to open options here
    }

    public void OnAchievementButtonClicked()
    {
        // Open the achievements menu
        Debug.Log("Opening achievements menu...");
        // Add your logic to open achievements here
    }

    public void OnQuitButtonClicked()
    {
        // Quit the game
        Debug.Log("Quitting the game...");
        Application.Quit();
    }
}
