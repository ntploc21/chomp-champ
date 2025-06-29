#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    [CustomEditor(typeof(SFXLibrary))]
    public class SFXLibraryEditor : Editor
    {
        private SFXLibrary sfxLibrary;
        private GUISkin customSkin;
        private int currentTab = 0;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private SFXLibrary.SFXCategory filterCategory = SFXLibrary.SFXCategory.Custom;
        private HashSet<int> expandedSFX = new HashSet<int>();

        private void OnEnable()
        {
            sfxLibrary = (SFXLibrary)target;
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
            var sfxClips = serializedObject.FindProperty("sfxClips");
            var defaultSFX = serializedObject.FindProperty("defaultSFX");
            var enableRandomization = serializedObject.FindProperty("enableRandomization");

            switch (currentTab)
            {
                case 0:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    ReachUIEditorHandler.DrawProperty(libraryName, customSkin, "Library Name");
                    ReachUIEditorHandler.DrawProperty(description, customSkin, "Description");
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 10);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total SFX:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(sfxLibrary.GetSFXCount().ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Total Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(sfxLibrary.GetTotalDuration())), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Categories:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.LabelField(new GUIContent(sfxLibrary.GetAllCategories().Count.ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Validate Library", customSkin.button)) ValidateLibrary();
                    if (GUILayout.Button("Clear Library", customSkin.button))
                    {
                        if (EditorUtility.DisplayDialog("Clear Library", "Are you sure you want to clear all SFX?", "Yes", "No"))
                        {
                            sfxLibrary.ClearLibrary();
                            EditorUtility.SetDirty(sfxLibrary);
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
                    filterCategory = (SFXLibrary.SFXCategory)EditorGUILayout.EnumPopup(filterCategory);
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        searchFilter = "";
                        filterCategory = SFXLibrary.SFXCategory.Custom;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add New SFX", customSkin.button)) AddNewSFX();
                    if (GUILayout.Button("Sort by Name", customSkin.button)) SortSFXByName();
                    GUILayout.EndHorizontal();
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    var filteredSFX = GetFilteredSFX();
                    if (filteredSFX.Count == 0)
                        EditorGUILayout.HelpBox("No SFX found matching the current filter.", MessageType.Info);
                    else
                        for (int i = 0; i < filteredSFX.Count; i++)
                            DrawSFXItem(filteredSFX[i], i);
                    GUILayout.EndScrollView();
                    break;
                case 2:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    enableRandomization.boolValue = ReachUIEditorHandler.DrawToggle(enableRandomization.boolValue, customSkin, "Enable Randomization");
                    ReachUIEditorHandler.DrawProperty(defaultSFX, customSkin, "Default SFX");
                    break;
                case 3:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Support", 6);
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(new GUIContent("Library Information"), customSkin.FindStyle("Text"));
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField($"Library Name: {sfxLibrary.libraryName}");
                    EditorGUILayout.LabelField($"SFX Count: {sfxLibrary.GetSFXCount()}");
                    EditorGUILayout.LabelField($"Total Duration: {FormatDuration(sfxLibrary.GetTotalDuration())}");
                    EditorGUILayout.LabelField($"Categories: {string.Join(", ", sfxLibrary.GetAllCategories())}");
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
        private void DrawSFXItem(SFXLibrary.SFXClip sfx, int index)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(expandedSFX.Contains(index) ? "Close" : "Edit", GUILayout.Width(70)))
            {
                if (expandedSFX.Contains(index)) expandedSFX.Remove(index);
                else expandedSFX.Add(index);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Delete Track", $"Are you sure you want to delete '{sfx.sfxName}'?", "Delete", "Cancel"))
                {
                    var sfxProp = serializedObject.FindProperty("sfxClips");
                    sfxProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    expandedSFX.Remove(index);
                    return; // Don't draw further for this item
                }
            }


            EditorGUILayout.LabelField(new GUIContent($"SFX {index + 1}"), customSkin.FindStyle("Text"), GUILayout.Width(60));
            EditorGUILayout.LabelField(new GUIContent(sfx.sfxName), customSkin.FindStyle("Text"), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (expandedSFX.Contains(index))
            {
                var sfxClips = serializedObject.FindProperty("sfxClips");
                if (index < sfxClips.arraySize)
                {
                    var sfxProp = sfxClips.GetArrayElementAtIndex(index);
                    sfxProp.isExpanded = true;
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sfxProp, true);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                if (sfx.audioClip != null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Duration:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(new GUIContent(FormatDuration(sfx.audioClip.length)), customSkin.FindStyle("Text"));
                    EditorGUILayout.LabelField(new GUIContent("Category:"), customSkin.FindStyle("Text"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(new GUIContent(sfx.category.ToString()), customSkin.FindStyle("Text"));
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("No audio clip assigned", MessageType.Warning);
                }
            }
            GUILayout.EndVertical();
        }
        private List<SFXLibrary.SFXClip> GetFilteredSFX()
        {
            var sfxList = new List<SFXLibrary.SFXClip>();
            foreach (var sfx in sfxLibrary.sfxClips)
            {
                if (sfx == null) continue;
                bool matchesSearch = string.IsNullOrEmpty(searchFilter) || sfx.sfxName.ToLower().Contains(searchFilter.ToLower()) || sfx.displayName.ToLower().Contains(searchFilter.ToLower());
                bool matchesCategory = filterCategory == SFXLibrary.SFXCategory.Custom || sfx.category == filterCategory;
                if (matchesSearch && matchesCategory) sfxList.Add(sfx);
            }
            return sfxList;
        }
        private void AddNewSFX()
        {
            var newSFX = new SFXLibrary.SFXClip { sfxName = "New SFX", displayName = "New SFX", category = SFXLibrary.SFXCategory.Custom };
            sfxLibrary.AddSFX(newSFX);
            EditorUtility.SetDirty(sfxLibrary);
        }
        private void SortSFXByName()
        {
            sfxLibrary.sfxClips.Sort((a, b) => a.sfxName.CompareTo(b.sfxName));
            EditorUtility.SetDirty(sfxLibrary);
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
            var sfxNames = new HashSet<string>();
            for (int i = 0; i < sfxLibrary.sfxClips.Count; i++)
            {
                var sfx = sfxLibrary.sfxClips[i];
                if (sfx == null) { issues.Add($"SFX at index {i} is null"); continue; }
                if (string.IsNullOrEmpty(sfx.sfxName)) { issues.Add($"SFX at index {i} has no name"); continue; }
                if (sfxNames.Contains(sfx.sfxName)) { issues.Add($"Duplicate SFX name: {sfx.sfxName}"); }
                else { sfxNames.Add(sfx.sfxName); }
                if (sfx.audioClip == null) { issues.Add($"SFX '{sfx.sfxName}' has no audio clip"); }
                if (sfx.category == SFXLibrary.SFXCategory.Custom && string.IsNullOrEmpty(sfx.customCategory)) { issues.Add($"SFX '{sfx.sfxName}' is set to Custom category but has no custom category name"); }
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