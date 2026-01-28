/*
 * Author: Will Thomsen
 * 
 * A modified version of the singleton class that is intended for objects 
 * that are meant to be a singleton for each scene but not necessarily for the game overall
 */

using System.Collections.Generic;
using UnityEngine;

// makes singletons a namespace that must be opted in to use
namespace Singletons { 
    public abstract class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Singleton instance dictionary to ensure one ProgressionManager per scene
        private static Dictionary<SceneAsset, T> instances = new Dictionary<SceneAsset, T>();

        // Get the ProgressionManager instance for a specific scene
        public static T GetInstance(SceneAsset scene)
        {
            if (instances.TryGetValue(scene, out T instance))
                return instance;
            else
            {
                return null;
            }
        }


        // Awake method to enforce the scene singleton pattern
        virtual protected void Awake()
        {
            // implement singletonish functionality

            SceneAsset asset = SceneAsset.GetSceneAssetOfObject(this.gameObject);
            if (instances.ContainsKey(asset))
            {
                Debug.LogWarning($"Another instance of SceneSingleton {typeof(T)} already exists in Scene {asset.name}. Destroying this component only.");
                Destroy(this);
            }
            else
            {
                instances.Add(asset, this as T);
            }
        }
    }
}
