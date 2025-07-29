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

        #region Volume Controls (Audio Mixer)

        /// <summary>
        /// Sets the master volume for all audio
        /// </summary>
        /// <param name="volume">Volume level (0-1)</param>
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
                sfxSource.volume = masterVolume * sfxVolume; if (uiSource != null)
                uiSource.volume = masterVolume * uiVolume;
        }

        #endregion

        #region Music Controls

        /// <summary>
        /// Plays a music track with optional fade-in effect
        /// </summary>
        /// <param name="musicName">Name of the music track to play</param>
        /// <param name="fadeIn">Whether to fade in the music</param>
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

        public void PlayMusicInCategory(MusicLibrary.MusicCategory category, string customCategory = null, bool fadeIn = false)
        {
            if (musicLibrary == null) return;

            var musicClip = musicLibrary.GetRandomTrackByCategory(category, customCategory);

            if (musicClip != null)
            {
                PlayMusic(musicClip.trackName, fadeIn);
            }
            else
            {
                Debug.LogWarning($"No music found in category '{category}'.");
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

        #endregion

        #region Sound Effects (SFX)

        /// <summary>
        /// Plays a sound effect using basic settings (legacy method for backward compatibility)
        /// </summary>
        /// <param name="soundName">Name of the sound effect to play</param>
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

        /// <summary>
        /// Plays a sound effect with all advanced settings from SFXLibrary (randomization, 3D audio, etc.)
        /// </summary>
        /// <param name="soundName">Name of the sound effect to play</param>
        /// <param name="position">3D position for spatial audio (optional)</param>
        public void PlaySFXWithSettings(string soundName, Vector3? position = null)
        {
            if (soundLibrary == null)
            {
                PlaySFX(soundName); // Fallback to basic method
                return;
            }

            var sfxClip = soundLibrary.GetSFXClip(soundName);
            if (sfxClip == null)
            {
                Debug.LogWarning($"Sound effect '{soundName}' not found in SFX library.");
                return;
            }

            PlaySFXClipWithSettings(sfxClip, position);
        }

        /// <summary>
        /// Plays an SFX clip with all its configured settings
        /// </summary>
        /// <param name="sfxClip">The SFX clip configuration to play</param>
        /// <param name="position">3D position for spatial audio (optional)</param>
        private void PlaySFXClipWithSettings(SFXLibrary.SFXClip sfxClip, Vector3? position = null)
        {
            if (sfxClip?.audioClip == null) return;

            // Determine which audio source to use
            AudioSource sourceToUse = sfxClip.use3D && position.HasValue ?
                CreateTemporary3DAudioSource(position.Value, sfxClip) : sfxSource;

            // Calculate randomized volume
            float finalVolume = sfxClip.defaultVolume;
            if (sfxClip.useRandomVolume)
            {
                finalVolume = Random.Range(sfxClip.minVolume, sfxClip.maxVolume);
            }

            // Calculate randomized pitch
            float finalPitch = sfxClip.defaultPitch;
            if (sfxClip.useRandomPitch)
            {
                finalPitch = Random.Range(sfxClip.minPitch, sfxClip.maxPitch);
            }

            // Apply settings to audio source
            if (!sfxClip.use3D)
            {
                // 2D audio
                sourceToUse.pitch = finalPitch;
                sourceToUse.PlayOneShot(sfxClip.audioClip, finalVolume * masterVolume * sfxVolume);
            }
            else
            {
                // 3D audio with temporary source
                sourceToUse.clip = sfxClip.audioClip;
                sourceToUse.volume = finalVolume * masterVolume * sfxVolume;
                sourceToUse.pitch = finalPitch;
                sourceToUse.loop = sfxClip.loopByDefault;
                sourceToUse.Play();

                // Clean up temporary source after clip finishes
                if (!sfxClip.loopByDefault)
                {
                    StartCoroutine(CleanupTemporaryAudioSource(sourceToUse, sfxClip.audioClip.length / finalPitch));
                }
            }
        }

        /// <summary>
        /// Creates a temporary 3D audio source for spatial audio effects
        /// </summary>
        private AudioSource CreateTemporary3DAudioSource(Vector3 position, SFXLibrary.SFXClip sfxClip)
        {
            GameObject tempAudioObject = new GameObject($"TempAudio_{sfxClip.sfxName}");
            tempAudioObject.transform.position = position;

            AudioSource tempSource = tempAudioObject.AddComponent<AudioSource>();
            tempSource.spatialBlend = 1f; // Full 3D
            tempSource.minDistance = sfxClip.minDistance;
            tempSource.maxDistance = sfxClip.maxDistance;
            tempSource.playOnAwake = false;

            // Apply audio mixer group if available
            if (sfxSource.outputAudioMixerGroup != null)
            {
                tempSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            }

            return tempSource;
        }

        /// <summary>
        /// Cleans up temporary audio sources after they finish playing
        /// </summary>
        private IEnumerator CleanupTemporaryAudioSource(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f); // Small buffer
            if (source != null && source.gameObject != null)
            {
                Destroy(source.gameObject);
            }
        }

        /// <summary>
        /// Plays a random SFX from a specific category
        /// </summary>
        /// <param name="category">The SFX category to choose from</param>
        /// <param name="position">3D position for spatial audio (optional)</param>
        public void PlayRandomSFXFromCategory(SFXLibrary.SFXCategory category, Vector3? position = null)
        {
            if (soundLibrary == null) return;

            var randomSFX = soundLibrary.GetRandomSFXByCategory(category);
            if (randomSFX != null)
            {
                PlaySFXClipWithSettings(randomSFX, position);
            }
        }

        /// <summary>
        /// Plays a random SFX from the entire library
        /// </summary>
        /// <param name="position">3D position for spatial audio (optional)</param>
        public void PlayRandomSFX(Vector3? position = null)
        {
            if (soundLibrary == null) return;

            var randomSFX = soundLibrary.GetRandomSFX();
            if (randomSFX != null)
            {
                PlaySFXClipWithSettings(randomSFX, position);
            }
        }

        // Convenience methods for common SFX categories
        public void PlayWeaponSFX(string soundName = null, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(soundName))
                PlayRandomSFXFromCategory(SFXLibrary.SFXCategory.Weapon, position);
            else
                PlaySFXWithSettings(soundName, position);
        }

        public void PlayExplosionSFX(string soundName = null, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(soundName))
                PlayRandomSFXFromCategory(SFXLibrary.SFXCategory.Explosion, position);
            else
                PlaySFXWithSettings(soundName, position);
        }

        public void PlayImpactSFX(string soundName = null, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(soundName))
                PlayRandomSFXFromCategory(SFXLibrary.SFXCategory.Impact, position);
            else
                PlaySFXWithSettings(soundName, position);
        }

        public void PlayMovementSFX(string soundName = null, Vector3? position = null)
        {
            if (string.IsNullOrEmpty(soundName))
                PlayRandomSFXFromCategory(SFXLibrary.SFXCategory.Movement, position);
            else
                PlaySFXWithSettings(soundName, position);
        }

        #endregion

        #region UI Sound Effects

        /// <summary>
        /// Plays a UI sound effect using basic settings (legacy method for backward compatibility)
        /// </summary>
        /// <param name="soundName">Name of the UI sound to play</param>
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

        #endregion

        #region Volume Controls

        // Volume Getters
        public float GetMasterVolume() { return masterVolume * 100f; }
        public float GetMusicVolume() { return musicVolume * 100f; }
        public float GetSFXVolume() { return sfxVolume * 100f; }
        public float GetUIVolume() { return uiVolume * 100f; }

        #endregion

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

        #region Library Management

        /// <summary>
        /// Reloads all audio libraries to refresh the cached clips
        /// </summary>
        public void ReloadLibraries()
        {
            LoadAudioLibraries();
        }

        /// <summary>
        /// Gets a list of all available music track names
        /// </summary>
        public List<string> GetMusicTrackNames()
        {
            return new List<string>(musicClips.Keys);
        }

        /// <summary>
        /// Gets a list of all available sound effect names
        /// </summary>
        public List<string> GetSoundEffectNames()
        {
            return new List<string>(soundClips.Keys);
        }

        /// <summary>
        /// Gets a list of all available UI sound names
        /// </summary>
        public List<string> GetUISoundNames()
        {
            return new List<string>(uiClips.Keys);
        }

        /// <summary>
        /// Gets a music clip by name
        /// </summary>
        /// <param name="name">Name of the music track</param>
        /// <returns>AudioClip if found, null otherwise</returns>
        public AudioClip GetMusicClip(string name)
        {
            return musicClips.ContainsKey(name) ? musicClips[name] : null;
        }

        /// <summary>
        /// Gets a sound effect clip by name
        /// </summary>
        /// <param name="name">Name of the sound effect</param>
        /// <returns>AudioClip if found, null otherwise</returns>
        public AudioClip GetSoundClip(string name)
        {
            return soundClips.ContainsKey(name) ? soundClips[name] : null;
        }

        /// <summary>
        /// Gets a UI sound clip by name
        /// </summary>
        /// <param name="name">Name of the UI sound</param>
        /// <returns>AudioClip if found, null otherwise</returns>
        public AudioClip GetUISoundClip(string name)
        {
            return uiClips.ContainsKey(name) ? uiClips[name] : null;
        }

        #endregion
    }
}