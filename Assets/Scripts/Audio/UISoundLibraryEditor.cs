#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CustomEditor(typeof(UILibrary))]
    public class UISoundLibraryEditor : Editor
    {
        private UILibrary uiLibrary;
        private GUISkin customSkin;
        private int currentTab = 0;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private UILibrary.UISoundCategory filterCategory = UILibrary.UISoundCategory.Custom;
        private HashSet<int> expandedSounds = new HashSet<int>();

        private void OnEnable()
        {
            uiLibrary = (UILibrary)target;
            if (EditorGUIUtility.isProSkin == true) { customSkin = ReachUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = ReachUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            // Use default header (no custom texture)
            EditorGUILayout.LabelField("UI Sound Library", EditorStyles.boldLabel);

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
            var uiSounds = serializedObject.FindProperty("uiSounds");
            var defaultHoverSound = serializedObject.FindProperty("defaultHoverSound");
            var defaultClickSound = serializedObject.FindProperty("defaultClickSound");
            var defaultNotificationSound = serializedObject.FindProperty("defaultNotificationSound");
            var defaultErrorSound = serializedObject.FindProperty("defaultErrorSound");
            var defaultSuccessSound = serializedObject.FindProperty("defaultSuccessSound");
            var enableRandomization = serializedObject.FindProperty("enableRandomization");
            var enableCooldowns = serializedObject.FindProperty("enableCooldowns");

            switch (currentTab)
            {
                case 0:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    ReachUIEditorHandler.DrawProperty(libraryName, customSkin, "Library Name");
                    ReachUIEditorHandler.DrawProperty(description, customSkin, "Description");
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 10);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total Sounds:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(uiLibrary.GetSoundCount().ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(uiLibrary.GetTotalDuration())), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Categories:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(uiLibrary.GetAllCategories().Count.ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Validate Library", customSkin.button)) ValidateLibrary();
                    if (GUILayout.Button("Clear Library", customSkin.button))
                    {
                        if (EditorUtility.DisplayDialog("Clear Library", "Are you sure you want to clear all sounds?", "Yes", "No"))
                        {
                            uiLibrary.ClearLibrary();
                            EditorUtility.SetDirty(uiLibrary);
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
                    filterCategory = (UILibrary.UISoundCategory)EditorGUILayout.EnumPopup(filterCategory);
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        searchFilter = "";
                        filterCategory = UILibrary.UISoundCategory.Custom;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add New Sound", customSkin.button)) AddNewSound();
                    if (GUILayout.Button("Sort by Name", customSkin.button)) SortSoundsByName();
                    GUILayout.EndHorizontal();
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    var filteredSounds = GetFilteredSounds();
                    if (filteredSounds.Count == 0)
                        EditorGUILayout.HelpBox("No sounds found matching the current filter.", MessageType.Info);
                    else
                        for (int i = 0; i < filteredSounds.Count; i++)
                            DrawSoundItem(filteredSounds[i], i);
                    GUILayout.EndScrollView();
                    break;
                case 2:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    enableRandomization.boolValue = ReachUIEditorHandler.DrawToggle(enableRandomization.boolValue, customSkin, "Enable Randomization");
                    enableCooldowns.boolValue = ReachUIEditorHandler.DrawToggle(enableCooldowns.boolValue, customSkin, "Enable Cooldowns");
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
                    ReachUIEditorHandler.DrawProperty(defaultHoverSound, customSkin, "Default Hover Sound");
                    ReachUIEditorHandler.DrawProperty(defaultClickSound, customSkin, "Default Click Sound");
                    ReachUIEditorHandler.DrawProperty(defaultNotificationSound, customSkin, "Default Notification Sound");
                    ReachUIEditorHandler.DrawProperty(defaultErrorSound, customSkin, "Default Error Sound");
                    ReachUIEditorHandler.DrawProperty(defaultSuccessSound, customSkin, "Default Success Sound");
                    break;
                case 3:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(new GUIContent("Library Information"), customSkin.FindStyle("Text"));
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField($"Library Name: {uiLibrary.libraryName}");
                    EditorGUILayout.LabelField($"Sound Count: {uiLibrary.GetSoundCount()}");
                    EditorGUILayout.LabelField($"Total Duration: {FormatDuration(uiLibrary.GetTotalDuration())}");
                    EditorGUILayout.LabelField($"Categories: {string.Join(", ", uiLibrary.GetAllCategories())}");
                    GUILayout.EndVertical();
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
        private void DrawSoundItem(UILibrary.UISound sound, int index)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(expandedSounds.Contains(index) ? "Close" : "Edit", GUILayout.Width(70)))
            {
                if (expandedSounds.Contains(index)) expandedSounds.Remove(index);
                else expandedSounds.Add(index);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Delete Track", $"Are you sure you want to delete '{sound.soundName}'?", "Delete", "Cancel"))
                {
                    var uiSounds = serializedObject.FindProperty("uiSounds");
                    uiSounds.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    expandedSounds.Remove(index);
                    return; // Don't draw further for this item
                }
            }

            EditorGUILayout.LabelField(new GUIContent($"Sound {index + 1}"), customSkin.FindStyle("Text"), GUILayout.Width(60));
            EditorGUILayout.LabelField(new GUIContent(sound.soundName), customSkin.FindStyle("Text"), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (expandedSounds.Contains(index))
            {
                var uiSounds = serializedObject.FindProperty("uiSounds");
                if (index < uiSounds.arraySize)
                {
                    var soundProp = uiSounds.GetArrayElementAtIndex(index);
                    soundProp.isExpanded = true;
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(soundProp, true);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                if (sound.audioClip != null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(sound.audioClip.length)), customSkin.FindStyle("Text"));
                    EditorGUILayout.LabelField(new GUIContent("Category:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(new GUIContent(sound.category.ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("No audio clip assigned", MessageType.Warning);
                }
            }
            GUILayout.EndVertical();
        }
        private List<UILibrary.UISound> GetFilteredSounds()
        {
            var soundList = new List<UILibrary.UISound>();
            foreach (var sound in uiLibrary.uiSounds)
            {
                if (sound == null) continue;
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) || sound.soundName.ToLower().Contains(searchFilter.ToLower()) || sound.displayName.ToLower().Contains(searchFilter.ToLower());
                bool matchesCategory = filterCategory == UILibrary.UISoundCategory.Custom || sound.category == filterCategory;
                if (matchesSearch && matchesCategory) soundList.Add(sound);
            }
            return soundList;
        }
        private void AddNewSound()
        {
            var newSound = new UILibrary.UISound { soundName = "New Sound", displayName = "New Sound", category = UILibrary.UISoundCategory.Custom };
            uiLibrary.AddSound(newSound);
            EditorUtility.SetDirty(uiLibrary);
        }
        private void SortSoundsByName()
        {
            uiLibrary.uiSounds.Sort((a, b) => a.soundName.CompareTo(b.soundName));
            EditorUtility.SetDirty(uiLibrary);
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
            var soundNames = new HashSet<string>();
            for (int i = 0; i < uiLibrary.uiSounds.Count; i++)
            {
                var sound = uiLibrary.uiSounds[i];
                if (sound == null) { issues.Add($"Sound at index {i} is null"); continue; }
                if (string.IsNullOrEmpty(sound.soundName)) { issues.Add($"Sound at index {i} has no name"); continue; }
                if (soundNames.Contains(sound.soundName)) { issues.Add($"Duplicate sound name: {sound.soundName}"); }
                else { soundNames.Add(sound.soundName); }
                if (sound.audioClip == null) { issues.Add($"Sound '{sound.soundName}' has no audio clip"); }
                if (sound.category == UILibrary.UISoundCategory.Custom && string.IsNullOrEmpty(sound.customCategory)) { issues.Add($"Sound '{sound.soundName}' is set to Custom category but has no custom category name"); }
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