using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class ChaseBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine chaseCoroutine;
        private BaseEnemy<TState, TTrigger> enemy;
        private Transform playerTarget;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            playerTarget = enemy.PlayerTarget;

            Debug.Log($"{enemy.gameObject.name}: ChaseBehavior.OnEnter - PlayerTarget: {(playerTarget != null ? playerTarget.name : "NULL")}, Agent: {(enemy.agent != null ? "EXISTS" : "NULL")}, Agent Enabled: {(enemy.agent != null ? enemy.agent.enabled.ToString() : "N/A")}");

            // Special handling for BaseCrawlerEnemy with ForceChasePlayer
            if (enemy is BaseCrawlerEnemy crawler && crawler.ForceChasePlayer)
            {
                Debug.Log($"{crawler.gameObject.name}: Using ForceChasePlayer mode");
                if (crawler.PlayerTarget != null && crawler.agent != null && crawler.agent.enabled)
                {
                    crawler.agent.isStopped = false;
                    crawler.agent.SetDestination(crawler.PlayerTarget.position);
                }
                crawler.SetEnemyColor(crawler.chaseColor);

                if (chaseCoroutine != null)
                    crawler.StopCoroutine(chaseCoroutine);

                // Still run the blob chase coroutine to allow transitions (attack, flee, etc.)
                chaseCoroutine = crawler.StartCoroutine(CrawlerChaseBlob(crawler));
                Debug.Log($"{crawler.gameObject.name}: Started CrawlerChaseBlob coroutine");
                return;
            }

            if (playerTarget != null && enemy.agent != null && enemy.agent.enabled)
            {
                enemy.agent.isStopped = false;
                enemy.agent.SetDestination(playerTarget.position);
                Debug.Log($"{enemy.gameObject.name}: Set initial destination to {playerTarget.position}");
            }

            enemy.SetEnemyColor(enemy.chaseColor);

            if (chaseCoroutine != null)
                enemy.StopCoroutine(chaseCoroutine);

            // Use blob chase for crawlers, default for others
            if (enemy is BaseCrawlerEnemy baseCrawler)
            {
                chaseCoroutine = enemy.StartCoroutine(CrawlerChaseBlob(baseCrawler));
                Debug.Log($"{baseCrawler.gameObject.name}: Started CrawlerChaseBlob coroutine");
            }
            else
            {
                chaseCoroutine = enemy.StartCoroutine(DefaultChasePlayerLoop());
                Debug.Log($"{enemy.gameObject.name}: Started DefaultChasePlayerLoop coroutine");
            }
        }

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (chaseCoroutine != null)
            {
                enemy.StopCoroutine(chaseCoroutine);
                chaseCoroutine = null;
            }
            if (enemy.agent != null)
                enemy.agent.ResetPath();
        }

        // Blob chase for crawlers
        private IEnumerator CrawlerChaseBlob(BaseCrawlerEnemy crawler)
        {
            while (crawler.enemyAI.State.Equals(CrawlerEnemyState.Chase))
            {
                // Re-acquire player target each frame (handles DontDestroyOnLoad)
                Transform player = crawler.PlayerTarget;
                
                if (player == null)
                {
                    Debug.LogWarning($"{crawler.gameObject.name}: Lost player target during chase!");
                    yield break;
                }
                
                Debug.Log($"{crawler.gameObject.name}: Chasing '{player.name}' at position {player.position} | My position: {crawler.transform.position} | Distance: {Vector3.Distance(crawler.transform.position, player.position):F2}");
                
                // Move as a blob toward the player, apply separation
                if (crawler.agent != null && crawler.agent.enabled)
                {
                    crawler.agent.isStopped = false;
                    crawler.agent.SetDestination(player.position);
                }

                crawler.ApplySeparation();

                // If close enough to attack, fire the correct trigger
                float minRadius = crawler.attackBoxDistance + (crawler.attackBoxSize.x * 0.5f);
                if (Vector3.Distance(crawler.transform.position, player.position) <= minRadius + 0.5f)
                {
                    if (!crawler.enableSwarmBehavior)
                        crawler.TryFireTriggerByName("InAttackRange");
                    else
                        crawler.TryFireTriggerByName("ReachSwarm");
                    yield break;
                }

                // --- FIX: Only allow Flee if not forced to chase by alarm ---
                // Only allow flee if not alarm-spawned or alarm is dead
                bool ignoreFlee = false;
                if (crawler.AlarmSource != null && crawler.AlarmSource.enemyAI != null)
                {
                    ignoreFlee = crawler.AlarmSource.enemyAI.State == AlarmCarrierState.Summoning;
                }

                if (!ignoreFlee)
                {
                    float playerToPocket = Vector3.Distance(player.position, crawler.PocketPosition);
                    if (playerToPocket > crawler.fleeDistanceFromPocket)
                    {
                        crawler.TryFireTriggerByName("Flee");
                        yield break;
                    }
                }
                // else: do NOT fire Flee, keep swarming/chasing

                yield return new WaitForSeconds(0.05f);
            }
        }

        // Original chase logic for non-crawlers
        private IEnumerator DefaultChasePlayerLoop()
        {
            Debug.Log($"{enemy.gameObject.name}: DefaultChasePlayerLoop started!");
            
            // Get the "Chase" state enum value for the current enemy type
            TState chaseState = GetChaseState();
            
            while (enemy.enemyAI.State.Equals(chaseState))
            {
                // Re-acquire player target each frame (handles DontDestroyOnLoad)
                if (playerTarget == null || enemy.PlayerTarget != playerTarget)
                {
                    playerTarget = enemy.PlayerTarget;
                    Debug.Log($"{enemy.gameObject.name}: Acquired new player target: {(playerTarget != null ? playerTarget.name : "NULL")}");
                }
                
                if (playerTarget == null)
                {
                    Debug.LogWarning($"{enemy.gameObject.name}: Lost player target during chase!");
                    yield break;
                }
                
                float attackRange = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance;
                float distance = Vector3.Distance(enemy.transform.position, playerTarget.position);

                // Debug.Log($"{enemy.gameObject.name}: Chasing '{playerTarget.name}' at position {playerTarget.position} | My position: {enemy.transform.position} | Distance: {distance:F2}");

                MoveToAttackRange(playerTarget);

                if (distance <= attackRange)
                {
                    enemy.TryFireTriggerByName("InAttackRange");
                    yield break;
                }

                yield return null;
            }
        }
        
        // Helper method to get the Chase state for the current enemy type
        private TState GetChaseState()
        {
            // Try to parse "Chase" as the generic TState enum
            if (System.Enum.TryParse(typeof(TState), "Chase", out object result))
            {
                return (TState)result;
            }
            
            // Fallback: return default value (should never happen if enum has "Chase")
            Debug.LogError($"ChaseBehavior: Could not find 'Chase' state in enum {typeof(TState).Name}");
            return default(TState);
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
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}