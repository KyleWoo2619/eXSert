// Editor-only: show a property only when another bool field is true
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Apply like: [ShowIfTrue("isInGame")] public GameObject pauseMenuUI;
/// The drawer shows the property in the inspector only when the bool field is present and true.
/// </summary>
public class ShowIfTrueAttribute : PropertyAttribute
{
    public readonly string BoolFieldName;
    public ShowIfTrueAttribute(string boolFieldName)
    {
        BoolFieldName = boolFieldName;
    }
}

[CustomPropertyDrawer(typeof(ShowIfTrueAttribute))]
public class ShowIfTrueDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var showAttr = (ShowIfTrueAttribute)attribute;
        var boolProp = property.serializedObject.FindProperty(showAttr.BoolFieldName);

        if (boolProp != null && boolProp.propertyType == SerializedPropertyType.Boolean && boolProp.boolValue)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        else
        {
            // Reserve a single-line height so the inspector doesn't abruptly shrink.
            // Draw a subtle disabled label to indicate why the field is hidden.
            using (new EditorGUI.DisabledScope(true))
            {
                var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(rect, label.text + " (hidden)");
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var showAttr = (ShowIfTrueAttribute)attribute;
        var boolProp = property.serializedObject.FindProperty(showAttr.BoolFieldName);

        if (boolProp != null && boolProp.propertyType == SerializedPropertyType.Boolean && boolProp.boolValue)
            return EditorGUI.GetPropertyHeight(property, label, true);

        // When hidden, return a single-line height to prevent large layout jumps in the inspector.
        return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}

#endif
