using UnityEngine;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CreateAssetMenu(fileName = "New UI Library", menuName = "Reach UI/Audio/UI Library")]
    public class UILibrary : ScriptableObject
    {
        [System.Serializable]
        public class UISound
        {
            [Header("Basic Info")]
            public string soundName;
            public string displayName;
            public AudioClip audioClip;
            
            [Header("Playback Settings")]
            [Range(0f, 1f)] public float defaultVolume = 1f;
            [Range(0.1f, 3f)] public float defaultPitch = 1f;
            public bool loopByDefault = false;
            
            [Header("Categories")]
            public UISoundCategory category;
            public string customCategory;
            
            [Header("Variations")]
            public bool useRandomPitch = false;
            [Range(0.1f, 2f)] public float minPitch = 0.9f;
            [Range(0.1f, 2f)] public float maxPitch = 1.1f;
            
            public bool useRandomVolume = false;
            [Range(0f, 1f)] public float minVolume = 0.8f;
            [Range(0f, 1f)] public float maxVolume = 1f;
            
            [Header("Behavior")]
            public bool playOnHover = false;
            public bool playOnClick = false;
            public bool playOnSelect = false;
            public bool playOnDeselect = false;
            
            [Header("Cooldown")]
            public bool useCooldown = false;
            [Range(0.1f, 5f)] public float cooldownTime = 0.1f;
        }

        public enum UISoundCategory
        {
            Hover,
            Click,
            Select,
            Deselect,
            Notification,
            Error,
            Success,
            Warning,
            Confirm,
            Cancel,
            Open,
            Close,
            Toggle,
            Slider,
            Dropdown,
            Input,
            Scroll,
            Drag,
            Drop,
            Custom
        }

        [Header("Library Settings")]
        public string libraryName = "UI Library";
        [TextArea(2, 4)] public string description;
        
        [Header("UI Sounds")]
        public List<UISound> uiSounds = new List<UISound>();

        [Header("Default Sounds")]
        public UISound defaultHoverSound;
        public UISound defaultClickSound;
        public UISound defaultNotificationSound;
        public UISound defaultErrorSound;
        public UISound defaultSuccessSound;

        [Header("Settings")]
        public bool enableRandomization = true;
        public bool enableCooldowns = true;

        private Dictionary<string, UISound> soundDictionary;
        private Dictionary<string, float> cooldownTimers;

        void OnEnable()
        {
            BuildDictionary();
            InitializeCooldowns();
        }

        private void BuildDictionary()
        {
            soundDictionary = new Dictionary<string, UISound>();
            
            foreach (UISound sound in uiSounds)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.soundName))
                {
                    if (!soundDictionary.ContainsKey(sound.soundName))
                    {
                        soundDictionary.Add(sound.soundName, sound);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate UI sound name '{sound.soundName}' found in UI Library '{libraryName}'", this);
                    }
                }
            }
        }

        private void InitializeCooldowns()
        {
            cooldownTimers = new Dictionary<string, float>();
        }

        public AudioClip GetUISound(string soundName)
        {
            if (soundDictionary == null)
                BuildDictionary();

            if (soundDictionary.TryGetValue(soundName, out UISound sound))
            {
                return sound.audioClip;
            }

            Debug.LogWarning($"UI sound '{soundName}' not found in library '{libraryName}'", this);
            return null;
        }

        public UISound GetUISoundData(string soundName)
        {
            if (soundDictionary == null)
                BuildDictionary();

            if (soundDictionary.TryGetValue(soundName, out UISound sound))
            {
                return sound;
            }

            Debug.LogWarning($"UI sound '{soundName}' not found in library '{libraryName}'", this);
            return null;
        }

        public List<UISound> GetSoundsByCategory(UISoundCategory category)
        {
            List<UISound> categorySounds = new List<UISound>();
            
            foreach (UISound sound in uiSounds)
            {
                if (sound.category == category)
                {
                    categorySounds.Add(sound);
                }
            }
            
            return categorySounds;
        }


        public List<string> GetAllSoundNames()
        {
            List<string> soundNames = new List<string>();
            
            foreach (UISound sound in uiSounds)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.soundName))
                {
                    soundNames.Add(sound.soundName);
                }
            }
            
            return soundNames;
        }

        public List<string> GetAllCategories()
        {
            List<string> categories = new List<string>();
            
            foreach (UISound sound in uiSounds)
            {
                if (sound.category == UISoundCategory.Custom && !string.IsNullOrEmpty(sound.customCategory))
                {
                    if (!categories.Contains(sound.customCategory))
                        categories.Add(sound.customCategory);
                }
                else
                {
                    string categoryName = sound.category.ToString();
                    if (!categories.Contains(categoryName))
                        categories.Add(categoryName);
                }
            }
            
            return categories;
        }

        public UISound GetRandomSound()
        {
            if (uiSounds.Count == 0)
                return null;

            return uiSounds[Random.Range(0, uiSounds.Count)];
        }

        public UISound GetRandomSoundByCategory(UISoundCategory category)
        {
            List<UISound> categorySounds = GetSoundsByCategory(category);
            
            if (categorySounds.Count == 0)
                return null;

            return categorySounds[Random.Range(0, categorySounds.Count)];
        }

        public bool HasSound(string soundName)
        {
            if (soundDictionary == null)
                BuildDictionary();

            return soundDictionary.ContainsKey(soundName);
        }

        public int GetSoundCount()
        {
            return uiSounds.Count;
        }

        public float GetTotalDuration()
        {
            float totalDuration = 0f;
            
            foreach (UISound sound in uiSounds)
            {
                if (sound.audioClip != null)
                {
                    totalDuration += sound.audioClip.length;
                }
            }
            
            return totalDuration;
        }

        public void AddSound(UISound newSound)
        {
            if (newSound != null && !string.IsNullOrEmpty(newSound.soundName))
            {
                if (HasSound(newSound.soundName))
                {
                    // Add suffix to avoid duplicates
                    int suffix = 1;
                    string originalName = newSound.soundName;

                    while (HasSound(newSound.soundName))
                    {
                        newSound.soundName = $"{originalName}_{suffix}";
                        suffix++;
                    }
                }

                uiSounds.Add(newSound);
                BuildDictionary();
            }
        }

        public void RemoveSound(string soundName)
        {
            for (int i = uiSounds.Count - 1; i >= 0; i--)
            {
                if (uiSounds[i].soundName == soundName)
                {
                    uiSounds.RemoveAt(i);
                    BuildDictionary();
                    break;
                }
            }
        }

        public void ClearLibrary()
        {
            uiSounds.Clear();
            soundDictionary?.Clear();
            cooldownTimers?.Clear();
        }

        // Cooldown management
        public bool IsOnCooldown(string soundName)
        {
            if (!enableCooldowns || cooldownTimers == null)
                return false;

            if (cooldownTimers.TryGetValue(soundName, out float lastPlayTime))
            {
                float cooldown = GetUISoundData(soundName)?.cooldownTime ?? 0f;
                return Time.time - lastPlayTime < cooldown;
            }

            return false;
        }

        public void SetCooldown(string soundName)
        {
            if (enableCooldowns && cooldownTimers != null)
            {
                cooldownTimers[soundName] = Time.time;
            }
        }

        // Convenience methods for common UI sounds
        public UISound GetHoverSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Hover) ?? defaultHoverSound;
        }

        public UISound GetClickSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Click) ?? defaultClickSound;
        }

        public UISound GetSelectSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Select);
        }

        public UISound GetDeselectSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Deselect);
        }

        public UISound GetNotificationSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Notification) ?? defaultNotificationSound;
        }

        public UISound GetErrorSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Error) ?? defaultErrorSound;
        }

        public UISound GetSuccessSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Success) ?? defaultSuccessSound;
        }

        public UISound GetWarningSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Warning);
        }

        public UISound GetConfirmSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Confirm);
        }

        public UISound GetCancelSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Cancel);
        }

        public UISound GetOpenSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Open);
        }

        public UISound GetCloseSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Close);
        }

        public UISound GetToggleSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Toggle);
        }

        public UISound GetSliderSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Slider);
        }

        public UISound GetDropdownSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Dropdown);
        }

        public UISound GetInputSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Input);
        }

        public UISound GetScrollSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Scroll);
        }

        public UISound GetDragSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Drag);
        }

        public UISound GetDropSound()
        {
            return GetRandomSoundByCategory(UISoundCategory.Drop);
        }

        // Utility methods for common UI categories
        public List<UISound> GetHoverSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Hover);
        }

        public List<UISound> GetClickSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Click);
        }

        public List<UISound> GetSelectSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Select);
        }

        public List<UISound> GetDeselectSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Deselect);
        }

        public List<UISound> GetNotificationSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Notification);
        }

        public List<UISound> GetErrorSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Error);
        }

        public List<UISound> GetSuccessSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Success);
        }

        public List<UISound> GetWarningSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Warning);
        }

        public List<UISound> GetConfirmSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Confirm);
        }

        public List<UISound> GetCancelSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Cancel);
        }

        public List<UISound> GetOpenSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Open);
        }

        public List<UISound> GetCloseSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Close);
        }

        public List<UISound> GetToggleSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Toggle);
        }

        public List<UISound> GetSliderSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Slider);
        }

        public List<UISound> GetDropdownSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Dropdown);
        }

        public List<UISound> GetInputSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Input);
        }

        public List<UISound> GetScrollSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Scroll);
        }

        public List<UISound> GetDragSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Drag);
        }

        public List<UISound> GetDropSounds()
        {
            return GetSoundsByCategory(UISoundCategory.Drop);
        }
    }
} 