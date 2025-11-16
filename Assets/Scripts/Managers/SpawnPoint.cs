using UnityEngine;

/// <summary>
/// Simple component to mark and identify spawn points in scenes.
/// Place this on empty GameObjects where you want the player to spawn.
/// Written by GitHub Copilot
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Unique identifier for this spawn point (default, checkpoint1, checkpoint2, etc.)")]
    public string spawnPointID = "default";

    private void OnDrawGizmos()
    {
        // Draw a visual indicator in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Spawn: {spawnPointID}");
        #endif
    }
}
