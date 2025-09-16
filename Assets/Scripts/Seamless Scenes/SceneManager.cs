/*
 * Author: Will Thomsen
 * 
 * A basic scene manager that can load and unload two scenes additively.
 * Meant to be a proof of concept to demonstrate seamless scene loading.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using Singletons;

public class SceneManager : Singleton<SceneManager>
{
    public string sceneAName;
    public string sceneBName;

    public bool isSceneALoaded { get; private set; } = false;
    public bool isSceneBLoaded { get; private set; } = false;

    // functions

    public void ToggleSceneA()
    {
        if (isSceneALoaded)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneAName);
            isSceneALoaded = false;
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneAName, LoadSceneMode.Additive);
            isSceneALoaded = true;
        }
    }

    public void ToggleSceneB()
    {
        if (isSceneBLoaded)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneBName);
            isSceneBLoaded = false;
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneBName, LoadSceneMode.Additive);
            isSceneBLoaded = true;
        }
    }
}
