using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class RecoverBehavior : IEnemyStateBehavior
    {
        private Coroutine recoverCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;
            // Optionally set color or other visual feedback
            // enemy.SetEnemyColor(enemy.patrolColor); // Or a custom recover color

            if (recoverCoroutine != null)
                enemy.StopCoroutine(recoverCoroutine);
            recoverCoroutine = enemy.StartCoroutine(RecoverHealthOverTime());
        }

        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
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
    }
}