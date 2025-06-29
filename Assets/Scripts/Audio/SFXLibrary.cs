using UnityEngine;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CreateAssetMenu(fileName = "New SFX Library", menuName = "Reach UI/Audio/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SFXClip
        {
            [Header("Basic Info")]
            public string sfxName;
            public string displayName;
            public AudioClip audioClip;
            
            [Header("Playback Settings")]
            [Range(0f, 1f)] public float defaultVolume = 1f;
            [Range(0.1f, 3f)] public float defaultPitch = 1f;
            [Range(0f, 1f)] public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
            public bool loopByDefault = false;
            
            [Header("Categories")]
            public SFXCategory category;
            public string customCategory;
            
            [Header("Variations")]
            public bool useRandomPitch = false;
            [Range(0.1f, 2f)] public float minPitch = 0.9f;
            [Range(0.1f, 2f)] public float maxPitch = 1.1f;
            
            public bool useRandomVolume = false;
            [Range(0f, 1f)] public float minVolume = 0.8f;
            [Range(0f, 1f)] public float maxVolume = 1f;
            
            [Header("Advanced Settings")]
            public bool use3D = false;
            [Range(0f, 5f)] public float minDistance = 1f;
            [Range(0f, 25f)] public float maxDistance = 500f;
        }

        public enum SFXCategory
        {
            Weapon,
            Explosion,
            Impact,
            Movement,
            Environment,
            Character,
            Vehicle,
            UI,
            Ambient,
            Custom
        }

        [Header("Library Settings")]
        public string libraryName = "SFX Library";
        [TextArea(2, 4)] public string description;
        
        [Header("SFX Clips")]
        public List<SFXClip> sfxClips = new List<SFXClip>();

        [Header("Default Settings")]
        public SFXClip defaultSFX;
        public bool enableRandomization = true;

        private Dictionary<string, SFXClip> sfxDictionary;

        void OnEnable()
        {
            BuildDictionary();
        }

        private void BuildDictionary()
        {
            sfxDictionary = new Dictionary<string, SFXClip>();
            
            foreach (SFXClip sfx in sfxClips)
            {
                if (sfx != null && !string.IsNullOrEmpty(sfx.sfxName))
                {
                    if (!sfxDictionary.ContainsKey(sfx.sfxName))
                    {
                        sfxDictionary.Add(sfx.sfxName, sfx);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate SFX name '{sfx.sfxName}' found in SFX Library '{libraryName}'", this);
                    }
                }
            }
        }

        public AudioClip GetSFX(string sfxName)
        {
            if (sfxDictionary == null)
                BuildDictionary();

            if (sfxDictionary.TryGetValue(sfxName, out SFXClip sfx))
            {
                return sfx.audioClip;
            }

            Debug.LogWarning($"SFX '{sfxName}' not found in library '{libraryName}'", this);
            return null;
        }

        public SFXClip GetSFXClip(string sfxName)
        {
            if (sfxDictionary == null)
                BuildDictionary();

            if (sfxDictionary.TryGetValue(sfxName, out SFXClip sfx))
            {
                return sfx;
            }

            Debug.LogWarning($"SFX '{sfxName}' not found in library '{libraryName}'", this);
            return null;
        }

        public List<SFXClip> GetSFXByCategory(SFXCategory category)
        {
            List<SFXClip> categorySFX = new List<SFXClip>();
            
            foreach (SFXClip sfx in sfxClips)
            {
                if (sfx.category == category)
                {
                    categorySFX.Add(sfx);
                }
            }
            
            return categorySFX;
        }

        public List<string> GetAllSFXNames()
        {
            List<string> sfxNames = new List<string>();
            
            foreach (SFXClip sfx in sfxClips)
            {
                if (sfx != null && !string.IsNullOrEmpty(sfx.sfxName))
                {
                    sfxNames.Add(sfx.sfxName);
                }
            }
            
            return sfxNames;
        }

        public List<string> GetAllCategories()
        {
            List<string> categories = new List<string>();
            
            foreach (SFXClip sfx in sfxClips)
            {
                if (sfx.category == SFXCategory.Custom && !string.IsNullOrEmpty(sfx.customCategory))
                {
                    if (!categories.Contains(sfx.customCategory))
                        categories.Add(sfx.customCategory);
                }
                else
                {
                    string categoryName = sfx.category.ToString();
                    if (!categories.Contains(categoryName))
                        categories.Add(categoryName);
                }
            }
            
            return categories;
        }

        public SFXClip GetRandomSFX()
        {
            if (sfxClips.Count == 0)
                return null;

            return sfxClips[Random.Range(0, sfxClips.Count)];
        }

        public SFXClip GetRandomSFXByCategory(SFXCategory category)
        {
            List<SFXClip> categorySFX = GetSFXByCategory(category);
            
            if (categorySFX.Count == 0)
                return null;

            return categorySFX[Random.Range(0, categorySFX.Count)];
        }
        
        public bool HasSFX(string sfxName)
        {
            if (sfxDictionary == null)
                BuildDictionary();

            return sfxDictionary.ContainsKey(sfxName);
        }

        public int GetSFXCount()
        {
            return sfxClips.Count;
        }

        public float GetTotalDuration()
        {
            float totalDuration = 0f;
            
            foreach (SFXClip sfx in sfxClips)
            {
                if (sfx.audioClip != null)
                {
                    totalDuration += sfx.audioClip.length;
                }
            }
            
            return totalDuration;
        }

        public void AddSFX(SFXClip newSFX)
        {
            if (newSFX != null && !string.IsNullOrEmpty(newSFX.sfxName))
            {
                if (!HasSFX(newSFX.sfxName))
                {
                    sfxClips.Add(newSFX);
                    BuildDictionary();
                }
                else
                {
                    Debug.LogWarning($"SFX '{newSFX.sfxName}' already exists in library", this);
                }
            }
        }

        public void RemoveSFX(string sfxName)
        {
            for (int i = sfxClips.Count - 1; i >= 0; i--)
            {
                if (sfxClips[i].sfxName == sfxName)
                {
                    sfxClips.RemoveAt(i);
                    BuildDictionary();
                    break;
                }
            }
        }

        public void ClearLibrary()
        {
            sfxClips.Clear();
            sfxDictionary?.Clear();
        }

        // Utility methods for common SFX categories
        public List<SFXClip> GetWeaponSFX()
        {
            return GetSFXByCategory(SFXCategory.Weapon);
        }

        public List<SFXClip> GetExplosionSFX()
        {
            return GetSFXByCategory(SFXCategory.Explosion);
        }

        public List<SFXClip> GetImpactSFX()
        {
            return GetSFXByCategory(SFXCategory.Impact);
        }

        public List<SFXClip> GetMovementSFX()
        {
            return GetSFXByCategory(SFXCategory.Movement);
        }

        public List<SFXClip> GetEnvironmentSFX()
        {
            return GetSFXByCategory(SFXCategory.Environment);
        }

        public List<SFXClip> GetCharacterSFX()
        {
            return GetSFXByCategory(SFXCategory.Character);
        }

        public List<SFXClip> GetVehicleSFX()
        {
            return GetSFXByCategory(SFXCategory.Vehicle);
        }

        public List<SFXClip> GetUISFX()
        {
            return GetSFXByCategory(SFXCategory.UI);
        }

        public List<SFXClip> GetAmbientSFX()
        {
            return GetSFXByCategory(SFXCategory.Ambient);
        }

        // Random selection methods
        public SFXClip GetRandomWeaponSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Weapon);
        }

        public SFXClip GetRandomExplosionSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Explosion);
        }

        public SFXClip GetRandomImpactSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Impact);
        }

        public SFXClip GetRandomMovementSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Movement);
        }

        public SFXClip GetRandomEnvironmentSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Environment);
        }

        public SFXClip GetRandomCharacterSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Character);
        }

        public SFXClip GetRandomVehicleSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Vehicle);
        }

        public SFXClip GetRandomUISFX()
        {
            return GetRandomSFXByCategory(SFXCategory.UI);
        }

        public SFXClip GetRandomAmbientSFX()
        {
            return GetRandomSFXByCategory(SFXCategory.Ambient);
        }
    }
} 