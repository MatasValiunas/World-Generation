using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfEnumAttribute))]
public class ShowIfEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfEnumAttribute showIf = (ShowIfEnumAttribute)attribute;
        SerializedProperty enumProp = property.serializedObject.FindProperty(showIf.enumFieldName);

        if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        string currentValue = enumProp.enumNames[enumProp.enumValueIndex];
        foreach (var value in showIf.enumValues)
        {
            if (currentValue == value.ToString())
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfEnumAttribute showIf = (ShowIfEnumAttribute)attribute;
        SerializedProperty enumProp = property.serializedObject.FindProperty(showIf.enumFieldName);

        if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        string currentValue = enumProp.enumNames[enumProp.enumValueIndex];
        foreach (var value in showIf.enumValues)
        {
            if (currentValue == value.ToString())
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        return -EditorGUIUtility.standardVerticalSpacing; // hide
    }
}