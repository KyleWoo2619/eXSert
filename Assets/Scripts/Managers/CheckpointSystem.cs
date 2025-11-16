using UnityEngine;
using Singletons;

/// <summary>
/// Tracks player's checkpoint progress throughout the game.
/// Integrates with SaveDataManager and ObjectiveManager to persist progress.
/// Written by GitHub Copilot
/// </summary>
public class CheckpointSystem : Singleton<CheckpointSystem>, IDataPersistenceManager
{
    [Header("Current Progress")]
    [SerializeField, ReadOnly] private string currentSceneName = "FP_Elevator";
    [SerializeField, ReadOnly] private string currentSpawnPointID = "default";
    
    [Header("Scene Progression")]
    [Tooltip("Define the order of scenes in your game")]
    [SerializeField] private string[] sceneProgression = new string[]
    {
        "FP_Elevator",
        "FP_Hanger",
        "FP_Crew_Quarters"
    };

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    protected override void Awake()
    {
        base.Awake();
        // Load checkpoint data from save system
        LoadCheckpointData();
    }

    /// <summary>
    /// Sets a new checkpoint. This saves the player's current position/scene.
    /// Call this when player reaches a checkpoint trigger.
    /// </summary>
    /// <param name="sceneName">The scene where the checkpoint is</param>
    /// <param name="spawnPointID">The spawn point identifier (default, checkpoint1, checkpoint2, etc.)</param>
    public void SetCheckpoint(string sceneName, string spawnPointID)
    {
        currentSceneName = sceneName;
        currentSpawnPointID = spawnPointID;
        
        Log($"Checkpoint set: {sceneName} - {spawnPointID}");
        
        // Save to persistent data
        SaveCheckpointData();
    }

    /// <summary>
    /// Gets the current checkpoint scene name.
    /// </summary>
    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }

    /// <summary>
    /// Gets the current spawn point ID.
    /// </summary>
    public string GetCurrentSpawnPointID()
    {
        return currentSpawnPointID;
    }

    /// <summary>
    /// Checks if the player has progressed past a certain scene.
    /// </summary>
    /// <param name="sceneName">The scene to check</param>
    /// <returns>True if player has reached or passed this scene</returns>
    public bool HasReachedScene(string sceneName)
    {
        int currentIndex = System.Array.IndexOf(sceneProgression, currentSceneName);
        int checkIndex = System.Array.IndexOf(sceneProgression, sceneName);
        
        if (currentIndex == -1 || checkIndex == -1)
        {
            Debug.LogWarning($"[CheckpointSystem] Scene not found in progression: current={currentSceneName}, check={sceneName}");
            return false;
        }
        
        return currentIndex >= checkIndex;
    }

    /// <summary>
    /// Resets progress to the beginning (new game).
    /// </summary>
    public void ResetProgress()
    {
        currentSceneName = sceneProgression.Length > 0 ? sceneProgression[0] : "FP_Elevator";
        currentSpawnPointID = "default";
        
        Log("Progress reset to beginning");
        SaveCheckpointData();
    }

    /// <summary>
    /// Loads checkpoint data from the save system.
    /// </summary>
    private void LoadCheckpointData()
    {
        // Get all profile data to access current save
        if (DataPersistenceManager.instance != null && DataPersistenceManager.instance.HasGameData())
        {
            // Data will be loaded through the IDataPersistenceManager interface
            // We'll implement that interface to get checkpoint data
            Log("CheckpointSystem ready to load data from save system");
        }
        else
        {
            // Default to first scene if no save data
            ResetProgress();
        }
    }

    /// <summary>
    /// Saves checkpoint data to the save system.
    /// </summary>
    private void SaveCheckpointData()
    {
        if (DataPersistenceManager.instance != null)
        {
            // Trigger a save through the DataPersistenceManager
            // Our SaveData method (from IDataPersistenceManager) will be called
            DataPersistenceManager.instance.SaveGame();
            Log("Checkpoint save triggered");
        }
    }

    // IDataPersistenceManager Implementation
    public void LoadData(GameData data)
    {
        if (data != null)
        {
            currentSceneName = string.IsNullOrEmpty(data.currentSceneName) ? "FP_Elevator" : data.currentSceneName;
            currentSpawnPointID = string.IsNullOrEmpty(data.currentSpawnPointID) ? "default" : data.currentSpawnPointID;
            Log($"Loaded checkpoint from save: {currentSceneName} - {currentSpawnPointID}");
        }
    }

    public void SaveData(GameData data)
    {
        if (data != null)
        {
            data.currentSceneName = currentSceneName;
            data.currentSpawnPointID = currentSpawnPointID;
            Log($"Saved checkpoint to save data: {currentSceneName} - {currentSpawnPointID}");
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CheckpointSystem] {message}");
        }
    }

    #region Editor Helpers
#if UNITY_EDITOR
    [ContextMenu("Reset Progress (Editor Only)")]
    private void EditorResetProgress()
    {
        ResetProgress();
        Debug.Log("Progress reset via editor context menu");
    }

    [ContextMenu("Print Current Checkpoint")]
    private void EditorPrintCheckpoint()
    {
        Debug.Log($"Current Checkpoint: Scene={currentSceneName}, SpawnPoint={currentSpawnPointID}");
    }
#endif
    #endregion
}
