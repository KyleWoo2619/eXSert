#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Remaps animation clip paths when moving Animator to a different hierarchy level.
/// USE ONCE ONLY, then delete this script.
/// </summary>
public class AnimationPathRemapper : EditorWindow
{
    private string oldRootPath = ""; // Leave empty if clips were on root
    private string newRootPath = "Roomba_Model"; // New parent path

    [MenuItem("Tools/Remap Animation Paths")]
    static void ShowWindow()
    {
        GetWindow<AnimationPathRemapper>("Remap Animation Paths");
    }

    void OnGUI()
    {
        GUILayout.Label("Animation Path Remapper", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "WARNING: This will modify all animation clips in your project!\n" +
            "Make a backup first!\n\n" +
            "Use this if you moved the Animator component to a different GameObject.",
            MessageType.Warning);

        oldRootPath = EditorGUILayout.TextField("Old Root Path:", oldRootPath);
        newRootPath = EditorGUILayout.TextField("New Root Path:", newRootPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Remap All Boss Animation Clips"))
        {
            RemapAllClips();
        }
    }

    void RemapAllClips()
    {
        // Find all animation clips with "Roomba" in the name
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip Roomba");
        
        int remappedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip != null && RemapClip(clip))
            {
                EditorUtility.SetDirty(clip);
                remappedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Remapped {remappedCount} animation clips!");
        EditorUtility.DisplayDialog("Done", $"Remapped {remappedCount} clips.\nCheck console for details.", "OK");
    }

    bool RemapClip(AnimationClip clip)
    {
        // Get all curve bindings
        var curveBindings = AnimationUtility.GetCurveBindings(clip);
        var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        bool modified = false;

        // Remap curve bindings
        foreach (var binding in curveBindings)
        {
            string newPath = RemapPath(binding.path);
            if (newPath != binding.path)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                
                // Remove old binding
                AnimationUtility.SetEditorCurve(clip, binding, null);
                
                // Add new binding
                var newBinding = binding;
                newBinding.path = newPath;
                AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                
                modified = true;
            }
        }

        // Remap object reference bindings
        foreach (var binding in objectBindings)
        {
            string newPath = RemapPath(binding.path);
            if (newPath != binding.path)
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                
                // Remove old binding
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                
                // Add new binding
                var newBinding = binding;
                newBinding.path = newPath;
                AnimationUtility.SetObjectReferenceCurve(clip, newBinding, curve);
                
                modified = true;
            }
        }

        if (modified)
        {
            Debug.Log($"Remapped: {clip.name}");
        }

        return modified;
    }

    string RemapPath(string oldPath)
    {
        // If oldRootPath is empty, we're adding a prefix
        if (string.IsNullOrEmpty(oldRootPath))
        {
            // Add new root path as prefix
            if (string.IsNullOrEmpty(oldPath))
                return newRootPath;
            return $"{newRootPath}/{oldPath}";
        }
        
        // Replace old root with new root
        if (oldPath.StartsWith(oldRootPath))
        {
            string remainder = oldPath.Substring(oldRootPath.Length).TrimStart('/');
            if (string.IsNullOrEmpty(newRootPath))
                return remainder;
            return $"{newRootPath}/{remainder}";
        }

        return oldPath;
    }
}
#endif
