// AttackBehavior.cs
// Purpose: Handles attack state logic for enemies (melee/ranged), damage application, and hit detection.
// Works with: BaseEnemy attack triggers, EnemyProjectile, Player health systems.
// Notes: Does not manage movement; only attack timing and hit application.

using UnityEngine;
using System.Collections;
using Utilities.Combat;

namespace Behaviors
{
    public class AttackBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine lookAtPlayerCoroutine;
        private Coroutine attackRangeMonitorCoroutine;
        private Coroutine attackLoopCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;
        private Transform playerTarget;

        // Cache the Attack state value for this enum type
        private TState attackStateValue;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            playerTarget = enemy.PlayerTarget;

            // Cache the Attack state value once
            attackStateValue = (TState)System.Enum.Parse(typeof(TState), "Attack");

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

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
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
            while (enemy.enemyAI.State.Equals(attackStateValue) && playerTarget != null)
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
            while (enemy.enemyAI.State.Equals(attackStateValue) && playerTarget != null)
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
            int safetyCounter = 0;
            const int maxIterations = 10000; // Prevent infinite loop in editor

            while (enemy.enemyAI.State.Equals(attackStateValue))
            {
                safetyCounter++;
                if (safetyCounter > maxIterations)
                {
                    Debug.LogError("AttackLoop exceeded max iterations! Breaking to prevent freeze.");
                    yield break;
                }

                bool playerInAttackBox = false;
                Collider playerCollider = null;
                bool didAttack = false;

                try
                {
                    Vector3 boxCenter = enemy.transform.position + enemy.transform.forward * enemy.attackBoxDistance;
                    Vector3 boxHalfExtents = enemy.attackBoxSize * 0.5f;
                    Quaternion boxRotation = enemy.transform.rotation;

                    if (boxHalfExtents == Vector3.zero)
                    {
                        Debug.LogWarning("Attack box size is zero!");
                        yield break;
                    }

                    Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation);

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

                        DealDamageToPlayerOnce(playerCollider);

                        didAttack = true;
                    }
                    else
                    {
                        enemy.isAttackBoxActive = false;
                        enemy.attackCollider.enabled = false;
                        enemy.SetEnemyColor(enemy.attackColor);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Exception in AttackLoop: " + ex);
                    yield break;
                }

                if (didAttack)
                {
                    yield return new WaitForSeconds(enemy.attackActiveDuration);
                    enemy.isAttackBoxActive = false;
                    enemy.attackCollider.enabled = false;
                    enemy.SetEnemyColor(enemy.attackColor);
                    ResetDamageFlag();
                    yield return new WaitForSeconds(enemy.attackInterval);

                    // Only do backup and rotate for crawlers
                    if (enemy is BaseCrawlerEnemy crawler)
                    {
                        yield return HandleAfterAttack();
                        if (SwarmManager.Instance != null)
                            SwarmManager.Instance.RotateAttackers();
                    }
                    // For other enemy types, you could add custom post-attack logic here if needed
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
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

            if (!playerCollider.CompareTag("Player")) return;

            // Parry: no damage
            if (CombatManager.isParrying)
            {
                CombatManager.ParrySuccessful();
                Debug.Log($"{enemy.gameObject.name} attack parried by player.");
                return;
            }

            playerCollider.TryGetComponent<IHealthSystem>(out var healthSystem);
            if (healthSystem == null)
            {
                Debug.LogWarning($"{playerCollider.gameObject.name} has Player tag but no IHealthSystem component.");
                return;
            }

            float dmg = enemy.damage;
            // Guard: half damage
            if (CombatManager.isGuarding)
            {
                dmg *= 0.5f; // temporary guard mitigation
                Debug.Log($"{enemy.gameObject.name} attack guarded. Applying reduced damage {dmg}.");
            }

            healthSystem.LoseHP(dmg);
            Debug.Log($"{enemy.gameObject.name} attacked {playerCollider.gameObject.name} for {dmg} damage.");
        }

        // Reset flag when hitbox is disabled
        // Call this at the end of each attack cycle
        private void ResetDamageFlag()
        {
            damageSentThisEnable = false;
        }

        private IEnumerator HandleAfterAttack()
        {
            // For crawlers with swarm disabled, skip back-away entirely
            if (enemy is BaseCrawlerEnemy crawler && !crawler.enableSwarmBehavior)
            {
                yield break;
            }

            // After attack finishes, before returning to swarm
            Vector3 awayDirection = (enemy.transform.position - playerTarget.position).normalized;
            float backupDistance = 2.0f; // Adjust as needed
            Vector3 backupTarget = enemy.transform.position + awayDirection * backupDistance;

            // Move the enemy back for a short time
            if (enemy.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
                enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().SetDestination(backupTarget);

            // Optionally, wait a short time before rejoining the swarm
            yield return new WaitForSeconds(0.5f);

            // Here you can add the code to make the enemy rejoin the swarm
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}