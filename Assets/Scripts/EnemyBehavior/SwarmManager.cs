using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class SwarmManager : MonoBehaviour
{
    public static SwarmManager Instance { get; private set; }

    private Queue<BaseCrawlerEnemy> attackQueue = new();
    [SerializeField] private int maxAttackers = 3;

    // Expose the list in the Inspector as read-only
    [ReadOnly, SerializeField, Tooltip("Currently managed swarming enemies (debug only, do not edit).")]
    private List<BaseCrawlerEnemy> debugSwarmMembers = new();

    private readonly List<BaseCrawlerEnemy> swarmMembers = new();
    private Transform player;

    private int crawlerLayerMask;
    [SerializeField] private float minSeparation = 2f;

    private readonly Collider[] overlapBuffer = new Collider[32]; // Adjust size as needed

    // --- Event for swarm count changes ---
    public event System.Action<int> OnActiveCrawlersChanged;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        crawlerLayerMask = 1 << LayerMask.NameToLayer("Crawler");
        StartCoroutine(SeparationRoutine());
    }

    // Unified registration method
    public void AddToSwarm(BaseCrawlerEnemy crawler)
    {
        if (!swarmMembers.Contains(crawler))
        {
            swarmMembers.Add(crawler);
            debugSwarmMembers.Clear();
            debugSwarmMembers.AddRange(swarmMembers);
            OnActiveCrawlersChanged?.Invoke(swarmMembers.Count);
            Debug.Log($"SwarmManager: Added {crawler.gameObject.name} to swarm.", crawler);

            if (!attackQueue.Contains(crawler))
                attackQueue.Enqueue(crawler);
        }
    }

    // Unified unregistration method
    public void RemoveFromSwarm(BaseCrawlerEnemy crawler)
    {
        if (swarmMembers.Contains(crawler))
        {
            swarmMembers.Remove(crawler);
            debugSwarmMembers.Clear();
            debugSwarmMembers.AddRange(swarmMembers);
            OnActiveCrawlersChanged?.Invoke(swarmMembers.Count);
            Debug.Log($"SwarmManager: Removed {crawler.gameObject.name} from swarm.", crawler);

            // Remove from attack queue
            var tempQueue = new Queue<BaseCrawlerEnemy>(attackQueue.Where(c => c != crawler));
            attackQueue = tempQueue;
        }
    }

    public void UpdateSwarm()
    {
        if (player == null) return;
        var sorted = swarmMembers.OrderBy(c => Vector3.Distance(c.transform.position, player.position)).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i < maxAttackers)
            {
                // Allow these to attack if not already attacking
                if (sorted[i].enemyAI.State == CrawlerEnemyState.Swarm)
                    sorted[i].TryFireTriggerByName("InAttackRange");
            }
            // else: do nothing, just keep encircling
        }
    }

    public IEnumerable<BaseCrawlerEnemy> GetAttackers()
    {
        // Return the first maxAttackers in the queue
        return attackQueue.Take(maxAttackers);
    }

    // Separation routine to avoid overlapping crawlers
    private IEnumerator SeparationRoutine()
    {
        while (true)
        {
            foreach (var crawler in swarmMembers)
            {
                if (crawler == null) continue; // Skip destroyed crawlers

                int hitCount = Physics.OverlapSphereNonAlloc(
                    crawler.transform.position,
                    minSeparation,
                    overlapBuffer,
                    crawlerLayerMask
                );

                Vector3 separation = Vector3.zero;
                int count = 0;
                for (int i = 0; i < hitCount; i++)
                {
                    var other = overlapBuffer[i].GetComponent<BaseCrawlerEnemy>();
                    if (other != null && other != crawler)
                    {
                        float dist = Vector3.Distance(crawler.transform.position, other.transform.position);
                        separation += (crawler.transform.position - other.transform.position).normalized * (minSeparation - dist);
                        count++;
                    }
                }
                if (count > 0)
                {
                    separation /= count;
                    crawler.agent.Move(separation * 0.5f);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public int GetSwarmIndex(BaseCrawlerEnemy crawler)
    {
        return swarmMembers.IndexOf(crawler);
    }

    public int GetSwarmCount()
    {
        return swarmMembers.Count;
    }

    public List<BaseCrawlerEnemy> GetActiveCrawlers() => swarmMembers;

    public void RotateAttackers()
    {
        if (attackQueue.Count > 0)
        {
            var crawler = attackQueue.Dequeue();
            attackQueue.Enqueue(crawler);
        }
    }
}