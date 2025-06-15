// Scripts/Audio/AudioManager.cs
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
  private static AudioManager instance;
  public static AudioManager Instance
  {
    get
    {
      Debug.Log("AudioManager Instance accessed");
      if (instance == null)
      {
        Debug.Log("Creating new AudioManager instance");
         // Try to find an existing instance in the scene
        instance = FindObjectOfType<AudioManager>();
        if (instance == null)
        {
          Debug.Log("No existing AudioManager found, creating a new one");
          GameObject obj = new GameObject("AudioManager");
          instance = obj.AddComponent<AudioManager>();
          DontDestroyOnLoad(obj);
        }
      }
      return instance;
    }
  }

  [Header("Audio Sources")]
  public AudioSource musicSource;
  public AudioSource sfxSource;
  public AudioSource uiSource;

  [Header("Audio Libraries")]
  public MusicLibrary musicLibrary;
  public SoundLibrary soundLibrary;

  [Header("Settings")]
  [Range(0f, 1f)] public float masterVolume = 1f;
  [Range(0f, 1f)] public float musicVolume = 1f;
  [Range(0f, 1f)] public float sfxVolume = 1f;

  private Dictionary<string, AudioClip> musicClips;
  private Dictionary<string, AudioClip> soundClips;

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

    Initialize();
  }

  public void Initialize()
  {
    LoadAudioLibraries();
    SetupAudioSources();
  }

  void LoadAudioLibraries()
  {
    musicClips = new Dictionary<string, AudioClip>();
    soundClips = new Dictionary<string, AudioClip>();

    // Load music clips
    if (musicLibrary != null)
    {
      foreach (var music in musicLibrary.musicTracks)
      {
        if (music.clip != null)
        {
          musicClips[music.name] = music.clip;
        }
      }
    }

    // Load sound clips
    if (soundLibrary != null)
    {
      foreach (var sound in soundLibrary.soundEffects)
      {
        if (sound.clip != null)
        {
          soundClips[sound.name] = sound.clip;
        }
      }
    }
  }

  void SetupAudioSources()
  {
    if (musicSource != null)
    {
      musicSource.loop = true;
      musicSource.playOnAwake = false;
    }

    if (sfxSource != null)
    {
      sfxSource.loop = false;
      sfxSource.playOnAwake = false;
    }

    if (uiSource != null)
    {
      uiSource.loop = false;
      uiSource.playOnAwake = false;
    }
  }

  // Music Controls
  public void PlayMusic(string musicName, bool fadeIn = false)
  {
    if (musicClips.ContainsKey(musicName))
    {
      if (fadeIn)
      {
        StartCoroutine(FadeInMusic(musicClips[musicName]));
      }
      else
      {
        musicSource.clip = musicClips[musicName];
        musicSource.Play();
      }
    }
  }

  public void StopMusic(bool fadeOut = false)
  {
    if (fadeOut)
    {
      StartCoroutine(FadeOutMusic());
    }
    else
    {
      musicSource.Stop();
    }
  }

  // Sound Effects
  public void PlaySFX(string soundName)
  {
    if (soundClips.ContainsKey(soundName))
    {
      sfxSource.PlayOneShot(soundClips[soundName]);
    }
  }

  public void PlayUISFX(string soundName)
  {
    if (soundClips.ContainsKey(soundName))
    {
      uiSource.PlayOneShot(soundClips[soundName]);
    }
  }

  // Volume Controls
  public void SetMasterVolume(float volume)
  {
    masterVolume = Mathf.Clamp01(volume / 100f);
    UpdateAudioSources();
  }

  public void SetMusicVolume(float volume)
  {
    musicVolume = Mathf.Clamp01(volume / 100f);
    UpdateAudioSources();
  }

  public void SetSFXVolume(float volume)
  {
    sfxVolume = Mathf.Clamp01(volume / 100f);
    UpdateAudioSources();
  }

  void UpdateAudioSources()
  {
    if (musicSource != null)
      musicSource.volume = masterVolume * musicVolume;

    if (sfxSource != null)
      sfxSource.volume = masterVolume * sfxVolume;

    if (uiSource != null)
      uiSource.volume = masterVolume * sfxVolume;
  }

  // Fade Effects
  System.Collections.IEnumerator FadeInMusic(AudioClip clip, float duration = 1f)
  {
    musicSource.clip = clip;
    musicSource.volume = 0f;
    musicSource.Play();

    float startVolume = 0f;
    float targetVolume = masterVolume * musicVolume;

    while (musicSource.volume < targetVolume)
    {
      musicSource.volume += (targetVolume / duration) * Time.deltaTime;
      yield return null;
    }

    musicSource.volume = targetVolume;
  }

  System.Collections.IEnumerator FadeOutMusic(float duration = 1f)
  {
    float startVolume = musicSource.volume;

    while (musicSource.volume > 0)
    {
      musicSource.volume -= (startVolume / duration) * Time.deltaTime;
      yield return null;
    }

    musicSource.Stop();
    musicSource.volume = masterVolume * musicVolume;
  }
}
