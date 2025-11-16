using UnityEngine;

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
    
    [SerializeField, Tooltip("Scene name where this checkpoint is located (leave empty to use current scene)")]
    private string sceneName = "";
    
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
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
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

        Log($"Checkpoint activated: {checkpointID} in scene {sceneName}");

        // Set checkpoint in system
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.SetCheckpoint(sceneName, checkpointID);
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
