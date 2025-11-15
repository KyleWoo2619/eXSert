// ChaseBehavior.cs
// Purpose: Behavior module implementing Chase logic: pursuit and attack-range checks.
// Works with: BaseEnemy state machine, PathRequestManager for pathing, NavMeshAgent movement.

using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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

            // Special handling for BaseCrawlerEnemy with ForceChasePlayer
            if (enemy is BaseCrawlerEnemy crawler && crawler.ForceChasePlayer)
            {
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
                return;
            }

            if (playerTarget != null && enemy.agent != null && enemy.agent.enabled)
            {
                enemy.agent.isStopped = false;
                // First tick toward player; loop will maintain pursuit
                enemy.agent.SetDestination(playerTarget.position);
            }

            enemy.SetEnemyColor(enemy.chaseColor);

            if (chaseCoroutine != null)
                enemy.StopCoroutine(chaseCoroutine);

            // Use blob chase for crawlers, default for others
            if (enemy is BaseCrawlerEnemy baseCrawler)
                chaseCoroutine = enemy.StartCoroutine(CrawlerChaseBlob(baseCrawler));
            else
                chaseCoroutine = enemy.StartCoroutine(DefaultChasePlayerLoop());
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
            Transform player = crawler.PlayerTarget;
            while (crawler.enemyAI.State.Equals(CrawlerEnemyState.Chase) && player != null)
            {
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

        // Chase logic for non-crawlers
        private IEnumerator DefaultChasePlayerLoop()
        {
            // A generous threshold to leave chase if the player is far (fallback when no explicit detection range available here)
            const float losePlayerDistance = 25f;

            while (enemy.enemyAI.State.ToString().Equals("Chase") && playerTarget != null)
            {
                if (enemy.agent != null && enemy.agent.enabled)
                {
                    MoveToAttackRange(playerTarget);
                }

                float attackRange = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance;
                float distance = Vector3.Distance(enemy.transform.position, playerTarget.position);

                if (distance <= attackRange)
                {
                    enemy.TryFireTriggerByName("InAttackRange");
                    yield break;
                }

                if (distance >= losePlayerDistance)
                {
                    enemy.TryFireTriggerByName("LosePlayer");
                    yield break;
                }

                yield return null;
            }
        }

        // Picks an approach point around the player at the desired reach and avoids obstacle corners
        private void MoveToAttackRange(Transform player)
        {
            if (enemy.agent == null) return;

            // Desired radial distance from player to stand at before attacking
            float chaseBuffer = 0.2f;
            float reach = (Mathf.Max(enemy.attackBoxSize.x, enemy.attackBoxSize.z) * 0.5f) + enemy.attackBoxDistance - chaseBuffer;
            reach = Mathf.Max(0.1f, reach);

            Vector3 toPlayer = player.position - enemy.transform.position; toPlayer.y = 0f;
            Vector3 baseDir = toPlayer.sqrMagnitude < 0.001f ? enemy.transform.forward : toPlayer.normalized;

            // Try candidates around an arc near the facing direction: 0, ±20, ±40, ±60 degrees
            float[] angles = new float[] { 0f, 20f, -20f, 40f, -40f, 60f, -60f };
            Vector3 best = Vector3.zero;
            bool found = false;
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(angles[i], Vector3.up) * baseDir;
                Vector3 candidate = player.position - dir * reach;
                candidate.y = enemy.transform.position.y;

                // Snap to closest navmesh point near candidate
                if (!NavMesh.SamplePosition(candidate, out var hit, 1.0f, NavMesh.AllAreas))
                    continue;

                // Prefer straight clear ray on navmesh between current and candidate
                if (!NavMesh.Raycast(enemy.transform.position, hit.position, out var navHit, NavMesh.AllAreas))
                {
                    best = hit.position;
                    found = true;
                    break;
                }

                // Keep first valid sample as fallback
                if (!found)
                {
                    best = hit.position;
                    found = true;
                }
            }

            if (!found)
            {
                // Final fallback: head directly to player's sampled position
                if (NavMesh.SamplePosition(player.position, out var phit, 1.5f, NavMesh.AllAreas))
                {
                    best = phit.position;
                    found = true;
                }
            }

            if (found)
            {
                enemy.agent.isStopped = false;
                enemy.agent.SetDestination(best);
            }
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}