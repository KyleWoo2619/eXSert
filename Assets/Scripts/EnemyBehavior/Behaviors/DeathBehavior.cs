using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class DeathBehavior : IEnemyStateBehavior
    {
        private Coroutine deathSequenceCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;

            // Disable movement and other components
            if (enemy.agent != null)
                enemy.agent.enabled = false;

            // Optionally set a "dead" color or visual
            enemy.SetEnemyColor(Color.black);

            // Start the death sequence coroutine
            if (deathSequenceCoroutine != null)
                enemy.StopCoroutine(deathSequenceCoroutine);
            deathSequenceCoroutine = enemy.StartCoroutine(DeathSequence());
        }

        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            if (deathSequenceCoroutine != null)
            {
                enemy.StopCoroutine(deathSequenceCoroutine);
                deathSequenceCoroutine = null;
            }
        }

        private IEnumerator DeathSequence()
        {
            // Wait a few seconds before playing SFX
            yield return new WaitForSeconds(2f);

            // Play SFX (placeholder, replace with actual SFX logic)
            PlayDeathSFX();

            // Wait for SFX duration
            yield return new WaitForSeconds(1f);

            // Destroy health bar if it exists
            if (enemy.healthBarInstance != null)
            {
                Object.Destroy(enemy.healthBarInstance.gameObject);
                enemy.healthBarInstance = null;
            }
            Object.Destroy(enemy.gameObject);
        }

        private void PlayDeathSFX()
        {
            // Placeholder for SFX logic
            Debug.Log($"{enemy.gameObject.name} death SFX played.");
        }
    }
}