using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class CriticalReferenceProjectScanner
{
    [MenuItem("Tools/Validate All Critical References")]
    public static void ValidateProject()
    {
        var missing = new HashSet<string>();

        // Gather asset guids for types that can contain MonoBehaviours or references
        var searchFilters = new[] { "t:Prefab", "t:GameObject", "t:Scene", "t:ScriptableObject" };
        var allGuids = new HashSet<string>();
        foreach (var filter in searchFilters)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
                allGuids.Add(guid);
        }

        int total = allGuids.Count;
        int i = 0;

        try
        {
            foreach (var guid in allGuids)
            {
                i++;
                var progress = (float)i / total;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("Validating Critical References", assetPath, progress);

                // Skip anything not in the project's Assets folder to avoid read-only package files
                if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/"))
                    continue;

                // Skip Unity's temporary or recovery folders which can contain incomplete assets
                if (assetPath.Contains("/_Recovery/") || assetPath.StartsWith("Assets/_Recovery") || assetPath.Contains("/Library/") || assetPath.Contains("/Packages/"))
                    continue;

                // Handle prefabs safely using PrefabUtility to avoid working with live scene objects
                if (assetPath.EndsWith(".prefab"))
                {
                    GameObject prefabRoot = null;
                    try
                    {
                        prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                        if (prefabRoot != null)
                        {
                            try
                            {
                                foreach (var mb in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
                                {
                                    if (mb == null) continue;
                                    CheckObject(mb, assetPath, missing, new HashSet<int>());
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"Error scanning prefab components '{assetPath}': {ex.Message}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Skipping prefab '{assetPath}' — could not load: {ex.Message}");
                    }
                    finally
                    {
                        if (prefabRoot != null)
                            PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }

                    continue;
                }

                // ScriptableObjects: load directly
                if (assetPath.EndsWith(".asset") || AssetDatabase.LoadMainAssetAtPath(assetPath) is ScriptableObject)
                {
                    ScriptableObject so = null;
                    try
                    {
                        so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                        if (so != null)
                        {
                            CheckObject(so, assetPath, missing, new HashSet<int>());
                            continue;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Skipping scriptable object '{assetPath}' — could not load: {ex.Message}");
                    }
                }

                // Scenes: only handle scenes inside Assets folder (skip package/read-only scenes)
                if (assetPath.EndsWith(".unity"))
                {
                    try
                    {
                        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(assetPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                            {
                                if (mb == null) continue; // skip missing scripts (null entries)
                                CheckObject(mb, assetPath, missing, new HashSet<int>());
                            }
                        }
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Skipping scene '{assetPath}' — could not open: {ex.Message}");
                    }

                    continue;
                }

                // As a fallback, inspect assets returned by LoadAllAssetsAtPath but only if they're safe types
                var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var asset in assets)
                {
                    if (asset == null) continue;

                    try
                    {
                        if (asset is MonoBehaviour mbAsset)
                        {
                            CheckObject(mbAsset, assetPath, missing, new HashSet<int>());
                        }
                        else if (asset is ScriptableObject soAsset)
                        {
                            CheckObject(soAsset, assetPath, missing, new HashSet<int>());
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error scanning asset '{assetPath}': {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
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

    private static void CheckObject(Object obj, string assetPath, HashSet<string> missing, HashSet<int> visited)
    {
        if (obj == null) return;

        // Prevent infinite recursion for objects referenced multiple times
        int id = obj.GetInstanceID();
        if (visited.Contains(id)) return;
        visited.Add(id);

        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(CriticalReference), false))
                continue;

            object value = null;
            try
            {
                value = field.GetValue(obj);
            }
            catch
            {
                // Reflection might fail for some Unity managed backing fields; skip these safely
                continue;
            }

            string context = $"{assetPath} → {type.Name}.{field.Name}";

            // If field is UnityEngine.Object (includes GameObject, Component, ScriptableObject etc.)
            if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                if (value == null)
                {
                    missing.Add(context);
                }
                else if (value is Object uobj)
                {
                    // For Unity objects, we may want to inspect nested serialized objects too
                    // e.g. ScriptableObjects assigned to fields
                    if (!(uobj is GameObject) && !(uobj is Component))
                    {
                        // Recursively check assigned ScriptableObjects
                        CheckObject(uobj, assetPath, missing, visited);
                    }
                }

                // done with this field
                continue;
            }

            // Handle collections (arrays, lists, IEnumerable) - check for null elements
            if (value is IEnumerable enumerable && !(value is string))
            {
                int idx = 0;
                foreach (var element in enumerable)
                {
                    var elementContext = context + $"[{idx}]";

                    if (element == null)
                    {
                        missing.Add(elementContext);
                    }
                    else
                    {
                        // If element is UnityEngine.Object, check null-ness (already non-null) and recurse for ScriptableObjects
                        if (element is Object elementObj)
                        {
                            if (!(elementObj is GameObject) && !(elementObj is Component))
                                CheckObject(elementObj, assetPath, missing, visited);
                        }
                        else
                        {
                            // For POCO elements, check their fields recursively if any are marked
                            CheckPocoFields(element, assetPath, missing, visited, elementContext);
                        }
                    }

                    idx++;
                }

                continue;
            }

            // For non-Unity, non-collection objects (nested serializable classes), recurse into their fields
            if (value != null && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum && !field.FieldType.Equals(typeof(string)))
            {
                CheckPocoFields(value, assetPath, missing, visited, context);
            }

            // If value is null (and not UnityEngine.Object) - consider this missing too
            if (value == null)
            {
                missing.Add(context);
            }
        }
    }

    private static void CheckPocoFields(object obj, string assetPath, HashSet<string> missing, HashSet<int> visited, string baseContext)
    {
        if (obj == null) return;

        // Use runtime object identity to avoid cycles where possible
        if (obj is Object unityObj)
        {
            CheckObject(unityObj, assetPath, missing, visited);
            return;
        }

        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!field.IsDefined(typeof(CriticalReference), false))
                continue;

            object value = null;
            try { value = field.GetValue(obj); } catch { continue; }

            var context = baseContext + "." + field.Name;

            if (value == null)
            {
                missing.Add(context);
                continue;
            }

            if (value is Object uobj)
            {
                CheckObject(uobj, assetPath, missing, visited);
                continue;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                int idx = 0;
                foreach (var el in enumerable)
                {
                    var elContext = context + $"[{idx}]";
                    if (el == null) missing.Add(elContext);
                    else if (el is Object uel) CheckObject(uel, assetPath, missing, visited);
                    else CheckPocoFields(el, assetPath, missing, visited, elContext);
                    idx++;
                }

                continue;
            }

            // Recurse into nested POCO
            CheckPocoFields(value, assetPath, missing, visited, context);
        }
    }
}
