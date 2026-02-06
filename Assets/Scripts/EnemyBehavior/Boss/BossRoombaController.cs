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
    [Tooltip("Delay in seconds after fight start or form change before alarm activates (random range)")]
    public Vector2 AlarmActivationDelayRange = new Vector2(3f, 5f);
    [Tooltip("Maximum health of the alarm. When destroyed, no more adds spawn.")]
    public float AlarmMaxHealth = 100f;
    [Tooltip("Current health of the alarm (runtime).")]
    [SerializeField] private float alarmCurrentHealth;
    [Tooltip("Damage multiplier when attacking the alarm (e.g., 0.5 = half damage)")]
    public float AlarmDamageMultiplier = 0.5f;
    [Tooltip("Should the alarm object fall off and be destroyed visually when health reaches 0?")]
    public bool AlarmFallsOffWhenDestroyed = true;
    [Tooltip("Force applied when alarm breaks off")]
    public float AlarmBreakOffForce = 200f;
    private bool alarmActivated;
    private bool alarmDestroyed;
    private float alarmTimer;
    private Coroutine alarmActivationDelayRoutine;

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
    public float MinStoppingDistance = 4.0f;
    [Tooltip("Additional offset from player during combat approach (added to stopping distance).")]
    public float CombatDistanceOffset = 2.0f;
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
        
        // Initialize alarm health
        InitializeAlarmHealth();
    }

    void OnEnable()
    {
        if (animParamsRoutine != null) StopCoroutine(animParamsRoutine);
        animParamsRoutine = StartCoroutine(AnimParamsLoop(0.05f));
        
        alarmTimer = 0f;
        alarmActivated = false;
        // Reset alarm on enable (in case of pooling/re-enabling)
        if (!alarmDestroyed)
        {
            InitializeAlarmHealth();
        }
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
        // Handle alarm timing - only during Duelist/Summoner form
        // During CageBull, the alarm should stay deactivated
        if (!alarmActivated && alarm != null && !alarmDestroyed)
        {
            // Check if brain exists and is in CageBull form - if so, don't auto-activate
            var brain = GetComponent<EnemyBehavior.Boss.BossRoombaBrain>();
            if (brain != null && brain.CurrentForm == EnemyBehavior.Boss.RoombaForm.CageBull)
            {
                // Reset timer during CageBull so it doesn't immediately activate when returning to Duelist
                alarmTimer = 0f;
                return;
            }
            
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
        // Use both stopping distance and combat offset for the ring
        float ring = Mathf.Max(agent.stoppingDistance, MinStoppingDistance) + CombatDistanceOffset;
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
        // Don't activate if destroyed
        if (alarmDestroyed) return;
        
        if (alarmActivated) return;
        
        alarmActivated = true;
        if (alarm != null) alarm.SetActive(true);
        
        Debug.Log("Alarm ACTIVATED - Starting spawn management");
        
        // Start spawn management routine
        if (spawnManagementRoutine != null) StopCoroutine(spawnManagementRoutine);
        spawnManagementRoutine = StartCoroutine(ManageSpawnsRoutine());
    }
    
    /// <summary>
    /// Activates the alarm after a random delay (for form changes or fight start).
    /// </summary>
    public void ActivateAlarmWithDelay()
    {
        if (alarmDestroyed) return;
        if (alarmActivated) return;
        
        if (alarmActivationDelayRoutine != null)
        {
            StopCoroutine(alarmActivationDelayRoutine);
        }
        alarmActivationDelayRoutine = StartCoroutine(AlarmActivationDelayRoutine());
    }
    
    private IEnumerator AlarmActivationDelayRoutine()
    {
        float delay = Random.Range(AlarmActivationDelayRange.x, AlarmActivationDelayRange.y);
        Debug.Log($"Alarm will activate in {delay:F1} seconds...");
        yield return WaitForSecondsCache.Get(delay);
        
        if (!alarmDestroyed && !alarmActivated)
        {
            ActivateAlarm();
        }
        alarmActivationDelayRoutine = null;
    }

    public void DeactivateAlarm()
    {
        alarmActivated = false;
        if (alarm != null && !alarmDestroyed) alarm.SetActive(false);
        
        Debug.Log("Alarm DEACTIVATED - Stopping spawn management");
        
        // Stop delayed activation if pending
        if (alarmActivationDelayRoutine != null)
        {
            StopCoroutine(alarmActivationDelayRoutine);
            alarmActivationDelayRoutine = null;
        }
        
        if (spawnManagementRoutine != null)
        {
            StopCoroutine(spawnManagementRoutine);
            spawnManagementRoutine = null;
        }
    }
    
    /// <summary>
    /// Returns true if the alarm is still functional (not destroyed).
    /// </summary>
    public bool IsAlarmAlive => !alarmDestroyed;
    
    /// <summary>
    /// Apply damage to the alarm. Called by the damage system when player attacks the alarm.
    /// </summary>
    public void DamageAlarm(float damage)
    {
        if (alarmDestroyed) return;
        
        float actualDamage = damage * AlarmDamageMultiplier;
        alarmCurrentHealth -= actualDamage;
        
        Debug.Log($"[BossRoombaController] Alarm took {actualDamage} damage ({alarmCurrentHealth}/{AlarmMaxHealth})");
        
        if (alarmCurrentHealth <= 0f)
        {
            DestroyAlarm();
        }
    }
    
    /// <summary>
    /// Destroys the alarm permanently. No more adds will spawn.
    /// Can be called from debug buttons or when alarm health reaches 0.
    /// </summary>
    public void DestroyAlarm()
    {
        if (alarmDestroyed) return;
        
        alarmDestroyed = true;
        alarmActivated = false;
        
        Debug.Log("[BossRoombaController] ALARM DESTROYED - No more adds will spawn!");
        
        // Stop spawn management
        if (spawnManagementRoutine != null)
        {
            StopCoroutine(spawnManagementRoutine);
            spawnManagementRoutine = null;
        }
        
        // Stop delayed activation if pending
        if (alarmActivationDelayRoutine != null)
        {
            StopCoroutine(alarmActivationDelayRoutine);
            alarmActivationDelayRoutine = null;
        }
        
        // Handle visual destruction
        if (alarm != null)
        {
            if (AlarmFallsOffWhenDestroyed)
            {
                // Detach and apply physics
                alarm.transform.SetParent(null);
                
                var rb = alarm.GetComponent<Rigidbody>();
                if (rb == null) rb = alarm.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // Add collider if needed
                if (alarm.GetComponent<Collider>() == null)
                {
                    var box = alarm.AddComponent<BoxCollider>();
                    box.size = Vector3.one * 0.5f;
                }
                
                // Apply break-off force
                Vector3 breakDirection = (alarm.transform.position - transform.position).normalized;
                breakDirection.y = 0.5f;
                rb.AddForce(breakDirection.normalized * AlarmBreakOffForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * AlarmBreakOffForce * 0.3f, ForceMode.Impulse);
                
                // Destroy after a delay
                Destroy(alarm, 5f);
            }
            else
            {
                alarm.SetActive(false);
            }
        }
        
        // Notify the brain
        var brain = GetComponent<EnemyBehavior.Boss.BossRoombaBrain>();
        if (brain != null)
        {
            brain.OnAlarmDestroyed();
        }
    }
    
    #region Debug Methods
    
    /// <summary>
    /// DEBUG: Kills one random active add (crawler or drone).
    /// Tests object pooling and respawn functionality.
    /// </summary>
    public void DebugKillOneRandomAdd()
    {
        var allAdds = new List<GameObject>();
        
        // Collect all active drones
        foreach (var kvp in activeDrones)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
                allAdds.Add(kvp.Value);
        }
        
        // Collect all active crawlers
        foreach (var kvp in activeCrawlers)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
                allAdds.Add(kvp.Value);
        }
        
        if (allAdds.Count == 0)
        {
            Debug.Log("[Boss DEBUG] No active adds to kill!");
            return;
        }
        
        // Pick a random one
        int index = UnityEngine.Random.Range(0, allAdds.Count);
        GameObject target = allAdds[index];
        
        Debug.Log($"[Boss DEBUG] Killing add: {target.name}");
        
        // Try to kill it through its health system
        var healthSystem = target.GetComponent<IHealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.LoseHP(9999f);
        }
        else
        {
            // Fallback: just deactivate it
            target.SetActive(false);
        }
    }
    
    /// <summary>
    /// DEBUG: Kills all active adds (crawlers and drones).
    /// </summary>
    public void DebugKillAllAdds()
    {
        int killCount = 0;
        
        // Kill all drones
        foreach (var kvp in activeDrones)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                var healthSystem = kvp.Value.GetComponent<IHealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.LoseHP(9999f);
                }
                else
                {
                    kvp.Value.SetActive(false);
                }
                killCount++;
            }
        }
        
        // Kill all crawlers
        foreach (var kvp in activeCrawlers)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                var healthSystem = kvp.Value.GetComponent<IHealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.LoseHP(9999f);
                }
                else
                {
                    kvp.Value.SetActive(false);
                }
                killCount++;
            }
        }
        
        Debug.Log($"[Boss DEBUG] Killed {killCount} adds!");
    }
    
    #endregion
    
    
    /// <summary>
    /// Orders all active adds to flee to their nearest spawn point.
    /// Called when the boss starts moving towards the vacuum position,
    /// giving adds time to clear the arena center before walls go up.
    /// </summary>
    public void OrderAddsToFleeToSpawnPoints()
    {
        Debug.Log("[BossRoombaController] Ordering all adds to flee to nearest spawn points");
        
        int dronesFleeing = 0;
        int crawlersFleeing = 0;
        
        // Order all active drones to flee to nearest drone spawn point
        foreach (var kvp in activeDrones)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                Transform nearestSpawn = FindNearestSpawnPoint(kvp.Value.transform.position, droneSpawnPoints);
                if (nearestSpawn != null)
                {
                    var droneAgent = kvp.Value.GetComponent<NavMeshAgent>();
                    if (droneAgent != null && droneAgent.enabled && droneAgent.isOnNavMesh)
                    {
                        droneAgent.isStopped = false;
                        droneAgent.SetDestination(nearestSpawn.position);
                        dronesFleeing++;
                        Debug.Log($"[BossRoombaController] Drone {kvp.Value.name} fleeing to spawn point at {nearestSpawn.position}");
                    }
                }
            }
        }
        
        // Order all active crawlers to flee to nearest crawler spawn point
        foreach (var kvp in activeCrawlers)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                Transform nearestSpawn = FindNearestSpawnPoint(kvp.Value.transform.position, crawlerSpawnPoints);
                if (nearestSpawn != null)
                {
                    var crawlerAgent = kvp.Value.GetComponent<NavMeshAgent>();
                    if (crawlerAgent != null && crawlerAgent.enabled && crawlerAgent.isOnNavMesh)
                    {
                        crawlerAgent.isStopped = false;
                        crawlerAgent.SetDestination(nearestSpawn.position);
                        crawlersFleeing++;
                        Debug.Log($"[BossRoombaController] Crawler {kvp.Value.name} fleeing to spawn point at {nearestSpawn.position}");
                    }
                }
            }
        }
        
        Debug.Log($"[BossRoombaController] Ordered {dronesFleeing} drones and {crawlersFleeing} crawlers to flee to spawn points");
    }
    
    /// <summary>
    /// Finds the nearest spawn point from the given position.
    /// </summary>
    private Transform FindNearestSpawnPoint(Vector3 position, Transform[] spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        
        Transform nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (var sp in spawnPoints)
        {
            if (sp == null) continue;
            
            float dist = Vector3.Distance(position, sp.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = sp;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Called when cage match starts (walls go up). Despawns ALL active adds.
    /// During the cage match, no adds should be present - it's a 1v1 with the boss.
    /// </summary>
    public void OnCageMatchStart()
    {
        Debug.Log("[BossRoombaController] OnCageMatchStart - despawning ALL active adds for 1v1 cage match");
        
        int despawnedDrones = 0;
        int despawnedCrawlers = 0;
        
        // Despawn ALL active drones
        foreach (var kvp in activeDrones)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                // Return to pool
                kvp.Value.SetActive(false);
                dronePool.Enqueue(kvp.Value);
                despawnedDrones++;
                Debug.Log($"[BossRoombaController] Despawned drone for cage match: {kvp.Value.name}");
            }
        }
        
        // Despawn ALL active crawlers
        foreach (var kvp in activeCrawlers)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                // Return to pool
                kvp.Value.SetActive(false);
                crawlerPool.Enqueue(kvp.Value);
                despawnedCrawlers++;
                Debug.Log($"[BossRoombaController] Despawned crawler for cage match: {kvp.Value.name}");
            }
        }
        
        Debug.Log($"[BossRoombaController] Cage match started - despawned {despawnedDrones} drones and {despawnedCrawlers} crawlers");
    }
    
    /// <summary>
    /// Initialize alarm health on Awake/Start.
    /// </summary>
    private void InitializeAlarmHealth()
    {
        alarmCurrentHealth = AlarmMaxHealth;
        alarmDestroyed = false;
    }

    private IEnumerator ManageSpawnsRoutine()
    {
        // Initial spawn wave - spawn all enemies immediately when alarm activates
        Debug.Log("[BossRoombaController] Initial spawn wave starting...");
        RespawnDeadEnemies(activeDrones, droneSpawnPoints, dronePrefab, maxDrones, dronePool);
        RespawnDeadEnemies(activeCrawlers, crawlerSpawnPoints, crawlerPrefab, maxCrawlers, crawlerPool);
        
        // Respawn check cadence
        var wait = WaitForSecondsCache.Get(2f);
        
        while (alarmActivated && !alarmDestroyed)
        {
            yield return wait;
            
            // Check and respawn any dead enemies
            RespawnDeadEnemies(activeDrones, droneSpawnPoints, dronePrefab, maxDrones, dronePool);
            RespawnDeadEnemies(activeCrawlers, crawlerSpawnPoints, crawlerPrefab, maxCrawlers, crawlerPool);
            
            // Ensure all active enemies continue chasing player (backup for state machine issues)
            if (player != null)
            {
                // Update crawler destinations
                foreach (var kvp in activeCrawlers)
                {
                    if (kvp.Value != null && kvp.Value.activeInHierarchy)
                    {
                        var crawler = kvp.Value.GetComponent<BaseCrawlerEnemy>();
                        if (crawler != null && crawler.agent != null && crawler.agent.enabled && crawler.agent.isOnNavMesh)
                        {
                            crawler.agent.isStopped = false;
                            crawler.agent.SetDestination(player.position);
                        }
                    }
                }
                
                // Update drone destinations - drones need continuous destination updates too
                foreach (var kvp in activeDrones)
                {
                    if (kvp.Value != null && kvp.Value.activeInHierarchy)
                    {
                        var drone = kvp.Value.GetComponent<DroneEnemy>();
                        if (drone != null && drone.agent != null && drone.agent.enabled && drone.agent.isOnNavMesh)
                        {
                            float distToPlayer = Vector3.Distance(drone.transform.position, player.position);
                            
                            // If drone is in Chase state and in attack range, transition to Fire
                            if (drone.enemyAI != null && drone.enemyAI.State == DroneState.Chase)
                            {
                                if (distToPlayer <= drone.attackRange)
                                {
                                    drone.TryFireTriggerByName("InAttackRange");
                                }
                                else
                                {
                                    drone.agent.isStopped = false;
                                    drone.agent.SetDestination(player.position);
                                }
                            }
                            // If drone is in Fire state but too far, go back to Chase
                            else if (drone.enemyAI != null && drone.enemyAI.State == DroneState.Fire)
                            {
                                if (distToPlayer > drone.chaseRange)
                                {
                                    drone.TryFireTriggerByName("OutOfAttackRange");
                                }
                                // Fire state handles its own shooting logic
                            }
                            // If drone is stuck in Idle, try to transition it to Chase
                            else if (drone.enemyAI != null && drone.enemyAI.State == DroneState.Idle)
                            {
                                drone.TryFireTriggerByName("SeePlayer");
                                drone.agent.isStopped = false;
                                drone.agent.SetDestination(player.position);
                            }
                        }
                    }
                }
            }
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
            Debug.Log($"[BossRoombaController] Instantiated new {prefab.name}");
        }
        else
        {
            Debug.Log($"[BossRoombaController] Reused pooled {enemy.name}");
        }

        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;
        enemy.SetActive(true);
        
        Debug.Log($"[BossRoombaController] Spawned {enemy.name} at {position}");

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
        
        // Configure crawlers spawned by the boss alarm to skip Ambush state and go directly to Chase
        // Check both on root and children (some prefabs have the component on a child)
        var crawler = g.GetComponent<BaseCrawlerEnemy>();
        if (crawler == null)
            crawler = g.GetComponentInChildren<BaseCrawlerEnemy>();
            
        if (crawler != null)
        {
            Debug.Log($"[BossRoombaController] Found BaseCrawlerEnemy on {g.name}, configuring for boss fight...");
            
            // Clear pocket reference - boss-spawned crawlers don't have a pocket to return to
            crawler.Pocket = null;
            
            // Force them to chase the player directly (no swarming/ambush behavior for boss adds)
            crawler.ForceChasePlayer = true;
            crawler.enableSwarmBehavior = false;
            
            // Reset health if the crawler has an IHealthSystem
            var healthSystem = crawler as IHealthSystem;
            if (healthSystem != null)
            {
                crawler.SetHealth(crawler.maxHealth);
            }
            
            // Reinitialize state machine for re-pooled enemies
            ResetCrawlerState(crawler);
            
            // Ensure the NavMeshAgent is properly configured - check crawler's own agent first
            var crawlerAgent = crawler.GetComponent<NavMeshAgent>();
            if (crawlerAgent == null)
                crawlerAgent = crawler.GetComponentInParent<NavMeshAgent>();
            if (crawlerAgent == null)
                crawlerAgent = agentComp;
                
            if (crawlerAgent != null)
            {
                crawlerAgent.enabled = true;
                if (crawlerAgent.isOnNavMesh && player != null)
                {
                    crawlerAgent.SetDestination(player.position);
                    Debug.Log($"[BossRoombaController] Set crawler destination to player");
                }
                else
                {
                    Debug.LogWarning($"[BossRoombaController] Crawler agent not on NavMesh: isOnNavMesh={crawlerAgent.isOnNavMesh}");
                }
            }
            
            
            // Force the crawler to skip Ambush and go to Chase after a frame
            // (this allows the state machine to be fully initialized)
            StartCoroutine(ForceCrawlerToChase(crawler));
        }
        else
        {
            Debug.Log($"[BossRoombaController] No BaseCrawlerEnemy found on {g.name} - checking for DroneEnemy...");
            
            // Try to configure drones to chase the player
            var drone = g.GetComponent<DroneEnemy>();
            if (drone == null)
                drone = g.GetComponentInChildren<DroneEnemy>();
                
            if (drone != null)
            {
                Debug.Log($"[BossRoombaController] Found DroneEnemy on {g.name}, configuring for boss fight...");
                
                // Set the player reference so the drone can track them
                if (player != null)
                {
                    drone.PlayerTarget = player;
                }
                
                // Trigger the drone to see the player and start chasing
                StartCoroutine(ForceDroneToChase(drone));
            }
            else
            {
                Debug.Log($"[BossRoombaController] No DroneEnemy found on {g.name} either");
            }
        }
    }
    
    /// <summary>
    /// Resets a crawler's state for re-use from the pool.
    /// </summary>
    private void ResetCrawlerState(BaseCrawlerEnemy crawler)
    {
        if (crawler == null) return;
        
        // Reset health
        crawler.SetHealth(crawler.maxHealth);
        
        // Reset any death/low health flags
        crawler.hasFiredLowHealth = false;
        
        // CRITICAL: Force the state machine back to Ambush so we can transition to Chase
        // The enemyAI might be in Death or some other state from previous use
        if (crawler.enemyAI != null)
        {
            // Use reflection or a public method to reset state if needed
            // For now, we'll let ForceCrawlerToChase handle the transition
            Debug.Log($"[BossRoombaController] ResetCrawlerState: {crawler.gameObject.name} current state = {crawler.enemyAI.State}");
        }
    }
    
    private IEnumerator ForceCrawlerToChase(BaseCrawlerEnemy crawler)
    {
        // Wait a frame for full initialization
        yield return null;
        
        if (crawler == null || crawler.gameObject == null)
        {
            Debug.LogWarning("[BossRoombaController] ForceCrawlerToChase: crawler is null after frame wait");
            yield break;
        }
        
        // Debug current state
        Debug.Log($"[BossRoombaController] ForceCrawlerToChase: {crawler.gameObject.name} state={crawler.enemyAI?.State}, agent.enabled={crawler.agent?.enabled}, isOnNavMesh={crawler.agent?.isOnNavMesh}");
        
        // Ensure NavMeshAgent is on NavMesh
        if (crawler.agent != null && !crawler.agent.isOnNavMesh)
        {
            Debug.LogWarning($"[BossRoombaController] {crawler.gameObject.name} not on NavMesh! Attempting to warp...");
            
            // Try to sample a valid position nearby
            if (UnityEngine.AI.NavMesh.SamplePosition(crawler.transform.position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                crawler.agent.Warp(hit.position);
                Debug.Log($"[BossRoombaController] Warped {crawler.gameObject.name} to {hit.position}");
            }
            else
            {
                Debug.LogError($"[BossRoombaController] Could not find valid NavMesh position for {crawler.gameObject.name}!");
                yield break;
            }
        }
        
        // Ensure agent is enabled and not stopped
        if (crawler.agent != null)
        {
            crawler.agent.enabled = true;
            crawler.agent.isStopped = false;
        }
        
        // CRITICAL: Set PlayerTarget BEFORE state transition so ChaseBehavior.OnEnter can capture it
        if (player != null)
        {
            crawler.PlayerTarget = player;
            Debug.Log($"[BossRoombaController] Set {crawler.gameObject.name} PlayerTarget to {player.name} BEFORE state transition");
        }
        else
        {
            Debug.LogError($"[BossRoombaController] player reference is NULL! Crawlers won't be able to chase.");
            yield break;
        }
        
        // Handle different starting states - we need to get the crawler into Chase
        if (crawler.enemyAI != null)
        {
            var currentState = crawler.enemyAI.State;
            Debug.Log($"[BossRoombaController] Crawler {crawler.gameObject.name} is in state: {currentState}");
            
            // If in Ambush, fire LosePlayer to go to Chase
            if (currentState == CrawlerEnemyState.Ambush)
            {
                bool fired = crawler.TryFireTriggerByName("LosePlayer");
                Debug.Log($"[BossRoombaController] Fired LosePlayer trigger: {fired}");
                
                // IMPORTANT: Also directly set the destination immediately, don't wait for the behavior
                // This ensures the crawler starts moving even if the state transition is delayed
                if (crawler.agent != null && crawler.agent.enabled && crawler.agent.isOnNavMesh && player != null)
                {
                    crawler.agent.isStopped = false;
                    crawler.agent.SetDestination(player.position);
                    Debug.Log($"[BossRoombaController] Set immediate destination for {crawler.gameObject.name}");
                }
            }
            // If in Death state, we need to reinitialize
            else if (currentState == CrawlerEnemyState.Death)
            {
                Debug.LogWarning($"[BossRoombaController] Crawler {crawler.gameObject.name} is in Death state! Cannot force to Chase - needs full reset.");
                // The crawler should have been properly reset before spawning
                yield break;
            }
            // If already in Chase, just ensure destination is set
            else if (currentState == CrawlerEnemyState.Chase)
            {
                Debug.Log($"[BossRoombaController] Crawler already in Chase state");
            }
            // For other states (Swarm, Attack, Flee), try to transition to Chase via LosePlayer
            else
            {
                bool fired = crawler.TryFireTriggerByName("LosePlayer");
                Debug.Log($"[BossRoombaController] Attempted LosePlayer trigger from {currentState}: {fired}");
            }
            
            // Wait another frame for state transition
            yield return null;
            
            // Verify and set destination
            if (crawler.enemyAI.State == CrawlerEnemyState.Chase || 
                crawler.enemyAI.State == CrawlerEnemyState.Attack ||
                crawler.enemyAI.State == CrawlerEnemyState.Swarm)
            {
                Debug.Log($"[BossRoombaController] SUCCESS: {crawler.gameObject.name} is now in state {crawler.enemyAI.State}");
                
                // Ensure destination is set (PlayerTarget was already set before transition)
                if (crawler.agent != null && crawler.agent.isOnNavMesh && player != null)
                {
                    crawler.agent.SetDestination(player.position);
                    Debug.Log($"[BossRoombaController] Set {crawler.gameObject.name} destination to player at {player.position}");
                }
            }
            else
            {
                Debug.LogWarning($"[BossRoombaController] {crawler.gameObject.name} still in state {crawler.enemyAI.State} - manually starting chase behavior");
                
                // Last resort: directly start the chase behavior
                if (crawler.agent != null && crawler.agent.isOnNavMesh && player != null)
                {
                    crawler.agent.isStopped = false;
                    crawler.agent.SetDestination(player.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Forces a drone to start chasing the player immediately after spawn.
    /// </summary>
    private IEnumerator ForceDroneToChase(DroneEnemy drone)
    {
        // Wait a frame for full initialization
        yield return null;
        
        if (drone == null || drone.gameObject == null)
        {
            Debug.LogWarning("[BossRoombaController] ForceDroneToChase: drone is null after frame wait");
            yield break;
        }
        
        // Debug current state
        Debug.Log($"[BossRoombaController] ForceDroneToChase: {drone.gameObject.name} state={drone.enemyAI?.State}, agent.enabled={drone.agent?.enabled}, isOnNavMesh={drone.agent?.isOnNavMesh}");
        
        // Ensure NavMeshAgent is on NavMesh
        if (drone.agent != null && !drone.agent.isOnNavMesh)
        {
            Debug.LogWarning($"[BossRoombaController] {drone.gameObject.name} not on NavMesh! Attempting to warp...");
            
            // Try to sample a valid position nearby
            if (UnityEngine.AI.NavMesh.SamplePosition(drone.transform.position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                drone.agent.Warp(hit.position);
                Debug.Log($"[BossRoombaController] Warped {drone.gameObject.name} to {hit.position}");
            }
            else
            {
                Debug.LogError($"[BossRoombaController] Could not find valid NavMesh position for {drone.gameObject.name}!");
                yield break;
            }
        }
        
        // Ensure agent is enabled and not stopped
        if (drone.agent != null)
        {
            drone.agent.enabled = true;
            drone.agent.isStopped = false;
        }
        
        // Set the player target
        if (player != null)
        {
            drone.PlayerTarget = player;
        }
        
        // Fire the SeePlayer trigger to transition from Idle to Chase
        if (drone.enemyAI != null && drone.enemyAI.State == DroneState.Idle)
        {
            bool fired = drone.TryFireTriggerByName("SeePlayer");
            Debug.Log($"[BossRoombaController] Fired SeePlayer trigger on {drone.gameObject.name}: {fired}");
        }
        
        // Wait another frame for state transition
        yield return null;
        
        // Verify state
        if (drone.enemyAI != null)
        {
            Debug.Log($"[BossRoombaController] Drone {drone.gameObject.name} is now in state: {drone.enemyAI.State}");
            
            // Set destination to player
            if (drone.agent != null && drone.agent.isOnNavMesh && player != null)
            {
                drone.agent.SetDestination(player.position);
                Debug.Log($"[BossRoombaController] Set {drone.gameObject.name} destination to player at {player.position}");
            }
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
