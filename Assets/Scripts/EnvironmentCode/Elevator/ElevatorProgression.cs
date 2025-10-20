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
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();

    [Space(5)]
    [SerializeField] private bool autoFindByTag = true;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField, Tooltip("How often to poll remaining enemies (seconds)")]
    private float checkInterval = 0.2f;

    [Header("What to flip when cleared")]
    [SerializeField] private GameObject basicElevatorWall;
    [SerializeField] private GameObject openedElevatorWall;

    [Header("What scene to load when cleared")]
    [SerializeField] private string nextSceneName = "DP_Bridge";   // make sure it's in Build Settings
    [SerializeField] private bool loadAdditive = true;

    private bool _done;

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
            // Clean up destroyed enemies & count alive ones
            enemies.RemoveAll(e => e == null || IsDefeated(e));

            if (enemies.Count == 0)
            {
                TriggerProgression();
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

    private void TriggerProgression()
    {
        if (_done) return;
        _done = true;

        if (basicElevatorWall) basicElevatorWall.SetActive(false);
        if (openedElevatorWall) openedElevatorWall.SetActive(true);

        // Load the next scene additively (or single if you prefer)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (loadAdditive)
                SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            else
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }

        Debug.Log("[EnemiesClearedProgression] All enemies cleared. Gate opened + scene loaded.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep list unique (no duplicates)
        if (enemies != null)
            enemies = enemies.Distinct().ToList();
    }

    [UnityEditor.MenuItem("GameObject/eXSert/Add Selected Enemies to Elevator Progression", false, 0)]
    private static void AddSelectedEnemiesToElevator()
    {
        var elevatorProgression = FindObjectOfType<EnemiesClearedProgression>();
        if (elevatorProgression == null)
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
                if (!elevatorProgression.enemies.Contains(obj))
                {
                    elevatorProgression.enemies.Add(obj);
                    enemiesAdded++;
                }
            }
        }

        if (enemiesAdded > 0)
        {
            UnityEditor.EditorUtility.SetDirty(elevatorProgression);
            Debug.Log($"Added {enemiesAdded} enemies to elevator progression.");
        }
        else
        {
            Debug.Log("No valid enemies found in selection.");
        }
    }
#endif
}
