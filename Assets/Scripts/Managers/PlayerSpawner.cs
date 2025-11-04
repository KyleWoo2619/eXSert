using UnityEngine;
using Singletons;

/// <summary>
/// Handles spawning the player prefab at designated spawn points in each scene.
/// Replaces the old DontDestroyOnLoad approach with proper instantiation per scene.
/// Written by GitHub Copilot
/// </summary>
public class PlayerSpawner : Singleton<PlayerSpawner>
{
    [Header("Player Prefab")]
    [SerializeField, Tooltip("The player prefab to spawn")]
    private GameObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField, Tooltip("Tag used to find spawn points in scenes")]
    private string spawnPointTag = "PlayerSpawn";
    
    [SerializeField, Tooltip("Default spawn position if no spawn point found")]
    private Vector3 defaultSpawnPosition = Vector3.zero;
    
    [SerializeField, Tooltip("Default spawn rotation if no spawn point found")]
    private Vector3 defaultSpawnRotation = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private GameObject currentPlayerInstance;

    /// <summary>
    /// Spawns the player at the specified spawn point.
    /// </summary>
    /// <param name="spawnPointID">The identifier of the spawn point (e.g., "default", "checkpoint1")</param>
    public void SpawnPlayer(string spawnPointID = "default")
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player prefab is not assigned! Cannot spawn player.");
            return;
        }

        // Find the spawn point
        Transform spawnPoint = FindSpawnPoint(spawnPointID);
        
        Vector3 spawnPosition = defaultSpawnPosition;
        Quaternion spawnRotation = Quaternion.Euler(defaultSpawnRotation);

        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
            Log($"Found spawn point '{spawnPointID}' at {spawnPosition}");
        }
        else
        {
            Log($"Spawn point '{spawnPointID}' not found, using default position");
        }

        // Destroy old player instance if it exists
        if (currentPlayerInstance != null)
        {
            Log($"Destroying old player instance: {currentPlayerInstance.name}");
            Destroy(currentPlayerInstance);
        }

        // Spawn new player
        currentPlayerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        currentPlayerInstance.name = "Player"; // Clean up the (Clone) suffix
        
        Log($"Player spawned at {spawnPosition}");

        // Update checkpoint system
        if (CheckpointSystem.Instance != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            CheckpointSystem.Instance.SetCheckpoint(currentScene, spawnPointID);
        }
    }

    /// <summary>
    /// Finds a spawn point by ID. Looks for GameObjects with SpawnPoint component first,
    /// then falls back to tag-based search.
    /// </summary>
    private Transform FindSpawnPoint(string spawnPointID)
    {
        // Method 1: Find by SpawnPoint component
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.spawnPointID == spawnPointID)
            {
                return sp.transform;
            }
        }

        // Method 2: Find by tag (for backward compatibility)
        GameObject[] taggedSpawns = GameObject.FindGameObjectsWithTag(spawnPointTag);
        if (taggedSpawns.Length > 0)
        {
            // If looking for "default", return the first one
            if (spawnPointID == "default")
            {
                return taggedSpawns[0].transform;
            }

            // Otherwise try to match by name
            foreach (GameObject spawn in taggedSpawns)
            {
                if (spawn.name.Contains(spawnPointID, System.StringComparison.OrdinalIgnoreCase))
                {
                    return spawn.transform;
                }
            }

            // If no name match, return first as fallback
            return taggedSpawns[0].transform;
        }

        return null;
    }

    /// <summary>
    /// Gets the current active player instance.
    /// </summary>
    public GameObject GetPlayerInstance()
    {
        return currentPlayerInstance;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerSpawner] {message}");
        }
    }
}
