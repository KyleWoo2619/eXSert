using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class SwarmBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private BaseEnemy<TState, TTrigger> enemy;
        private Transform player;
        private bool isSwarming = false;
        private Coroutine swarmCoroutine;

        // Tweak these as needed
        private float outOfRangeBuffer = 7.5f;
        private float attackBuffer = 0.5f;

        public virtual void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            this.enemy = enemy;
            player = enemy.PlayerTarget;
            isSwarming = true;

            if (enemy.PlayerTarget != null && enemy.agent != null && enemy.agent.enabled)
            {
                enemy.agent.isStopped = false;
                enemy.agent.SetDestination(enemy.PlayerTarget.position);
            }

            // Start the swarm coroutine
            if (swarmCoroutine != null)
                enemy.StopCoroutine(swarmCoroutine);
            swarmCoroutine = enemy.StartCoroutine(SwarmTowardsPlayer());
        }

        public virtual void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            isSwarming = false;
            if (swarmCoroutine != null)
                enemy.StopCoroutine(swarmCoroutine);
            swarmCoroutine = null;
        }

        private IEnumerator SwarmTowardsPlayer()
        {
            var crawler = enemy as BaseCrawlerEnemy;
            if (crawler == null)
                yield break;

            // Wait one frame after OnEntry to avoid firing triggers during transition
            yield return null;

            while (isSwarming && player != null)
            {
                // Calculate dynamic swarm radius based on attack range and swarm size
                int swarmCount = SwarmManager.Instance != null ? SwarmManager.Instance.GetSwarmCount() : 1;
                int myIndex = SwarmManager.Instance != null ? SwarmManager.Instance.GetSwarmIndex(crawler) : 0;

                // Calculate swarm radius based on attack hit box
                float desiredRadius = (Mathf.Max(crawler.attackBoxSize.x, crawler.attackBoxSize.z) * 0.5f) + crawler.attackBoxDistance - 0.5f; // Slightly inside attack range

                // Distribute crawlers evenly around the player
                float angleStep = 2 * Mathf.PI / Mathf.Max(swarmCount, 1);
                float myAngle = myIndex * angleStep;

                // Calculate base position around the player
                Vector3 offset = new Vector3(Mathf.Cos(myAngle), 0, Mathf.Sin(myAngle)) * desiredRadius;

                // Add jitter for organic movement
                Vector3 jitter = Vector3.zero;
                if (Random.value < 0.2f) // Only apply jitter 20% of the time
                {
                    jitter = new Vector3(
                        Random.Range(-0.3f, 0.3f),
                        0,
                        Random.Range(-0.3f, 0.3f)
                    );
                }

                Vector3 target = player.position + offset + jitter;

                float distToCenter = Vector3.Distance(crawler.transform.position, player.position);

                // If close enough to attack, fire attack trigger
                if (distToCenter <= crawler.attackBoxDistance + attackBuffer)
                {
                    crawler.TryFireTriggerByName("InAttackRange");
                    yield break;
                }

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

                // If player is out of swarm range but not far enough to flee, chase
                if (distToCenter > desiredRadius + outOfRangeBuffer)
                {
                    crawler.TryFireTriggerByName("LosePlayer");
                    yield break;
                }

                // Only update destination if the target has changed significantly
                if (crawler.agent != null && crawler.agent.enabled)
                {
                    if (Vector3.Distance(crawler.agent.destination, target) > 0.5f) // Only update if needed
                        crawler.agent.SetDestination(target);
                }

                // Face the player
                Vector3 lookDirection = (player.position - crawler.transform.position);
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                    crawler.transform.rotation = Quaternion.LookRotation(lookDirection);

                crawler.ApplySeparation();

                yield return new WaitForSeconds(0.05f);
            }
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}