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
    public GameObject alarm;

    [Header("Alarm Settings")]
    [Tooltip("Time in seconds before alarm activates automatically (if player doesn't mount first)")]
    public float AlarmAutoActivateTime = 15f;
    private bool alarmActivated;
    private float alarmTimer;

    [Header("Spawn Pockets")]
    [Tooltip("Pockets for drone spawns (flying)")]
    public Transform[] droneSpawnPoints;
    [Tooltip("Pockets for crawler spawns (ground)")]
    public Transform[] crawlerSpawnPoints;

    [Tooltip("Legacy: used if specific arrays are empty")]
    public Transform[] pocketSpawnPoints;

    public GameObject dronePrefab;
    public GameObject crawlerPrefab;
    [Tooltip("Maximum number of each enemy type that can exist at once")]
    public int maxDrones = 4;
    public int maxCrawlers = 2;
    public int dronesPerSpawn = 4;
    public int crawlersPerSpawn = 2;
    public float suctionRadius = 5f;
    public float suctionStrength = 10f;
    public float dashSpeedMultiplier = 3f;

    [Header("Locomotion")]
    [Tooltip("Minimum stopping distance to avoid overlapping the player.")]
    public float MinStoppingDistance = 2.0f;
    [Tooltip("Ensure a kinematic Rigidbody for stable collisions and platform carry.")]
    public bool EnsureKinematicRigidbody = true;
    [Tooltip("Skip automatic collider setup - user will configure colliders manually.")]
    public bool ManualColliderSetup = false;
    [Tooltip("Extra buffer to re-enable movement after stopping; prevents jitter.")]
    public float ApproachHysteresis = 0.75f;
    [Tooltip("Max distance to adjust ring target to a nearby NavMesh point.")]
    public float ApproachSampleMaxDistance = 1.0f;

    [Header("Animator Parameters")]
    [SerializeField] private string ParamSpeed = "Speed";
    [SerializeField] private string ParamIsMoving = "IsMoving";
    [SerializeField] private string ParamTurn = "Turn";

    [Header("Top-Wander (Player On Top)")]
    [Tooltip("Speed multiplier during top-wander movement.")]
    public float TopWanderSpeedMultiplier = 1.1f;
    [Tooltip("Random target radius range (meters) for top-wander.")]
    public Vector2 TopWanderRadiusRange = new Vector2(4f, 10f);
    [Tooltip("Time range (seconds) before repicking a new wander target.")]
    public Vector2 TopWanderRepathTimeRange = new Vector2(0.7f, 1.4f);

    // Object pools
    private readonly Dictionary<Transform, GameObject> activeDrones = new Dictionary<Transform, GameObject>();
    private readonly Dictionary<Transform, GameObject> activeCrawlers = new Dictionary<Transform, GameObject>();
    private readonly Queue<GameObject> dronePool = new Queue<GameObject>();
    private readonly Queue<GameObject> crawlerPool = new Queue<GameObject>();
    public int initialPoolSize = 8;

    private Coroutine followRoutine;
    private Coroutine animParamsRoutine;
    private Coroutine topWanderRoutine;
    private Coroutine spawnManagementRoutine;
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
            
            // Only add collider if not doing manual setup
            if (!ManualColliderSetup)
            {
                // ADD: Ensure there's a physical (non-trigger) collider for standing on top
                var physicalCollider = GetComponent<CapsuleCollider>();
                if (physicalCollider == null)
                {
                    // Check if there's any non-trigger collider
                    var existingColliders = GetComponents<Collider>();
                    bool hasPhysicalCollider = false;
                    foreach (var col in existingColliders)
                    {
                        if (!col.isTrigger)
                        {
                            hasPhysicalCollider = true;
                            break;
                        }
                    }
                    
                    // If no physical collider exists, add one
                    if (!hasPhysicalCollider)
                    {
                        physicalCollider = gameObject.AddComponent<CapsuleCollider>();
                        physicalCollider.isTrigger = false;
                        physicalCollider.radius = 1.5f; // Adjust to match boss size
                        physicalCollider.height = 2f;   // Adjust to match boss height
                        physicalCollider.center = new Vector3(0, 1f, 0); // Center at half-height
                        Debug.Log("[BossRoombaController] Added physical CapsuleCollider for player collision/standing");
                    }
                }
            }
        }

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        
        // Prewarm pools
        for (int i = 0; i < initialPoolSize; i++)
        {
            if (dronePrefab != null)
            {
                var g = Instantiate(dronePrefab);
                g.SetActive(false);
                dronePool.Enqueue(g);
            }
            if (crawlerPrefab != null)
            {
                var c = Instantiate(crawlerPrefab);
                c.SetActive(false);
                crawlerPool.Enqueue(c);
            }
        }
    }

    void OnEnable()
    {
        if (animParamsRoutine != null) StopCoroutine(animParamsRoutine);
        animParamsRoutine = StartCoroutine(AnimParamsLoop(0.05f));
        
        alarmTimer = 0f;
        alarmActivated = false;
    }

    void OnDisable()
    {
        if (animParamsRoutine != null) { StopCoroutine(animParamsRoutine); animParamsRoutine = null; }
        if (spawnManagementRoutine != null) { StopCoroutine(spawnManagementRoutine); spawnManagementRoutine = null; }
        StopTopWander();
        StopFollowing();
    }

    private IEnumerator AnimParamsLoop(float cadence)
    {
        var wait = WaitForSecondsCache.Get(Mathf.Max(0.02f, cadence));
        if (animator == null) yield break;
        while (true)
        {
            float spd = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;
            
            if (!string.IsNullOrEmpty(ParamSpeed))
                animator.SetFloat(ParamSpeed, spd);
            
            if (!string.IsNullOrEmpty(ParamIsMoving))
                animator.SetBool(ParamIsMoving, spd > 0.1f);

            if (!string.IsNullOrEmpty(ParamTurn) && agent != null && agent.enabled)
            {
                Vector3 desired = agent.desiredVelocity;
                if (desired.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.SignedAngle(transform.forward, desired, Vector3.up);
                    float turn = Mathf.Clamp(angle / 45f, -1f, 1f);
                    animator.SetFloat(ParamTurn, turn);
                }
                else
                {
                    animator.SetFloat(ParamTurn, 0f);
                }
            }

            yield return wait;
        }
    }

    void Start()
    {
        ApplyProfile();
        player = GameObject.FindWithTag("Player")?.transform;
        
        var ca = new EnemyBehavior.Crowd.CrowdAgent() { Agent = agent, Profile = profile };
        if (EnemyBehavior.Crowd.CrowdController.Instance != null)
            EnemyBehavior.Crowd.CrowdController.Instance.Register(ca);
    }

    void Update()
    {
        // Handle alarm timing
        if (!alarmActivated && alarm != null)
        {
            alarmTimer += Time.deltaTime;
            if (alarmTimer >= AlarmAutoActivateTime)
            {
                ActivateAlarm();
            }
        }
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
        var wait = WaitForSecondsCache.Get(Mathf.Max(0.02f, cadence));
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

    public void StartTopWander()
    {
        if (topWanderActive) return;
        topWanderActive = true;

        // Trigger alarm on player mount (if not already activated)
        if (!alarmActivated)
        {
            ActivateAlarm();
        }

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

        agent.speed = savedSpeed;
        agent.autoBraking = savedAutoBraking;
        agent.stoppingDistance = savedStoppingDistance;

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

    public void ActivateAlarm()
    {
        if (alarmActivated) return;
        
        alarmActivated = true;
        if (alarm != null) alarm.SetActive(true);
        
        Debug.Log("Alarm ACTIVATED - Starting spawn management");
        
        // Start spawn management routine
        if (spawnManagementRoutine != null) StopCoroutine(spawnManagementRoutine);
        spawnManagementRoutine = StartCoroutine(ManageSpawnsRoutine());
    }

    public void DeactivateAlarm()
    {
        alarmActivated = false;
        if (alarm != null) alarm.SetActive(false);
        
        Debug.Log("Alarm DEACTIVATED - Stopping spawn management");
        
        if (spawnManagementRoutine != null)
        {
            StopCoroutine(spawnManagementRoutine);
            spawnManagementRoutine = null;
        }
    }

    private IEnumerator ManageSpawnsRoutine()
    {
        var wait = WaitForSecondsCache.Get(2f);
        
        while (alarmActivated)
        {
            RespawnDeadEnemies(activeDrones, droneSpawnPoints, dronePrefab, maxDrones, dronePool);
            RespawnDeadEnemies(activeCrawlers, crawlerSpawnPoints, crawlerPrefab, maxCrawlers, crawlerPool);
            
            yield return wait;
        }
    }

    private void RespawnDeadEnemies(Dictionary<Transform, GameObject> activeEnemies, Transform[] spawnPoints, 
        GameObject prefab, int maxCount, Queue<GameObject> pool)
    {
        if (prefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        var deadSpawnPoints = new List<Transform>();
        foreach (var kvp in activeEnemies)
        {
            if (kvp.Value == null || !kvp.Value.activeInHierarchy)
            {
                deadSpawnPoints.Add(kvp.Key);
            }
        }

        foreach (var sp in deadSpawnPoints)
        {
            activeEnemies.Remove(sp);
        }

        int currentCount = activeEnemies.Count;
        int toSpawn = Mathf.Min(maxCount - currentCount, spawnPoints.Length);

        for (int i = 0; i < toSpawn; i++)
        {
            Transform spawnPoint = null;
            foreach (var sp in spawnPoints)
            {
                if (!activeEnemies.ContainsKey(sp))
                {
                    spawnPoint = sp;
                    break;
                }
            }

            if (spawnPoint != null)
            {
                var enemy = SpawnEnemy(prefab, spawnPoint.position, pool);
                if (enemy != null)
                {
                    activeEnemies[spawnPoint] = enemy;
                }
            }
        }
    }

    private GameObject SpawnEnemy(GameObject prefab, Vector3 position, Queue<GameObject> pool)
    {
        GameObject enemy = null;

        while (pool.Count > 0)
        {
            enemy = pool.Dequeue();
            if (enemy != null) break;
        }

        if (enemy == null)
        {
            enemy = Instantiate(prefab);
        }

        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;
        enemy.SetActive(true);

        RegisterSpawned(enemy);

        return enemy;
    }

    public void TriggerSpawnWave(int totalDrones, int totalCrawlers)
    {
        StartCoroutine(SpawnAddsCoroutine(totalDrones, totalCrawlers));
    }

    private IEnumerator SpawnAddsCoroutine(int totalDrones, int totalCrawlers)
    {
        var drones = (droneSpawnPoints != null && droneSpawnPoints.Length > 0) ? droneSpawnPoints : pocketSpawnPoints;
        var crawlers = (crawlerSpawnPoints != null && crawlerSpawnPoints.Length > 0) ? crawlerSpawnPoints : pocketSpawnPoints;

        if (dronePrefab != null && drones != null && drones.Length > 0 && totalDrones > 0)
        {
            for (int i = 0; i < totalDrones; i++)
            {
                var p = drones[i % drones.Length];
                if (p == null) continue;
                var g = SpawnEnemy(dronePrefab, p.position, dronePool);
                yield return null;
            }
        }

        if (crawlerPrefab != null && crawlers != null && crawlers.Length > 0 && totalCrawlers > 0)
        {
            for (int j = 0; j < totalCrawlers; j++)
            {
                var p = crawlers[j % crawlers.Length];
                if (p == null) continue;
                var c = SpawnEnemy(crawlerPrefab, p.position, crawlerPool);
                yield return null;
            }
        }
    }

    private void RegisterSpawned(GameObject g)
    {
        var agentComp = g.GetComponent<NavMeshAgent>();
        if (agentComp != null && EnemyBehavior.Crowd.CrowdController.Instance != null)
        {
            var ca = new EnemyBehavior.Crowd.CrowdAgent() { Agent = agentComp, Profile = profile };
            EnemyBehavior.Crowd.CrowdController.Instance.Register(ca);
        }
    }

    public void BeginSuction(float duration)
    {
        StartCoroutine(SuctionCoroutine(duration));
    }

    private IEnumerator SuctionCoroutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            
            if (player != null)
            {
                var dir = (transform.position - player.position);
                float d = dir.magnitude;
                if (d < suctionRadius)
                {
                    var force = dir.normalized * (suctionStrength / Mathf.Max(1f, d));
                }
            }

            var coll = Physics.OverlapSphere(transform.position, suctionRadius);
            foreach (var c in coll)
            {
                if (c == null) continue;
                if (c.gameObject.CompareTag("Enemy"))
                {
                    var rb = c.attachedRigidbody;
                    if (rb != null)
                        rb.AddForce((transform.position - c.transform.position).normalized * suctionStrength * Time.deltaTime, ForceMode.VelocityChange);
                }
            }

            yield return null;
        }
    }
}
