using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Place this on trigger volumes to set checkpoints when player enters.
/// Automatically saves progress when triggered.
/// Written by GitHub Copilot
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField, Tooltip("Unique ID for this checkpoint (default, checkpoint1, checkpoint2, etc.)")]
    private string checkpointID = "checkpoint1";

    [FormerlySerializedAs("sceneName")]
    [SerializeField, Tooltip("Optional explicit scene to associate with this checkpoint. Leave empty to derive from the linked spawn point or host scene.")]
    private string sceneOverride = string.Empty;

    [SerializeField, Tooltip("Optional spawn point this checkpoint should align with. Leave empty to resolve by checkpoint ID.")]
    private SpawnPoint linkedSpawnPoint;
    
    [SerializeField, Tooltip("Only trigger once per game session")]
    private bool triggerOnce = true;
    
    [SerializeField, Tooltip("Show debug messages")]
    private bool showDebugLogs = true;

    [Header("Visual Feedback")]
    [SerializeField, Tooltip("Optional particle system to play when checkpoint is activated")]
    private ParticleSystem activationEffect;
    
    [SerializeField, Tooltip("Optional audio clip to play when checkpoint is activated")]
    private AudioClip activationSound;

    private bool hasBeenTriggered = false;
    private string fallbackSceneName;

    private void Start()
    {
        // Ensure this is set as a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[CheckpointTrigger] {gameObject.name} collider is not set as trigger. Auto-fixing...");
            col.isTrigger = true;
        }

        // Use current scene if not specified
        fallbackSceneName = SceneManager.GetActiveScene().name;
        EnsureSpawnPointLink();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (!other.CompareTag("Player"))
            return;

        // Check if already triggered
        if (triggerOnce && hasBeenTriggered)
            return;

        // Activate checkpoint
        ActivateCheckpoint();
    }

    private void ActivateCheckpoint()
    {
        hasBeenTriggered = true;

        string resolvedScene = ResolveSceneName();
        Log($"Checkpoint activated by '{name}': spawn '{checkpointID}' => scene '{resolvedScene}'");
        SpawnPoint.LogCheckpointSelection(name, checkpointID, resolvedScene);

        // Set checkpoint in system
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.SetCheckpoint(resolvedScene, checkpointID);
        }
        else
        {
            Debug.LogError("[CheckpointTrigger] CheckpointSystem not found!");
        }

        // Play visual effect
        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        // Play sound effect
        if (activationSound != null && SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(activationSound);
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CheckpointTrigger] {message}");
        }
    }

    private void EnsureSpawnPointLink()
    {
        if (linkedSpawnPoint == null && !string.IsNullOrWhiteSpace(checkpointID))
        {
            SpawnPoint.TryGetSpawnPoint(checkpointID, out linkedSpawnPoint);
        }

        if (linkedSpawnPoint == null)
            return;

        if (!string.Equals(checkpointID, linkedSpawnPoint.spawnPointID, System.StringComparison.Ordinal))
        {
            Log($"Syncing checkpoint ID '{checkpointID}' with linked spawn '{linkedSpawnPoint.spawnPointID}'.");
            checkpointID = linkedSpawnPoint.spawnPointID;
        }
    }

    private string ResolveSceneName()
    {
        if (!string.IsNullOrWhiteSpace(sceneOverride))
            return sceneOverride;

        SpawnPoint target = linkedSpawnPoint;
        if (target == null && !string.IsNullOrWhiteSpace(checkpointID))
        {
            SpawnPoint.TryGetSpawnPoint(checkpointID, out target);
        }

        if (target != null)
            return target.SceneName;

        return !string.IsNullOrWhiteSpace(fallbackSceneName)
            ? fallbackSceneName
            : SceneManager.GetActiveScene().name;
    }

    private void OnDrawGizmos()
    {
        // Draw checkpoint visualization in editor
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = hasBeenTriggered ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Checkpoint: {checkpointID}");
            #endif
        }
    }
}
