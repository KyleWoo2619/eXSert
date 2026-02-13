using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ProgressionZoneFactoryEditor : EditorWindow
{
    private Vector2 scroll;
    private Type[] concreteTypes;

    [MenuItem("GameObject/Progression/Create Progression Zone...", false, 10)]
    private static void OpenWindow()
    {
        var window = GetWindow<ProgressionZoneFactoryEditor>("Create Progression Zone");
        window.RefreshTypes();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshTypes();
    }

    private void RefreshTypes()
    {
        // Find the base type in loaded assemblies. Adjust namespace if necessary.
        var baseType = Type.GetType("Progression.ProgressionZone, Assembly-CSharp");
        if (baseType == null)
        {
            // fallback search across assemblies
            baseType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypesSafe())
                .FirstOrDefault(t => t.Name == "ProgressionZone" && t.IsAbstract);
        }

        if (baseType == null)
        {
            concreteTypes = Array.Empty<Type>();
            return;
        }

        concreteTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypesSafe())
            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Disabled while in Play mode.", MessageType.Info);
            return;
        }

        if (concreteTypes == null || concreteTypes.Length == 0)
        {
            EditorGUILayout.HelpBox("No concrete ProgressionZone subclasses found. Create at least one non-abstract class that inherits ProgressionZone.", MessageType.Warning);
            if (GUILayout.Button("Refresh")) RefreshTypes();
            return;
        }

        EditorGUILayout.LabelField("Create a Progression Zone of type:", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var type in concreteTypes)
        {
            if (GUILayout.Button(type.FullName))
            {
                CreateProgressionZoneOfType(type);
            }
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Refresh"))
            RefreshTypes();
    }

    private void CreateProgressionZoneOfType(Type type)
    {
        GameObject go = new GameObject(type.Name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {type.Name}");
        GameObject parent = Selection.activeGameObject;
        if (parent != null)
            GameObjectUtility.SetParentAndAlign(go, parent);

        // Ensure BoxCollider exists and is trigger (ProgressionZone requires it)
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(5f, 2f, 5f);

        // Add the progression zone component using reflection
        go.AddComponent(type);

        // Place at scene view pivot if available
        if (SceneView.lastActiveSceneView != null)
            go.transform.position = SceneView.lastActiveSceneView.pivot;

        Selection.activeGameObject = go;

        // Mark scene dirty
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            EditorSceneManager.MarkSceneDirty(go.scene);
    }
}

// Small helper extension to safely get types from assemblies without throwing on reflection-only or dynamic assemblies.
internal static class ReflectionHelpers
{
    public static System.Collections.Generic.IEnumerable<Type> GetTypesSafe(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
