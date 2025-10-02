/*
 * Author: Will Thomsen
 * 
 * A basic scene manager that can load and unload two scenes additively.
 * Meant to be a proof of concept to demonstrate seamless scene loading.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using Singletons;

public class SceneLoadManager : Singleton<SceneLoadManager>
{
    [SerializeField]
    private SceneAsset sceneA;
    [SerializeField]
    private SceneAsset sceneB;

    // functions

    public void ToggleSceneA()
    {
        if (sceneA.IsLoaded())
        {
            sceneA.Unload();
        }
        else
        {
            sceneA.Load();
        }
    }

    public void ToggleSceneB()
    {
        if (sceneB.IsLoaded())
        {
            sceneB.Unload();
        }
        else
        {
            sceneB.Load();
        }
    }
}
