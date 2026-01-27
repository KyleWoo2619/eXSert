/*
 * Author: Will Thomsen
 * 
 * A generic singleton class for Unity MonoBehaviours.
 * Ensures that only one instance of the class exists and provides a global access point to it.
 * The instance is created if it doesn't already exist and is marked to not be destroyed on scene load.
 */

using UnityEngine;

// makes singletons a namespace that must be opted in to use
namespace Singletons { 
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // The private static instance of the singleton
        private static T _instance;

        /// <summary>
        /// Override to disable DontDestroyOnLoad behavior for Scene-scoped singletons.
        /// </summary>
        protected virtual bool ShouldPersistAcrossScenes => true;

        // The Public static property to access the singleton instance
        public static T Instance
        {

            // special functionality which tries to find or creates the singleton instance if it doesn't exist already
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing instance of the singleton type T in the scene
                    _instance = (T)FindAnyObjectByType(typeof(T));
                    if (_instance == null)
                    {
                        // If no instance is found, create a new GameObject and attach the singleton component to it
                        Debug.LogWarning($"No instance of singleton {typeof(T)} found in the scene. Creating a new one.");
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        if (_instance is Singleton<T> singleton && singleton.ShouldPersistAcrossScenes)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                }
                return _instance;
            }

            private set { _instance = value; }
        }

        // Awake method to enforce the singleton pattern
        virtual protected void Awake()
        {
            if(_instance == null)
            {
                _instance = this as T;
                if (ShouldPersistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"Another instance of singleton {typeof(T)} already exists. Destroying this component only.");
                // Only destroy the component, not the entire GameObject
                // This prevents destroying Player when InputReader is a duplicate
                Destroy(this);
            }
        }
    }
}
