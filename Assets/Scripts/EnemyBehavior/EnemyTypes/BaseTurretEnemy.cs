using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseTurretEnemy : BaseEnemy<EnemyState, EnemyTrigger>, IProjectileShooter
{
    [Header("Turret Settings")]
    [Tooltip("How quickly the turret turns to face the target (degrees/second).")]
    [SerializeField] private float turnSpeed = 360f;

    [Tooltip("Seconds between shots while in Attack state.")]
    [SerializeField] protected float fireCooldown = 1.0f;

    [Tooltip("Projectile prefab spawned when firing. Should have a Rigidbody and EnemyProjectile (or custom) component.")]
    [SerializeField] protected GameObject projectilePrefab;

    [Tooltip("Initial linear velocity magnitude applied to the projectile.")]
    [SerializeField] protected float projectileSpeed = 25f;

    [Tooltip("Muzzle transform used as the spawn position and forward direction when firing.")]
    [SerializeField] protected Transform firePoint;

    [Tooltip("If true, only rotate around Y to face the target (keeps turret upright).")]
    [SerializeField] private bool rotateYawOnly = true;

    [Tooltip("Vertical aim offset in meters to target the player's center/head instead of feet.")]
    [SerializeField] private float aimYOffset = 1.0f;

    [Header("Firing")]
    [Tooltip("Spawn the projectile slightly forward from the muzzle to avoid self-collision at spawn.")]
    [SerializeField] private float muzzleForwardOffset = 0.1f;

    [Header("Projectile Pool")]
    [Tooltip("How many projectiles to pre-instantiate for this turret.")]
    [SerializeField] private int projectilePoolSize = 16;

    [Header("Detection Hysteresis")]
    [Tooltip("Enter Attack when player is within detectionRange + this.")]
    [SerializeField] private float enterBuffer = 0.0f;

    [Tooltip("Leave Attack when player is beyond detectionRange + this.")]
    [SerializeField] private float exitBuffer = 0.5f;

    [Tooltip("Seconds the condition must hold before switching into Attack.")]
    [SerializeField] private float enterSustain = 0.15f;

    [Tooltip("Seconds the condition must hold before leaving Attack.")]
    [SerializeField] private float exitSustain = 0.25f;

    private readonly List<GameObject> projectilePool = new List<GameObject>(32);
    private Transform projectilePoolParent;

    private Transform player;
    private Coroutine attackLoop;
    private Coroutine detectLoop;

    // Cache own colliders to ignore self-collision on fired projectiles
    private Collider[] ownColliders;

    // Cooldown tracking persists across state flaps to prevent edge rapid-fire
    private float lastShotTime = -1e9f;

    // IProjectileShooter implementation
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public Transform FirePoint => firePoint;

    protected override void Awake()
    {
        base.Awake();

        // Turret is stationary. Remove NavMeshAgent added by base.
        if (agent != null)
        {
            Destroy(agent);
            agent = null;
        }

        // Turn off melee attack collider for turrets
        if (attackCollider != null) attackCollider.enabled = false;

        // Cache player
        var found = GameObject.FindGameObjectWithTag("Player");
        player = found != null ? found.transform : null;

        // Cache only the turret's own colliders BEFORE creating the pool (so pool colliders are excluded)
        ownColliders = GetComponentsInChildren<Collider>(includeInactive: true);

        // Create pool parent under this turret
        var poolObj = new GameObject("ProjPool");
        poolObj.transform.SetParent(transform);
        poolObj.transform.localPosition = Vector3.zero;
        projectilePoolParent = poolObj.transform;

        InitializeProjectilePool();

        // Start state machine
        InitializeStateMachine(EnemyState.Idle);
        ConfigureStateMachine();

        // Detection loop with hysteresis/debounce
        detectLoop = StartCoroutine(DetectionLoop());
    }

    protected override void ConfigureStateMachine()
    {
        enemyAI.Configure(EnemyState.Idle)
            .OnEntry(() => { SetEnemyColor(patrolColor); StopAttackLoop(); })
            .Permit(EnemyTrigger.SeePlayer, EnemyState.Attack)
            .Permit(EnemyTrigger.InAttackRange, EnemyState.Attack)
            .Permit(EnemyTrigger.Die, EnemyState.Death);

        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => { SetEnemyColor(attackColor); StartAttackLoop(); })
            .OnExit(() => { StopAttackLoop(); })
            .Permit(EnemyTrigger.LosePlayer, EnemyState.Idle)
            .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Idle)
            .Ignore(EnemyTrigger.SeePlayer)
            .Ignore(EnemyTrigger.InAttackRange)
            .Permit(EnemyTrigger.Die, EnemyState.Death);

        enemyAI.Configure(EnemyState.Death)
            .OnEntry(() => { SetEnemyColor(Color.black); StopAttackLoop(); })
            .Ignore(EnemyTrigger.SeePlayer)
            .Ignore(EnemyTrigger.LosePlayer)
            .Ignore(EnemyTrigger.InAttackRange)
            .Ignore(EnemyTrigger.OutOfAttackRange)
            .Ignore(EnemyTrigger.Die);
    }

    private IEnumerator DetectionLoop()
    {
        const float interval = 0.15f; // slightly slower to reduce boundary chatter
        float enterTimer = 0f;
        float exitTimer = 0f;

        while (true)
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
                yield return new WaitForSeconds(interval);
                continue;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            // Compute thresholds and enforce exit > enter
            float enterThreshold = Mathf.Max(0f, detectionRange + Mathf.Max(0f, enterBuffer));
            float exitThreshold = detectionRange + Mathf.Max(exitBuffer, enterBuffer + 0.5f);
            if (exitThreshold <= enterThreshold)
                exitThreshold = enterThreshold + 0.5f;

            if (enemyAI.State.Equals(EnemyState.Idle))
            {
                if (dist <= enterThreshold)
                {
                    enterTimer += interval;
                    if (enterTimer >= enterSustain)
                    {
                        TryFireTriggerByName("SeePlayer"); // -> Attack
                        enterTimer = 0f;
                        exitTimer = 0f;
                    }
                }
                else
                {
                    enterTimer = 0f;
                }
            }
            else if (enemyAI.State.Equals(EnemyState.Attack))
            {
                if (dist > exitThreshold)
                {
                    exitTimer += interval;
                    if (exitTimer >= exitSustain)
                    {
                        TryFireTriggerByName("LosePlayer"); // -> Idle
                        exitTimer = 0f;
                        enterTimer = 0f;
                    }
                }
                else
                {
                    exitTimer = 0f;
                }
            }

            yield return new WaitForSeconds(interval);
        }
    }

    private void StartAttackLoop()
    {
        StopAttackLoop();
        attackLoop = StartCoroutine(AttackLoop());
    }

    private void StopAttackLoop()
    {
        if (attackLoop != null)
        {
            StopCoroutine(attackLoop);
            attackLoop = null;
        }
    }

    private IEnumerator AttackLoop()
    {
        while (enemyAI.State.Equals(EnemyState.Attack))
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
                yield return null;
                continue;
            }

            // Aim at player (optionally yaw-only)
            Vector3 targetPos = player.position + Vector3.up * aimYOffset;
            Vector3 dir = (targetPos - transform.position);
            if (rotateYawOnly) dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // Fire on cooldown using a persistent timestamp (prevents edge rapid-fire)
            if (Time.time - lastShotTime >= fireCooldown)
            {
                FireProjectile(player);
                lastShotTime = Time.time;
            }

            yield return null;
        }
    }

    public void FireProjectile(Transform target)
    {
        if (ProjectilePrefab == null || FirePoint == null || target == null) return;

        Vector3 targetPos = target.position + Vector3.up * aimYOffset;
        Vector3 dir = (targetPos - FirePoint.position).normalized;

        var proj = GetPooledProjectile();

        // Set up pooling helper/return parent (in case prefab doesn't have it)
        var pooled = proj.GetComponent<TurretPooledProjectile>();
        if (pooled == null) pooled = proj.AddComponent<TurretPooledProjectile>();
        pooled.InitReturnParent(projectilePoolParent);

        // Prevent projectile from colliding with its own turret BEFORE activation
        IgnoreSelfCollision(proj);

        // Detach active projectile so turret rotation won't affect it
        proj.transform.SetParent(ProjectileHierarchy.GetActiveEnemyProjectilesParent(), true);
        Vector3 spawnPos = FirePoint.position + dir * Mathf.Max(0f, muzzleForwardOffset);
        proj.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(dir));

        // Ensure fast bullets register collisions
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.angularVelocity = Vector3.zero;
        }

        // Activate after all setup to avoid immediate self-collision, then set velocity
        proj.SetActive(true);
        if (rb != null)
        {
            rb.linearVelocity = dir * ProjectileSpeed;
        }
    }

    // Permanently ignore collisions between this turret's colliders and this projectile's colliders
    private void IgnoreSelfCollision(GameObject projectile)
    {
        if (ownColliders == null || ownColliders.Length == 0) return;

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

    private void InitializeProjectilePool()
    {
        projectilePool.Clear();
        if (ProjectilePrefab == null) return;

        for (int i = 0; i < projectilePoolSize; i++)
        {
            var proj = Instantiate(ProjectilePrefab, projectilePoolParent);
            proj.SetActive(false);

            var pooled = proj.GetComponent<TurretPooledProjectile>();
            if (pooled == null) pooled = proj.AddComponent<TurretPooledProjectile>();
            pooled.InitReturnParent(projectilePoolParent);

            projectilePool.Add(proj);
        }
    }

    private GameObject GetPooledProjectile()
    {
        for (int i = 0; i < projectilePool.Count; i++)
        {
            if (projectilePool[i] != null && !projectilePool[i].activeSelf)
                return projectilePool[i];
        }
        var proj = Instantiate(ProjectilePrefab, projectilePoolParent);
        proj.SetActive(false);
        var pooled = proj.GetComponent<TurretPooledProjectile>();
        if (pooled == null) pooled = proj.AddComponent<TurretPooledProjectile>();
        pooled.InitReturnParent(projectilePoolParent);
        projectilePool.Add(proj);
        return proj;
    }

    private void OnDisable()
    {
        if (detectLoop != null)
        {
            StopCoroutine(detectLoop);
            detectLoop = null;
        }
        StopAttackLoop();
    }
}