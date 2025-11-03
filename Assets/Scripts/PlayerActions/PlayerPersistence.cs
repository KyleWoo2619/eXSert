using UnityEngine;
using UnityEngine.SceneManagement;

/*
Written by Kyle Woo
Keeps the player GameObject persistent across scene loads.
Place this on the top-level Player object (parent of model, controller, camera, etc.).
*/

public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;

    [Header("Spawn Settings")]
    [Tooltip("Automatically move player to spawn point when entering new scenes")]
    [SerializeField] private bool autoPositionAtSpawn = true;
    [SerializeField] private string spawnTag = "PlayerSpawn";

    void Awake()
    {
        // Prevent duplicates when returning to a scene that also has a player prefab
        if (instance != null && instance != this)
        {
            Debug.Log($"[PlayerPersistence] Destroying duplicate player: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);   // <-- keeps this GameObject across scene loads
        Debug.Log($"[PlayerPersistence] Player persistence enabled for: {gameObject.name}");
    }

    void OnEnable()
    {
        if (autoPositionAtSpawn)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (autoPositionAtSpawn)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only reposition if this is the persistent player instance
        if (instance != this) return;

        var spawn = GameObject.FindWithTag(spawnTag);
        if (spawn != null)
        {
            transform.position = spawn.transform.position;
            transform.rotation = spawn.transform.rotation;
            Debug.Log($"[PlayerPersistence] Player positioned at spawn in scene: {scene.name}");
        }
        else
        {
            Debug.LogWarning($"[PlayerPersistence] No spawn point with tag '{spawnTag}' found in scene: {scene.name}");
        }
    }

    /// <summary>
    /// Manually destroy the persistent player (useful for returning to main menu, etc.)
    /// </summary>
    public static void DestroyPersistentPlayer()
    {
        if (instance != null)
        {
            Debug.Log("[PlayerPersistence] Manually destroying persistent player");
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    /// <summary>
    /// Check if there's currently a persistent player in the scene
    /// </summary>
    public static bool HasPersistentPlayer()
    {
        return instance != null;
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Persistent Player: {(instance == this ? "YES" : "NO")}");
        GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
        GUILayout.Label($"Auto Position: {autoPositionAtSpawn}");
        GUILayout.EndArea();
    }
#endif
}