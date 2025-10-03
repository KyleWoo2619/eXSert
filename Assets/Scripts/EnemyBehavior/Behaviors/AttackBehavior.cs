using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class AttackBehavior : IEnemyStateBehavior
    {
        private Coroutine lookAtPlayerCoroutine;
        private Coroutine attackRangeMonitorCoroutine;
        private Coroutine attackLoopCoroutine;
        private BaseEnemy<EnemyState, EnemyTrigger> enemy;
        private Transform playerTarget;

        public void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            this.enemy = enemy;
            playerTarget = enemy.PlayerTarget;

            enemy.SetEnemyColor(enemy.attackColor);

            if (lookAtPlayerCoroutine != null)
                enemy.StopCoroutine(lookAtPlayerCoroutine);
            lookAtPlayerCoroutine = enemy.StartCoroutine(LookAtPlayerLoop());

            if (attackRangeMonitorCoroutine != null)
                enemy.StopCoroutine(attackRangeMonitorCoroutine);
            attackRangeMonitorCoroutine = enemy.StartCoroutine(MonitorAttackRangeLoop());

            if (attackLoopCoroutine != null)
                enemy.StopCoroutine(attackLoopCoroutine);
            attackLoopCoroutine = enemy.StartCoroutine(AttackLoop());
        }

        public void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy)
        {
            if (lookAtPlayerCoroutine != null)
            {
                enemy.StopCoroutine(lookAtPlayerCoroutine);
                lookAtPlayerCoroutine = null;
            }
            if (attackRangeMonitorCoroutine != null)
            {
                enemy.StopCoroutine(attackRangeMonitorCoroutine);
                attackRangeMonitorCoroutine = null;
            }
            if (attackLoopCoroutine != null)
            {
                enemy.StopCoroutine(attackLoopCoroutine);
                attackLoopCoroutine = null;
            }
            enemy.attackCollider.enabled = false;
        }

        private IEnumerator LookAtPlayerLoop()
        {
            while (enemy.enemyAI.State.Equals(EnemyState.Attack) && playerTarget != null)
            {
                if (!enemy.isAttackBoxActive)
                {
                    Vector3 direction = (playerTarget.position - enemy.transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        float t = 0f;
                        Quaternion startRotation = enemy.transform.rotation;
                        while (t < 1f && !enemy.isAttackBoxActive)
                        {
                            t += Time.deltaTime;
                            enemy.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                            yield return null;
                        }
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator MonitorAttackRangeLoop()
        {
            while (enemy.enemyAI.State.Equals(EnemyState.Attack) && playerTarget != null)
            {
                float attackRange = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance;
                float distance = Vector3.Distance(enemy.transform.position, playerTarget.position);

                if (distance > attackRange)
                {
                    enemy.TryFireTriggerByName("OutOfAttackRange");
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator AttackLoop()
        {
            while (enemy.enemyAI.State.Equals(EnemyState.Attack))
            {
                Vector3 boxCenter = enemy.transform.position + enemy.transform.forward * enemy.attackBoxDistance;
                Vector3 boxHalfExtents = enemy.attackBoxSize * 0.5f;
                Quaternion boxRotation = enemy.transform.rotation;

                bool playerInAttackBox = false;
                Collider playerCollider = null;

                Collider[] hits = Physics.OverlapBox(
                    boxCenter,
                    boxHalfExtents,
                    boxRotation
                );

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        playerInAttackBox = true;
                        playerCollider = hit;
                        break;
                    }
                }

                if (playerInAttackBox)
                {
                    enemy.isAttackBoxActive = true;
                    enemy.attackCollider.enabled = true;
                    enemy.SetEnemyColor(enemy.hitboxActiveColor);

                    // Only call damage once per hitbox enable
                    DealDamageToPlayerOnce(playerCollider);

                    yield return new WaitForSeconds(enemy.attackActiveDuration);
                    enemy.isAttackBoxActive = false;
                    enemy.attackCollider.enabled = false;
                    enemy.SetEnemyColor(enemy.attackColor);

                    ResetDamageFlag();
                }
                else
                {
                    enemy.isAttackBoxActive = false;
                    enemy.attackCollider.enabled = false;
                    enemy.SetEnemyColor(enemy.attackColor);
                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(enemy.attackInterval);
            }
            enemy.isAttackBoxActive = false;
            enemy.attackCollider.enabled = false;
            enemy.SetEnemyColor(enemy.attackColor);
        }

        private bool damageSentThisEnable = false;

        private void DealDamageToPlayerOnce(Collider playerCollider)
        {
            if (damageSentThisEnable) return;
            damageSentThisEnable = true;

            // Only apply damage if the collider has the "Player" tag
            if (playerCollider.CompareTag("Player"))
            {
                var healthSystem = playerCollider.GetComponent<IHealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.LoseHP(enemy.damage);
                    Debug.Log($"{enemy.gameObject.name} attacked {playerCollider.gameObject.name} for {enemy.damage} damage.");
                }
                else
                {
                    Debug.LogWarning($"{playerCollider.gameObject.name} has Player tag but no IHealthSystem component.");
                }
            }
        }

        // Reset flag when hitbox is disabled
        // Call this at the end of each attack cycle
        private void ResetDamageFlag()
        {
            damageSentThisEnable = false;
        }
    }
}