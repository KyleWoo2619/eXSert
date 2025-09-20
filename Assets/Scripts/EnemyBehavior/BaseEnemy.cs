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

    // Expose the current state in the Inspector (read-only)
    [SerializeField] private TState currentState;

    // Health properties // THIS IS NOT CURRENTLY USING BRANDON'S HEALTH UI SYSTEM
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField, MaxHealthSlider]
    protected float currentHealth = 100f;
    [SerializeField] protected float lowHealthThresholdPercent = 0.25f;
    protected bool hasFiredLowHealth = false;
    protected Coroutine recoverCoroutine;

    // Zone management
    [SerializeField] public Zone currentZone;
    private Vector3 lastZoneCheckPosition;

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
    protected virtual void OnEnterIdle()
    {
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

    protected Coroutine idleTimerCoroutine;
    [SerializeField]
    protected float idleTimerDuration = 15f;

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

    protected Coroutine idleWanderCoroutine;

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

    // Simple wandering behavior within the current zone
    protected Coroutine zoneArrivalCoroutine;

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

    [SerializeField]
    protected bool handleLowHealth = true; // Toggle to enable/disable low health behavior (fleeing, recovering, etc.)

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

    /* 
       // Phase-based behavior (optional), we can use this later for bosses or special enemies
       // For a boss, you can override CheckHealthThreshold() and/or SetHealth() in the derived class to implement custom phase logic, 
       // ignoring the base low health logic if desired. Instead of doing this V
    [SerializeField]
    protected float[] phaseThresholdPercents = new float[] { 0.75f, 0.5f, 0.25f };
    protected int currentPhase = 0;

    protected virtual void CheckPhaseThresholds()
    {
        // Example: phase triggers named "Phase1", "Phase2", etc.
        while (currentPhase < phaseThresholdPercents.Length &&
               currentHealth <= maxHealth * phaseThresholdPercents[currentPhase])
        {
            string triggerName = $"Phase{currentPhase + 1}";
            if (System.Enum.TryParse(triggerName, out TTrigger phaseTrigger))
            {
                enemyAI.Fire(phaseTrigger);
            }
            else
            {
                Debug.LogWarning($"{typeof(TTrigger).Name} does not contain a '{triggerName}' trigger.");
            }
            currentPhase++;
        }
    }
    */

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