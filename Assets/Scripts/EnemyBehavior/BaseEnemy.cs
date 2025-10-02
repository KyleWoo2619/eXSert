using System.Collections;
using Stateless;
using UnityEngine;
using UnityEngine.AI;

// BaseEnemy is generic so derived classes can define their own states and triggers
public abstract class BaseEnemy<TState, TTrigger> : MonoBehaviour
    where TState : struct, System.Enum
    where TTrigger : struct, System.Enum
{
    protected NavMeshAgent agent;
    protected StateMachine<TState, TTrigger> enemyAI; // StateMachine<StateEnum, TriggerEnum> is from the Stateless library

    [Header("State Machine")]
    [SerializeField, Tooltip("The current state of the enemy's state machine. Read-only; for debugging and visualization.")]
    private TState currentState;

    [Header("Health")]
    [SerializeField, Tooltip("Maximum health value for this enemy.")]
    protected float maxHealth = 100f;
    [SerializeField, MaxHealthSlider, Tooltip("Current health value for this enemy.")]
    protected float currentHealth = 100f;
    [SerializeField, Tooltip("Percent of max health at which the enemy is considered low health (e.g., will flee or recover).")]
    protected float lowHealthThresholdPercent = 0.25f;
    [SerializeField, Tooltip("Enable or disable low health behavior (fleeing, recovering, etc.).")]
    protected bool handleLowHealth = true;

    [Header("Zone Management")]
    [SerializeField, Tooltip("The zone this enemy is currently in.")]
    public Zone currentZone;
    [SerializeField, Tooltip("How long the enemy remains idle before relocating to another zone.")]
    protected float idleTimerDuration = 15f;

    [Header("Detection")]
    [SerializeField, Tooltip("Radius of the detection sphere for spotting the player.")]
    protected float detectionRange = 10f;
    [SerializeField, Tooltip("Show the detection range gizmo in the Scene view.")]
    protected bool showDetectionGizmo = true;

    [Header("Attack")]
    [SerializeField, Tooltip("Size of the attack box collider (width, height, depth) used for attack range.")]
    protected Vector3 attackBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField, Tooltip("Distance in front of the enemy where the attack box is positioned.")]
    protected float attackBoxDistance = 1.5f;
    [SerializeField, Tooltip("Time in seconds between attacks (attack cooldown).")]
    protected float attackInterval = 1.0f;
    [SerializeField, Tooltip("Time in seconds the attack box is enabled (attack active duration).")]
    protected float attackActiveDuration = 0.5f;
    [SerializeField, Tooltip("Show the attack range gizmo in the Scene view.")]
    protected bool showAttackGizmo = true;
    

    // Non-serialized fields
    protected SphereCollider detectionCollider;
    protected BoxCollider attackCollider;
    protected bool isAttackBoxActive = false;
    protected bool hasFiredLowHealth = false;
    protected Coroutine recoverCoroutine;
    protected Coroutine idleTimerCoroutine;
    protected Coroutine idleWanderCoroutine;
    protected Coroutine zoneArrivalCoroutine;
    protected Coroutine attackLoopCoroutine;
    private Vector3 lastZoneCheckPosition;

    protected Renderer enemyRenderer;
    protected Color patrolColor = Color.green;
    protected Color chaseColor = Color.yellow;
    protected Color attackColor = new Color(1f, 0.5f, 0f); // Orange
    protected Color hitboxActiveColor = Color.red;

    // Awake is called when the script instance is being loaded
    protected virtual void Awake()
    {
        // Ensure the GameObject has a NavMeshAgent component
        if (this.gameObject.GetComponent<NavMeshAgent>() == null)
        {
            this.gameObject.AddComponent<NavMeshAgent>();
        }
        agent = this.gameObject.GetComponent<NavMeshAgent>();
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
        // Not sure if Update will be needed in the base class
        // but it is here if we need it later
        // Trying to not use it as much as possible for performance reasons
    }

    // --- PASSIVE MOVEMENT AND BEHAVIOR METHODS ---
    protected virtual void SetEnemyColor(Color color)
    {
        if (enemyRenderer != null)
            enemyRenderer.material.color = color;
    }

    protected virtual void OnEnterIdle()
    {
        SetEnemyColor(patrolColor);
        Debug.Log($"{gameObject.name} entered Idle state.");
        ResetIdleTimer();
        hasFiredLowHealth = false;
        CheckHealthThreshold();

        if (idleWanderCoroutine != null)
        {
            StopCoroutine(idleWanderCoroutine);
        }
        UpdateCurrentZone();
        idleWanderCoroutine = StartCoroutine(IdleWanderLoop());
    }

    // The timer for how long the enemy has been idle before relocating
    protected virtual void ResetIdleTimer()
    {
        if (idleTimerCoroutine != null)
        {
            StopCoroutine(idleTimerCoroutine);
        }
        idleTimerCoroutine = StartCoroutine(IdleTimerCoroutine());
    }

    protected virtual IEnumerator IdleTimerCoroutine()
    {
        yield return new WaitForSeconds(idleTimerDuration);

        // Fire the IdleTimerElapsed trigger if still in Idle state
        if (enemyAI.State.Equals((TState)System.Enum.Parse(typeof(TState), "Idle")))
        {
            if (TryFireTriggerByName("IdleTimerElapsed"))
            {
                Debug.Log($"{gameObject.name} Idle timer elapsed, firing IdleTimerElapsed trigger. Relocating...");
            }
        }
        idleTimerCoroutine = null;
    }

    protected virtual IEnumerator IdleWanderLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(2f, 4f);
            yield return new WaitForSeconds(waitTime);
            IdleWander();
        }
    }

    protected Zone[] GetOtherZones()
    {
        Zone[] allZones = Object.FindObjectsByType<Zone>(FindObjectsSortMode.None);
        if (currentZone == null)
            return allZones;
        // Exclude the current zone
        var otherZones = new System.Collections.Generic.List<Zone>();
        foreach (var zone in allZones)
        {
            if (zone != currentZone)
                otherZones.Add(zone);
        }
        return otherZones.ToArray();
    }

    protected virtual void UpdateCurrentZone()
    {
        Debug.Log($"{gameObject.name} Updating current zone.");
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

    protected virtual void IdleWander()
    {
        if (currentZone == null) return;
        Vector3 target = currentZone.GetRandomPointInZone();

        UnityEngine.AI.NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    protected virtual IEnumerator WaitForArrivalAndUpdateZone()
    {
        // Wait until the agent reaches its destination
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            yield return null;

        // Optionally, wait until the agent fully stops
        while (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            yield return null;

        TryFireTriggerByName("ReachZone");
        UpdateCurrentZone();
        zoneArrivalCoroutine = null;
    }

    protected virtual void OnEnterRelocate()
    {
        SetEnemyColor(patrolColor);
        Zone[] otherZones = GetOtherZones();
        if (otherZones.Length == 0)
        {
            // No other zones to relocate to, transition back to Idle
            TryFireTriggerByName("ReachZone");
            return;
        }

        // Pick a random other zone and set as target
        Zone targetZone = otherZones[Random.Range(0, otherZones.Length)];
        // Move to a random point in the target zone
        Vector3 target = targetZone.GetRandomPointInZone();
        UnityEngine.AI.NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            // Start a coroutine to check when destination is reached
            if (zoneArrivalCoroutine != null)
                StopCoroutine(zoneArrivalCoroutine);
            zoneArrivalCoroutine = StartCoroutine(WaitForArrivalAndUpdateZone());
        }
    }

    // --- HEALTH MANAGEMENT METHODS ---
    public virtual void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        CheckHealthThreshold();
    }

    protected virtual void OnEnterRecover()
    {
        if (recoverCoroutine != null)
            StopCoroutine(recoverCoroutine);
        recoverCoroutine = StartCoroutine(RecoverHealthOverTime());
    }

    protected virtual IEnumerator RecoverHealthOverTime()
    {
        float targetHealth = maxHealth * 0.8f;
        float recoverRate = 0.1f; // 10% of missing health per second (adjust as needed)

        while (currentHealth < targetHealth)
        {
            float missing = maxHealth - currentHealth;
            float delta = recoverRate * missing * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + delta, targetHealth);
            yield return null;
        }

        // Fire the RecoveredHealth trigger when done
        TryFireTriggerByName("RecoveredHealth");

        recoverCoroutine = null;
    }

    protected virtual void CheckHealthThreshold()
    {
        if (!handleLowHealth)
            return;

        if (!hasFiredLowHealth && currentHealth <= maxHealth * lowHealthThresholdPercent)
        {
            hasFiredLowHealth = true;
            TryFireTriggerByName("LowHealth");
        }
    }

    protected virtual void OnRecoveredHealth()
    {
        hasFiredLowHealth = false;
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
    protected bool TryFireTriggerByName(string triggerName)
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
                if (!enemyAI.State.Equals((TState)System.Enum.Parse(typeof(TState), "Chase")))
                {
                    TryFireTriggerByName("SeePlayer");
                }
            }
        }
    }

    protected virtual void MoveToAttackRange(Transform player)
    {
        // Get direction from enemy to player
        Vector3 direction = (player.position - transform.position).normalized;

        // Calculate reach: half the box's depth (z) plus the offset distance in front of the enemy
        // Subtract a small buffer to move a little closer
        // This is to prevent stopping just before the attack box edge
        float chaseBuffer = 0.2f;
        float reach = (Mathf.Max(attackBoxSize.x, attackBoxSize.z) * 0.5f) + attackBoxDistance - chaseBuffer;

        // Position the enemy so the player is just inside the front face of the attack box
        Vector3 targetPosition = player.position - direction * reach;

        // Keep the target position at the same Y as the enemy (for ground-based movement)
        targetPosition.y = transform.position.y;

        agent.SetDestination(targetPosition);
    }

    protected virtual IEnumerator AttackLoop()
    {
        while (enemyAI.State.Equals((TState)System.Enum.Parse(typeof(TState), "Attack")))
        {
            // Calculate the world position and rotation for the attack box
            Vector3 boxCenter = transform.position + transform.forward * attackBoxDistance;
            Vector3 boxHalfExtents = attackBoxSize * 0.5f;
            Quaternion boxRotation = transform.rotation;

            // Check if player is within the attack box bounds before enabling
            bool playerInAttackBox = false;
            Collider[] hits = Physics.OverlapBox(
                boxCenter,
                boxHalfExtents,
                boxRotation
            );

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    playerInAttackBox = true;
                    break;
                }
            }

            if (playerInAttackBox)
            {
                isAttackBoxActive = true;
                attackCollider.enabled = true;
                SetEnemyColor(hitboxActiveColor);
                yield return new WaitForSeconds(attackActiveDuration);
                isAttackBoxActive = false;
                attackCollider.enabled = false;
                SetEnemyColor(attackColor);
            }
            else
            {
                isAttackBoxActive = false;
                attackCollider.enabled = false;
                SetEnemyColor(attackColor);
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(attackInterval);
        }
        isAttackBoxActive = false;
        attackCollider.enabled = false;
        SetEnemyColor(attackColor);
    }

    protected virtual void OnEnterChase()
    {
        SetEnemyColor(chaseColor);
    }

    protected virtual void OnEnterAttack()
    {
        SetEnemyColor(attackColor);
        if (attackLoopCoroutine != null)
            StopCoroutine(attackLoopCoroutine);
        attackLoopCoroutine = StartCoroutine(AttackLoop());
    }

    protected virtual void OnExitAttack()
    {
        if (attackLoopCoroutine != null)
            StopCoroutine(attackLoopCoroutine);
        attackCollider.enabled = false;
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

public enum EnemyState
{
    Idle,           // My idea is that when in Idle, the enemy is moving around a section of the map (zone)
                    // so it is idle in the sense that it is not actively searching for the player

    Relocate,       // Relocate is a substate of Patrol where the enemy moves to a new area or waypoint
                    // before transitioning to Idle. This could be used when the enemy loses sight of the player
                    // and needs to move to a different location to search.

    Patrol,         // Patrol is the main state where the enemy is actively moving from zone to zone.
                    // It contains the shared trigger of seeing the player to transition to Chase.

    Reinforcements, // This state would be used when another enemy calls for help

    Chase,          // Chase is when the enemy has detected the player and is actively pursuing them.

    Attack,         // Attack is when the enemy is in range to attack the player.

    Flee,           // Flee is when the enemy is low on health and tries to escape from the player.

    Fled,           // Fled is when the enemy has successfully escaped (out of attack range) and is no longer in immediate danger.

    Recover         // Recover is when the enemy is regaining health passively while idle.
}

public enum EnemyTrigger
{
    SeePlayer,         // Within a certain detection radius or line of sight

    LosePlayer,        // Out of detection radius or line of sight for a certain time

    AidRequested,      // Another enemy has called for help

    FailedAid,         // The aid request was unsuccessful (e.g., arrived and player was gone)
                       // It would start a timer to relocate after a certain time after arriving if no player is seen

    LowHealth,         // I was thinking LowHealth would be like 20-25% of max health

    RecoveredHealth,   // We discussed whether or not enemies should have passive health regen
                       // I think it could be interesting if they do, but it should be slow and only while idle
                       // So I am including functionality for it for now

    InAttackRange,     // These ranges are based on the enemy's attack range, and not the player's
    OutOfAttackRange,  // Therefore, they may need to be adjusted for how they interact with fleeing behavior

    ReachZone,         // Reached the new zone after relocating

    IdleTimerElapsed,  // Timer for how long the enemy has been idle before relocating

    Attacked           // The enemy has been attacked by the player
}

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
            .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Chase) // If the player moves out of range, it chases again
            .Permit(EnemyTrigger.LowHealth, EnemyState.Flee);  // If low on health, it flees

        sm.Configure(EnemyState.Flee) // This state can be used for unique fleeing behavior like calling for reinforcements or defensive manuevers
            .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Fled); // Once out of range, it goes to Fled

        sm.Configure(EnemyState.Fled)
            .Permit(EnemyTrigger.InAttackRange, EnemyState.Flee) // If the player comes back into range, it goes back to Flee
            .Permit(EnemyTrigger.LosePlayer, EnemyState.Relocate); // If it loses the player while fleeing, it relocates

        sm.Configure(EnemyState.Recover)  // Essentially the same as Idle but with health regen, maybe no movement at all
            .SubstateOf(EnemyState.Patrol) // Recover is a substate of Patrol
            .Permit(EnemyTrigger.RecoveredHealth, EnemyState.Idle); // Once it recovers health, it goes back to Idle
    }
}