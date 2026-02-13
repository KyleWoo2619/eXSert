using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Progression;

public static class ProgressionManagerCreator
{
    [MenuItem("GameObject/Progression/Create Progression Manager", false, 9)]
    private static void CreateProgressionManager(MenuCommand menuCommand)
    {
        if (Application.isPlaying)
            return;

        // Check for existing ProgressionManager in the currently loaded scenes
        var existing = Object.FindFirstObjectByType<ProgressionManager>();
        if (existing != null)
        {
            bool createAnyway = EditorUtility.DisplayDialog(
                "Progression Manager Exists",
                $"A ProgressionManager already exists in the scene ({existing.gameObject.name}). Creating another may be unnecessary.\n\nCreate another anyway?",
                "Create",
                "Cancel");

            if (!createAnyway)
                return;
        }

        // Create the GameObject and register undo
        var go = new GameObject("Progression Manager");
        Undo.RegisterCreatedObjectUndo(go, "Create Progression Manager");
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        // Add the ProgressionManager component
        go.AddComponent<ProgressionManager>();

        // Place at Scene view pivot if available
        if (SceneView.lastActiveSceneView != null)
            go.transform.position = SceneView.lastActiveSceneView.pivot;

        Selection.activeGameObject = go;

        // Mark scene dirty so user can save
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            EditorSceneManager.MarkSceneDirty(go.scene);
    }

    // Disable menu while playing
    [MenuItem("GameObject/Progression/Create Progression Manager", true)]
    private static bool CreateProgressionManager_Validate()
    {
        return !Application.isPlaying;
    }
}