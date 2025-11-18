// Assets/Editor/AddComponentChooserWindow.cs
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class AddComponentChooserWindow : EditorWindow
{
    private GameObject targetGameObject;
    private List<(Component comp, string fieldName, Type fieldType)> missingList;
    private Vector2 scroll;

    private struct Candidate
    {
        public GameObject obj;
        public string display;
    }

    public static void ShowWindow(GameObject go, List<(Component comp, string fieldName, Type fieldType)> missing)
    {
        if (missing == null || missing.Count == 0)
        {
            Debug.Log("No missing fields to fix.");
            return;
        }

        var w = GetWindow<AddComponentChooserWindow>(true, "Add / Auto-Fix Required Components", true);
        w.targetGameObject = go;
        w.missingList = missing;
        w.minSize = new Vector2(420, 200);
        w.Show();
    }

    private void OnGUI()
    {
        if (targetGameObject == null)
        {
            EditorGUILayout.LabelField("No GameObject selected.");
            if (GUILayout.Button("Close")) Close();
            return;
        }

        EditorGUILayout.LabelField($"GameObject: {targetGameObject.name}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // List missing fields
        EditorGUILayout.LabelField("Missing required fields:", EditorStyles.label);
        foreach (var m in missingList)
        {
            EditorGUILayout.LabelField($"• {m.comp.GetType().Name}.{m.fieldName} ({m.fieldType.Name})");
        }

        EditorGUILayout.Space();

        // Build candidate list: this, parents, children
        var candidates = new List<Candidate>();
        candidates.Add(new Candidate { obj = targetGameObject, display = "This GameObject" });

        var parent = targetGameObject.transform.parent;
        int depth = 0;
        while (parent != null && depth < 10) // limit depth reasonably
        {
            candidates.Add(new Candidate { obj = parent.gameObject, display = $"Parent: {parent.gameObject.name}" });
            parent = parent.parent;
            depth++;
        }

        // children: show immediate children (optionally could list deeper)
        foreach (Transform child in targetGameObject.transform)
            candidates.Add(new Candidate { obj = child.gameObject, display = $"Child: {child.gameObject.name}" });

        EditorGUILayout.LabelField("Where to add the component(s)?", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
        foreach (var cand in candidates)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(cand.display, GUILayout.Width(260));
            if (GUILayout.Button("Auto-assign existing", GUILayout.Width(140)))
            {
                TryAutoAssignToCandidate(cand.obj);
                Close();
                return;
            }
            if (GUILayout.Button("Add missing", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Confirm Add",
                    $"Add the missing component type(s) to \"{cand.obj.name}\"? This will modify the scene/prefab.",
                    "Add", "Cancel"))
                {
                    AddMissingToCandidate(cand.obj);
                    Close();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Cancel")) Close();
    }

    // Try to auto-assign: if a required component already exists on candidate (or parents/children) assign to the missing field
    private void TryAutoAssignToCandidate(GameObject candidate)
    {
        foreach (var m in missingList)
        {
            var comp = m.comp;
            var field = comp.GetType().GetField(m.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) continue;

            // try find in candidate, candidate parents, children
            Component found = candidate.GetComponent(m.fieldType);
            if (found == null)
                found = candidate.GetComponentInParent(m.fieldType, true);
            if (found == null)
                found = candidate.GetComponentInChildren(m.fieldType, true);

            if (found != null)
            {
                Undo.RecordObject(comp, "Auto-assign component reference");
                field.SetValue(comp, found);
                EditorUtility.SetDirty(comp);
            }
            else
            {
                // try look anywhere in the candidate's scene (optional) - skip for safety
            }
        }

        // Clear autoAssigned flags for fields we fixed manually via drawer mechanism if necessary
        // We don't track here which fields were set by drawer; removing possible stale flags is okay
        RepaintHierarchy();
    }

    private void AddMissingToCandidate(GameObject candidate)
    {
        foreach (var m in missingList)
        {
            var comp = m.comp;
            var field = comp.GetType().GetField(m.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) continue;

            // If the component already exists on a suitable object, assign it first
            Component found = candidate.GetComponent(m.fieldType);
            if (found == null)
                found = candidate.AddComponent(m.fieldType);
            else
            {
                // component exists already; nothing to add
            }

            // assign
            Undo.RecordObject(comp, "Assign required component reference");
            field.SetValue(comp, found);
            EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(candidate);
        }

        // mark scene dirty
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        RepaintHierarchy();
    }

    private void RepaintHierarchy()
    {
        // ensure hierarchy repaints to reflect the fixed states
        EditorApplication.RepaintHierarchyWindow();
    }
}