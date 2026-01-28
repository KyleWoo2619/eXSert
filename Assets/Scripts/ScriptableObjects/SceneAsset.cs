/*
 * Author: Will Thomsen
 * 
 * Basic Scene ScriptableObject to hold scene names for easy reference.
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
[CreateAssetMenu(fileName = "New Game Scene", menuName = "Scene/Scene Asset")]
public class SceneAsset : ScriptableObject
{
    // The name of the scene, serialized for easy editing in the inspector
    public string sceneName { get => this.name; }

    // Check if the scene is currently loaded
    public bool IsLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    // Load the scene additively, with an option to force reload if already loaded
    public void Load(bool forceReload = false)
    {
        if (!IsLoaded() || forceReload)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }

    // Unload the scene if it is currently loaded
    public void Unload()
    {
        if (IsLoaded())
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' is not loaded, cannot unload.");
        }
    }
    public static SceneAsset GetSceneAssetByName(string name)
    {
        return Resources.Load<SceneAsset>($"Scene Assets/{name}");
    }

    // dont forget to implement this method
    public static SceneAsset GetSceneAssetOfObject(GameObject go)
    {

    }
}
