using UnityEngine;
using System.Collections;
using UnityEngine.AI;

namespace Behaviors
{
    public class IdleBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine idleTimerCoroutine;
        private Coroutine idleWanderCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            Debug.Log("IdleBehavior.OnEnter called!");
            if (enemy.agent == null)
            {
                Debug.LogError("NavMeshAgent not initialized!");
                return;
            }
            // Removed SetEnemyColor - using animations instead
            Debug.Log($"{enemy.gameObject.name} entered Idle state.");
            enemy.hasFiredLowHealth = false;
            enemy.CheckHealthThreshold();

            ResetIdleTimer();
            enemy.UpdateCurrentZone();

            idleWanderCoroutine = enemy.StartCoroutine(IdleWanderLoop());
        }

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (idleTimerCoroutine != null)
            {
                enemy.StopCoroutine(idleTimerCoroutine);
                idleTimerCoroutine = null;
            }
            if (idleWanderCoroutine != null)
            {
                enemy.StopCoroutine(idleWanderCoroutine);
                idleWanderCoroutine = null;
            }
        }

        private void ResetIdleTimer()
        {
            if (idleTimerCoroutine != null)
            {
                enemy.StopCoroutine(idleTimerCoroutine);
            }
            idleTimerCoroutine = enemy.StartCoroutine(IdleTimerCoroutine());
        }

        private IEnumerator IdleTimerCoroutine()
        {
            yield return new WaitForSeconds(enemy.idleTimerDuration);
            if (enemy.enemyAI.State.Equals(EnemyState.Idle))
            {
                enemy.TryFireTriggerByName("IdleTimerElapsed");
            }
            idleTimerCoroutine = null;
        }

        private IEnumerator IdleWanderLoop()
        {
            while (true)
            {
                float waitTime = Random.Range(2f, 4f);
                yield return new WaitForSeconds(waitTime);
                IdleWander();
            }
        }

        private void IdleWander()
        {
            if (enemy.currentZone == null) {
                Debug.LogWarning($"{enemy.gameObject.name} has no currentZone assigned!");
                return;
            }
            Vector3 target = enemy.currentZone.GetRandomPointInZone();
            Debug.Log($"{enemy.gameObject.name} IdleWander target: {target}");

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                Debug.Log($"{enemy.gameObject.name} setting destination to {hit.position}");
                enemy.agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning($"{enemy.gameObject.name} could not find valid NavMesh position near {target}");
            }
        }

        public virtual void Tick(BaseEnemy<TState, TTrigger> enemy) { }
    }
}