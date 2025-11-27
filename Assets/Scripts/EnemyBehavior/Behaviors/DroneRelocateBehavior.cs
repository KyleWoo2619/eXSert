// DroneRelocateBehavior.cs
// Purpose: Handles drone relocation logic within a swarm, integrates with pathing.
// Works with: PathRequestManager, DroneSwarmManager.

using Behaviors;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DroneRelocateBehavior<TState, TTrigger> : RelocateBehavior<TState, TTrigger>
    where TState : struct, System.Enum
    where TTrigger : struct, System.Enum
{
    private Coroutine relocateCoroutine;

    public override void OnEnter(BaseEnemy<TState, TTrigger> enemy)
    {
        var drone = enemy as DroneEnemy;
        if (drone != null)
        {
            // Pick a new zone different from currentZone
            Zone newZone = null;
            var zones = UnityEngine.Object.FindObjectsByType<Zone>(FindObjectsSortMode.None);
            if (zones.Length > 1)
            {
                var candidates = new List<Zone>(zones);
                candidates.Remove(drone.currentZone);
                newZone = candidates[Random.Range(0, candidates.Count)];
            }
            else if (zones.Length == 1)
            {
                newZone = zones[0];
            }

            if (newZone != null)
                drone.currentZone = newZone;

            // Set cluster target to a random point in the new zone
            Vector3 relocateTarget = drone.currentZone.GetRandomPointInZone();
            drone.Cluster.target.position = relocateTarget;

            // Start coroutine to monitor arrival
            relocateCoroutine = drone.StartCoroutine(RelocateAndReturnToIdle(drone, relocateTarget));
            drone.Cluster.BeginRelocate(relocateTarget);
        }
    }

    public override void OnExit(BaseEnemy<TState, TTrigger> enemy)
    {
        var drone = enemy as DroneEnemy;
        if (drone != null && relocateCoroutine != null)
        {
            drone.StopCoroutine(relocateCoroutine);
            relocateCoroutine = null;
        }
        if (drone != null)
            drone.StopTickCoroutine();
        drone.Cluster.EndRelocate();
    }

    // Coroutine: Wait until all cluster members are inside the target zone, then fire trigger to return to Idle
    private IEnumerator RelocateAndReturnToIdle(DroneEnemy drone, Vector3 target)
    {
        Zone targetZone = drone.currentZone;
        float stuckCheckInterval = 6f;
        float stuckThreshold = 0.5f; // Minimum movement over the interval to not be considered stuck
        Dictionary<DroneEnemy, Vector3> stuckCheckStartPositions = new Dictionary<DroneEnemy, Vector3>();
        float stuckCheckStartTime = Time.time;

        foreach (var member in drone.Cluster.drones)
            stuckCheckStartPositions[member] = member.transform.position;

        drone.Cluster.BeginRelocate(target);

        while (true)
        {
            int inZoneCount = 0;
            int total = drone.Cluster.drones.Count;

            foreach (var member in drone.Cluster.drones)
            {
                if (targetZone != null && targetZone.Contains(member.transform.position))
                    inZoneCount++;
            }

            // Normal transition if enough are in the zone
            if (inZoneCount >= Mathf.CeilToInt(total / 2f))
            {
                foreach (var member in drone.Cluster.drones)
                {
                    bool fired = member.TryFireTriggerByName("RelocateComplete");
                    Debug.Log($"Drone {member.name} in cluster transitioned to Idle (RelocateComplete fired: {fired})");
                }
                yield break;
            }

            // Failsafe: check movement over the entire interval
            if (Time.time - stuckCheckStartTime > stuckCheckInterval)
            {
                int stuckCount = 0;
                foreach (var member in drone.Cluster.drones)
                {
                    float moved = Vector3.Distance(member.transform.position, stuckCheckStartPositions[member]);
                    if (moved < stuckThreshold)
                        stuckCount++;
                }

                // If the majority of drones are stuck, trigger failsafe
                if (stuckCount >= Mathf.CeilToInt(total / 2f))
                {
                    Debug.LogWarning($"[Failsafe] Cluster appears stuck in Relocate. {stuckCount}/{total} drones moved less than {stuckThreshold} units in {stuckCheckInterval} seconds. Forcing Idle in nearest zone.");
                    Zone nearest = null;
                    float minDist = float.MaxValue;
                    foreach (var z in UnityEngine.Object.FindObjectsByType<Zone>(FindObjectsSortMode.None))
                    {
                        float dist = Vector3.Distance(drone.transform.position, z.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = z;
                        }
                    }
                    foreach (var member in drone.Cluster.drones)
                    {
                        member.currentZone = nearest;
                        bool fired = member.TryFireTriggerByName("RelocateComplete");
                        Debug.Log($"[Failsafe] Drone {member.name} forced to Idle in nearest zone (RelocateComplete fired: {fired})");
                    }
                    yield break;
                }
                // Reset for next interval
                stuckCheckStartTime = Time.time;
                foreach (var member in drone.Cluster.drones)
                    stuckCheckStartPositions[member] = member.transform.position;
            }

            drone.Cluster.UpdateClusterMovement(drone.Cluster.target.position);
            yield return null;
        }
    }

    public override void Tick(BaseEnemy<TState, TTrigger> enemy)
    {
        var drone = enemy as DroneEnemy;
        if (drone != null && drone.Cluster != null && drone.currentZone != null)
        {
            float distToPlayer = Vector3.Distance(drone.transform.position, drone.GetPlayerTransform().position);
            if (distToPlayer <= drone.DetectionRange)
            {
                drone.Cluster.AlertClusterSeePlayer();
            }
            // ... existing idle movement logic ...
        }
    }
}