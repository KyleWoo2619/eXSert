// DroneIdleBehavior.cs
// Purpose: Idle behavior for drones within a swarm, optionally triggers relocation.
// Works with: DroneSwarmManager, DroneRelocateBehavior.

using Behaviors;
using UnityEngine;

public class DroneIdleBehavior<TState, TTrigger> : IdleBehavior<TState, TTrigger>
    where TState : struct, System.Enum
    where TTrigger : struct, System.Enum
{
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
            // Move cluster to a new random point every interval
            if (Time.time - drone.lastZoneMoveTime > drone.zoneMoveInterval)
            {
                Vector3 newTarget = drone.currentZone.GetRandomPointInZone();
                drone.Cluster.target.position = newTarget;
                drone.lastZoneMoveTime = Time.time;
            }
            drone.Cluster.UpdateClusterMovement();
        }
    }

    public override void OnEnter(BaseEnemy<TState, TTrigger> enemy)
    {
        var drone = enemy as DroneEnemy;
        if (drone != null)
        {
            drone.StartTickCoroutine(() => Tick(enemy));
            drone.StartIdleTimer();
        }
    }

    public override void OnExit(BaseEnemy<TState, TTrigger> enemy)
    {
        var drone = enemy as DroneEnemy;
        if (drone != null)
        {
            drone.StopIdleTimer();
            drone.StopTickCoroutine();
        }
    }
}