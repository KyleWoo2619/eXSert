// FireBehavior.cs
// Purpose: Manages firing logic for enemies (rate of fire, projectile spawning).
// Works with: EnemyProjectile, Turret enemies, pooling systems.

using System.Collections.Generic;
using UnityEngine;

namespace Behaviors
{
    public class FireBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        // Per-cluster discrete re-positioning state with early/late windows
        private class ClusterFireState
        {
            public float baseAngleRad;
            public float stepAngleRad;
            public float radius;

            public float earliestNextTime; // earliest time we may step (requires majority arrived)
            public float latestNextTime;   // hard cap; step even if not arrived

            public float intervalMin;
            public float intervalMax;
            public float jitter;
            public float crossSwapChance;
        }

        private static readonly Dictionary<DroneCluster, ClusterFireState> s_states = new Dictionary<DroneCluster, ClusterFireState>();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Clear static state when entering play mode in editor
            s_states.Clear();
        }
#endif

        public void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            var drone = enemy as DroneEnemy;
            if (drone?.Cluster == null) return;

            drone.Cluster.RandomizeFormationOffset();

            if (IsLeader(drone))
            {
                if (!s_states.TryGetValue(drone.Cluster, out var st))
                {
                    st = new ClusterFireState();
                    s_states[drone.Cluster] = st;
                }

                st.baseAngleRad = Random.value * Mathf.PI * 2f;
                st.stepAngleRad = Mathf.Deg2Rad * Mathf.Max(5f, drone.FireStepAngleDeg);
                st.radius = Mathf.Max(0.5f, drone.attackRange);
                st.intervalMin = Mathf.Max(0.1f, drone.FireRepositionIntervalMin);
                st.intervalMax = Mathf.Max(st.intervalMin, drone.FireRepositionIntervalMax);
                st.jitter = Mathf.Max(0f, drone.FireRepositionJitter);
                st.crossSwapChance = Mathf.Clamp01(drone.FireCrossSwapChance);

                var now = Time.time;
                st.earliestNextTime = now + Random.Range(st.intervalMin, st.intervalMin + st.jitter);
                st.latestNextTime = now + Random.Range(st.intervalMin, st.intervalMax) + Random.Range(-st.jitter, st.jitter);

                // First assignment immediately
                AssignClusterTargets(drone, st);
            }
        }

        public void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            var drone = enemy as DroneEnemy;
            if (drone != null && IsLeader(drone) && drone.Cluster != null)
            {
                s_states.Remove(drone.Cluster);
            }
        }

        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            var drone = enemy as DroneEnemy;
            if (drone == null) return;

            var player = drone.GetPlayerTransform();
            
            // If no player or player inactive, transition back to relocate
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                enemy.TryFireTriggerByName("LosePlayer");
                return;
            }

            // Leader schedules discrete repositions, not continuous orbit
            if (drone.Cluster != null && IsLeader(drone))
            {
                if (!s_states.TryGetValue(drone.Cluster, out var st))
                {
                    st = new ClusterFireState
                    {
                        baseAngleRad = Random.value * Mathf.PI * 2f,
                        stepAngleRad = Mathf.Deg2Rad * Mathf.Max(5f, drone.FireStepAngleDeg),
                        radius = Mathf.Max(0.5f, drone.attackRange),
                        intervalMin = Mathf.Max(0.1f, drone.FireRepositionIntervalMin),
                        intervalMax = Mathf.Max(drone.FireRepositionIntervalMin, drone.FireRepositionIntervalMax),
                        jitter = Mathf.Max(0f, drone.FireRepositionJitter),
                        crossSwapChance = Mathf.Clamp01(drone.FireCrossSwapChance)
                    };
                    var nowInit = Time.time;
                    st.earliestNextTime = nowInit + st.intervalMin;
                    st.latestNextTime = nowInit + st.intervalMax;
                    s_states[drone.Cluster] = st;
                    AssignClusterTargets(drone, st);
                }

                var now = Time.time;
                bool canStepByTimeCap = now >= st.latestNextTime;
                bool canStepByMajority = now >= st.earliestNextTime && MajorityArrived(drone.Cluster, drone.FireArrivalEpsilon);

                if (canStepByTimeCap || canStepByMajority)
                {
                    float delta = st.stepAngleRad;
                    if (Random.value < st.crossSwapChance)
                        delta += Mathf.PI;

                    st.baseAngleRad += delta;
                    AssignClusterTargets(drone, st);

                    st.earliestNextTime = now + st.intervalMin + Random.Range(-st.jitter, st.jitter);
                    st.latestNextTime = now + st.intervalMax + Random.Range(-st.jitter, st.jitter);
                    if (st.latestNextTime < st.earliestNextTime)
                        st.latestNextTime = st.earliestNextTime + 0.1f;
                }
            }

            // Shoot using the drone's internal cooldown method
            drone.TryFireAtPlayer();

            // Exit rules with hysteresis
            float dist = Vector3.Distance(drone.transform.position, player.position);
            if (dist > drone.chaseRange)
            {
                enemy.TryFireTriggerByName("LosePlayer");
                return;
            }
            if (dist > drone.FireExitDistance)
            {
                enemy.TryFireTriggerByName("OutOfAttackRange");
            }
        }

        private static bool IsLeader(DroneEnemy drone)
        {
            return drone.Cluster != null &&
                   drone.Cluster.drones != null &&
                   drone.Cluster.drones.Count > 0 &&
                   ReferenceEquals(drone.Cluster.drones[0], drone);
        }

        private static void AssignClusterTargets(DroneEnemy leader, ClusterFireState st)
        {
            var cluster = leader.Cluster;
            var list = cluster.drones;
            if (list == null || list.Count == 0) return;

            var player = leader.GetPlayerTransform();
            if (player == null) return;

            int count = list.Count;
            Vector3 center = new Vector3(player.position.x, leader.transform.position.y, player.position.z);

            // Generate target slots for this step; members will travel straight to them
            for (int i = 0; i < count; i++)
            {
                var member = list[i];
                if (member == null) continue;

                float angle = st.baseAngleRad + (Mathf.PI * 2f * i / count);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * st.radius;
                Vector3 desired = center + offset;
                member.MoveTo(desired);
            }
        }

        private static bool MajorityArrived(DroneCluster cluster, float epsilon)
        {
            if (cluster == null || cluster.drones == null || cluster.drones.Count == 0) return false;

            int arrived = 0;
            int total = 0;

            foreach (var d in cluster.drones)
            {
                if (d == null) continue;
                total++;

                var agent = d.agent;
                if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending)
                {
                    if (!agent.hasPath || agent.remainingDistance <= Mathf.Max(epsilon, agent.stoppingDistance))
                        arrived++;
                }
            }

            if (total == 0) return false;
            return arrived >= Mathf.CeilToInt(total * 0.7f);
        }
    }
}