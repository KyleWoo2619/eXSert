using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Put this on an empty GameObject in the level.
public class EnemiesClearedProgression : MonoBehaviour
{
    [Header("How to find enemies")]
    [Tooltip("Drag specific enemy roots here (preferred). If empty and Auto Find is on, we'll find by tag instead.")]
    [SerializeField] internal List<GameObject> enemies = new List<GameObject>();

    [Space(5)]
    [SerializeField] private bool autoFindByTag = true;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField, Tooltip("How often to poll remaining enemies (seconds)")]
    private float checkInterval = 0.2f;

    [Space(10)]

    [Header("What scene to load when cleared")]
    [SerializeField] private string nextSceneName = "DP_Bridge";   // make sure it's in Build Settings
    [SerializeField] private bool loadAdditive = true;

    [Space(5)]
    [Tooltip("Drop an object, like a key, when enemies are cleared")]
    [SerializeField] private bool dropObjectOnClear = true;
    [SerializeField] private GameObject objectToDrop;
    [SerializeField] private bool dropAtLastEnemyPosition = true;

    private bool _done;
    private GameObject _lastEnemyAlive; // Cache the last alive enemy
    private static HashSet<string> _loadedScenes = new HashSet<string>(); // Track scenes we've already loaded

    private void Start()
    {
        // If no explicit list, collect by tag
        if ((enemies == null || enemies.Count == 0) && autoFindByTag)
        {
            enemies = GameObject.FindGameObjectsWithTag(enemyTag).ToList();
        }

        // Remove nulls just in case
        enemies.RemoveAll(e => e == null);

        StartCoroutine(MonitorEnemies());
    }

    private IEnumerator MonitorEnemies()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (!_done)
        {
            // Cache the last alive enemy before removing
            if (enemies.Count > 0)
            {
                _lastEnemyAlive = enemies[enemies.Count - 1];
            }

            // Clean up destroyed enemies & count alive ones
            enemies.RemoveAll(e => e == null || IsDefeated(e));
            
            if (enemies.Count == 0)
            {
                TriggerProgression();
                if (_lastEnemyAlive != null)
                {
                    DropObjectOnClear(objectToDrop, _lastEnemyAlive.transform.position + Vector3.forward, dropAtLastEnemyPosition);
                }
                else
                {
                    Debug.LogWarning("[EnemiesClearedProgression] No last enemy to drop object at.");
                }
                yield break;
            }

            yield return wait;
        }
    }

    // Treat as defeated if:
    //  - the object is destroyed, OR
    //  - it has an IHealthSystem and HP <= 0.
    private bool IsDefeated(GameObject root)
    {
        var health = root.GetComponentInChildren<IHealthSystem>();
        if (health == null) return root == null; // fallback: destroyed only
        return health.currentHP <= 0f;
    }

    private void DropObjectOnClear(GameObject progressionObj, Vector3 dropPosition, bool dropAtLastEnemy)
    {
        if(!dropObjectOnClear && progressionObj == null) return;

        if(dropAtLastEnemy)
            progressionObj.transform.position = dropPosition;
        else 
            progressionObj.transform.position = progressionObj.transform.position;

        progressionObj.gameObject.SetActive(true);
    }

    private void TriggerProgression()
    {
        if (_done) return;
        _done = true;

        // if (basicElevatorWall) basicElevatorWall.SetActive(false);
        // if (openedElevatorWall) openedElevatorWall.SetActive(true);

        // Load the next scene additively (or single if you prefer)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // CRITICAL: Prevent loading the currently active scene
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (nextSceneName == activeSceneName)
            {
                Debug.LogError($"Progression BLOCKED! Cannot load '{nextSceneName}' - it's the currently active scene! Please set a different scene in the inspector.");
                return;
            }
            
            // Check if the scene is already loaded OR if we've already requested to load it
            if (IsSceneLoaded(nextSceneName))
            {
                Debug.LogWarning($"[EnemiesClearedProgression] Scene '{nextSceneName}' is already loaded. Skipping load.");
            }
            else if (_loadedScenes.Contains(nextSceneName))
            {
                Debug.LogWarning($"[EnemiesClearedProgression] Scene '{nextSceneName}' is already being loaded by another progression trigger. Skipping duplicate load.");
            }
            else
            {
                // Mark this scene as being loaded to prevent other progression triggers from loading it
                _loadedScenes.Add(nextSceneName);
                
                if (loadAdditive)
                {
                    Debug.Log($"[EnemiesClearedProgression] Loading scene '{nextSceneName}' additively.");
                    SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
                }
                else
                {
                    Debug.Log($"[EnemiesClearedProgression] Loading scene '{nextSceneName}' as single.");
                    SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
                }
            }
        }

        Debug.Log("[EnemiesClearedProgression] All enemies cleared. Gate opened.");
    }
    
    // Helper method to check if a scene is already loaded
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
            {
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep list unique (no duplicates) - commented out to allow duplicates if needed
        // if (enemies != null)
        //     enemies = enemies.Distinct().ToList();
        
        // Just remove nulls
        if (enemies != null)
            enemies.RemoveAll(e => e == null);
    }

    [UnityEditor.MenuItem("GameObject/eXSert/Add Selected Enemies to Progression", false, 0)]
    private static void AddSelectedEnemiesToProgression()
    {
        var progression = FindFirstObjectByType<EnemiesClearedProgression>();
        if (progression == null)
        {
            Debug.LogWarning("No EnemiesClearedProgression found in scene!");
            return;
        }

        var selectedObjects = UnityEditor.Selection.gameObjects;
        var enemiesAdded = 0;

        foreach (var obj in selectedObjects)
        {
            // Check if it has a health system (likely an enemy)
            if (obj.GetComponentInChildren<IHealthSystem>() != null || obj.CompareTag("Enemy"))
            {
                if (!progression.enemies.Contains(obj))
                {
                    progression.enemies.Add(obj);
                    enemiesAdded++;
                }
            }
        }

        if (enemiesAdded > 0)
        {
            UnityEditor.EditorUtility.SetDirty(progression);
            Debug.Log($"Added {enemiesAdded} enemies to progression.");
        }
        else
        {
            Debug.Log("No valid enemies found in selection.");
        }
    }
#endif
}
