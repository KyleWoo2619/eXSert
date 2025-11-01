// BossRoombaController.cs
// Purpose: Controller for the Roomba-style boss: movement, special abilities (suction, dash), and spawning pocket adds.
// Works with: CrowdController (registers spawned adds), ScenePoolManager (uses local pools), EnemyBehaviorProfile.
// Notes: Uses scene-local pooling to reduce Instantiate/Destroy overhead for spawned adds.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossRoombaController : MonoBehaviour
{
    public EnemyBehaviorProfile profile;
    private NavMeshAgent agent;
    private Transform player;
    public GameObject alarm; // visual alarm object
    public Transform[] pocketSpawnPoints;
    public GameObject dronePrefab;
    public GameObject crawlerPrefab;
    public int dronesPerSpawn =4;
    public int crawlersPerSpawn =2;
    public float suctionRadius =5f;
    public float suctionStrength =10f;
    public float dashSpeedMultiplier =3f;
    private bool alarmActive = false;

    // simple local pools
    private readonly Queue<GameObject> dronePool = new Queue<GameObject>();
    private readonly Queue<GameObject> crawlerPool = new Queue<GameObject>();
    public int initialPoolSize =8;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // prewarm pools
        for (int i =0; i < initialPoolSize; i++)
        {
            if (dronePrefab != null) { var g = Instantiate(dronePrefab); g.SetActive(false); dronePool.Enqueue(g); }
            if (crawlerPrefab != null) { var c = Instantiate(crawlerPrefab); c.SetActive(false); crawlerPool.Enqueue(c); }
        }
    }

    void Start()
    {
        ApplyProfile();
        player = GameObject.FindWithTag("Player")?.transform;
        // optionally register boss as a high-importance crowd agent if desired
        var ca = new EnemyBehavior.Crowd.CrowdAgent() { Agent = agent, Profile = profile };
        if (EnemyBehavior.Crowd.CrowdController.Instance != null)
            EnemyBehavior.Crowd.CrowdController.Instance.Register(ca);
    }

    void ApplyProfile()
    {
        if (profile == null) return;
        agent.speed = Random.Range(profile.SpeedRange.x, profile.SpeedRange.y);
        agent.acceleration = profile.Acceleration;
        agent.angularSpeed = profile.AngularSpeed;
        agent.stoppingDistance = profile.StoppingDistance;
        agent.avoidancePriority = profile.AvoidancePriority;
        agent.autoBraking = false;
    }

    void Update()
    {
        if (player == null) return;
        // Simple behavior: approach player
        if (!alarmActive) agent.SetDestination(player.position);
    }

    // Example: trigger alarm and spawn adds
    public void ActivateAlarm()
    {
        if (alarm != null) alarm.SetActive(true);
        alarmActive = true;
        StartCoroutine(SpawnAddsCoroutine());
    }

    private IEnumerator SpawnAddsCoroutine()
    {
        if (pocketSpawnPoints == null) yield break;
        // spawn drones and crawlers from pockets using pooling ideally; simple instantiate for now
        foreach (var p in pocketSpawnPoints)
        {
            if (p == null) continue;
            for (int i =0; i < dronesPerSpawn; i++)
            {
                var g = SpawnFromPool(dronePool, dronePrefab);
                if (g != null) { g.transform.position = p.position; g.SetActive(true); RegisterSpawned(g); }
            }
            for (int j =0; j < crawlersPerSpawn; j++)
            {
                var c = SpawnFromPool(crawlerPool, crawlerPrefab);
                if (c != null) { c.transform.position = p.position; c.SetActive(true); RegisterSpawned(c); }
            }
            yield return new WaitForSeconds(0.2f); // stagger
        }
    }

    private GameObject SpawnFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (prefab == null) return null;
        if (pool.Count >0)
        {
            var g = pool.Dequeue();
            return g;
        }
        var newG = Instantiate(prefab);
        return newG;
    }

    private void RegisterSpawned(GameObject g)
    {
        // try to register with crowd controller
        var agentComp = g.GetComponent<NavMeshAgent>();
        if (agentComp != null && EnemyBehavior.Crowd.CrowdController.Instance != null)
        {
            var ca = new EnemyBehavior.Crowd.CrowdAgent() { Agent = agentComp, Profile = profile };
            EnemyBehavior.Crowd.CrowdController.Instance.Register(ca);
        }
    }

    // Suction: pull player and adds toward boss center
    public void BeginSuction(float duration)
    {
        StartCoroutine(SuctionCoroutine(duration));
    }

    private IEnumerator SuctionCoroutine(float duration)
    {
        float t =0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // affect player
            if (player != null)
            {
                var dir = (transform.position - player.position);
                float d = dir.magnitude;
                if (d < suctionRadius)
                {
                    var force = dir.normalized * (suctionStrength / Mathf.Max(1f, d));
                    // pseudocode: apply to player controller
                }
            }

            // destroy or pull in drones/crawlers
            var coll = Physics.OverlapSphere(transform.position, suctionRadius);
            foreach (var c in coll)
            {
                if (c == null) continue;
                if (c.gameObject.CompareTag("Enemy"))
                {
                    // pull toward center (if has rigidbody), or disable
                    var rb = c.attachedRigidbody;
                    if (rb != null)
                        rb.AddForce((transform.position - c.transform.position).normalized * suctionStrength * Time.deltaTime, ForceMode.VelocityChange);
                }
            }

            yield return null;
        }
    }

    // Slam logic could go here

    // Parry callback pseudocode
    private void OnParriedCallback(GameObject player)
    {
        StopAllCoroutines();
        // enter stagger state
    }
}
