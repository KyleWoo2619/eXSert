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
    [Header("Behavior Profile")]
    [SerializeField, Tooltip(
        "ScriptableObject that tunes nav/avoidance/importance and planner hints.\n" +
        "Assign an asset from Assets > Scripts > EnemyBehavior > Profiles (Create > AI > EnemyBehaviorProfile).\n" +
        "Values are applied to the boss NavMeshAgent on Start and passed to Crowd/Path systems.")]
    public EnemyBehaviorProfile profile;

    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    public GameObject alarm; // visual alarm object

    [Header("Spawn Pockets")]
    [Tooltip("Pockets for drone spawns (flying)")]
    public Transform[] droneSpawnPoints;
    [Tooltip("Pockets for crawler spawns (ground)")]
    public Transform[] crawlerSpawnPoints;

    [Tooltip("Legacy: used if specific arrays are empty")]
    public Transform[] pocketSpawnPoints;

    public GameObject dronePrefab;
    public GameObject crawlerPrefab;
    public int dronesPerSpawn =4;
    public int crawlersPerSpawn =2;
    public float suctionRadius =5f;
    public float suctionStrength =10f;
    public float dashSpeedMultiplier =3f;

    [Header("Locomotion")]
    [Tooltip("Minimum stopping distance to avoid overlapping the player.")]
    public float MinStoppingDistance = 2.0f;
    [Tooltip("Ensure a kinematic Rigidbody for stable collisions and platform carry.")]
    public bool EnsureKinematicRigidbody = true;
    [Tooltip("Extra buffer to re-enable movement after stopping; prevents jitter.")]
    public float ApproachHysteresis = 0.75f;
    [Tooltip("Max distance to adjust ring target to a nearby NavMesh point.")]
    public float ApproachSampleMaxDistance = 1.0f;

    [Header("Top-Wander (Player On Top)")]
    [Tooltip("Speed multiplier during top-wander movement.")]
    public float TopWanderSpeedMultiplier = 1.1f;
    [Tooltip("Random target radius range (meters) for top-wander.")]
    public Vector2 TopWanderRadiusRange = new Vector2(4f, 10f);
    [Tooltip("Time range (seconds) before repicking a new wander target.")]
    public Vector2 TopWanderRepathTimeRange = new Vector2(0.7f, 1.4f);

    // simple local pools (fallback if ScenePoolManager not present)
    private readonly Queue<GameObject> dronePool = new Queue<GameObject>();
    private readonly Queue<GameObject> crawlerPool = new Queue<GameObject>();
    public int initialPoolSize =8;

    private Coroutine followRoutine;
    private Coroutine animParamsRoutine;
    private Coroutine topWanderRoutine;
    private float lastFollowCadence = 0.1f;

    // Saved agent settings for top-wander
    private bool topWanderActive;
    private float savedSpeed;
    private bool savedAutoBraking;
    private float savedStoppingDistance;

    void Awake()
    {
        if (EnsureKinematicRigidbody)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        // prewarm pools
        for (int i =0; i < initialPoolSize; i++)
        {
            if (dronePrefab != null) { var g = Instantiate(dronePrefab); g.SetActive(false); dronePool.Enqueue(g); }
            if (crawlerPrefab != null) { var c = Instantiate(crawlerPrefab); c.SetActive(false); crawlerPool.Enqueue(c); }
        }
    }

    void OnEnable()
    {
        if (animParamsRoutine != null) StopCoroutine(animParamsRoutine);
        animParamsRoutine = StartCoroutine(AnimParamsLoop(0.05f));
    }

    void OnDisable()
    {
        if (animParamsRoutine != null) { StopCoroutine(animParamsRoutine); animParamsRoutine = null; }
        StopTopWander();
        StopFollowing();
    }

    private IEnumerator AnimParamsLoop(float cadence)
    {
        var wait = new WaitForSeconds(Mathf.Max(0.02f, cadence));
        if (animator == null) yield break; // single null check at start
        while (true)
        {
            float spd = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", spd);
            animator.SetBool("IsMoving", spd > 0.1f);
            yield return wait;
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
        agent.stoppingDistance = Mathf.Max(profile.StoppingDistance, MinStoppingDistance);
        agent.avoidancePriority = profile.AvoidancePriority;
        agent.autoBraking = false;
    }

    // Event-driven follow (avoids Update)
    public void StartFollowingPlayer(float cadenceSeconds)
    {
        lastFollowCadence = Mathf.Max(0.02f, cadenceSeconds);
        if (followRoutine != null) StopCoroutine(followRoutine);
        followRoutine = StartCoroutine(FollowLoop(lastFollowCadence));
    }

    public void StopFollowing()
    {
        if (followRoutine != null) { StopCoroutine(followRoutine); followRoutine = null; }
    }

    private Vector3 ComputeApproachPoint(Vector3 bossPos, Vector3 playerPos)
    {
        Vector3 toPlayer = playerPos - bossPos;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        if (dist < 0.001f)
            return bossPos;
        float ring = Mathf.Max(agent.stoppingDistance, MinStoppingDistance);
        Vector3 candidate = playerPos - toPlayer.normalized * ring;
        if (NavMesh.SamplePosition(candidate, out var hit, ApproachSampleMaxDistance, NavMesh.AllAreas))
            return hit.position;
        return candidate;
    }

    private IEnumerator FollowLoop(float cadence)
    {
        var wait = new WaitForSeconds(Mathf.Max(0.02f, cadence));
        while (true)
        {
            if (player != null)
            {
                Vector3 bossPos = transform.position;
                Vector3 playerPos = player.position;
                Vector3 flat = playerPos - bossPos; flat.y = 0f;
                float dist = flat.magnitude;
                float stop = Mathf.Max(agent.stoppingDistance, MinStoppingDistance);

                if (dist <= stop)
                {
                    if (!agent.isStopped)
                    {
                        agent.ResetPath();
                        agent.isStopped = true;
                    }
                }
                else if (dist > stop + ApproachHysteresis)
                {
                    agent.isStopped = false;
                    Vector3 target = ComputeApproachPoint(bossPos, playerPos);
                    agent.SetDestination(target);
                }
            }
            yield return wait;
        }
    }

    // Top-wander movement while player is on top
    public void StartTopWander()
    {
        if (topWanderActive) return;
        topWanderActive = true;

        // Save and apply wander settings
        savedSpeed = agent.speed;
        savedAutoBraking = agent.autoBraking;
        savedStoppingDistance = agent.stoppingDistance;

        agent.autoBraking = false;
        agent.stoppingDistance = 0f;
        agent.speed = savedSpeed * TopWanderSpeedMultiplier;

        StopFollowing();
        if (topWanderRoutine != null) StopCoroutine(topWanderRoutine);
        topWanderRoutine = StartCoroutine(TopWanderLoop());
    }

    public void StopTopWander()
    {
        if (!topWanderActive) return;
        topWanderActive = false;

        if (topWanderRoutine != null)
        {
            StopCoroutine(topWanderRoutine);
            topWanderRoutine = null;
        }

        // Restore saved movement settings
        agent.speed = savedSpeed;
        agent.autoBraking = savedAutoBraking;
        agent.stoppingDistance = savedStoppingDistance;

        // Resume following after wander ends
        StartFollowingPlayer(lastFollowCadence);
    }

    private IEnumerator TopWanderLoop()
    {
        while (true)
        {
            Vector3 origin = transform.position;
            float radius = Random.Range(TopWanderRadiusRange.x, TopWanderRadiusRange.y);
            Vector2 dir2D = Random.insideUnitCircle.normalized;
            Vector3 candidate = origin + new Vector3(dir2D.x, 0f, dir2D.y) * radius;
            if (NavMesh.SamplePosition(candidate, out var hit, 2.0f, NavMesh.AllAreas))
                candidate = hit.position;

            agent.isStopped = false;
            agent.SetDestination(candidate);

            float timeout = Random.Range(TopWanderRepathTimeRange.x, TopWanderRepathTimeRange.y);
            float t = 0f;
            while (t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    // Alarm visual only (no spawning)
    public void ActivateAlarm()
    {
        if (alarm != null) alarm.SetActive(true);
    }

    public void DeactivateAlarm()
    {
        if (alarm != null) alarm.SetActive(false);
    }

    // Spawn a total count distributed across available spawn points
    public void TriggerSpawnWave(int totalDrones, int totalCrawlers)
    {
        StartCoroutine(SpawnAddsCoroutine(totalDrones, totalCrawlers));
    }

    private IEnumerator SpawnAddsCoroutine(int totalDrones, int totalCrawlers)
    {
        var drones = (droneSpawnPoints != null && droneSpawnPoints.Length > 0) ? droneSpawnPoints : pocketSpawnPoints;
        var crawlers = (crawlerSpawnPoints != null && crawlerSpawnPoints.Length > 0) ? crawlerSpawnPoints : pocketSpawnPoints;

        // Drones: distribute across points
        if (dronePrefab != null && drones != null && drones.Length > 0 && totalDrones > 0)
        {
            for (int i = 0; i < totalDrones; i++)
            {
                var p = drones[i % drones.Length];
                if (p == null) continue;
                var g = Spawn(dronePrefab, p.position);
                if (g != null) { g.SetActive(true); RegisterSpawned(g); }
                yield return null;
            }
        }

        // Crawlers: distribute across points
        if (crawlerPrefab != null && crawlers != null && crawlers.Length > 0 && totalCrawlers > 0)
        {
            for (int j = 0; j < totalCrawlers; j++)
            {
                var p = crawlers[j % crawlers.Length];
                if (p == null) continue;
                var c = Spawn(crawlerPrefab, p.position);
                if (c != null) { c.SetActive(true); RegisterSpawned(c); }
                yield return null;
            }
        }
    }

    private GameObject Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        // Prefer scene pool manager if available
        var spm = EnemyBehavior.Crowd.ScenePoolManager.Instance;
        if (spm != null)
            return spm.Spawn(prefab, pos, Quaternion.identity);

        // Fallback local pools
        if (prefab == dronePrefab && dronePool.Count >0)
        {
            var g = dronePool.Dequeue();
            g.transform.position = pos;
            return g;
        }
        if (prefab == crawlerPrefab && crawlerPool.Count >0)
        {
            var c = crawlerPool.Dequeue();
            c.transform.position = pos;
            return c;
        }
        var newG = Instantiate(prefab, pos, Quaternion.identity);
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
