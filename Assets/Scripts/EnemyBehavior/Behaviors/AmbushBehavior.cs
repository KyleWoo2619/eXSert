using UnityEngine;
using System.Collections;

namespace Behaviors
{
    public class AmbushBehavior<TState, TTrigger> : IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        private Coroutine ambushCoroutine;

        public void OnEnter(BaseEnemy<TState, TTrigger> enemy)
        {
            if (ambushCoroutine != null)
                enemy.StopCoroutine(ambushCoroutine);
            ambushCoroutine = enemy.StartCoroutine(ClusterAsBlob(enemy));
        }

        public void OnExit(BaseEnemy<TState, TTrigger> enemy)
        {
            if (ambushCoroutine != null)
                enemy.StopCoroutine(ambushCoroutine);
        }

        private IEnumerator ClusterAsBlob(BaseEnemy<TState, TTrigger> enemy)
        {
            var crawler = enemy as BaseCrawlerEnemy;
            if (crawler == null) yield break;

            Transform pocket = crawler.Pocket != null ? crawler.Pocket.transform : null;
            Transform player = crawler.PlayerTarget;
            float ambushRadius = 4f; // How far from pocket to cluster
            float ambushDetection = 12f; // How far player can move before crawlers chase/flee

            // Wait one frame after OnEnter to avoid firing triggers during transition
            yield return null;

            while (true)
            {
                // Cluster center is near the pocket
                Vector3 clusterCenter = pocket != null ? pocket.position : crawler.transform.position;
                Vector3 toCenter = (clusterCenter - crawler.transform.position);
                float distToCenter = toCenter.magnitude;

                // Move toward cluster center, apply separation
                Vector3 target = clusterCenter + (crawler.transform.position - clusterCenter).normalized * ambushRadius;
                if (crawler.agent != null && crawler.agent.enabled)
                    crawler.agent.SetDestination(target);

                crawler.ApplySeparation();

                // If player moves too far from pocket, flee
                if (player != null && pocket != null && Vector3.Distance(player.position, pocket.position) > ambushDetection * 2f)
                {
                    crawler.enemyAI.Fire(CrawlerEnemyTrigger.Flee);
                    yield break;
                }
                // If player moves out of ambush area but not far enough to flee, chase
                if (player != null && Vector3.Distance(player.position, clusterCenter) > ambushDetection)
                {
                    crawler.enemyAI.Fire(CrawlerEnemyTrigger.LosePlayer);
                    yield break;
                }

                // If close enough to cluster center, notify pocket
                if (distToCenter < 0.5f)
                {
                    if (crawler.Pocket != null && crawler.IsClustered())
                        crawler.Pocket.NotifyCrawlerReady(crawler);
                    break;
                }

                yield return new WaitForSeconds(0.05f);
            }

            // Only fire AmbushReady for crawlers still in Ambush state
            // After all crawlers are clustered, wait a short time before firing AmbushReady
            float minClusterTime = 1.5f; // seconds
            yield return new WaitForSeconds(minClusterTime);
            if (crawler.Pocket != null)
            {
                foreach (var c in crawler.Pocket.activeEnemies)
                {
                    if (c is BaseCrawlerEnemy baseCrawler &&
                        baseCrawler.enemyAI.State.Equals(CrawlerEnemyState.Ambush))
                    {
                        baseCrawler.enemyAI.Fire(CrawlerEnemyTrigger.AmbushReady);
                    }
                }
            }
        }
        public void Tick(BaseEnemy<TState, TTrigger> enemy)
        {
            // No per-frame logic needed for death
        }
    }
}