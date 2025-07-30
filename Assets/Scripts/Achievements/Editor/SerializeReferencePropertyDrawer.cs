#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Custom property drawer for SerializeReference fields that provides a dropdown
/// to select concrete implementations and manage array/list elements
/// </summary>
[CustomPropertyDrawer(typeof(AchievementCondition), true)]
public class AchievementConditionPropertyDrawer : PropertyDrawer
{
  private static readonly Dictionary<string, Type[]> typeCache = new Dictionary<string, Type[]>();
  private const float BUTTON_WIDTH = 60f;
  private const float SPACING = 8f;

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    EditorGUI.BeginProperty(position, label, property);

    // Get all available condition types
    Type[] conditionTypes = GetConditionTypes();

    // Calculate positions
    Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
    Rect dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
        position.width - EditorGUIUtility.labelWidth - BUTTON_WIDTH - SPACING, EditorGUIUtility.singleLineHeight);
    Rect buttonRect = new Rect(position.xMax - BUTTON_WIDTH, position.y, BUTTON_WIDTH, EditorGUIUtility.singleLineHeight);

    // Draw label
    EditorGUI.LabelField(labelRect, label);

    // Get current type
    Type currentType = property.managedReferenceValue?.GetType();
    string currentTypeName = currentType != null ? GetDisplayName(currentType) : "None";

    // Draw dropdown for type selection
    if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentTypeName), FocusType.Keyboard))
    {
      ShowTypeSelectionMenu(property, conditionTypes, dropdownRect);
    }

    // Draw clear button
    if (GUI.Button(buttonRect, "Clear"))
    {
      property.managedReferenceValue = null;
      property.serializedObject.ApplyModifiedProperties();
    }

    // Draw the actual condition properties if one is selected
    if (property.managedReferenceValue != null)
    {
      Rect propertyRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + SPACING,
          position.width, position.height - EditorGUIUtility.singleLineHeight - SPACING);

      EditorGUI.indentLevel++;
      DrawConditionProperties(propertyRect, property);
      EditorGUI.indentLevel--;
    }

    EditorGUI.EndProperty();
  }
  public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
  {
    float height = EditorGUIUtility.singleLineHeight;

    if (property.managedReferenceValue != null)
    {
      // Add space for the condition properties
      height += SPACING;

      // Calculate height by iterating through the same properties we draw
      SerializedProperty conditionProperty = property.Copy();
      if (conditionProperty.hasChildren)
      {
        conditionProperty.NextVisible(true); // Enter children
        do
        {
          if (conditionProperty.depth <= property.depth + 1) // Only direct children
          {
            height += EditorGUI.GetPropertyHeight(conditionProperty, true) + EditorGUIUtility.standardVerticalSpacing;
          }
        }
        while (conditionProperty.NextVisible(false));
      }
    }

    return height;
  }

  private void ShowTypeSelectionMenu(SerializedProperty property, Type[] conditionTypes, Rect rect)
  {
    GenericMenu menu = new GenericMenu();

    menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () =>
    {
      property.managedReferenceValue = null;
      property.serializedObject.ApplyModifiedProperties();
    });

    menu.AddSeparator("");

    foreach (Type conditionType in conditionTypes)
    {
      string displayName = GetDisplayName(conditionType);
      bool isSelected = property.managedReferenceValue?.GetType() == conditionType;

      menu.AddItem(new GUIContent(displayName), isSelected, () =>
      {
        property.managedReferenceValue = Activator.CreateInstance(conditionType);
        property.serializedObject.ApplyModifiedProperties();
      });
    }

    menu.DropDown(rect);
  }
  private void DrawConditionProperties(Rect position, SerializedProperty property)
  {
    float currentY = position.y;

    // Use SerializedProperty to draw the condition's fields
    SerializedProperty conditionProperty = property.Copy();
    if (conditionProperty.hasChildren)
    {
      conditionProperty.NextVisible(true); // Enter children
      do
      {
        if (conditionProperty.depth <= property.depth + 1) // Only direct children
        {
          float propertyHeight = EditorGUI.GetPropertyHeight(conditionProperty, true);
          Rect fieldRect = new Rect(position.x, currentY, position.width, propertyHeight);
          EditorGUI.PropertyField(fieldRect, conditionProperty, true);
          currentY += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
        }
      }
      while (conditionProperty.NextVisible(false));
    }
  }

  private Type[] GetConditionTypes()
  {
    string cacheKey = "AchievementCondition";

    if (!typeCache.TryGetValue(cacheKey, out Type[] types))
    {
      types = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(assembly => assembly.GetTypes())
          .Where(type => type.IsSubclassOf(typeof(AchievementCondition)) && !type.IsAbstract)
          .OrderBy(type => GetDisplayName(type))
          .ToArray();

      typeCache[cacheKey] = types;
    }

    return types;
  }
  private string GetDisplayName(Type type)
  {
    // Convert CamelCase to readable names
    string name = type.Name;
    if (name.EndsWith("Condition"))
    {
      name = name.Substring(0, name.Length - 9); // Remove "Condition"
    }

    // Add spaces before capital letters
    string result = "";
    for (int i = 0; i < name.Length; i++)
    {
      if (i > 0 && char.IsUpper(name[i]))
      {
        result += " ";
      }
      result += name[i];
    }

    return result;
  }
}

/// <summary>
/// Custom property drawer for arrays/lists of SerializeReference objects
/// Provides add/remove buttons and proper type selection
/// </summary>
[CustomPropertyDrawer(typeof(AchievementCondition[]), true)]
[CustomPropertyDrawer(typeof(List<AchievementCondition>), true)]
public class AchievementConditionArrayPropertyDrawer : PropertyDrawer
{
  private const float BUTTON_HEIGHT = 20f;
  private const float SPACING = 8f;

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    EditorGUI.BeginProperty(position, label, property);

    // Draw the array size field and foldout
    Rect foldoutRect = new Rect(position.x, position.y, position.width - 100f, EditorGUIUtility.singleLineHeight);
    Rect addButtonRect = new Rect(position.xMax - 100f, position.y, 50f, EditorGUIUtility.singleLineHeight);
    Rect clearButtonRect = new Rect(position.xMax - 45f, position.y, 45f, EditorGUIUtility.singleLineHeight);

    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded,
        $"{label.text} (Size: {property.arraySize})", true);

    // Add button
    if (GUI.Button(addButtonRect, "Add"))
    {
      ShowAddConditionMenu(property, addButtonRect);
    }

    // Clear all button
    if (GUI.Button(clearButtonRect, "Clear"))
    {
      property.ClearArray();
      property.serializedObject.ApplyModifiedProperties();
    }

    if (property.isExpanded)
    {
      EditorGUI.indentLevel++;

      float currentY = position.y + EditorGUIUtility.singleLineHeight + SPACING;

      for (int i = 0; i < property.arraySize; i++)
      {
        SerializedProperty element = property.GetArrayElementAtIndex(i);

        // Calculate height for this element
        float elementHeight = GetElementHeight(element);
        Rect elementRect = new Rect(position.x, currentY, position.width - 30f, elementHeight);
        Rect removeButtonRect = new Rect(position.xMax - 25f, currentY, 25f, EditorGUIUtility.singleLineHeight);

        // Draw the element
        EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"), true);

        // Draw remove button
        if (GUI.Button(removeButtonRect, "×"))
        {
          property.DeleteArrayElementAtIndex(i);
          property.serializedObject.ApplyModifiedProperties();
          break; // Exit loop to avoid index issues
        }

        currentY += elementHeight + SPACING;
      }

      EditorGUI.indentLevel--;
    }

    EditorGUI.EndProperty();
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
  {
    float height = EditorGUIUtility.singleLineHeight;

    if (property.isExpanded)
    {
      height += SPACING;

      for (int i = 0; i < property.arraySize; i++)
      {
        SerializedProperty element = property.GetArrayElementAtIndex(i);
        height += GetElementHeight(element) + SPACING;
      }
    }

    return height;
  }

  private float GetElementHeight(SerializedProperty element)
  {
    if (element.managedReferenceValue == null)
    {
      return EditorGUIUtility.singleLineHeight;
    }

    // Use the AchievementConditionPropertyDrawer to calculate height
    var drawer = new AchievementConditionPropertyDrawer();
    return drawer.GetPropertyHeight(element, GUIContent.none);
  }

  private void ShowAddConditionMenu(SerializedProperty arrayProperty, Rect rect)
  {
    GenericMenu menu = new GenericMenu();

    Type[] conditionTypes = GetConditionTypes();

    foreach (Type conditionType in conditionTypes)
    {
      string displayName = GetDisplayName(conditionType);

      menu.AddItem(new GUIContent(displayName), false, () =>
      {
        arrayProperty.arraySize++;
        SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        newElement.managedReferenceValue = Activator.CreateInstance(conditionType);
        arrayProperty.serializedObject.ApplyModifiedProperties();
      });
    }

    menu.DropDown(rect);
  }

  private Type[] GetConditionTypes()
  {
    return AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => type.IsSubclassOf(typeof(AchievementCondition)) && !type.IsAbstract)
        .OrderBy(type => GetDisplayName(type))
        .ToArray();
  }

  private string GetDisplayName(Type type)
  {
    string name = type.Name;
    if (name.EndsWith("Condition"))
    {
      name = name.Substring(0, name.Length - 9);
    }

    string result = "";
    for (int i = 0; i < name.Length; i++)
    {
      if (i > 0 && char.IsUpper(name[i]))
      {
        result += " ";
      }
      result += name[i];
    }

    return result;
  }
}
#endif
