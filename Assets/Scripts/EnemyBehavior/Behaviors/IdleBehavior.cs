using UnityEngine;
using System.Collections;
using UnityEngine.AI;

namespace Behaviors
{
    public class IdleBehavior : IEnemyStateBehavior
    {
        private Coroutine idleTimerCoroutine;
        private Coroutine idleWanderCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;
            Debug.Log("IdleBehavior.OnEnter called!");
            if (enemy.agent == null)
            {
                Debug.LogError("NavMeshAgent not initialized!");
                return;
            }
            enemy.SetEnemyColor(enemy.patrolColor);
            Debug.Log($"{enemy.gameObject.name} entered Idle state.");
            enemy.hasFiredLowHealth = false;
            enemy.CheckHealthThreshold();

            ResetIdleTimer();
            enemy.UpdateCurrentZone();

            idleWanderCoroutine = enemy.StartCoroutine(IdleWanderLoop());
        }

        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
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
            if (enemy.currentZone == null) return;
            Vector3 target = enemy.currentZone.GetRandomPointInZone();

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                enemy.agent.SetDestination(hit.position);
            }
        }
    }
}