// BaseEnemy.cs
// Purpose: Generic base class for all enemy types. Implements common systems: health, detection, colliders, state machine integration, and optional CrowdAgent registration.
// Works with: Stateless state machine library, EnemyStateMachineConfig, NavMeshAgent, CrowdController, EnemyBehaviorProfile, HealthBar UI.
// Notes: Derived classes define their own TState/TTrigger enums and configure the state machine. This file registers a CrowdAgent when available.

using System.Collections;
using Stateless;
using UnityEngine;
using UnityEngine.AI;
using EnemyBehavior.Crowd;

// BaseEnemy is generic so derived classes can define their own states and triggers
public abstract class BaseEnemy<TState, TTrigger> : MonoBehaviour, IHealthSystem
 where TState : struct, System.Enum
 where TTrigger : struct, System.Enum
{
    [Header("Behavior Profile")]
    [SerializeField, Tooltip(
        "ScriptableObject that tunes nav/avoidance/importance and planner hints.\n" +
        "Assign an asset from Assets > Scripts > EnemyBehavior > Profiles (Create > AI > EnemyBehaviorProfile).\n" +
        "Values are applied to this enemy's NavMeshAgent on Awake and passed to Crowd/Path systems.")]
    public EnemyBehaviorProfile behaviorProfile;

    [SerializeField, Tooltip("Reference to the NavMeshAgent attached to this enemy. Serialized so agent settings can be tweaked per-enemy in the Inspector.")]
    public NavMeshAgent agent;
    public StateMachine<TState, TTrigger> enemyAI; // StateMachine<StateEnum, TriggerEnum> is from the Stateless library

    [Header("State Machine")]
    [SerializeField, Tooltip("The current state of the enemy's state machine. Read-only; for debugging and visualization.")]
    private TState currentState;

    [Header("Health")]
    [SerializeField, Tooltip("Maximum health value for this enemy.")]
    public float maxHealth = 100f;
    [SerializeField, MaxHealthSlider, Tooltip("Current health value for this enemy.")]
    public float currentHealth = 100f;
    [SerializeField, Tooltip("Percent of max health at which the enemy is considered low health (e.g., will flee or recover).")]
    protected float lowHealthThresholdPercent = 0.25f;
    [SerializeField, Tooltip("Enable or disable low health behavior (fleeing, recovering, etc.).")]
    protected bool handleLowHealth = true;

    [Header("Zone Management")]
    [SerializeField, Tooltip("The zone this enemy is currently in.")]
    public Zone currentZone;
    [SerializeField, Tooltip("How long the enemy remains idle before relocating to another zone.")]
    public float idleTimerDuration = 15f;

    [Header("Detection")]
    [SerializeField, Tooltip("Radius of the detection sphere for spotting the player.")]
    protected float detectionRange = 10f;
    [SerializeField, Tooltip("Show the detection range gizmo in the Scene view.")]
    protected bool showDetectionGizmo = true;

    [Header("Attack")]
    [SerializeField, Tooltip("Damage dealt to the player per attack.")]
    public float damage = 10f;
    [SerializeField, Tooltip("Size of the attack box collider (width, height, depth) used for attack range.")]
    public Vector3 attackBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField, Tooltip("Distance in front of the enemy where the attack box is positioned.")]
    public float attackBoxDistance = 1.5f;
    [SerializeField, Tooltip("Time in seconds between attacks (attack cooldown).")]
    public float attackInterval = 1.0f;
    [SerializeField, Tooltip("Time in seconds the attack box is enabled (attack active duration).")]
    public float attackActiveDuration = 0.5f;
    [SerializeField, Tooltip("Show the attack range gizmo in the Scene view.")]
    protected bool showAttackGizmo = true;

    [Header("Enemy Health Bar")]
    [SerializeField, Tooltip("Prefab for the enemy's health bar UI.")]
    public GameObject healthBarPrefab;

    [Header("Anti-Stuck")] 
    [SerializeField, Tooltip("Enable a lightweight anti-stuck behavior that side-steps or backs off when velocity is near zero for a short time while having a path.")]
    private bool enableAntiStuck = true;
    [SerializeField, Tooltip("Speed (m/s) below which the agent is considered stalled.")]
    private float stuckSpeedThreshold = 0.05f;
    [SerializeField, Tooltip("Seconds the agent must remain under the speed threshold before relief kicks in.")]
    private float stuckSeconds = 0.9f;
    [SerializeField, Tooltip("Meters to step sideways when attempting relief.")]
    private float sidestepDistance = 1.0f;
    [SerializeField, Tooltip("Meters to back off when attempting relief if sidestep is not possible.")]
    private float backoffDistance = 1.5f;
    [SerializeField, Tooltip("Radius used with NavMesh.SamplePosition when probing relief points.")]
    private float reliefSampleRadius = 1.0f;
    [SerializeField, Tooltip("Cooldown after a relief move before checking for another relief (seconds).")]
    private float reliefCooldownSeconds = 1.5f;

    // Non-serialized fields
    [HideInInspector]
    public EnemyHealthBar healthBarInstance;
    protected SphereCollider detectionCollider;
    [HideInInspector]
    public BoxCollider attackCollider;
    [HideInInspector]
    public bool isAttackBoxActive = false;
    [HideInInspector]
    public bool hasFiredLowHealth = false;
    protected Coroutine recoverCoroutine;
    protected Coroutine idleTimerCoroutine;
    protected Coroutine idleWanderCoroutine;
    protected Coroutine zoneArrivalCoroutine;
    protected Coroutine attackLoopCoroutine;
    //private Vector3 lastZoneCheckPosition;

    protected Renderer enemyRenderer;
    [HideInInspector]
    public Color patrolColor = Color.green;
    [HideInInspector]
    public Color chaseColor = Color.yellow;
    [HideInInspector]
    public Color attackColor = new Color(1f, 0.5f, 0f); // Orange
    [HideInInspector]
    public Color hitboxActiveColor = Color.red;

    private Transform playerTarget;
    public Transform PlayerTarget
    {
        get => playerTarget;
        set => playerTarget = value;
    }

    // Anti-stuck state
    private float _stuckTimer;
    private float _reliefUntil;

    // Awake is called when the script instance is being loaded
    protected virtual void Awake()
    {
        // Ensure the GameObject has a NavMeshAgent component
        if (this.gameObject.GetComponent<NavMeshAgent>() == null)
        {
            this.gameObject.AddComponent<NavMeshAgent>();
        }
        agent = this.gameObject.GetComponent<NavMeshAgent>();

        // Apply behavior profile to nav agent if present
        ApplyBehaviorProfile();

        // enemyAI and currentState should be initialized in the derived class

        // Detection collider
        detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRange;

        // Attack collider
        attackCollider = gameObject.AddComponent<BoxCollider>();
        attackCollider.isTrigger = true;
        attackCollider.size = attackBoxSize;
        attackCollider.center = new Vector3(0f, 0f, attackBoxDistance);
        attackCollider.enabled = false; // Default off

        // Automatically assign the capsule's MeshRenderer
        enemyRenderer = GetComponent<MeshRenderer>();

        // Register with CrowdController as a CrowdAgent if available
        try
        {
            var ca = new CrowdAgent() { Agent = agent, Profile = behaviorProfile };
            if (CrowdController.Instance != null)
            {
                CrowdController.Instance.Register(ca);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("BaseEnemy: failed to register CrowdAgent: " + ex.Message);
        }
    }

    protected void ApplyBehaviorProfile()
    {
        if (behaviorProfile == null || agent == null) return;
        // Speed can be randomized within the provided range
        agent.speed = Random.Range(behaviorProfile.SpeedRange.x, behaviorProfile.SpeedRange.y);
        agent.acceleration = behaviorProfile.Acceleration;
        agent.angularSpeed = behaviorProfile.AngularSpeed;
        agent.stoppingDistance = behaviorProfile.StoppingDistance;
        // Jitter priority slightly to break symmetrical face-offs in corridors
        int basePrio = Mathf.Clamp(behaviorProfile.AvoidancePriority, 0, 99);
        int jittered = Mathf.Clamp(basePrio + Random.Range(-7, 8), 0, 99);
        agent.avoidancePriority = jittered;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = false;
    }

    // Helper to initialize the state machine and inspector state
    protected void InitializeStateMachine(TState initialState)
    {
        enemyAI = new StateMachine<TState, TTrigger>(initialState);

        // Set the initial state for the Inspector
        currentState = enemyAI.State;

        // Update currentState only when the state machine transitions
        // Stateless provides this OnTransitioned event to hook into transitions natively
        enemyAI.OnTransitioned(t => currentState = t.Destination);
    }

    protected virtual void ConfigureStateMachine()
    {
        // Intentionally left blank for derived class override
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        // Lightweight anti-stuck relief for narrow corridors
        if (enableAntiStuck && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (Time.time >= _reliefUntil && agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f)
            {
                float spd = agent.velocity.magnitude;
                if (spd < stuckSpeedThreshold)
                {
                    _stuckTimer += Time.deltaTime;
                    if (_stuckTimer >= stuckSeconds)
                    {
                        TryStuckRelief();
                        _stuckTimer = 0f;
                        _reliefUntil = Time.time + reliefCooldownSeconds;
                    }
                }
                else
                {
                    _stuckTimer = 0f;
                }
            }
        }
    }

    private void TryStuckRelief()
    {
        if (agent == null) return;
        Vector3 pos = agent.transform.position;
        Vector3 dir = agent.desiredVelocity.sqrMagnitude > 0.001f ? agent.desiredVelocity.normalized : agent.transform.forward;
        Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized;

        Vector3[] probes = new Vector3[3];
        probes[0] = pos + perp * sidestepDistance; // left
        probes[1] = pos - perp * sidestepDistance; // right
        probes[2] = pos - dir * backoffDistance;   // back

        for (int i = 0; i < probes.Length; i++)
        {
            if (NavMesh.SamplePosition(probes[i], out NavMeshHit hit, reliefSampleRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    // --- PASSIVE MOVEMENT AND BEHAVIOR METHODS ---
    public void SetEnemyColor(Color color)
    {
        if (enemyRenderer != null)
            enemyRenderer.material.color = color;
    }

    public virtual void UpdateCurrentZone()
    {
        //Debug.Log($"{gameObject.name} Updating current zone.");
        Zone[] zones = Object.FindObjectsByType<Zone>(FindObjectsSortMode.None);
        foreach (var zone in zones)
        {
            if (zone.Contains(transform.position))
            {
                currentZone = zone;
                return;
            }
        }
        currentZone = null; // Not in any zone
    }
    // --- HEALTH MANAGEMENT METHODS ---
    // --- IHealthSystem Implementation ---
    // Property for currentHP (read-only for interface, uses currentHealth internally)
    public float currentHP => currentHealth;

    // Property for maxHP (read-only for interface, uses maxHealth internally)
    public float maxHP => maxHealth;

    // LoseHP is called to apply damage to the enemy
    public void LoseHP(float damage)
    {
        SetHealth(currentHealth - damage);
    }

    // HealHP is called to restore health (used by RecoverBehavior)
    public void HealHP(float hp)
    {
        SetHealth(currentHealth + hp);
    }

    // SetHealth now clamps and updates health, but expects the new value
    public virtual void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        CheckHealthThreshold();
    }

    public virtual void CheckHealthThreshold()
    {
        // Fire Die trigger if health reaches zero
        if (currentHealth <= 0)
        {
            TryFireTriggerByName("Die");
        }

        if (!handleLowHealth)
            return;

        if (!hasFiredLowHealth && currentHealth <= maxHealth * lowHealthThresholdPercent)
        {
            hasFiredLowHealth = true;
            TryFireTriggerByName("LowHealth");
        }
    }

    // Method to fire triggers safely by value, returns true if fired
    protected bool FireTrigger(TTrigger trigger)
    {
        if (enemyAI.CanFire(trigger))
        {
            enemyAI.Fire(trigger);
            return true;
        }
        else
        {
            Debug.LogWarning($"Cannot fire trigger {trigger} from state {enemyAI.State}");
            return false;
        }
    }

    // Helper to fire triggers by name (string), returns true if fired
    public virtual bool TryFireTriggerByName(string triggerName)
    {
        if (System.Enum.TryParse(triggerName, out TTrigger trigger))
        {
            return FireTrigger(trigger);
        }
        else
        {
            Debug.LogWarning($"{typeof(TTrigger).Name} does not contain a '{triggerName}' trigger. Check your enum definition.");
            return false;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (enemyAI == null)
            return;

        if (other.CompareTag("Player"))
        {
            // Check which collider is currently triggering this event
            // This works because OnTriggerEnter is called for each trigger collider on the GameObject
            // Use Physics.OverlapSphere or OverlapBox if you need to check proximity

            // Check if this is the detection collider
            if (detectionCollider != null && detectionCollider.enabled && detectionCollider.bounds.Contains(other.transform.position))
            {
                if (TryFireTriggerByName("SeePlayer")) // Doing it this way to not cause many duplicate Debug.Logs
                {
                    Debug.Log($"{gameObject.name} detected player (detection collider).");
                }
            }
            // Check if this is the attack collider
            else if (attackCollider != null && attackCollider.enabled && attackCollider.bounds.Contains(other.transform.position))
            {
                if (TryFireTriggerByName("InAttackRange"))
                {
                    Debug.Log($"{gameObject.name} detected player (attack collider).");
                }
            }
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (detectionCollider != null && detectionCollider.enabled && detectionCollider.bounds.Contains(other.transform.position))
            {
                // Only fire SeePlayer if not already in Chase
                // Use string comparison to avoid generic enum casting issues in the base class
                if (!enemyAI.State.ToString().Equals("Chase"))
                {
                    TryFireTriggerByName("SeePlayer");
                }
            }
        }
    }

    // Draw gizmos for detection and attack colliders
    protected virtual void OnDrawGizmos()
    {
        // Detection range gizmo (sphere)
        if (showDetectionGizmo)
        {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f); // Cyan, semi-transparent
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.1f);
            Gizmos.DrawSphere(transform.position, detectionRange);
        }

        // Attack range gizmo (box)
        if (showAttackGizmo)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f); // Red, semi-transparent
            Vector3 boxCenter = transform.position + transform.forward * attackBoxDistance;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.1f);
            Gizmos.DrawCube(Vector3.zero, attackBoxSize);
            Gizmos.matrix = Matrix4x4.identity;
            // Also draw the effective attack range as a sphere for reference
            float attackRange = (Mathf.Max(attackBoxSize.x, attackBoxSize.z) * 0.5f) + attackBoxDistance;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

    protected virtual void OnValidate()
    {
        if (detectionCollider != null)
            detectionCollider.radius = detectionRange;
        if (attackCollider != null)
        {
            attackCollider.size = attackBoxSize;
            attackCollider.center = new Vector3(0f, 0f, attackBoxDistance);
        }
    }
}

#region States and Triggers
public enum EnemyState
{
 Idle, // My idea is that when in Idle, the enemy is moving around a section of the map (zone)
 // so it is idle in the sense that it is not actively searching for the player

 Relocate, // Relocate is a substate of Patrol where the enemy moves to a new area or waypoint
 // before transitioning to Idle. This could be used when the enemy loses sight of the player
 // and needs to move to a different location to search.

 Patrol, // Patrol is the main state where the enemy is actively moving from zone to zone.
 // It contains the shared trigger of seeing the player to transition to Chase.

 Reinforcements, // This state would be used when another enemy calls for help

 Chase, // Chase is when the enemy has detected the player and is actively pursuing them.

 Attack, // Attack is when the enemy is in range to attack the player.

 Flee, // Flee is when the enemy is low on health and tries to escape from the player.

 Fled, // Fled is when the enemy has successfully escaped (out of attack range) and is no longer in immediate danger.

 Recover, // Recover is when the enemy is regaining health passively while idle.

 Death // Death is when the enemy has been defeated and is no longer active.
}

public enum EnemyTrigger
{
 SeePlayer, // Within a certain detection radius or line of sight

 LosePlayer, // Out of detection radius or line of sight for a certain time

 AidRequested, // Another enemy has called for help

 FailedAid, // The aid request was unsuccessful (e.g., arrived and player was gone)
 // It would start a timer to relocate after a certain time after arriving if no player is seen

 LowHealth, // I was thinking LowHealth would be like20-25% of max health

 RecoveredHealth, // We discussed whether or not enemies should have passive health regen
 // I think it could be interesting if they do, but it should be slow and only while idle
 // So I am including functionality for it for now

 InAttackRange, // These ranges are based on the enemy's attack range, and not the player's
 OutOfAttackRange, // Therefore, they may need to be adjusted for how they interact with fleeing behavior

 ReachZone, // Reached the new zone after relocating

 IdleTimerElapsed, // Timer for how long the enemy has been idle before relocating

 Attacked, // The enemy has been attacked by the player

 Die // The enemy has been defeated
}
#endregion
// Static class to hold shared (default) state machine configurations
// It cannot be stored in BaseEnemy because it is generic
// This also only stores Permits, not OnEntry/OnExit actions
// Derived enemy classes will handle OnEntry/OnExit actions in their own ConfigureStateMachine method
public static class EnemyStateMachineConfig
{
 public static void ConfigureBasic(StateMachine<EnemyState, EnemyTrigger> sm)
 {
 sm.Configure(EnemyState.Idle)
 .SubstateOf(EnemyState.Patrol) // Idle is a substate of Patrol
 .Permit(EnemyTrigger.LowHealth, EnemyState.Recover) // Only in Idle will it transition to Recover
 .Permit(EnemyTrigger.IdleTimerElapsed, EnemyState.Relocate); // After some time in Idle, it relocates
 // My idea is that it would be more dynamic
 // if the enemy moved around from zone to zone
 // instead of just standing still in one spot

 sm.Configure(EnemyState.Relocate)
 .SubstateOf(EnemyState.Patrol) // Relocate is a substate of Patrol
 .Permit(EnemyTrigger.ReachZone, EnemyState.Idle); // Once it reaches the new zone, it goes to Idle

 sm.Configure(EnemyState.Patrol)
 .Permit(EnemyTrigger.SeePlayer, EnemyState.Chase) // Shared trigger to Chase from Patrol
 .Permit(EnemyTrigger.AidRequested, EnemyState.Reinforcements) // Shared trigger to call for reinforcements from Patrol
 .Permit(EnemyTrigger.Attacked, EnemyState.Chase); // If attacked while patrolling, it chases the player

 sm.Configure(EnemyState.Reinforcements)
 .Permit(EnemyTrigger.SeePlayer, EnemyState.Chase) // Shared trigger to Chase from Reinforcements
 .Permit(EnemyTrigger.FailedAid, EnemyState.Relocate); // If the aid request fails, it relocates

 sm.Configure(EnemyState.Chase)
 .Permit(EnemyTrigger.LosePlayer, EnemyState.Relocate) // If it loses the player, it relocates
 .Permit(EnemyTrigger.InAttackRange, EnemyState.Attack); // If it gets in range, it attacks

 sm.Configure(EnemyState.Attack)
 .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Chase); // If the player moves out of range, it chases again
 //.Permit(EnemyTrigger.LowHealth, EnemyState.Flee); // If low on health, it flees
 // Commented out until fleeing behavior is functional, or if we even want to use it at all

 sm.Configure(EnemyState.Flee) // This state can be used for unique fleeing behavior like calling for reinforcements or defensive manuevers
 .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Fled); // Once out of range, it goes to Fled

 sm.Configure(EnemyState.Fled)
 .Permit(EnemyTrigger.InAttackRange, EnemyState.Flee) // If the player comes back into range, it goes back to Flee
 .Permit(EnemyTrigger.LosePlayer, EnemyState.Relocate); // If it loses the player while fleeing, it relocates

 sm.Configure(EnemyState.Recover) // Essentially the same as Idle but with health regen, maybe no movement at all
 .SubstateOf(EnemyState.Patrol) // Recover is a substate of Patrol
 .Permit(EnemyTrigger.RecoveredHealth, EnemyState.Idle); // Once it recovers health, it goes back to Idle

 // Permit Die from any state except Death itself
 foreach (EnemyState state in System.Enum.GetValues(typeof(EnemyState)))
 {
 if (state != EnemyState.Death)
 {
 sm.Configure(state)
 .Permit(EnemyTrigger.Die, EnemyState.Death);
 }
 else
 {
 sm.Configure(state)
 .Ignore(EnemyTrigger.Die);
 }
 }

 // Configure Death state (no outgoing transitions)
 sm.Configure(EnemyState.Death);
 }
}