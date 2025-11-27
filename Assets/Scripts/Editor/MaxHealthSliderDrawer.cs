// MaxHealthSliderDrawer.cs
// Purpose: Drawer for MaxHealthSliderAttribute to render the slider in the inspector.
// Works with: MaxHealthSliderAttribute.

using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MaxHealthSliderAttribute))]
public class MaxHealthSliderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty maxHealthProp = property.serializedObject.FindProperty("maxHealth");
        float max = maxHealthProp != null ? maxHealthProp.floatValue : 100f;
        property.floatValue = EditorGUI.Slider(position, label, property.floatValue, 0, max);
    }
}
#endif
