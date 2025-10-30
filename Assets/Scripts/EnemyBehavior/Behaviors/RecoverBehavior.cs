using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class RecoverBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine recoverCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            // Optionally set color or other visual feedback
            // enemy.SetEnemyColor(enemy.patrolColor); // Or a custom recover color

            if (recoverCoroutine != null)
                enemy.StopCoroutine(recoverCoroutine);
            recoverCoroutine = enemy.StartCoroutine(RecoverHealthOverTime());
        }

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (recoverCoroutine != null)
            {
                enemy.StopCoroutine(recoverCoroutine);
                recoverCoroutine = null;
            }
        }

        private IEnumerator RecoverHealthOverTime()
        {
            float targetHealth = enemy.maxHP * 0.8f;
            float recoverRate = 0.1f; // 10% of missing health per second

            while (enemy.currentHP < targetHealth)
            {
                float missing = enemy.maxHP - enemy.currentHP;
                float delta = recoverRate * missing * Time.deltaTime;
                enemy.HealHP(delta);
                yield return null;
            }

            // Fire the RecoveredHealth trigger when done
            enemy.TryFireTriggerByName("RecoveredHealth");
            recoverCoroutine = null;
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}