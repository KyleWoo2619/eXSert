using UnityEngine;
using System.Collections;
using UnityEngine.AI;

namespace Behaviors
{
    public class RelocateBehavior : IEnemyStateBehavior
    {
        private Coroutine zoneArrivalCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;
            // Removed SetEnemyColor - using animations instead
            Zone[] otherZones = GetOtherZones();
            if (otherZones.Length == 0)
            {
                // No other zones to relocate to, transition back to Idle
                enemy.TryFireTriggerByName("ReachZone");
                return;
            }

            // Pick a random other zone and set as target
            Zone targetZone = otherZones[Random.Range(0, otherZones.Length)];
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
        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            if (zoneArrivalCoroutine != null)
            {
                enemy.StopCoroutine(zoneArrivalCoroutine);
                zoneArrivalCoroutine = null;
            }
        }
        private Zone[] GetOtherZones()
        {
            Zone[] allZones = Object.FindObjectsByType<Zone>(FindObjectsSortMode.None);
            if (enemy.currentZone == null)
                return allZones;
            // Exclude the current zone
            var otherZones = new System.Collections.Generic.List<Zone>();
            foreach (var zone in allZones)
            {
                if (zone != enemy.currentZone)
                    otherZones.Add(zone);
            }
            return otherZones.ToArray();
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