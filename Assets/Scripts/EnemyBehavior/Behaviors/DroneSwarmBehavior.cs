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
        private const float UpdateInterval = 0.15f;

        // Anti-stall tracking
        private float lastRemainingDistance = float.PositiveInfinity;
        private float stuckTimer = 0f;

        public void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            drone = enemy as DroneEnemy;
            if (drone == null) return;

            lastRemainingDistance = float.PositiveInfinity;
            stuckTimer = 0f;

            // Use DroneEnemy's generic tick slot for Chase updates
            drone.StartTickCoroutine(() => Tick(enemy), UpdateInterval);
        }

        public void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            (enemy as DroneEnemy)?.StopTickCoroutine();
        }

        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            var d = enemy as DroneEnemy;
            if (d == null) return;

            var player = d.GetPlayerTransform();
            if (player == null) return;

            float dist = Vector3.Distance(d.transform.position, player.position);

            // Too far -> relocate
            if (dist > d.chaseRange)
            {
                d.enemyAI.Fire(DroneTrigger.LosePlayer);
                return;
            }

            // Hysteresis: enter Fire earlier (buffer)
            if (dist <= d.FireEnterDistance)
            {
                d.enemyAI.Fire(DroneTrigger.InAttackRange);
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
                    d.enemyAI.Fire(DroneTrigger.InAttackRange);
                    stuckTimer = 0f;
                    return;
                }
            }

            // Drive movement while chasing (cluster or fallback)
            if (d.Cluster != null)
            {
                d.Cluster.UpdateClusterMovement(
                    overrideTarget: player.position,
                    customRadius: Mathf.Max(d.attackRange - 1f, 0.5f),
                    customSpeed: 2f
                );
            }
            else
            {
                // Fallback: move directly towards the player (flatten Y to NavMesh plane)
                var flatTarget = new Vector3(player.position.x, d.transform.position.y, player.position.z);
                d.MoveTo(flatTarget);
            }
        }
    }
}