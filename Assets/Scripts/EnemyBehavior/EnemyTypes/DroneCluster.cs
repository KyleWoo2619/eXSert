// DroneCluster.cs
// Purpose: Grouping utility for drone-type enemies (spawning, formation management).
// Works with: DroneSwarmManager, CrowdController, FlowFieldService.

using System.Collections.Generic;
using UnityEngine;

public class DroneCluster : MonoBehaviour
{
    public List<DroneEnemy> drones = new List<DroneEnemy>();
    public Transform target;

    [Header("Cluster Movement")]
    public float orbitRadius = 6f;
    public float orbitSpeed = 1f;

    private Vector3? relocateCenter = null;
    private Vector3 relocateTargetPosition;

    private float formationAngleOffset = 0f;

    private void Awake()
    {
        if (target == null)
        {
            var targetGO = new GameObject($"{name}_Target");
            targetGO.transform.parent = transform;
            target = targetGO.transform;
            target.position = transform.position;
        }
    }

    // Call this when starting relocate
    public void BeginRelocate(Vector3 relocateTarget)
    {
        // Set center to current average position
        if (drones.Count == 0) return;
        Vector3 center = Vector3.zero;
        foreach (var drone in drones)
            center += drone.transform.position;
        center /= drones.Count;
        relocateCenter = center;
        relocateTargetPosition = relocateTarget;
        _lastRelocateCenterSet = false; // Reset so first movement update will set destinations
    }

    // Call this when ending relocate
    public void EndRelocate()
    {
        relocateCenter = null;
    }

    public void UpdateClusterRelocateMovement(float orbitRadius = 3f, float orbitSpeed = 2f, float centerMoveSpeed = 5f)
    {
        if (drones.Count == 0 || relocateCenter == null) return;

        // Move center toward relocate target
        Vector3 oldCenter = relocateCenter.Value;
        relocateCenter = Vector3.MoveTowards(oldCenter, relocateTargetPosition, Time.deltaTime * centerMoveSpeed);

        // Only update drone destinations if center has moved significantly (prevents excessive SetDestination calls)
        if (Vector3.Distance(oldCenter, relocateCenter.Value) < 0.01f && _lastRelocateCenterSet)
            return;
        _lastRelocateCenterSet = true;

        // Fixed formation around the moving center (no Time.time rotation to prevent constant destination changes)
        float angleStep = 360f / drones.Count;
        for (int i = 0; i < drones.Count; i++)
        {
            float angle = formationAngleOffset + angleStep * i;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.right * orbitRadius);
            Vector3 desiredPos = relocateCenter.Value + offset + Vector3.up * drones[i].HoverHeight;
            drones[i].MoveTo(desiredPos);
        }
    }
    private bool _lastRelocateCenterSet = false;

    // Call this when entering fire state
    public void RandomizeFormationOffset()
    {
        formationAngleOffset = Random.Range(0f, 360f);
    }

    // Throttle tracking for UpdateClusterMovement
    private Vector3 _lastClusterTarget = Vector3.positiveInfinity;
    private const float ClusterTargetChangeThreshold = 0.5f;

    // UpdateClusterMovement should use formationAngleOffset
    public void UpdateClusterMovement(Vector3? overrideTarget = null, float? customRadius = null, float? customSpeed = null)
    {
        if (drones.Count == 0) return;
        Vector3 center = overrideTarget ?? (target != null ? target.position : transform.position);
        float radius = customRadius ?? orbitRadius;

        // Throttle: only update if target has moved significantly
        if (Vector3.Distance(center, _lastClusterTarget) < ClusterTargetChangeThreshold)
            return;
        _lastClusterTarget = center;

        int count = drones.Count;
        for (int i = 0; i < count; i++)
        {
            // Calculate angle for this drone, add the offset
            float angle = formationAngleOffset + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
            Vector3 desiredPos = center + offset;
            drones[i].MoveTo(desiredPos);
        }
    }

    // Cluster-wide state triggers
    public void AlertClusterSeePlayer()
    {
        foreach (var drone in drones)
            drone.TryFireTriggerByName("SeePlayer");
    }

    public void AlertClusterInAttackRange()
    {
        foreach (var drone in drones)
            drone.TryFireTriggerByName("InAttackRange");
    }

    public void AlertClusterOutOfAttackRange()
    {
        foreach (var drone in drones)
            drone.TryFireTriggerByName("OutOfAttackRange");
    }

    public void AlertClusterLosePlayer()
    {
        foreach (var drone in drones)
            drone.TryFireTriggerByName("LosePlayer");
    }
}