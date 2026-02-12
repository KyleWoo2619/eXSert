using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Behaviors
{
    public class RelocateBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine zoneArrivalCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            // Removed SetEnemyColor - using animations instead
            
            // If zone relocation is disabled, stay within current zone
            if (!enemy.allowZoneRelocation)
            {
                MoveWithinCurrentZone();
                return;
            }
            
            var otherZones = GetOtherZones();
            if (otherZones.Count == 0)
            {
                // No other zones to relocate to, move within current zone instead
                MoveWithinCurrentZone();
                return;
            }

            // Pick a random other zone and set as target
            Zone targetZone = otherZones[Random.Range(0, otherZones.Count)];
            // Move to a random point in the target zone
            Vector3 target = targetZone.GetRandomPointInZone();
            UnityEngine.AI.NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
            {
                enemy.agent.SetDestination(hit.position);
                // Start a coroutine to check when destination is reached
                if (zoneArrivalCoroutine != null)
                    enemy.StopCoroutine(zoneArrivalCoroutine);
                zoneArrivalCoroutine = enemy.StartCoroutine(WaitForArrivalAndUpdateZone());
            }
        }
        
        private void MoveWithinCurrentZone()
        {
            if (enemy.currentZone != null)
            {
                Vector3 target = enemy.currentZone.GetRandomPointInZone();
                NavMeshHit hit;
                if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
                {
                    enemy.agent.SetDestination(hit.position);
                    if (zoneArrivalCoroutine != null)
                        enemy.StopCoroutine(zoneArrivalCoroutine);
                    zoneArrivalCoroutine = enemy.StartCoroutine(WaitForArrivalAndUpdateZone());
                    return;
                }
            }
            // No current zone or failed to find valid position, transition back to Idle
            enemy.TryFireTriggerByName("ReachZone");
        }
        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (zoneArrivalCoroutine != null)
            {
                enemy.StopCoroutine(zoneArrivalCoroutine);
                zoneArrivalCoroutine = null;
            }
        }
        public virtual void Tick(BaseEnemy<TState, TTrigger> enemy) { }
        
        // Reusable list to avoid allocations in fallback path
        private readonly List<Zone> fallbackZoneList = new List<Zone>(16);
        
        private IReadOnlyList<Zone> GetOtherZones()
        {
            // Use ZoneManager if available for cached zones (avoids FindObjectsByType allocation)
            if (ZoneManager.Instance != null)
            {
                return ZoneManager.Instance.GetOtherZones(enemy.currentZone);
            }
            
            // Fallback to FindObjectsByType if ZoneManager not present
            Zone[] allZones = Object.FindObjectsByType<Zone>(FindObjectsSortMode.None);
            fallbackZoneList.Clear();
            foreach (var zone in allZones)
            {
                if (zone != enemy.currentZone)
                    fallbackZoneList.Add(zone);
            }
            return fallbackZoneList;
        }
        private IEnumerator WaitForArrivalAndUpdateZone()
        {
            // Wait until the agent reaches its destination
            while (enemy.agent.pathPending || enemy.agent.remainingDistance > enemy.agent.stoppingDistance)
                yield return null;

            // Optionally, wait until the agent fully stops
            while (enemy.agent.hasPath && enemy.agent.velocity.sqrMagnitude > 0.01f)
                yield return null;

            enemy.TryFireTriggerByName("ReachZone");
            enemy.UpdateCurrentZone();
            zoneArrivalCoroutine = null;
        }
    }
}