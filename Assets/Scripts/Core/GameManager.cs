using UnityEngine;

public class GameManager : MonoBehaviour
{
  private static GameManager instance;
  public static GameManager Instance
  {
    get
    {
      Debug.Log("GameManager Instance accessed");
      if (instance == null)
      {
        Debug.Log("Creating new GameManager instance");
        instance = FindObjectOfType<GameManager>();
        if (instance == null)
        {
          Debug.Log("No existing GameManager found, creating a new one");
          GameObject obj = new GameObject("GameManager");
          instance = obj.AddComponent<GameManager>();
          DontDestroyOnLoad(obj);
        }
      }
      return instance;
    }
  }

  [Header("Game State")]
  [SerializeField]
  private bool isInitialized = false;
  public bool IsInitialized { get { return isInitialized; } private set { isInitialized = value; } }

  [Header("Player Data")]
  public PlayerData currentPlayerData;

  [Header("Game Settings")]
  public GameSettings gameSettings;

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
      return;
    }

    // Initialize the game manager
    Initialize();
  }

  void Initialize()
  {
    // Initialize core systems
    InitializePlayerData();
    InitializeGameSettings();
    IsInitialized = true;
    Debug.Log("GameManager initialized successfully");
  }

  void InitializePlayerData()
  {
    currentPlayerData = new PlayerData();
  }

  void InitializeGameSettings()
  {
    gameSettings = new GameSettings();
  }

  public void LoadAllData()
  {
    LoadPlayerData();
    LoadSettings();
    LoadProgress();
  }

  void LoadPlayerData()
  {
    // Load player stats, inventory, progress
    currentPlayerData.LoadFromPrefs();
  }

  void LoadSettings()
  {
    // Load audio, video, control settings
    gameSettings.LoadFromPrefs();
  }

  void LoadProgress()
  {
    // Load level progress, achievements
    // Implementation depends on your save system
  }

  public void ApplySettings()
  {
    // Apply audio settings
    AudioManager.Instance.SetMasterVolume(gameSettings.masterVolume);
    AudioManager.Instance.SetMusicVolume(gameSettings.musicVolume);
    AudioManager.Instance.SetSFXVolume(gameSettings.sfxVolume);

    // Apply video settings
    Application.targetFrameRate = gameSettings.targetFPS;
    Screen.fullScreen = gameSettings.fullscreen;
    QualitySettings.SetQualityLevel(gameSettings.qualityLevel);
  }

  public void SaveAllData()
  {
    currentPlayerData.SaveToPrefs();
    gameSettings.SaveToPrefs();
    PlayerPrefs.Save();
  }

  void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus) SaveAllData();
  }

  void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus) SaveAllData();
  }
}

[System.Serializable]
public class PlayerData
{
  public int level = 1;
  public int experience = 0;
  public int highScore = 0;
  public int currency = 0;

  public void LoadFromPrefs()
  {
    level = PlayerPrefs.GetInt("PlayerLevel", 1);
    experience = PlayerPrefs.GetInt("PlayerExp", 0);
    highScore = PlayerPrefs.GetInt("HighScore", 0);
    currency = PlayerPrefs.GetInt("Currency", 0);
  }

  public void SaveToPrefs()
  {
    PlayerPrefs.SetInt("PlayerLevel", level);
    PlayerPrefs.SetInt("PlayerExp", experience);
    PlayerPrefs.SetInt("HighScore", highScore);
    PlayerPrefs.SetInt("Currency", currency);
  }
}

[System.Serializable]
public class GameSettings
{
  public float masterVolume = 100f;
  public float musicVolume = 100f;
  public float sfxVolume = 100f;
  public bool fullscreen = true;
  public int targetFPS = 60;
  public int qualityLevel = 2;

  public void LoadFromPrefs()
  {
    masterVolume = PlayerPrefs.GetFloat("MasterVolume", 100f);
    musicVolume = PlayerPrefs.GetFloat("MusicVolume", 100f);
    sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 100f);
    fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    targetFPS = PlayerPrefs.GetInt("TargetFPS", 60);
    qualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
  }

  public void SaveToPrefs()
  {
    PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
    PlayerPrefs.SetInt("TargetFPS", targetFPS);
    PlayerPrefs.SetInt("QualityLevel", qualityLevel);
  }
}
