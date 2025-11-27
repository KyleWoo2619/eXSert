/*
 * Written by Will T
 * 
 * This code defines a custom property drawer for Unity's inspector that ensures a critical component reference is assigned.
 * It is designed for refernces that cannot be null and will attempt to auto-assign the reference if it is missing.
 * If it cannot be auto-assigned, it displays an error message in the inspector.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;

[CustomPropertyDrawer(typeof(CriticalReferenceDrawer))]
public class CriticalReferenceDrawer : PropertyDrawer
{
    // tracks auto-assigned (componentInstanceID.fieldName) so hierarchy can show yellow
    public static HashSet<string> autoAssignedSet = new HashSet<string>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // draw the normal field
        EditorGUI.PropertyField(position, property, label);

        // only works for Object references (components)
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return;

        // if it already has value, nothing to do (and clear autoAssigned flag for safety)
        if (property.objectReferenceValue != null)
        {
            string key = MakeKey(property);
            if (autoAssignedSet.Contains(key))
                autoAssignedSet.Remove(key);
            return;
        }

        // get the component that owns this property
        var targetComponent = property.serializedObject.targetObject as Component;
        if (targetComponent == null)
            return;

        Type requiredType = fieldInfo.FieldType;
        if (!typeof(Component).IsAssignableFrom(requiredType))
            return;

        // Try find on self
        Component found = targetComponent.GetComponent(requiredType);

        // Try parents
        if (found == null)
            found = targetComponent.GetComponentInParent(requiredType, true);

        // Try children
        if (found == null)
            found = targetComponent.GetComponentInChildren(requiredType, true);

        if (found != null)
        {
            // assign the found component
            property.objectReferenceValue = found;
            property.serializedObject.ApplyModifiedProperties();

            // mark as auto-assigned (not "saved" as user hasn't purposely set it)
            string key = MakeKey(property);
            if (!autoAssignedSet.Contains(key))
                autoAssignedSet.Add(key);

            // mark dirty so inspector knows something changed
            EditorUtility.SetDirty(targetComponent);
        }
        else
        {
            // nothing found; draw a small help line below the property
            var helpRect = new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + 2,
                position.width,
                EditorGUIUtility.singleLineHeight * 1.5f
            );

            EditorGUI.HelpBox(helpRect, $"Missing required component of type {requiredType.Name}.", MessageType.Warning);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return EditorGUIUtility.singleLineHeight;

        if (property.objectReferenceValue == null)
            return EditorGUIUtility.singleLineHeight * 3f;

        return EditorGUIUtility.singleLineHeight;
    }

    private string MakeKey(SerializedProperty property)
    {
        var target = property.serializedObject.targetObject as UnityEngine.Object;
        return $"{target.GetInstanceID()}.{fieldInfo.Name}";
    }
}