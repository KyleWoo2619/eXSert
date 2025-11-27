using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class CriticalReferenceSafetyGate : IPreprocessBuildWithReport
{
    static CriticalReferenceSafetyGate()
    {
        // Block Play Mode
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    // ----------------------
    // Play Mode Blocking
    // ----------------------
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var missing = GatherMissingReferencesInActiveScene();
            if (missing.Count > 0)
            {
                // Cancel Play Mode
                EditorApplication.isPlaying = false;

                // Force all editor windows to repaint
                EditorApplication.RepaintHierarchyWindow();
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();

                Debug.LogError("❌ Play Mode blocked: missing required critical references:\n" +
                               string.Join("\n", missing));
            }
        }
    }

    // ----------------------
    // Build Blocking
    // ----------------------
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var missing = GatherMissingReferencesInActiveScene();
        if (missing.Count > 0)
        {
            string msg = "❌ Build blocked: missing required critical references:\n" +
                         string.Join("\n", missing);
            Debug.LogError(msg);
            throw new BuildFailedException(msg);
        }
    }

    // ----------------------
    // Core scanning logic
    // ----------------------
    private static List<string> GatherMissingReferencesInActiveScene()
    {
        var missing = new List<string>();
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.isLoaded)
            return missing;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                CheckComponent(mb, scene.name, missing);
            }
        }

        return missing;
    }

    private static void CheckComponent(Object obj, string sceneName, List<string> missingList)
    {
        if (obj == null) return;

        var fields = obj.GetType().GetFields(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(CriticalReference), false))
                continue;

            var value = field.GetValue(obj);
            if (value == null)
                missingList.Add($"{sceneName} → {obj.GetType().Name}.{field.Name} (GameObject: {((obj as Component)?.gameObject.name ?? "N/A")})");
        }
    }
}
