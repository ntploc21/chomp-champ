#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Reach
{
    [CustomEditor(typeof(SceneManager))]
    public class SceneManagerEditor : Editor
    {
        private GUISkin customSkin;
        private SceneManager smTarget;
        private int currentTab;

        private void OnEnable()
        {
            smTarget = (SceneManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = ReachUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = ReachUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            ReachUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_PanelManager");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Settings");
            toolbarTabs[2] = new GUIContent("Events");

            currentTab = ReachUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Events", "Events"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var scenes = serializedObject.FindProperty("scenes");
            var currentSceneIndex = serializedObject.FindProperty("currentSceneIndex");
            
            var useLoadingScreen = serializedObject.FindProperty("useLoadingScreen");
            var loadingScreenPrefab = serializedObject.FindProperty("loadingScreenPrefab");
            var loadingAnimationIn = serializedObject.FindProperty("loadingAnimationIn");
            var loadingAnimationOut = serializedObject.FindProperty("loadingAnimationOut");
            var transitionSpeed = serializedObject.FindProperty("transitionSpeed");
            
            var initializeButtons = serializedObject.FindProperty("initializeButtons");
            var bypassLoadingOnEnable = serializedObject.FindProperty("bypassLoadingOnEnable");
            var updateMode = serializedObject.FindProperty("updateMode");
            var sceneMode = serializedObject.FindProperty("sceneMode");

            var onSceneChanged = serializedObject.FindProperty("onSceneChanged");
            var onSceneLoadStart = serializedObject.FindProperty("onSceneLoadStart");
            var onSceneLoadComplete = serializedObject.FindProperty("onSceneLoadComplete");

            switch (currentTab)
            {
                case 0:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (smTarget.currentSceneIndex > smTarget.scenes.Count - 1) { smTarget.currentSceneIndex = 0; }
                    if (smTarget.scenes.Count != 0)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();

                        GUI.enabled = false;
                        EditorGUILayout.LabelField(new GUIContent("Current Scene:"), customSkin.FindStyle("Text"), GUILayout.Width(82));
                        GUI.enabled = true;
                        EditorGUILayout.LabelField(new GUIContent(smTarget.scenes[currentSceneIndex.intValue].sceneName), customSkin.FindStyle("Text"));

                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        if (Application.isPlaying == true) { GUI.enabled = false; }

                        currentSceneIndex.intValue = EditorGUILayout.IntSlider(currentSceneIndex.intValue, 0, smTarget.scenes.Count - 1);

                        GUI.enabled = true;
                        GUILayout.EndVertical();
                    }

                    else { EditorGUILayout.HelpBox("Scene List is empty. Create a new item to see more options.", MessageType.Info); }

                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(scenes, new GUIContent("Scene Items"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();

                    if (smTarget.scenes.Count != 0 && GUILayout.Button("Add New Scene", customSkin.button))
                    {
                        smTarget.AddNewItem();
                        EditorUtility.SetDirty(smTarget);
                    }

                    break;

                case 1:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    
                    useLoadingScreen.boolValue = ReachUIEditorHandler.DrawToggle(useLoadingScreen.boolValue, customSkin, "Use Loading Screen", "Enable or disable loading screen functionality.");
                    
                    if (useLoadingScreen.boolValue == true)
                    {
                        ReachUIEditorHandler.DrawProperty(loadingScreenPrefab, customSkin, "Loading Screen Prefab");
                        ReachUIEditorHandler.DrawProperty(loadingAnimationIn, customSkin, "Loading Animation In");
                        ReachUIEditorHandler.DrawProperty(loadingAnimationOut, customSkin, "Loading Animation Out");
                    }
                    
                    ReachUIEditorHandler.DrawProperty(transitionSpeed, customSkin, "Transition Speed");
                    ReachUIEditorHandler.DrawProperty(sceneMode, customSkin, "Scene Mode");
                    ReachUIEditorHandler.DrawProperty(updateMode, customSkin, "Update Mode");
                    
                    GUILayout.Space(10);
                    
                    initializeButtons.boolValue = ReachUIEditorHandler.DrawToggle(initializeButtons.boolValue, customSkin, "Initialize Buttons", "Initialize buttons on scene change.");
                    bypassLoadingOnEnable.boolValue = ReachUIEditorHandler.DrawToggle(bypassLoadingOnEnable.boolValue, customSkin, "Bypass Loading On Enable", "Skips loading screen when the scene is enabled.");

                    break;

                case 2:
                    ReachUIEditorHandler.DrawHeader(customSkin, "Header_Events", 6);
                    
                    EditorGUILayout.PropertyField(onSceneChanged, new GUIContent("On Scene Changed"), true);
                    EditorGUILayout.PropertyField(onSceneLoadStart, new GUIContent("On Scene Load Start"), true);
                    EditorGUILayout.PropertyField(onSceneLoadComplete, new GUIContent("On Scene Load Complete"), true);

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif 