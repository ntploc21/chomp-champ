#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CustomEditor(typeof(MusicLibrary))]
    public class MusicLibraryEditor : Editor
    {
        private MusicLibrary musicLibrary;
        private GUISkin customSkin;
        private int currentTab = 0;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private MusicLibrary.MusicCategory filterCategory = MusicLibrary.MusicCategory.Custom;

        private void OnEnable()
        {
            musicLibrary = (MusicLibrary)target;
            if (EditorGUIUtility.isProSkin == true) { customSkin = ReachUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = ReachUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            GUIContent[] toolbarTabs = new GUIContent[4];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Library");
            toolbarTabs[2] = new GUIContent("Settings");
            toolbarTabs[3] = new GUIContent("Debug");

            currentTab = ReachUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Library", "Library"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;
            if (GUILayout.Button(new GUIContent("Debug", "Debug"), customSkin.FindStyle("Tab_Content")))
                currentTab = 3;

            GUILayout.EndHorizontal();

            var libraryName = serializedObject.FindProperty("libraryName");
            var description = serializedObject.FindProperty("description");
            var musicTracks = serializedObject.FindProperty("musicTracks");
            var shuffleOnStart = serializedObject.FindProperty("shuffleOnStart");
            var autoPlayOnStart = serializedObject.FindProperty("autoPlayOnStart");

            switch (currentTab)
            {
                case 0:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    ReachUIEditorHandler.DrawProperty(libraryName, customSkin, "Library Name");
                    ReachUIEditorHandler.DrawProperty(description, customSkin, "Description");
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 10);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total Tracks:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(musicLibrary.GetTrackCount().ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(musicLibrary.GetTotalDuration())), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Categories:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(musicLibrary.GetAllCategories().Count.ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add New Track", customSkin.button)) AddNewTrack();
                    if (GUILayout.Button("Sort by Name", customSkin.button)) SortTracksByName();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Validate Library", customSkin.button)) ValidateLibrary();
                    if (GUILayout.Button("Clear Library", customSkin.button))
                    {
                        if (EditorUtility.DisplayDialog("Clear Library", "Are you sure you want to clear all tracks?", "Yes", "No"))
                        {
                            musicLibrary.ClearLibrary();
                            EditorUtility.SetDirty(musicLibrary);
                        }
                    }
                    GUILayout.EndHorizontal();
                    break;
                case 1:
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Search:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Category:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    filterCategory = (MusicLibrary.MusicCategory)EditorGUILayout.EnumPopup(filterCategory);
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        searchFilter = "";
                        filterCategory = MusicLibrary.MusicCategory.Custom;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 10);
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    var filteredTracks = GetFilteredTracks();
                    if (filteredTracks.Count == 0)
                        EditorGUILayout.HelpBox("No tracks found matching the current filter.", MessageType.Info);
                    else
                        for (int i = 0; i < filteredTracks.Count; i++)
                            DrawTrackItem(filteredTracks[i], i);
                    GUILayout.EndScrollView();
                    break;
                case 2:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    shuffleOnStart.boolValue = ReachUIEditorHandler.DrawToggle(shuffleOnStart.boolValue, customSkin, "Shuffle on Start");
                    autoPlayOnStart.boolValue = ReachUIEditorHandler.DrawToggle(autoPlayOnStart.boolValue, customSkin, "Auto Play on Start");
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
                    var categories = musicLibrary.GetAllCategories();
                    foreach (var category in categories)
                    {
                        var categoryTracks = GetTracksByCategory(category);
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(new GUIContent(category), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.LabelField(new GUIContent($"{categoryTracks.Count} tracks"), customSkin.FindStyle("Text"));
                        GUILayout.EndHorizontal();
                    }
                    break;
                case 3:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(new GUIContent("Library Information"), customSkin.FindStyle("Text"));
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField($"Library Name: {musicLibrary.libraryName}");
                    EditorGUILayout.LabelField($"Track Count: {musicLibrary.GetTrackCount()}");
                    EditorGUILayout.LabelField($"Total Duration: {FormatDuration(musicLibrary.GetTotalDuration())}");
                    EditorGUILayout.LabelField($"Categories: {string.Join(", ", musicLibrary.GetAllCategories())}");
                    GUILayout.EndVertical();
                    GUILayout.Space(10);
                    var issues = ValidateLibraryInternal();
                    if (issues.Count == 0)
                        EditorGUILayout.HelpBox("Library validation passed. No issues found.", MessageType.Info);
                    else
                    {
                        EditorGUILayout.HelpBox($"Found {issues.Count} issue(s):", MessageType.Warning);
                        foreach (var issue in issues)
                            EditorGUILayout.HelpBox(issue, MessageType.Error);
                    }
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
        private HashSet<int> expandedTracks = new HashSet<int>();

        private void DrawTrackItem(MusicLibrary.MusicTrack track, int index)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(expandedTracks.Contains(index) ? "Close" : "Edit", GUILayout.Width(50)))
            {
                if (expandedTracks.Contains(index)) expandedTracks.Remove(index);
                else expandedTracks.Add(index);
            }
            EditorGUILayout.LabelField(new GUIContent($"Track {index + 1}"), customSkin.FindStyle("Text"), GUILayout.Width(60), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(new GUIContent(track.trackName), customSkin.FindStyle("Text"), GUILayout.Width(120), GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (expandedTracks.Contains(index))
            {
                // Draw editable fields for this track
                var musicTracks = serializedObject.FindProperty("musicTracks");
                if (index < musicTracks.arraySize)
                {
                    var trackProp = musicTracks.GetArrayElementAtIndex(index);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(trackProp, true);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                if (track.audioClip != null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(60), GUILayout.ExpandWidth(false));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(track.audioClip.length)), customSkin.FindStyle("Text"), GUILayout.Width(60), GUILayout.ExpandWidth(false));
                    EditorGUILayout.LabelField(new GUIContent("Category:"), customSkin.FindStyle("Text"), GUILayout.Width(60), GUILayout.ExpandWidth(false));
                    EditorGUILayout.LabelField(new GUIContent(track.category.ToString()), customSkin.FindStyle("Text"), GUILayout.Width(80), GUILayout.ExpandWidth(false));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("No audio clip assigned", MessageType.Warning);
                }
            }

            GUILayout.EndVertical();
        }

        private List<MusicLibrary.MusicTrack> GetFilteredTracks()
        {
            var tracks = new List<MusicLibrary.MusicTrack>();
            foreach (var track in musicLibrary.musicTracks)
            {
                if (track == null) continue;
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) || track.trackName.ToLower().Contains(searchFilter.ToLower()) || track.displayName.ToLower().Contains(searchFilter.ToLower());
                bool matchesCategory = filterCategory == MusicLibrary.MusicCategory.Custom || track.category == filterCategory;
                if (matchesSearch && matchesCategory) tracks.Add(track);
            }
            return tracks;
        }
        private List<MusicLibrary.MusicTrack> GetTracksByCategory(string categoryName)
        {
            var tracks = new List<MusicLibrary.MusicTrack>();
            foreach (var track in musicLibrary.musicTracks)
            {
                if (track == null) continue;
                if (track.category == MusicLibrary.MusicCategory.Custom)
                {
                    if (track.customCategory == categoryName) tracks.Add(track);
                }
                else if (track.category.ToString() == categoryName) tracks.Add(track);
            }
            return tracks;
        }
        private void AddNewTrack()
        {
            var newTrack = new MusicLibrary.MusicTrack { trackName = "New Track", displayName = "New Track", category = MusicLibrary.MusicCategory.Custom };
            musicLibrary.AddTrack(newTrack);
            EditorUtility.SetDirty(musicLibrary);
        }
        private void SortTracksByName()
        {
            musicLibrary.musicTracks.Sort((a, b) => a.trackName.CompareTo(b.trackName));
            EditorUtility.SetDirty(musicLibrary);
        }
        private void FocusOnTrack(MusicLibrary.MusicTrack track)
        {
            var musicTracks = serializedObject.FindProperty("musicTracks");
            for (int i = 0; i < musicTracks.arraySize; i++)
            {
                var trackElement = musicTracks.GetArrayElementAtIndex(i);
                var trackName = trackElement.FindPropertyRelative("trackName");
                if (trackName.stringValue == track.trackName)
                {
                    musicTracks.isExpanded = true;
                    trackElement.isExpanded = true;
                    break;
                }
            }
        }
        private void ValidateLibrary()
        {
            var issues = ValidateLibraryInternal();
            if (issues.Count == 0)
                EditorUtility.DisplayDialog("Validation", "Library validation passed. No issues found.", "OK");
            else
                EditorUtility.DisplayDialog("Validation Issues", $"Found {issues.Count} issue(s):\n\n" + string.Join("\n", issues), "OK");
        }
        private List<string> ValidateLibraryInternal()
        {
            var issues = new List<string>();
            var trackNames = new HashSet<string>();
            for (int i = 0; i < musicLibrary.musicTracks.Count; i++)
            {
                var track = musicLibrary.musicTracks[i];
                if (track == null) { issues.Add($"Track at index {i} is null"); continue; }
                if (string.IsNullOrEmpty(track.trackName)) { issues.Add($"Track at index {i} has no name"); continue; }
                if (trackNames.Contains(track.trackName)) { issues.Add($"Duplicate track name: {track.trackName}"); }
                else { trackNames.Add(track.trackName); }
                if (track.audioClip == null) { issues.Add($"Track '{track.trackName}' has no audio clip"); }
                if (track.category == MusicLibrary.MusicCategory.Custom && string.IsNullOrEmpty(track.customCategory)) { issues.Add($"Track '{track.trackName}' is set to Custom category but has no custom category name"); }
            }
            return issues;
        }
        private string FormatDuration(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
#endif