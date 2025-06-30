using UnityEditor;
using UnityEngine;

namespace Gyvr.Mythril2D
{
    [CustomPropertyDrawer(typeof(BooleanEntry))]
    public class BooleanEntryPropertyDrawer : PropertyDrawer
    {
        private const int fieldOffset = 3;
        private const int checkboxSize = 20;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty id = property.FindPropertyRelative("id");
            SerializedProperty value = property.FindPropertyRelative("value");

            var idRect = new Rect(position.x, position.y, position.width - checkboxSize, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(idRect.xMax + fieldOffset, position.y, checkboxSize, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(idRect, id, GUIContent.none);
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
