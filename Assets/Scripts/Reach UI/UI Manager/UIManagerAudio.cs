using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

namespace Michsky.UI.Reach
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class UIManagerAudio : MonoBehaviour
    {
        // Static Instance
        public static UIManagerAudio instance;

        // Resources
        public UIManager UIManagerAsset;
        [SerializeField] private AudioMixer audioMixer;
        public AudioSource audioSource;

        // Volume Sliders (for UI integration)
        [SerializeField] private SliderManager masterSlider;
        [SerializeField] private SliderManager musicSlider;
        [SerializeField] private SliderManager SFXSlider;
        [SerializeField] private SliderManager UISlider;

        // Audio Sources
        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource uiSource;

        // Audio Libraries
        [Header("Audio Libraries")]
        public MusicLibrary musicLibrary;
        public SFXLibrary soundLibrary;
        public UILibrary uiAudioLibrary;

        // Settings
        [Header("Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;

        // Private Collections
        private Dictionary<string, AudioClip> musicClips;
        private Dictionary<string, AudioClip> soundClips;
        private Dictionary<string, AudioClip> uiClips;

        // Current Music Track
        private string currentMusicTrack;
        private Coroutine fadeCoroutine;

        void Awake()
        {
            // Singleton pattern
            if (instance == null)
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

        void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            InitVolume();
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
            uiClips = new Dictionary<string, AudioClip>();

            // Load music clips
            if (musicLibrary != null)
            {
                foreach (var music in musicLibrary.musicTracks)
                {
                    if (music.audioClip != null)
                    {
                        musicClips[music.trackName] = music.audioClip;
                    }
                }
            }

            // Load sound clips
            if (soundLibrary != null)
            {
                foreach (var sound in soundLibrary.sfxClips)
                {
                    if (sound.audioClip != null)
                    {
                        soundClips[sound.sfxName] = sound.audioClip;
                    }
                }
            }

            // Load UI audio clips
            if (uiAudioLibrary != null)
            {
                foreach (var uiSound in uiAudioLibrary.uiSounds)
                {
                    if (uiSound.audioClip != null)
                    {
                        uiClips[uiSound.soundName] = uiSound.audioClip;
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

        public void InitVolume()
        {
            if (audioMixer == null)
            {
                Debug.Log("Audio Mixer is missing, cannot initialize the volume.", this);
                return;
            }

            if (masterSlider != null)
            {
                float masterValue = PlayerPrefs.GetFloat("Slider_" + masterSlider.saveKey, 1f);
                audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(masterValue, 0.0001f)) * 20);
                masterSlider.mainSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (musicSlider != null)
            {
                float musicValue = PlayerPrefs.GetFloat("Slider_" + musicSlider.saveKey, 1f);
                audioMixer.SetFloat("Music", Mathf.Log10(Mathf.Max(musicValue, 0.0001f)) * 20);
                musicSlider.mainSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (SFXSlider != null)
            {
                float sfxValue = PlayerPrefs.GetFloat("Slider_" + SFXSlider.saveKey, 1f);
                audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(sfxValue, 0.0001f)) * 20);
                SFXSlider.mainSlider.onValueChanged.AddListener(SetSFXVolume);
            }

            if (UISlider != null)
            {
                float uiValue = PlayerPrefs.GetFloat("Slider_" + UISlider.saveKey, 1f);
                audioMixer.SetFloat("UI", Mathf.Log10(Mathf.Max(uiValue, 0.0001f)) * 20);
                UISlider.mainSlider.onValueChanged.AddListener(SetUIVolume);
            }
        }

        // Volume Controls (Audio Mixer)
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
            UpdateAudioSources();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            audioMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
            UpdateAudioSources();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
            UpdateAudioSources();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            audioMixer.SetFloat("UI", Mathf.Log10(volume) * 20);
            UpdateAudioSources();
        }

        void UpdateAudioSources()
        {
            if (musicSource != null)
                musicSource.volume = masterVolume * musicVolume;

            if (sfxSource != null)
                sfxSource.volume = masterVolume * sfxVolume;

            if (uiSource != null)
                uiSource.volume = masterVolume * uiVolume;
        }

        // Music Controls
        public void PlayMusic(string musicName, bool fadeIn = false)
        {
            if (musicClips.ContainsKey(musicName))
            {
                currentMusicTrack = musicName;
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
            else
            {
                Debug.LogWarning($"Music track '{musicName}' not found in library.");
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
                currentMusicTrack = null;
            }
        }

        public void PauseMusic()
        {
            musicSource.Pause();
        }

        public void ResumeMusic()
        {
            musicSource.UnPause();
        }

        public string GetCurrentMusicTrack()
        {
            return currentMusicTrack;
        }

        // Sound Effects
        public void PlaySFX(string soundName)
        {
            if (soundClips.ContainsKey(soundName))
            {
                sfxSource.PlayOneShot(soundClips[soundName]);
            }
            else
            {
                Debug.LogWarning($"Sound effect '{soundName}' not found in library.");
            }
        }

        public void PlayUISFX(string soundName)
        {
            if (uiClips.ContainsKey(soundName))
            {
                uiSource.PlayOneShot(uiClips[soundName]);
            }
            else if (soundClips.ContainsKey(soundName))
            {
                uiSource.PlayOneShot(soundClips[soundName]);
            }
            else
            {
                Debug.LogWarning($"UI sound '{soundName}' not found in library.");
            }
        }

        public void PlayHoverSound()
        {
            if (uiAudioLibrary.GetHoverSound() != null)
            {
                uiSource.PlayOneShot(uiAudioLibrary.GetHoverSound().audioClip);
            }
            else
            {
                Debug.LogWarning("Hover sound not found in UI library.");
            }
        }

        public void PlayClickSound()
        {
            if (uiAudioLibrary.GetClickSound() != null)
            {
                uiSource.PlayOneShot(uiAudioLibrary.GetClickSound().audioClip);
            }
            else
            {
                Debug.LogWarning("Click sound not found in UI library.");
            }
        }

        public void PlayNotificationSound()
        {
            if (uiAudioLibrary.GetNotificationSound() != null)
            {
                uiSource.PlayOneShot(uiAudioLibrary.GetNotificationSound().audioClip);
            }
            else
            {
                Debug.LogWarning("Notification sound not found in UI library.");
            }
        }

        // Volume Getters
        public float GetMasterVolume() { return masterVolume * 100f; }
        public float GetMusicVolume() { return musicVolume * 100f; }
        public float GetSFXVolume() { return sfxVolume * 100f; }
        public float GetUIVolume() { return uiVolume * 100f; }

        #region Fade Effects
        // Fade Effects
        IEnumerator FadeInMusic(AudioClip clip, float duration = 1f)
        {
            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.Play();

            float targetVolume = masterVolume * musicVolume;
            float elapsed = 0f;

            while (musicSource.volume < targetVolume)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, (targetVolume / duration) * Time.deltaTime);
                yield return null;
            }

            musicSource.volume = targetVolume;
        }

        IEnumerator FadeOutMusic(float duration = 1f)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (musicSource.volume > 0f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0f, (startVolume / duration) * Time.deltaTime);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = masterVolume * musicVolume;
            currentMusicTrack = null;
        }
        #endregion

        // Library Management
        public void ReloadLibraries()
        {
            LoadAudioLibraries();
        }

        public List<string> GetMusicTrackNames()
        {
            return new List<string>(musicClips.Keys);
        }

        public List<string> GetSoundEffectNames()
        {
            return new List<string>(soundClips.Keys);
        }

        public List<string> GetUISoundNames()
        {
            return new List<string>(uiClips.Keys);
        }

        // Audio Clip Getters
        public AudioClip GetMusicClip(string name)
        {
            return musicClips.ContainsKey(name) ? musicClips[name] : null;
        }

        public AudioClip GetSoundClip(string name)
        {
            return soundClips.ContainsKey(name) ? soundClips[name] : null;
        }

        public AudioClip GetUISoundClip(string name)
        {
            return uiClips.ContainsKey(name) ? uiClips[name] : null;
        }
    }
}