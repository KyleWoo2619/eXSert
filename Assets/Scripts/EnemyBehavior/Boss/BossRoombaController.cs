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
    private bool alarmActive = false;

    // simple local pools (fallback if ScenePoolManager not present)
    private readonly Queue<GameObject> dronePool = new Queue<GameObject>();
    private readonly Queue<GameObject> crawlerPool = new Queue<GameObject>();
    public int initialPoolSize =8;

    private Coroutine followRoutine;
    private Coroutine animParamsRoutine;

    void Awake()
    {
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
        // Drive locomotion parameters at a small cadence instead of every frame
        if (animParamsRoutine != null) StopCoroutine(animParamsRoutine);
        animParamsRoutine = StartCoroutine(AnimParamsLoop(0.05f));
    }

    void OnDisable()
    {
        if (animParamsRoutine != null) { StopCoroutine(animParamsRoutine); animParamsRoutine = null; }
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
        agent.stoppingDistance = profile.StoppingDistance;
        agent.avoidancePriority = profile.AvoidancePriority;
        agent.autoBraking = false;
    }

    // Event-driven follow (avoids Update)
    public void StartFollowingPlayer(float cadenceSeconds)
    {
        if (followRoutine != null) StopCoroutine(followRoutine);
        followRoutine = StartCoroutine(FollowLoop(cadenceSeconds));
    }

    private IEnumerator FollowLoop(float cadence)
    {
        var wait = new WaitForSeconds(Mathf.Max(0.02f, cadence));
        while (true)
        {
            if (!alarmActive && player != null)
                agent.SetDestination(player.position);
            yield return wait;
        }
    }

    // Example: trigger alarm and spawn adds
    public void ActivateAlarm()
    {
        if (alarm != null) alarm.SetActive(true);
        alarmActive = true;
        StartCoroutine(SpawnAddsCoroutine());
    }

    public void DeactivateAlarm()
    {
        alarmActive = false;
        if (alarm != null) alarm.SetActive(false);
    }

    private IEnumerator SpawnAddsCoroutine()
    {
        // Choose arrays: fall back to pocketSpawnPoints if specific ones not set
        var drones = (droneSpawnPoints != null && droneSpawnPoints.Length > 0) ? droneSpawnPoints : pocketSpawnPoints;
        var crawlers = (crawlerSpawnPoints != null && crawlerSpawnPoints.Length > 0) ? crawlerSpawnPoints : pocketSpawnPoints;

        // Drones
        if (drones != null)
        {
            foreach (var p in drones)
            {
                if (p == null) continue;
                for (int i = 0; i < dronesPerSpawn; i++)
                {
                    var g = Spawn(dronePrefab, p.position);
                    if (g != null) { g.SetActive(true); RegisterSpawned(g); }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Crawlers
        if (crawlers != null)
        {
            foreach (var p in crawlers)
            {
                if (p == null) continue;
                for (int j = 0; j < crawlersPerSpawn; j++)
                {
                    var c = Spawn(crawlerPrefab, p.position);
                    if (c != null) { c.SetActive(true); RegisterSpawned(c); }
                }
                yield return new WaitForSeconds(0.1f);
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
