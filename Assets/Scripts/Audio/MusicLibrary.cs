using UnityEngine;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CreateAssetMenu(fileName = "New Music Library", menuName = "Reach UI/Audio/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        [System.Serializable]
        public class MusicTrack
        {
            [Header("Basic Info")]
            public string trackName;
            public string displayName;
            public AudioClip audioClip;
            
            [Header("Metadata")]
            public string artist;
            public string album;
            [Range(0f, 1f)] public float defaultVolume = 1f;
            public bool loopByDefault = true;
            
            [Header("Categories")]
            public MusicCategory category;
            public string customCategory;
            
            [Header("Settings")]
            public bool fadeIn = true;
            public bool fadeOut = true;
            [Range(0.1f, 5f)] public float fadeSpeed = 2f;
        }

        public enum MusicCategory
        {
            MainMenu,
            Gameplay,
            Combat,
            Ambient,
            Victory,
            Defeat,
            Credits,
            Custom
        }

        [Header("Library Settings")]
        public string libraryName = "Music Library";
        [TextArea(2, 4)] public string description;
        
        [Header("Music Tracks")]
        public List<MusicTrack> musicTracks = new List<MusicTrack>();

        [Header("Default Settings")]
        public bool shuffleOnStart = false;
        public bool autoPlayOnStart = false;

        private Dictionary<string, MusicTrack> trackDictionary;

        void OnEnable()
        {
            BuildDictionary();
        }

        private void BuildDictionary()
        {
            trackDictionary = new Dictionary<string, MusicTrack>();
            
            foreach (MusicTrack track in musicTracks)
            {
                if (track != null && !string.IsNullOrEmpty(track.trackName))
                {
                    if (!trackDictionary.ContainsKey(track.trackName))
                    {
                        trackDictionary.Add(track.trackName, track);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate track name '{track.trackName}' found in Music Library '{libraryName}'", this);
                    }
                }
            }
        }

        public AudioClip GetMusic(string trackName)
        {
            if (trackDictionary == null)
                BuildDictionary();

            if (trackDictionary.TryGetValue(trackName, out MusicTrack track))
            {
                return track.audioClip;
            }

            Debug.LogWarning($"Music track '{trackName}' not found in library '{libraryName}'", this);
            return null;
        }

        public MusicTrack GetMusicTrack(string trackName)
        {
            if (trackDictionary == null)
                BuildDictionary();

            if (trackDictionary.TryGetValue(trackName, out MusicTrack track))
            {
                return track;
            }

            Debug.LogWarning($"Music track '{trackName}' not found in library '{libraryName}'", this);
            return null;
        }

        public List<MusicTrack> GetTracksByCategory(MusicCategory category, string customCategory = null)
        {
            if (category == MusicCategory.Custom && string.IsNullOrEmpty(customCategory))
            {
                Debug.LogWarning("Custom category is required when filtering by MusicCategory.Custom", this);
                return new List<MusicTrack>();
            }

            List<MusicTrack> categoryTracks = new List<MusicTrack>();
            
            foreach (MusicTrack track in musicTracks)
            {
                if (track.category == category)
                {
                    if (category == MusicCategory.Custom && track.customCategory == customCategory)
                        categoryTracks.Add(track);
                    else if (category != MusicCategory.Custom)
                        categoryTracks.Add(track);
                }
            }
            
            return categoryTracks;
        }

        public List<string> GetAllTrackNames()
        {
            List<string> trackNames = new List<string>();
            
            foreach (MusicTrack track in musicTracks)
            {
                if (track != null && !string.IsNullOrEmpty(track.trackName))
                {
                    trackNames.Add(track.trackName);
                }
            }
            
            return trackNames;
        }

        public List<string> GetAllCategories()
        {
            List<string> categories = new List<string>();
            
            foreach (MusicTrack track in musicTracks)
            {
                if (track.category == MusicCategory.Custom && !string.IsNullOrEmpty(track.customCategory))
                {
                    if (!categories.Contains(track.customCategory))
                        categories.Add(track.customCategory);
                }
                else
                {
                    string categoryName = track.category.ToString();
                    if (!categories.Contains(categoryName))
                        categories.Add(categoryName);
                }
            }
            
            return categories;
        }

        public MusicTrack GetRandomTrack()
        {
            if (musicTracks.Count == 0)
                return null;

            return musicTracks[Random.Range(0, musicTracks.Count)];
        }

        public MusicTrack GetRandomTrackByCategory(MusicCategory category, string customCategory = null)
        {
            List<MusicTrack> categoryTracks = GetTracksByCategory(category, customCategory);

            if (categoryTracks.Count == 0)
                return null;

            return categoryTracks[Random.Range(0, categoryTracks.Count)];
        }

        public bool HasTrack(string trackName)
        {
            if (trackDictionary == null)
                BuildDictionary();

            return trackDictionary.ContainsKey(trackName);
        }

        public int GetTrackCount()
        {
            return musicTracks.Count;
        }

        public float GetTotalDuration()
        {
            float totalDuration = 0f;
            
            foreach (MusicTrack track in musicTracks)
            {
                if (track.audioClip != null)
                {
                    totalDuration += track.audioClip.length;
                }
            }
            
            return totalDuration;
        }

        public void AddTrack(MusicTrack newTrack)
        {
            if (newTrack != null && !string.IsNullOrEmpty(newTrack.trackName))
            {
                if (!HasTrack(newTrack.trackName))
                {
                    musicTracks.Add(newTrack);
                    BuildDictionary();
                }
                else
                {
                    Debug.LogWarning($"Track '{newTrack.trackName}' already exists in library", this);
                }
            }
        }

        public void RemoveTrack(string trackName)
        {
            for (int i = musicTracks.Count - 1; i >= 0; i--)
            {
                if (musicTracks[i].trackName == trackName)
                {
                    musicTracks.RemoveAt(i);
                    BuildDictionary();
                    break;
                }
            }
        }

        public void ClearLibrary()
        {
            musicTracks.Clear();
            trackDictionary?.Clear();
        }
    }
} 