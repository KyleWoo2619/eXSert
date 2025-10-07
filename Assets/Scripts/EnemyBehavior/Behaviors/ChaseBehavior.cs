using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class ChaseBehavior : IEnemyStateBehavior
    {
        private Coroutine chaseCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;
        private Transform playerTarget;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;
            // Removed SetEnemyColor - using animations instead

            // Find the player target (if not already set)
            if (playerTarget == null) { playerTarget = enemy.PlayerTarget; }

            // Start chasing the player
            if (playerTarget != null)
            {
                if (chaseCoroutine != null)
                    enemy.StopCoroutine(chaseCoroutine);
                chaseCoroutine = enemy.StartCoroutine(ChasePlayerLoop());
            }
        }

        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            if (chaseCoroutine != null)
            {
                enemy.StopCoroutine(chaseCoroutine);
                chaseCoroutine = null;
            }
            enemy.agent.ResetPath();
        }

        private IEnumerator ChasePlayerLoop()
        {
            while (enemy.enemyAI.State.Equals(EnemyState.Chase) && playerTarget != null)
            {
                float attackRange = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance;
                float distance = Vector3.Distance(enemy.transform.position, playerTarget.position);

                MoveToAttackRange(playerTarget);

                if (distance <= attackRange)
                {
                    enemy.TryFireTriggerByName("InAttackRange");
                    yield break; // Stop coroutine when attack range is reached
                }

                yield return null; // Wait for next frame
            }
        }

        private void MoveToAttackRange(Transform player)
        {
            // Get direction from enemy to player
            Vector3 direction = (player.position - enemy.transform.position).normalized;

            // Calculate reach: half the box's depth (z) plus the offset distance in front of the enemy
            // Subtract a small buffer to move a little closer
            // This is to prevent stopping just before the attack box edge
            float chaseBuffer = 0.2f;
            float reach = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance - chaseBuffer;

            // Position the enemy so the player is just inside the front face of the attack box
            Vector3 targetPosition = player.position - direction * reach;

            // Keep the target position at the same Y as the enemy (for ground-based movement)
            targetPosition.y = enemy.transform.position.y;

            enemy.agent.SetDestination(targetPosition);
        }
    }
}