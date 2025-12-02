using System.Collections.Generic;
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

    [SerializeField, Tooltip("Scene associated with this spawn. Leave empty to use the scene this object resides in.")]
    private string sceneNameOverride = string.Empty;


    [Header("Checkpoint Trigger")]
    [SerializeField, Tooltip("When enabled, the player entering this trigger will set the checkpoint to this spawn.")]
    private bool autoRegisterOnTrigger = true;
    [SerializeField, Tooltip("Tag used to identify the player for trigger detection.")]
    private string triggerPlayerTag = "Player";
    [SerializeField, Tooltip("If true, this trigger only fires once per scene load.")]
    private bool triggerOnce = false;

    private static readonly Dictionary<string, SpawnPoint> registry = new();
    private static readonly List<string> registryRemovals = new();
    private bool triggerConsumed;

    public string SceneName => string.IsNullOrWhiteSpace(sceneNameOverride)
        ? gameObject.scene.name
        : sceneNameOverride;

    private void OnEnable()
    {
        Register();
        triggerConsumed = false;
        EnsureColliderSetup();
    }

    private void OnDisable()
    {
        if (string.IsNullOrWhiteSpace(spawnPointID))
            return;

        if (registry.TryGetValue(spawnPointID, out var existing) && existing == this)
        {
            registry.Remove(spawnPointID);
        }
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(spawnPointID))
            return;

        CleanupRegistry();
        if (registry.TryGetValue(spawnPointID, out var existing) && existing != null && existing != this)
        {
            Debug.LogWarning($"[SpawnPoint] Duplicate ID '{spawnPointID}' detected. Overwriting registration with '{name}'.", this);
        }
        registry[spawnPointID] = this;
    }

    public static bool TryGetSpawnPoint(string spawnPointId, out SpawnPoint point)
    {
        CleanupRegistry();
        return registry.TryGetValue(spawnPointId, out point) && point != null;
    }

    public static bool TryGetSceneForSpawn(string spawnPointId, out string sceneName)
    {
        if (TryGetSpawnPoint(spawnPointId, out var point) && point != null)
        {
            sceneName = point.SceneName;
            return true;
        }

        sceneName = null;
        return false;
    }

    private static void CleanupRegistry()
    {
        registryRemovals.Clear();
        foreach (var kvp in registry)
        {
            if (kvp.Value == null)
            {
                registryRemovals.Add(kvp.Key);
            }
        }

        for (int i = 0; i < registryRemovals.Count; i++)
        {
            registry.Remove(registryRemovals[i]);
        }

        registryRemovals.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoRegisterOnTrigger)
            return;
        if (triggerOnce && triggerConsumed)
            return;
        if (other == null || !other.CompareTag(triggerPlayerTag))
            return;

        triggerConsumed = true;
        ApplyCheckpointFromTrigger();
    }

    private void ApplyCheckpointFromTrigger()
    {
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.SetCheckpoint(SceneName, spawnPointID);
        }
        else
        {
            Debug.LogWarning($"[SpawnPoint] CheckpointSystem missing when trigger '{name}' fired.");
        }

        SpawnPoint.LogCheckpointSelection(name, spawnPointID, SceneName);
    }

    private void EnsureColliderSetup()
    {
        if (!autoRegisterOnTrigger)
            return;

        if (!TryGetComponent(out Collider col))
        {
            Debug.LogWarning($"[SpawnPoint] Trigger mode enabled but no collider found on '{name}'.");
            return;
        }

        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

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

    public static void LogCheckpointSelection(string triggerName, string spawnPointId, string resolvedScene)
    {
        string id = string.IsNullOrWhiteSpace(spawnPointId) ? "<none>" : spawnPointId;
        string targetScene = string.IsNullOrWhiteSpace(resolvedScene) ? "<not set>" : resolvedScene;
        string extra = string.Empty;

        if (!string.IsNullOrWhiteSpace(spawnPointId) && TryGetSpawnPoint(spawnPointId, out var point) && point != null)
        {
            extra = $" via SpawnPoint '{point.name}' (SceneOverride='{point.SceneName}')";
        }

        Debug.Log($"[SpawnPoint] Trigger '{triggerName}' selected spawn '{id}' -> scene '{targetScene}'{extra}.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            Register();
            EnsureColliderSetup();
        }
    }
#endif
}
