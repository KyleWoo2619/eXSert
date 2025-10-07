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
            // Get the EnemyHealthManager component
            EnemyHealthManager healthManager = enemy.GetComponent<EnemyHealthManager>();
            if (healthManager == null)
            {
                Debug.LogError($"{enemy.gameObject.name}: RecoverBehavior requires EnemyHealthManager component!");
                yield break;
            }

            float targetHealth = healthManager.maxHP * 0.8f; // Recover to 80% health
            float recoverRate = 0.1f; // 10% of missing health per second

            Debug.Log($"{enemy.gameObject.name}: Starting health recovery. Current: {healthManager.currentHP}/{healthManager.maxHP}, Target: {targetHealth}");

            while (healthManager.currentHP < targetHealth)
            {
                float missing = healthManager.maxHP - healthManager.currentHP;
                float delta = recoverRate * missing * Time.deltaTime;
                healthManager.HealHP(delta);
                yield return null;
            }

            Debug.Log($"{enemy.gameObject.name}: Health recovery complete! Final HP: {healthManager.currentHP}/{healthManager.maxHP}");

            // Fire the RecoveredHealth trigger when done
            enemy.TryFireTriggerByName("RecoveredHealth");
            recoverCoroutine = null;
        }
    }
}