using UnityEngine;

namespace Utility.SceneManagement {
    public class SceneManager : Singletons.Singleton<SceneManager>
    {
        public static void LoadScene(SceneAsset scene)
        {
            if (!scene.IsLoaded())
            {
                scene.Load();
            }
        }

        public static void UnloadScene(SceneAsset scene)
        {
            if (scene.IsLoaded())
            {
                scene.Unload();
            }
        }
    }
}
