// DroneEnemy.cs
// Purpose: Drone enemy implementation with flying movement and swarm behavior.
// Works with: DroneSwarmManager, FlowFieldService, CrowdController.

using UnityEngine;
using UnityEngine.AI;
using Behaviors;
using System.Collections;
using System.Collections.Generic;

public enum DroneState
{
    Idle,
    Relocate,
    Chase,
    Fire,
    Death
}

public enum DroneTrigger
{
    SeePlayer,
    LosePlayer,
    InAttackRange,
    OutOfAttackRange,
    Die,
    RelocateComplete
}

[RequireComponent(typeof(NavMeshAgent))]
public class DroneEnemy : BaseEnemy<DroneState, DroneTrigger>, IProjectileShooter
{
    [Header("Drone Settings")]
    [Tooltip("Desired vertical hover height above ground used for visuals/pathing.")]
    [SerializeField] private float hoverHeight = 5f;

    [Tooltip("Preferred combat radius. Drones enter Fire near this distance (with hysteresis).")]
    [SerializeField] public float attackRange = 15f;

    [Tooltip("Seconds between shots for this drone.")]
    [SerializeField] private float fireCooldown = 1.5f;

    [Tooltip("Projectile prefab spawned when firing. Should have a Rigidbody and EnemyProjectile component.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Initial linear velocity magnitude applied to the projectile.")]
    [SerializeField] private float projectileSpeed = 20f;

    [Tooltip("Muzzle transform used as the spawn position and forward direction when firing.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Max distance to pursue the player before giving up and relocating.")]
    [SerializeField] public float chaseRange = 30f;

    [Tooltip("Number of projectiles pre-instantiated for this drone's object pool.")]
    [SerializeField] private int projectilePoolSize = 20;

    [Header("NavMesh edge handling")]
    [Tooltip("Radius used with NavMesh.SamplePosition when clamping desired destinations to the mesh.")]
    [SerializeField] private float navSampleRadius = 2.0f;

    [Tooltip("Max distance to shrink the formation radius toward the player when clamping off-mesh points.")]
    [SerializeField] private float navEdgeFallbackMaxShrink = 5.0f;

    [Header("Chase/Fire hysteresis and anti-stall")]
    [Tooltip("Extra distance added to attackRange for entering Fire. Helps near NavMesh edges.")]
    [SerializeField] private float fireEnterBuffer = 1.25f;

    [Tooltip("Extra distance added to attackRange before exiting Fire back to Chase. Prevents flip-flop.")]
    [SerializeField] private float fireExitBuffer = 2.0f;

    [Tooltip("Seconds with little NavMeshAgent progress before forcing Fire near target (anti-stall).")]
    [SerializeField] private float chaseStuckSeconds = 1.5f;

    [Header("Fire movement (discrete re-positioning)")]
    [Tooltip("Angle (degrees) to rotate the formation each re-position step while in Fire.")]
    [SerializeField] private float fireStepAngleDeg = 30f;

    [Tooltip("Minimum seconds between re-position assignments in Fire. Majority must arrive before this earliest time.")]
    [SerializeField] private float fireRepositionIntervalMin = 1.0f;

    [Tooltip("Maximum seconds between re-position assignments in Fire. A hard cap even if not all arrived.")]
    [SerializeField] private float fireRepositionIntervalMax = 2.0f;

    [Tooltip("Extra random seconds added/subtracted to the interval for variety.")]
    [SerializeField] private float fireRepositionJitter = 0.25f;

    [Tooltip("Distance within which a drone is considered to have reached its Fire target.")]
    [SerializeField] private float fireArrivalEpsilon = 0.6f;

    [Tooltip("Chance (0..1) that a Fire step will flip across the circle (180°), producing \"cross-over\" swaps.")]
    [SerializeField, Range(0f, 1f)] private float fireCrossSwapChance = 0.15f;

    [Header("Projectile/Firing")]
    [Tooltip("Spawn the projectile slightly forward from the muzzle to avoid self-collision at spawn.")]
    [SerializeField] private float muzzleForwardOffset = 0.1f;

    [Header("Aiming")]
    [Tooltip("Vertical aim offset to target the player's center/head. 0 = feet, ~0.8 = chest, ~1.5 = head.")]
    [SerializeField] private float aimYOffset = 0.8f;

    [Header("Aim Randomization")]
    [Tooltip("Chance [0..1] that a shot will intentionally miss left/right. 0 = always accurate, 1 = always miss.")]
    [SerializeField, Range(0f, 1f)] private float missChance = 0.15f;

    [Tooltip("Minimum degrees to offset when missing (use 0 for any amount up to Max).")]
    [SerializeField, Range(0f, 45f)] private float minMissAngleDeg = 0f;

    [Tooltip("Maximum degrees to offset when missing (yaw only).")]
    [SerializeField, Range(0f, 45f)] private float maxMissAngleDeg = 6f;

    [Header("Facing")]
    [Tooltip("Degrees/second to rotate toward the desired facing direction.")]
    [SerializeField] private float turnSpeed = 540f;
    [Tooltip("Minimum planar speed before we use agent velocity for facing.")]
    [SerializeField] private float velocityFacingThreshold = 0.2f;

    [Header("Hit Reaction")]
    [Tooltip("Seconds the drone is staggered (no firing) after being hit.")]
    [SerializeField] private float hitStaggerDuration = 0.1f;

    private Queue<GameObject> projectilePool;

    public float HoverHeight => hoverHeight;
    public DroneCluster Cluster { get; set; }

    // Expose thresholds
    public float FireEnterDistance => attackRange + fireEnterBuffer;
    public float FireExitDistance  => attackRange + fireExitBuffer;
    public float ChaseStuckSeconds => chaseStuckSeconds;

    // Public read-only properties to expose the Fire tuning values to behaviors (place with other public getters)
    public float FireStepAngleDeg => fireStepAngleDeg;
    public float FireRepositionIntervalMin => fireRepositionIntervalMin;
    public float FireRepositionIntervalMax => fireRepositionIntervalMax;
    public float FireRepositionJitter => fireRepositionJitter;
    public float FireArrivalEpsilon => fireArrivalEpsilon;
    public float FireCrossSwapChance => fireCrossSwapChance;

    // IProjectileShooter implementation
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public Transform FirePoint => firePoint;
    public float DetectionRange => detectionRange;

    // Cache own colliders to ignore self-hit
    private Collider[] ownColliders;

    public void FireProjectile(Transform target)
    {
        if (ProjectilePrefab == null || FirePoint == null || target == null) return;

        // Aim slightly upward (player center/head)
        Vector3 targetPos = target.position + Vector3.up * aimYOffset;
        Vector3 dir = GetAimedDirection(FirePoint.position, targetPos);

        // Apply random miss offset?
        if (Random.value < missChance)
        {
            float missAngle = Random.Range(minMissAngleDeg, maxMissAngleDeg);
            dir = Quaternion.Euler(0f, missAngle, 0f) * dir;
        }

        var proj = GetPooledProjectile();

        // Prevent projectile colliding with the drone BEFORE activation
        IgnoreSelfCollision(proj);

        // Detach so drone rotation doesn't affect flight during lifetime
        proj.transform.SetParent(ProjectileHierarchy.GetActiveEnemyProjectilesParent(), true);

        // Spawn slightly forward to avoid initial overlap
        Vector3 spawnPos = FirePoint.position + dir * Mathf.Max(0f, muzzleForwardOffset);
        proj.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(dir));

        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Ensure consistent, straight flight
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.angularVelocity = Vector3.zero;
        }

        // Activate after setup, then apply velocity
        proj.SetActive(true);
        if (rb != null)
        {
            rb.linearVelocity = dir * ProjectileSpeed;
        }

        // Keep owner reference for pool return
        var enemyProj = proj.GetComponent<EnemyProjectile>();
        if (enemyProj != null)
            enemyProj.SetOwner(this);
    }

    private Coroutine tickCoroutine;
    private float lastFireTime = 0f;
    private float staggerUntilTime = 0f;
    private Transform player;

    private IEnemyStateBehavior<DroneState, DroneTrigger> idleBehavior, relocateBehavior, swarmBehavior, fireBehavior, deathBehavior;

    public float zoneMoveInterval = 5f;
    public float lastZoneMoveTime = 0f;

    // Add this near the other coroutine fields
    private Coroutine fireTickCoroutine;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        idleBehavior = new DroneIdleBehavior<DroneState, DroneTrigger>();
        relocateBehavior = new DroneRelocateBehavior<DroneState, DroneTrigger>();
        swarmBehavior = new DroneSwarmBehavior<DroneState, DroneTrigger>();
        fireBehavior = new FireBehavior<DroneState, DroneTrigger>();
        deathBehavior = new DeathBehavior<DroneState, DroneTrigger>();

        idleTimerDuration = 15f;
        detectionRange = 15f;

        // Cache own colliders (exclude pool to avoid unnecessary ignores)
        ownColliders = GetComponentsInChildren<Collider>(includeInactive: true);

        // Create BulletPool child
        var poolObj = new GameObject("BulletPool");
        poolObj.transform.SetParent(transform);
        poolObj.transform.localPosition = Vector3.zero;
        bulletPoolParent = poolObj.transform;
    }

    private void Start()
    {
        InitializeStateMachine(DroneState.Idle);
        ConfigureStateMachine();

        if (currentZone == null)
        {
            currentZone = FindNearestZone(transform.position);
        }

        if (enemyAI.State.Equals(DroneState.Idle))
        {
            idleBehavior.OnEnter(this);
        }

        StartIdleTimer();
    }

    protected override void Update()
    {
        base.Update();

        float speed01 = 0f;
        if (agent != null && agent.enabled)
        {
            float normalizedDivisor = Mathf.Max(agent.speed, 0.01f);
            speed01 = Mathf.Clamp01(agent.velocity.magnitude / normalizedDivisor);
        }

        PlayLocomotionAnim(speed01);
        UpdateFacing();
    }

    public void StartIdleTimer()
    {
        if (idleTimerCoroutine != null)
            StopCoroutine(idleTimerCoroutine);
        idleTimerCoroutine = StartCoroutine(IdleTimerRoutine());
    }

    private IEnumerator IdleTimerRoutine()
    {
        yield return new WaitForSeconds(idleTimerDuration);
        enemyAI.Fire(DroneTrigger.LosePlayer);
    }

    protected override void ConfigureStateMachine()
    {
        enemyAI.Configure(DroneState.Idle)
            .OnEntry(() => { idleBehavior.OnEnter(this); })
            .OnExit(() => idleBehavior.OnExit(this))
            .Permit(DroneTrigger.SeePlayer, DroneState.Chase)
            .Permit(DroneTrigger.Die, DroneState.Death)
            .Permit(DroneTrigger.LosePlayer, DroneState.Relocate);

        enemyAI.Configure(DroneState.Relocate)
            .OnEntry(() => { relocateBehavior.OnEnter(this); })
            .OnExit(() => relocateBehavior.OnExit(this))
            .Permit(DroneTrigger.LosePlayer, DroneState.Idle)
            .Permit(DroneTrigger.SeePlayer, DroneState.Chase)
            .Permit(DroneTrigger.Die, DroneState.Death)
            .Permit(DroneTrigger.InAttackRange, DroneState.Fire)
            .Permit(DroneTrigger.RelocateComplete, DroneState.Idle);

        enemyAI.Configure(DroneState.Chase)
            .OnEntry(() => {
                swarmBehavior.OnEnter(this);
                ResetAgentDestination();
            })
            .OnExit(() => swarmBehavior.OnExit(this))
            .Permit(DroneTrigger.InAttackRange, DroneState.Fire)
            .Permit(DroneTrigger.LosePlayer, DroneState.Relocate)
            .Permit(DroneTrigger.Die, DroneState.Death)
            .Ignore(DroneTrigger.SeePlayer)
            .Ignore(DroneTrigger.RelocateComplete);

        enemyAI.Configure(DroneState.Fire)
            .OnEntry(() => { fireBehavior.OnEnter(this); StartFireTick(); })   // start tick
            .OnExit(() => { fireBehavior.OnExit(this); StopFireTick(); })                                // stop tick
            .Permit(DroneTrigger.OutOfAttackRange, DroneState.Chase)
            .Permit(DroneTrigger.LosePlayer, DroneState.Relocate)
            .Permit(DroneTrigger.Die, DroneState.Death)
            .Ignore(DroneTrigger.SeePlayer)
            .Ignore(DroneTrigger.InAttackRange)
            .Ignore(DroneTrigger.RelocateComplete);

        enemyAI.Configure(DroneState.Death)
            .OnEntry(() => { deathBehavior.OnEnter(this); })
            .OnExit(() => deathBehavior.OnExit(this))
            .Ignore(DroneTrigger.SeePlayer)
            .Ignore(DroneTrigger.LosePlayer)
            .Ignore(DroneTrigger.InAttackRange)
            .Ignore(DroneTrigger.OutOfAttackRange)
            .Ignore(DroneTrigger.Die);
    }

    // Movement, attack, and utility methods as before...
    public void MoveTo(Vector3 position)
    {
        if (agent == null || !agent.enabled) return;

        // Ensure agent is on a NavMesh, or try to recover
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var selfHit, 2f, NavMesh.AllAreas))
                agent.Warp(selfHit.position);
            else
                return;
        }

        // Flatten Y to the agent plane
        Vector3 desired = new Vector3(position.x, transform.position.y, position.z);
        Vector3 final = desired;

        // Try sampling near the desired point
        if (!NavMesh.SamplePosition(desired, out var hit, navSampleRadius, NavMesh.AllAreas))
        {
            // Fallback: shrink toward player center until we find a valid on-mesh point
            var playerTf = GetPlayerTransform();
            if (playerTf != null)
            {
                Vector3 center = new Vector3(playerTf.position.x, transform.position.y, playerTf.position.z);
                Vector3 toDesired = desired - center;
                float radius = toDesired.magnitude;

                if (radius > 0.001f)
                {
                    Vector3 dir = toDesired / radius;
                    float shrink = 0f;
                    bool found = false;

                    while (shrink <= navEdgeFallbackMaxShrink)
                    {
                        float r = Mathf.Max(radius - shrink, 0.5f);
                        Vector3 candidate = center + dir * r;

                        if (NavMesh.SamplePosition(candidate, out hit, navSampleRadius, NavMesh.AllAreas))
                        {
                            final = hit.position;
                            found = true;
                            break;
                        }
                        shrink += 0.5f;
                    }

                    if (!found)
                    {
                        // Last resort: sample near our current position
                        if (NavMesh.SamplePosition(transform.position, out var nearSelf, navSampleRadius, NavMesh.AllAreas))
                            final = nearSelf.position;
                        else
                            return;
                    }
                }
            }
            else
            {
                // No player reference; try desired again
                if (!NavMesh.SamplePosition(desired, out hit, navSampleRadius, NavMesh.AllAreas))
                    return;
                final = hit.position;
            }
        }
        else
        {
            final = hit.position;
        }

        agent.SetDestination(final);
    }

    public void TryFireAtPlayer()
    {
        if (Time.time < staggerUntilTime)
            return;

        if (player == null) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        lastFireTime = Time.time;

        if (projectilePrefab != null && firePoint != null)
        {
            Vector3 targetPos = player.position + Vector3.up * aimYOffset;
            Vector3 dir = GetAimedDirection(firePoint.position, targetPos);
            var proj = GetPooledProjectile();

            // Prevent self-collision
            IgnoreSelfCollision(proj);

            // Detach and spawn forward
            proj.transform.SetParent(ProjectileHierarchy.GetActiveEnemyProjectilesParent(), true);
            Vector3 spawnPos = firePoint.position + dir * Mathf.Max(0f, muzzleForwardOffset);
            proj.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(dir));

            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.angularVelocity = Vector3.zero;
            }

            proj.SetActive(true);
            if (rb != null) rb.linearVelocity = dir * projectileSpeed;

            var enemyProj = proj.GetComponent<EnemyProjectile>();
            if (enemyProj != null) enemyProj.SetOwner(this);

            PlayAttackAnim();
        }
    }

    public bool IsPlayerInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    public Transform GetPlayerTransform()
    {
        return player;
    }

    private Zone FindNearestZone(Vector3 position)
    {
        var zones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
        Zone nearest = null;
        float minDist = float.MaxValue;
        foreach (var zone in zones)
        {
            float dist = Vector3.Distance(position, zone.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = zone;
            }
        }
        return nearest;
    }

    public void StartTickCoroutine(System.Action tickAction, float interval = 0f)
    {
        StopTickCoroutine();
        tickCoroutine = StartCoroutine(TickRoutine(tickAction, interval));
    }

    public void StopTickCoroutine()
    {
        if (tickCoroutine != null)
        {
            StopCoroutine(tickCoroutine);
            tickCoroutine = null;
        }
    }

    private IEnumerator TickRoutine(System.Action tickAction, float interval)
    {
        while (true)
        {
            tickAction?.Invoke();
            yield return interval > 0f ? new WaitForSeconds(interval) : null;
        }
    }

    public void StopIdleTimer()
    {
        if (idleTimerCoroutine != null)
        {
            StopCoroutine(idleTimerCoroutine);
            idleTimerCoroutine = null;
        }
    }

    public void ResetAgentDestination()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    public void StartFireTick()
    {
        StopFireTick();
        fireTickCoroutine = StartCoroutine(TickRoutine(() => fireBehavior.Tick(this), 0f));
    }

    public void StopFireTick()
    {
        if (fireTickCoroutine != null)
        {
            StopCoroutine(fireTickCoroutine);
            fireTickCoroutine = null;
        }
    }

    private void InitializeProjectilePool()
    {
        projectilePool = new Queue<GameObject>(projectilePoolSize);
        for (int i = 0; i < projectilePoolSize; i++)
        {
            var proj = Instantiate(projectilePrefab, bulletPoolParent);
            proj.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private GameObject GetPooledProjectile()
    {
        if (projectilePool.Count > 0)
        {
            var proj = projectilePool.Dequeue();
            if (proj != null)
            {
                // ensure it returns to pool parent on deactivation
                return proj;
            }
        }
        var newProj = Instantiate(projectilePrefab, bulletPoolParent);
        newProj.SetActive(false);
        return newProj;
    }

    public void ReturnProjectileToPool(GameObject proj)
    {
        if (proj == null) return;

        // Reparent back under pool, reset physics, then deactivate and enqueue
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        proj.transform.SetParent(bulletPoolParent, false);
        proj.SetActive(false);
        projectilePool.Enqueue(proj);
    }

    private void OnEnable()
    {
        InitializeProjectilePool();
    }

    private void OnDisable()
    {
        StopFireTick();
        StopTickCoroutine();
        StopIdleTimer();
    }

    private Transform bulletPoolParent;

    // Ignore collisions between this drone and a given projectile's colliders
    private void IgnoreSelfCollision(GameObject projectile)
    {
        if (ownColliders == null || ownColliders.Length == 0 || projectile == null) return;
        var projCols = projectile.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < ownColliders.Length; i++)
        {
            var oc = ownColliders[i];
            if (oc == null) continue;
            for (int j = 0; j < projCols.Length; j++)
            {
                var pc = projCols[j];
                if (pc == null) continue;
                Physics.IgnoreCollision(pc, oc, true);
            }
        }
    }

    private Vector3 GetAimedDirection(Vector3 fireOrigin, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - fireOrigin).normalized;

        // Random miss: yaw-only offset around world up so vertical aim stays unchanged
        if (maxMissAngleDeg > 0f && Random.value < Mathf.Clamp01(missChance))
        {
            float minA = Mathf.Clamp(minMissAngleDeg, 0f, maxMissAngleDeg);
            float angle = Random.Range(minA, maxMissAngleDeg);
            if (angle > 0f)
            {
                float sign = Random.value < 0.5f ? -1f : 1f; // left or right
                dir = Quaternion.AngleAxis(sign * angle, Vector3.up) * dir;
            }
        }
        return dir;
    }

    private void UpdateFacing()
    {
        Vector3 forward = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            Vector3 planarVel = new Vector3(agent.velocity.x, 0f, agent.velocity.z);
            if (planarVel.sqrMagnitude >= velocityFacingThreshold * velocityFacingThreshold)
            {
                forward = planarVel;
            }
        }

        if (forward == Vector3.zero && player != null)
        {
            forward = new Vector3(player.position.x - transform.position.x, 0f, player.position.z - transform.position.z);
        }

        if (forward.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    protected override void OnDamageTaken(float amount)
    {
        if (currentHealth > 0f)
        {
            staggerUntilTime = Time.time + hitStaggerDuration;
        }
        base.OnDamageTaken(amount);
    }

}