// AlarmCarrierEnemy.cs
// Purpose: Enemy type that carries an alarm and can activate group behavior or spawn reinforcements.
// Works with: BossRoombaController, CrowdController, EnemyStateMachineConfig, EnemyBehaviorProfile, Zone
// Notes: Scene-scoped; designed to integrate with the crowd/pathfinding systems. Main logic below.

using UnityEngine;
using System.Collections;
using System.Linq;
using Behaviors;
using UnityEngine.AI;
using System.Net.NetworkInformation;

public enum AlarmCarrierState
{
    Idle,
    Roaming,
    AlarmTriggered,
    Summoning,
    Flee,
    Death
}

public enum AlarmCarrierTrigger
{
    SeePlayer,
    PlayerInRange,
    AlarmStart,
    AlarmEnd,
    Summon,
    Flee,
    Die,
    LosePlayer,
    IdleTimerElapsed
}

public class AlarmCarrierEnemy : BaseEnemy<AlarmCarrierState, AlarmCarrierTrigger>
{
    [Header("Alarm Settings")]
    [SerializeField, Tooltip("How long the alarm must go off before summoning reinforcements.")]
    private float alarmDuration = 3f;
    [SerializeField, Tooltip("Detection range for triggering the alarm.")]
    private float alarmRange = 10f;
    [SerializeField, Tooltip("How far from the pocket the alarm bot can roam while fleeing the player.")]
    private float keepNearPocketRadius = 8f;
    [SerializeField, Tooltip("How often the alarm bot updates its flee destination (seconds).")]
    private float fleeCheckInterval = 0.1f;
    [SerializeField, Tooltip("Only update destination if the new target is this far from the current destination.")]
    private float minMoveDistance = 2f;
    [SerializeField, Tooltip("Only update destination if the player is within this radius of the pocket.")]
    private float playerChaseRadius = 20f;

    [Header("Crawler Prefabs")]
    [SerializeField, Tooltip("Prefab for the base crawler enemy.")]
    private BaseCrawlerEnemy baseCrawlerPrefab;
    [SerializeField, Tooltip("Prefab for the bomb carrier enemy.")]
    private BombCarrierEnemy bombCrawlerPrefab;

    [Header("Alarm Debug")]
    [ReadOnly, SerializeField]
    private float currentDynamicSpawnInterval;

    private Coroutine spawnCoroutine;
    private Coroutine alarmCountdownCoroutine;
    private Coroutine alarmFleeCoroutine;
    private CrawlerPocket nearestPocket;

    // Track the number of active crawlers spawned by the alarm
    private int activeAlarmSpawnedCrawlers = 0;

    // Behaviors
    private IEnemyStateBehavior<AlarmCarrierState, AlarmCarrierTrigger> idleBehavior, relocateBehavior, deathBehavior;

    protected override void Awake()
    {
        base.Awake();

        // Safety: enforce minimums if Inspector values are zero or negative
        if (keepNearPocketRadius <= 0f) keepNearPocketRadius = 8f;
        if (fleeCheckInterval <= 0f) fleeCheckInterval = 0.1f;
        if (minMoveDistance <= 0f) minMoveDistance = 2f;
        if (playerChaseRadius <= 0f) playerChaseRadius = 20f;

        idleBehavior = new IdleBehavior<AlarmCarrierState, AlarmCarrierTrigger>();
        relocateBehavior = new RelocateBehavior<AlarmCarrierState, AlarmCarrierTrigger>();
        deathBehavior = new DeathBehavior<AlarmCarrierState, AlarmCarrierTrigger>();

        var alarmTrigger = gameObject.AddComponent<SphereCollider>();
        alarmTrigger.isTrigger = true;
        alarmTrigger.radius = alarmRange;

        // Assign currentZone if not set
        if (currentZone == null)
        {
            currentZone = FindNearestZone();
            Debug.Log($"{gameObject.name} assigned to zone: {currentZone?.gameObject.name}");
        }
    }

    protected virtual void Start()
    {
        InitializeStateMachine(AlarmCarrierState.Idle);
        ConfigureStateMachine();
        Debug.Log($"{gameObject.name} State machine initialized");
        if (enemyAI.State.Equals(AlarmCarrierState.Idle))
        {
            Debug.Log($"{gameObject.name} Manually calling OnEnterIdle for initial Idle state");
            idleBehavior.OnEnter(this);
        }

        if (healthBarPrefab != null)
        {
            healthBarInstance = EnemyHealthBar.SetupHealthBar(healthBarPrefab, this);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: healthBarPrefab is not assigned in the Inspector.");
        }
    }

    protected override void ConfigureStateMachine()
    {
        enemyAI.Configure(AlarmCarrierState.Idle)
            .OnEntry(() => idleBehavior?.OnEnter(this))
            .OnExit(() => idleBehavior?.OnExit(this))
            .Permit(AlarmCarrierTrigger.PlayerInRange, AlarmCarrierState.AlarmTriggered)
            .Permit(AlarmCarrierTrigger.IdleTimerElapsed, AlarmCarrierState.Roaming)
            .Permit(AlarmCarrierTrigger.Die, AlarmCarrierState.Death);

        enemyAI.Configure(AlarmCarrierState.Roaming)
            .OnEntry(() => relocateBehavior?.OnEnter(this))
            .OnExit(() => relocateBehavior?.OnExit(this))
            .Permit(AlarmCarrierTrigger.PlayerInRange, AlarmCarrierState.AlarmTriggered)
            .Permit(AlarmCarrierTrigger.Die, AlarmCarrierState.Death);

        enemyAI.Configure(AlarmCarrierState.AlarmTriggered)
            .OnEntry(() => {
                StartAlarm();
                if (alarmFleeCoroutine != null)
                    StopCoroutine(alarmFleeCoroutine);
                alarmFleeCoroutine = StartCoroutine(AlarmFleeBehavior());
                Debug.Log($"{gameObject.name} Entered AlarmTriggered state");
            })
            .Permit(AlarmCarrierTrigger.AlarmEnd, AlarmCarrierState.Summoning)
            .Permit(AlarmCarrierTrigger.Die, AlarmCarrierState.Death);

        enemyAI.Configure(AlarmCarrierState.Summoning)
            .OnEntry(() => {
                // Reset agent to ensure movement
                if (agent != null) {
                    if (!agent.enabled) agent.enabled = true;
                    if (!agent.isOnNavMesh) {
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
                            agent.Warp(hit.position);
                    }
                    agent.ResetPath();
                    agent.isStopped = false;
                }
                Debug.Log($"{gameObject.name} agent.enabled={agent.enabled}, isOnNavMesh={agent.isOnNavMesh}, isStopped={agent.isStopped}, speed={agent.speed}, acceleration={agent.acceleration}");
                StartSummoning();
            })
            .Permit(AlarmCarrierTrigger.Die, AlarmCarrierState.Death);

        enemyAI.Configure(AlarmCarrierState.Death)
            .OnEntry(() => {
                deathBehavior?.OnEnter(this);
                // Stop flee coroutine on death
                if (alarmCountdownCoroutine != null)
                {
                    StopCoroutine(alarmCountdownCoroutine);
                    alarmCountdownCoroutine = null;
                }
                if (alarmFleeCoroutine != null)
                {
                    StopCoroutine(alarmFleeCoroutine);
                    alarmFleeCoroutine = null;
                }
            });
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if ((enemyAI.State.Equals(AlarmCarrierState.Idle) || enemyAI.State.Equals(AlarmCarrierState.Roaming)) && other.CompareTag("Player"))
        {
            PlayerTarget = other.transform;
            enemyAI.Fire(AlarmCarrierTrigger.PlayerInRange);
        }
    }

    protected override void OnTriggerStay(Collider other)
    {
        // Do nothing or implement custom logic if needed.
    }

    private void StartAlarm()
    {
        if (alarmCountdownCoroutine == null)
            alarmCountdownCoroutine = StartCoroutine(AlarmCountdown());

        // Visual/audio feedback for alarm
        // Example: AudioManager.Play("AlarmSiren");
        // Example: animator.SetTrigger("Alarm");
    }

    private IEnumerator AlarmCountdown()
    {
        Debug.Log($"{gameObject.name} AlarmCountdown started for {alarmDuration} seconds");
        yield return new WaitForSeconds(alarmDuration);
        Debug.Log($"{gameObject.name} AlarmCountdown finished, firing AlarmEnd");
        enemyAI.Fire(AlarmCarrierTrigger.AlarmEnd);
        alarmCountdownCoroutine = null;
    }

    private void StartSummoning()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnCrawlersRoutine());
    }

    private IEnumerator SpawnCrawlersRoutine()
    {
        // Find nearest pocket each time in case the environment changes
        while (true)
        {
            nearestPocket = FindNearestPocket();
            if (nearestPocket != null)
            {
                if (Random.value < 0.7f)
                    SpawnBaseCrawlerAtPocket(nearestPocket);
                else
                    SpawnBombCrawlerAtPocket(nearestPocket);
            }

            float minInterval = 0.5f;
            float maxInterval = 10f;
            float dynamicInterval;

            if (activeAlarmSpawnedCrawlers < 10)
            {
                // Linear increase from 1.5s to 2.5s as count goes from 0 to 10
                dynamicInterval = Mathf.Lerp(1.5f, 2.5f, activeAlarmSpawnedCrawlers / 10f);
            }
            else
            {
                // Logarithmic scaling for 11+
                dynamicInterval = Mathf.Clamp(
                    2.5f * (1f + Mathf.Log10(1f + (activeAlarmSpawnedCrawlers - 9))),
                    minInterval, maxInterval);
            }

            currentDynamicSpawnInterval = dynamicInterval; // Expose in Inspector

            yield return new WaitForSeconds(dynamicInterval);
        }
    }

    private CrawlerPocket FindNearestPocket()
    {
        var pockets = FindObjectsByType<CrawlerPocket>(FindObjectsSortMode.None);
        if (pockets.Length == 0) return null;
        return pockets.OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).FirstOrDefault();
    }

    private void SpawnBaseCrawlerAtPocket(CrawlerPocket pocket)
    {
        float clusterRadius = pocket.ClusterRadius;
        Vector3 spawnPos = pocket.transform.position + Random.insideUnitSphere * 0.5f * clusterRadius;
        spawnPos.y = pocket.transform.position.y;

        var crawler = Instantiate(baseCrawlerPrefab, spawnPos, Quaternion.identity);
        if (crawler != null)
        {
            crawler.Pocket = pocket;
            crawler.AlarmSource = this;
            SwarmManager.Instance.AddToSwarm(crawler);

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                crawler.PlayerTarget = playerObj.transform;

            // Track alarm-spawned crawlers
            activeAlarmSpawnedCrawlers++;
            crawler.OnDestroyCallback = () => { activeAlarmSpawnedCrawlers--; };
        }
    }

    private void SpawnBombCrawlerAtPocket(CrawlerPocket pocket)
    {
        Vector3 spawnPos = pocket.transform.position + Random.insideUnitSphere * 0.5f * pocket.ClusterRadius;
        spawnPos.y = pocket.transform.position.y;

        var bomb = Instantiate(bombCrawlerPrefab, spawnPos, Quaternion.identity);
        if (bomb != null)
        {
            bomb.Pocket = pocket;
            bomb.SetSpawnSource(true, this, pocket); // spawnedByAlarm = true, alarm = this, pocket = pocket
        }
    }

    // Alarm flee logic
    private IEnumerator AlarmFleeBehavior()
    {
        while (enemyAI.State == AlarmCarrierState.AlarmTriggered || enemyAI.State == AlarmCarrierState.Summoning)
        {
            nearestPocket = FindNearestPocket();

            if (PlayerTarget != null && nearestPocket != null)
            {
                Vector3 pocketPos = nearestPocket.transform.position;
                Vector3 toPlayer = (PlayerTarget.position - pocketPos).normalized;
                Vector3 keepAwayDir = -toPlayer;
                Vector3 targetPos = pocketPos + keepAwayDir * keepNearPocketRadius;
                targetPos += Random.insideUnitSphere * 1.5f;
                targetPos.y = pocketPos.y;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
                {
                    if (agent != null && agent.enabled)
                    {
                        // Only update if the new target is far from the current destination
                        if (Vector3.Distance(agent.destination, hit.position) > minMoveDistance)
                        {
                            agent.isStopped = false;
                            agent.SetDestination(hit.position);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(fleeCheckInterval);
        }
    }

    private void OnDestroy()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        if (alarmCountdownCoroutine != null)
        {
            StopCoroutine(alarmCountdownCoroutine);
            alarmCountdownCoroutine = null;
        }
        if (alarmFleeCoroutine != null)
        {
            StopCoroutine(alarmFleeCoroutine);
            alarmFleeCoroutine = null;
        }
    }

    private Zone FindNearestZone()
    {
        var zones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
        if (zones.Length == 0) return null;
        return zones.OrderBy(z => Vector3.Distance(transform.position, z.transform.position)).FirstOrDefault();
    }
}