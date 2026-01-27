using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class CriticalReferenceProjectScanner
{
    [MenuItem("Tools/Validate All Critical References")]
    public static void ValidateProject()
    {
        var missing = new List<string>();

        // Find all MonoBehaviours in project
        string[] guids = AssetDatabase.FindAssets("t:MonoBehaviour");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoBehaviour[] behaviours = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<MonoBehaviour>().ToArray();

            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                CheckObject(behaviour, path, missing);
            }
        }

        // Scenes
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue; // skip missing scripts (null entries)
                    CheckObject(mb, path, missing);
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }

        if (missing.Count == 0)
        {
            Debug.Log("✅ All required references are valid.");
        }
        else
        {
            Debug.LogError("❌ Missing Required References:\n" + string.Join("\n", missing));
        }
    }

    private static void CheckObject(Object obj, string assetPath, List<string> missing)
    {
        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(CriticalReference), false))
                continue;

            var value = field.GetValue(obj);

            if (value == null)
            {
                missing.Add($"{assetPath} → {obj.GetType().Name}.{field.Name}");
            }
        }
    }
}
