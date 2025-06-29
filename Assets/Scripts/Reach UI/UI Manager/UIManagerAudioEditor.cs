#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Reach
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIManagerAudio))]
    public class UIManagerAudioEditor : Editor
    {
        private UIManagerAudio uamTarget;
        private GUISkin customSkin;
        private int currentTab = 0;

        private void OnEnable()
        {
            uamTarget = (UIManagerAudio)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = ReachUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = ReachUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            if (customSkin == null)
            {
                EditorGUILayout.HelpBox("Editor resources are missing. Please re-import the Reach UI package.", MessageType.Error);
                return;
            }

            ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 10);

            GUIContent[] toolbarTabs = new GUIContent[4];
            toolbarTabs[0] = new GUIContent("Resources");
            toolbarTabs[1] = new GUIContent("Libraries");
            toolbarTabs[2] = new GUIContent("Settings");
            toolbarTabs[3] = new GUIContent("Debug");

            currentTab = ReachUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Libraries", "Libraries"), customSkin.FindStyle("Tab_Content")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;
            if (GUILayout.Button(new GUIContent("Debug", "Debug"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 3;

            GUILayout.EndHorizontal();

            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var audioMixer = serializedObject.FindProperty("audioMixer");
            var audioSource = serializedObject.FindProperty("audioSource");
            var masterSlider = serializedObject.FindProperty("masterSlider");
            var musicSlider = serializedObject.FindProperty("musicSlider");
            var SFXSlider = serializedObject.FindProperty("SFXSlider");
            var UISlider = serializedObject.FindProperty("UISlider");

            var musicSource = serializedObject.FindProperty("musicSource");
            var sfxSource = serializedObject.FindProperty("sfxSource");
            var uiSource = serializedObject.FindProperty("uiSource");

            var musicLibrary = serializedObject.FindProperty("musicLibrary");
            var soundLibrary = serializedObject.FindProperty("soundLibrary");
            var uiAudioLibrary = serializedObject.FindProperty("uiAudioLibrary");

            var masterVolume = serializedObject.FindProperty("masterVolume");
            var musicVolume = serializedObject.FindProperty("musicVolume");
            var sfxVolume = serializedObject.FindProperty("sfxVolume");
            var uiVolume = serializedObject.FindProperty("uiVolume");

            switch (currentTab)
            {
                case 0: // Resources
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    ReachUIEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");
                    ReachUIEditorHandler.DrawProperty(audioMixer, customSkin, "Audio Mixer");
                    ReachUIEditorHandler.DrawProperty(audioSource, customSkin, "Audio Source");

                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 10);
                    ReachUIEditorHandler.DrawProperty(musicSource, customSkin, "Music Source");
                    ReachUIEditorHandler.DrawProperty(sfxSource, customSkin, "SFX Source");
                    ReachUIEditorHandler.DrawProperty(uiSource, customSkin, "UI Source");

                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
                    ReachUIEditorHandler.DrawProperty(masterSlider, customSkin, "Master Slider");
                    ReachUIEditorHandler.DrawProperty(musicSlider, customSkin, "Music Slider");
                    ReachUIEditorHandler.DrawProperty(SFXSlider, customSkin, "SFX Slider");
                    ReachUIEditorHandler.DrawProperty(UISlider, customSkin, "UI Slider");
                    break;

                case 1: // Libraries
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    ReachUIEditorHandler.DrawProperty(musicLibrary, customSkin, "Music Library");
                    ReachUIEditorHandler.DrawProperty(soundLibrary, customSkin, "Sound Library");
                    ReachUIEditorHandler.DrawProperty(uiAudioLibrary, customSkin, "UI Audio Library");

                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reload Libraries", GUILayout.Height(30)))
                    {
                        uamTarget.ReloadLibraries();
                        EditorUtility.SetDirty(uamTarget);
                    }
                    if (GUILayout.Button("Create Library Assets", GUILayout.Height(30)))
                    {
                        CreateLibraryAssets();
                    }
                    GUILayout.EndHorizontal();

                    // Library Info
                    if (Application.isPlaying)
                    {
                        GUILayout.Space(10);
                        ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 6);

                        var musicTracks = uamTarget.GetMusicTrackNames();
                        var soundEffects = uamTarget.GetSoundEffectNames();
                        var uiSounds = uamTarget.GetUISoundNames();

                        EditorGUILayout.LabelField($"Music Tracks: {musicTracks.Count}", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"Sound Effects: {soundEffects.Count}", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"UI Sounds: {uiSounds.Count}", customSkin.FindStyle("Text"));
                    }
                    break;

                case 2: // Settings
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    ReachUIEditorHandler.DrawProperty(masterVolume, customSkin, "Master Volume");
                    ReachUIEditorHandler.DrawProperty(musicVolume, customSkin, "Music Volume");
                    ReachUIEditorHandler.DrawProperty(sfxVolume, customSkin, "SFX Volume");
                    ReachUIEditorHandler.DrawProperty(uiVolume, customSkin, "UI Volume");

                    GUILayout.Space(10);
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 6);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Test Hover Sound", GUILayout.Height(25)))
                    {
                        if (Application.isPlaying)
                            uamTarget.PlayHoverSound();
                    }
                    if (GUILayout.Button("Test Click Sound", GUILayout.Height(25)))
                    {
                        if (Application.isPlaying)
                            uamTarget.PlayClickSound();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Test Notification", GUILayout.Height(25)))
                    {
                        if (Application.isPlaying)
                            uamTarget.PlayNotificationSound();
                    }
                    if (GUILayout.Button("Mute All", GUILayout.Height(25)))
                    {
                        if (Application.isPlaying)
                            uamTarget.SetMasterVolume(0f);
                    }
                    GUILayout.EndHorizontal();
                    break;

                case 3: // Debug
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 6);

                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField("Current Music Track:", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField(uamTarget.GetCurrentMusicTrack() ?? "None", customSkin.FindStyle("Text"));

                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Volume Levels:", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"Master: {uamTarget.GetMasterVolume():F0}%", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"Music: {uamTarget.GetMusicVolume():F0}%", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"SFX: {uamTarget.GetSFXVolume():F0}%", customSkin.FindStyle("Text"));
                        EditorGUILayout.LabelField($"UI: {uamTarget.GetUIVolume():F0}%", customSkin.FindStyle("Text"));

                        GUILayout.Space(10);
                        if (GUILayout.Button("Print Library Contents", GUILayout.Height(25)))
                        {
                            PrintLibraryContents();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Debug information is only available in Play mode.", MessageType.Info);
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }

        private void CreateLibraryAssets()
        {
            // Create Music Library
            if (uamTarget.musicLibrary == null)
            {
                var musicLib = ScriptableObject.CreateInstance<MusicLibrary>();
                AssetDatabase.CreateAsset(musicLib, "Assets/Audio/MusicLibrary.asset");
                uamTarget.musicLibrary = musicLib;
                EditorUtility.SetDirty(uamTarget);
            }

            // Create Sound Library
            if (uamTarget.soundLibrary == null)
            {
                var soundLib = ScriptableObject.CreateInstance<SFXLibrary>();
                AssetDatabase.CreateAsset(soundLib, "Assets/Audio/SoundLibrary.asset");
                uamTarget.soundLibrary = soundLib;
                EditorUtility.SetDirty(uamTarget);
            }

            // Create UI Audio Library
            if (uamTarget.uiAudioLibrary == null)
            {
                var uiLib = ScriptableObject.CreateInstance<UILibrary>();
                AssetDatabase.CreateAsset(uiLib, "Assets/Audio/UIAudioLibrary.asset");
                uamTarget.uiAudioLibrary = uiLib;
                EditorUtility.SetDirty(uamTarget);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void PrintLibraryContents()
        {
            Debug.Log("=== Audio Library Contents ===");

            var musicTracks = uamTarget.GetMusicTrackNames();
            Debug.Log($"Music Tracks ({musicTracks.Count}):");
            foreach (var track in musicTracks)
            {
                Debug.Log($"  - {track}");
            }

            var soundEffects = uamTarget.GetSoundEffectNames();
            Debug.Log($"Sound Effects ({soundEffects.Count}):");
            foreach (var effect in soundEffects)
            {
                Debug.Log($"  - {effect}");
            }

            var uiSounds = uamTarget.GetUISoundNames();
            Debug.Log($"UI Sounds ({uiSounds.Count}):");
            foreach (var sound in uiSounds)
            {
                Debug.Log($"  - {sound}");
            }
        }
    }
}
#endif