using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class CriticalReferenceHighlighter
{

    static CriticalReferenceHighlighter()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    // Highlight colors
    private static readonly Color MissingColor = new Color(1f, 0.3f, 0.3f);      // Red
    private static readonly Color AutoAssignedColor = new Color(1f, 0.8f, 0.3f); // Yellow

    private static void OnHierarchyGUI(int instanceID, Rect rect)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);
        if (!(obj is GameObject go))
            return;

        // Check for missing / auto-assigned fields
        bool hasMissing = false;
        bool hasAutoAssigned = false;
        List<string> missingFields = null;

        foreach (var component in go.GetComponents<MonoBehaviour>())
        {
            if (component == null)
                continue;

            var fields = component.GetType().GetFields(BindingFlags.Instance |
                                                      BindingFlags.Public |
                                                      BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(CriticalReference), false))
                    continue;

                object value = field.GetValue(component);

                // Null → missing reference
                if (value == null)
                {
                    if (!hasMissing)
                    {
                        hasMissing = true;
                        missingFields = new List<string>();
                    }

                    missingFields.Add($"{component.GetType().Name}.{field.Name}");
                    continue;
                }

                // Detect "auto-assigned but not saved" using the drawer's transient tracking set
                string key = $"{component.GetInstanceID()}.{field.Name}";
                if (CriticalReferenceDrawer.autoAssignedSet.Contains(key))
                {
                    hasAutoAssigned = true;
                }
            }
        }

        // Nothing to highlight
        if (!hasMissing && !hasAutoAssigned)
            return;

        // Draw label with correct offset (fixed cube icon overlap)
        if (hasMissing)
            DrawColoredLabel(rect, go.name, MissingColor);
        else if (hasAutoAssigned)
            DrawColoredLabel(rect, go.name, AutoAssignedColor);

        // Tooltip for missing fields
        if (hasMissing && missingFields != null)
        {
            string tooltip = "Missing Required References:\n" + string.Join("\n", missingFields);
            GUI.Label(rect, new GUIContent("", tooltip));
        }
    }

    /// <summary>
    /// Draws colored label without overlapping Unity's hierarchy icon.
    /// </summary>
    private static void DrawColoredLabel(Rect rect, string label, Color color)
    {
        const float iconSize = 16f;
        const float spacing = 1f;

        float labelX = rect.x + iconSize + spacing;

        Rect labelRect = new Rect(
            labelX,
            rect.y,
            rect.width - (iconSize + spacing),
            rect.height
        );

        // Clone EditorStyles.label without changing font size
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Normal; // match hierarchy text exactly

        EditorGUI.LabelField(labelRect, label, style);
    }
}
