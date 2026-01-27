// DroneSwarmBehavior.cs
// Purpose: High-level swarm behavior orchestration for drone groups.
// Works with: DroneSwarmManager, CrowdController, FlowFieldService for many-to-one movement.

using UnityEngine;

namespace Behaviors
{
    public class DroneSwarmBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private DroneEnemy drone;
        // Increased interval to reduce SetDestination calls which cause memory leaks
        private const float UpdateInterval = 0.5f;

        // Anti-stall tracking
        private float lastRemainingDistance = float.PositiveInfinity;
        private float stuckTimer = 0f;

        /// <summary>
        /// Returns true if this drone is the leader of its cluster.
        /// Only the leader should run cluster-wide movement logic.
        /// </summary>
        private bool IsClusterLeader(DroneEnemy d)
        {
            if (d?.Cluster == null || d.Cluster.drones == null || d.Cluster.drones.Count == 0)
                return false;
            return ReferenceEquals(d.Cluster.drones[0], d);
        }

        public void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            drone = enemy as DroneEnemy;
            if (drone == null) return;

            lastRemainingDistance = float.PositiveInfinity;
            stuckTimer = 0f;

            // Only the cluster leader runs the tick coroutine for movement
            if (IsClusterLeader(drone))
            {
                drone.StartTickCoroutine(() => Tick(enemy), UpdateInterval);
            }
        }

        public void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            var d = enemy as DroneEnemy;
            if (d != null && IsClusterLeader(d))
            {
                d.StopTickCoroutine();
            }
        }

        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            var d = enemy as DroneEnemy;
            if (d == null) return;

            // Double-check we're the leader
            if (!IsClusterLeader(d)) return;

            var player = d.GetPlayerTransform();
            
            // If no player or player inactive, transition ALL drones back to relocate/idle
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                d.Cluster.AlertClusterLosePlayer();
                return;
            }

            float dist = Vector3.Distance(d.transform.position, player.position);

            // Too far -> relocate (all drones)
            if (dist > d.chaseRange)
            {
                d.Cluster.AlertClusterLosePlayer();
                return;
            }

            // Hysteresis: enter Fire earlier (buffer) - all drones
            if (dist <= d.FireEnterDistance)
            {
                d.Cluster.AlertClusterInAttackRange();
                return;
            }

            // Anti-stall: if agent isn't making progress for a while but is close enough, force Fire
            var agent = d.agent;
            if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath && !agent.pathPending)
            {
                float rd = agent.remainingDistance;
                if (Mathf.Abs(rd - lastRemainingDistance) < 0.05f)
                    stuckTimer += UpdateInterval;
                else
                    stuckTimer = 0f;

                lastRemainingDistance = rd;

                if (stuckTimer >= d.ChaseStuckSeconds && dist <= d.FireEnterDistance + 0.5f)
                {
                    d.Cluster.AlertClusterInAttackRange();
                    stuckTimer = 0f;
                    return;
                }
            }

            // Drive movement while chasing (cluster leader moves all drones)
            if (d.Cluster != null)
            {
                d.Cluster.UpdateClusterMovement(
                    overrideTarget: player.position,
                    customRadius: Mathf.Max(d.attackRange - 1f, 0.5f),
                    customSpeed: 2f
                );
            }
        }
    }
}