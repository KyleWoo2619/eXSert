using System.Collections;
using System.Collections.Generic;
using Stateless;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// BaseEnemy is generic so derived classes can define their own states and triggers
public abstract class BaseEnemy<TState, TTrigger> : MonoBehaviour, IHealthSystem
    where TState : struct, System.Enum
    where TTrigger : struct, System.Enum
{
    [HideInInspector]
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
    [SerializeField, Tooltip("Vertical offset (in meters) applied to the attack box center.")]
    public float attackBoxHeightOffset = 0f;
    [SerializeField, Tooltip("Time in seconds between attacks (attack cooldown).")]
    public float attackInterval = 1.0f;
    [SerializeField, Tooltip("Time in seconds the attack box is enabled (attack active duration).")]
    public float attackActiveDuration = 0.5f;
    [SerializeField, Tooltip("Show the attack range gizmo in the Scene view.")]
    protected bool showAttackGizmo = true;

    [Header("Enemy Health Bar")]
    [SerializeField, Tooltip("Prefab for the enemy's health bar UI.")]
    public GameObject healthBarPrefab;
    [SerializeField, Tooltip("Optional anchor transform for the health bar instance. Defaults to this enemy's transform.")]
    private Transform healthBarAnchor;

    // Non-serialized fields
    [HideInInspector]
    public EnemyHealthBar healthBarInstance;
    protected SphereCollider detectionCollider;
    [HideInInspector]
    public BoxCollider attackCollider;

    [Header("Trigger Overrides")]
    [SerializeField, Tooltip("Optional detection trigger. Leave empty to auto-create a trigger that matches Detection Range.")]
    private SphereCollider detectionColliderOverride;
    [SerializeField, Tooltip("Optional melee attack trigger. Leave empty to auto-create a trigger that matches Attack Box settings.")]
    private BoxCollider attackColliderOverride;
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
    protected Animator animator;

    private bool deathSequenceTriggered;
    private Coroutine deathFallbackRoutine;

    [Header("External Helper Roots")]
    [SerializeField, Tooltip("Any helper GameObjects that live outside this enemy's hierarchy (IK targets, FX anchors, etc.). They will be destroyed automatically when this enemy is destroyed.")]
    private List<GameObject> externalHelperRoots = new();

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Animator state name used when forcing the idle pose.")]
    private string idleStateName = "Idle";
    [SerializeField, Tooltip("Animator state used when no locomotion parameter exists.")]
    private string locomotionStateName = "Locomotion";
    [SerializeField, Tooltip("Animator state used as a fallback for attack.")]
    private string attackStateName = "Attack";
    [SerializeField, Tooltip("Animator state used as a fallback for hit reactions.")]
    private string hitStateName = "Hit";
    [SerializeField, Tooltip("Animator state used as a fallback for death.")]
    private string dieStateName = "Die";
    [SerializeField, Tooltip("Animator trigger name for attack (optional).")]
    private string attackTriggerName = "Attack";
    [SerializeField, Tooltip("Animator trigger name for hit reactions (optional).")]
    private string hitTriggerName = "Hit";
    [SerializeField, Tooltip("Animator trigger name for death (optional).")]
    private string dieTriggerName = "Die";
    [SerializeField, Tooltip("Animator float parameter name for locomotion speed (optional).")]
    private string moveSpeedParameterName = "MoveSpeed";
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

    /// <summary>
    /// Ensures an EnemyHealthBar exists and is bound to this enemy's IHealthSystem implementation.
    /// Prefers an already-assigned instance (e.g., from the prefab hierarchy) before instantiating a new one.
    /// </summary>
    protected void EnsureHealthBarBinding()
    {
        if (healthBarInstance == null && healthBarPrefab != null)
        {
            Transform parent = healthBarAnchor != null ? healthBarAnchor : transform;
            GameObject instance = Instantiate(healthBarPrefab, parent);
            healthBarInstance = instance.GetComponent<EnemyHealthBar>();
            if (healthBarInstance == null)
            {
                Debug.LogWarning($"[{name}] Health bar prefab is missing the EnemyHealthBar component.");
                Destroy(instance);
            }
        }

        if (healthBarInstance != null)
        {
            healthBarInstance.BindToHealthSystem(this);
        }
        else if (healthBarPrefab == null)
        {
            Debug.LogWarning($"[{name}] No healthBarPrefab assigned; enemy health will not be displayed.");
        }
    }

    // Awake is called when the script instance is being loaded
    protected virtual void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        agent = this.gameObject.GetComponent<NavMeshAgent>();

        EnsureRigidBodyForTriggers();
        EnsureDetectionCollider();
        EnsureAttackCollider();
        EnsurePlayerTargetReference();

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }
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
    public void SetEnemyColor(Color color)
    {
        if (enemyRenderer != null)
            enemyRenderer.material.color = color;
    }

    // --- ANIMATION API ---
    protected virtual void PlayIdleAnim()
    {
        PlayState(idleStateName);
    }

    protected void RegisterExternalHelper(GameObject helper)
    {
        if (helper == null)
            return;

        if (!externalHelperRoots.Contains(helper))
            externalHelperRoots.Add(helper);
    }

    protected void UnregisterExternalHelper(GameObject helper)
    {
        if (helper == null)
            return;

        externalHelperRoots.Remove(helper);
    }

    private void CleanupExternalHelpers()
    {
        foreach (var helper in externalHelperRoots)
        {
            if (helper != null)
                Destroy(helper);
        }

        externalHelperRoots.Clear();
    }

    private void EnsureRigidBodyForTriggers()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        rb.isKinematic = true;
    }

    private void EnsureDetectionCollider()
    {
        detectionCollider = detectionColliderOverride ?? detectionCollider;

        if (detectionCollider == null)
        {
            // Prefer an existing trigger sphere on this GameObject
            detectionCollider = GetComponent<SphereCollider>();
            if (detectionCollider != null && !detectionCollider.isTrigger)
            {
                detectionCollider = null;
            }
        }

        if (detectionCollider == null)
        {
            var detectionRoot = new GameObject("DetectionTrigger");
            detectionRoot.transform.SetParent(transform);
            detectionRoot.transform.localPosition = Vector3.zero;
            detectionRoot.transform.localRotation = Quaternion.identity;
            detectionRoot.transform.localScale = Vector3.one;
            detectionRoot.layer = gameObject.layer;
            detectionCollider = detectionRoot.AddComponent<SphereCollider>();
            detectionColliderOverride = detectionCollider;
        }

        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRange;
    }

    private void EnsureAttackCollider()
    {
        attackCollider = attackColliderOverride ?? attackCollider;

        if (attackCollider == null)
        {
            // Prefer an existing trigger box on this GameObject
            attackCollider = GetComponent<BoxCollider>();
            if (attackCollider != null && !attackCollider.isTrigger)
            {
                attackCollider = null;
            }
        }

        if (attackCollider == null)
        {
            var attackRoot = new GameObject("AttackTrigger");
            attackRoot.transform.SetParent(transform);
            attackRoot.transform.localPosition = Vector3.zero;
            attackRoot.transform.localRotation = Quaternion.identity;
            attackRoot.transform.localScale = Vector3.one;
            attackRoot.layer = gameObject.layer;
            attackCollider = attackRoot.AddComponent<BoxCollider>();
            attackColliderOverride = attackCollider;
        }

        attackCollider.isTrigger = true;
        attackCollider.size = attackBoxSize;
        attackCollider.center = new Vector3(0f, attackBoxHeightOffset, attackBoxDistance);
        attackCollider.enabled = false;
    }

    private void EnsurePlayerTargetReference()
    {
        if (playerTarget != null && playerTarget.gameObject != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void EnsurePlayerTargetReference(Transform candidate)
    {
        if (candidate == null)
            return;

        if (playerTarget == null || playerTarget.gameObject == null || !playerTarget.gameObject.activeInHierarchy)
        {
            playerTarget = candidate;
        }
    }

    protected virtual void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        CleanupExternalHelpers();
    }

    protected virtual void PlayLocomotionAnim(float moveSpeed)
    {
        if (animator == null)
            return;

        // Drive additive locomotion overlays when the optional IsMoving bool exists
        if (AnimatorHasParameter("IsMoving", AnimatorControllerParameterType.Bool))
        {
            bool isMoving = moveSpeed > 0.05f;
            animator.SetBool("IsMoving", isMoving);
        }

        if (!string.IsNullOrEmpty(moveSpeedParameterName) && AnimatorHasParameter(moveSpeedParameterName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(moveSpeedParameterName, moveSpeed);
        }
        else
        {
            PlayState(locomotionStateName);
        }
    }

    protected virtual void PlayAttackAnim()
    {
        if (!TrySetTrigger(attackTriggerName))
        {
            PlayState(attackStateName);
        }
    }

    protected virtual void PlayHitAnim()
    {
        if (!TrySetTrigger(hitTriggerName))
        {
            PlayState(hitStateName);
        }
    }

    protected virtual void PlayDieAnim()
    {
        if (!TrySetTrigger(dieTriggerName))
        {
            PlayState(dieStateName);
        }
    }

    private bool TrySetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return false;

        if (!AnimatorHasParameter(triggerName, AnimatorControllerParameterType.Trigger))
            return false;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
        return true;
    }

    private bool AnimatorHasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }
        return false;
    }

    private void PlayState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        animator.Play(stateName, 0, 0f);
    }

    public virtual void TriggerAttackAnimation()
    {
        PlayAttackAnim();
    }

    protected virtual void OnDamageTaken(float amount)
    {
        if (currentHealth > 0f)
        {
            PlayHitAnim();
        }
    }

    public virtual void UpdateCurrentZone()
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
    // --- HEALTH MANAGEMENT METHODS ---
    // --- IHealthSystem Implementation ---
    // Property for currentHP (read-only for interface, uses currentHealth internally)
    public float currentHP => currentHealth;

    // Property for maxHP (read-only for interface, uses maxHealth internally)
    public float maxHP => maxHealth;

    // LoseHP is called to apply damage to the enemy
    public void LoseHP(float damage)
    {
        if (damage <= 0f)
            return;

        float previousHealth = currentHealth;
        SetHealth(currentHealth - damage);

        float actualDamage = Mathf.Max(0f, previousHealth - currentHealth);
        if (actualDamage > 0f)
        {
            OnDamageTaken(actualDamage);
        }
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
        if (currentHealth > 0f && deathSequenceTriggered)
        {
            deathSequenceTriggered = false;
            if (deathFallbackRoutine != null)
            {
                StopCoroutine(deathFallbackRoutine);
                deathFallbackRoutine = null;
            }
        }
        CheckHealthThreshold();
    }

    public virtual void CheckHealthThreshold()
    {
        if (currentHealth <= 0f)
        {
            if (!deathSequenceTriggered)
            {
                deathSequenceTriggered = true;
                bool fired = TryFireTriggerByName("Die");
                if (!fired)
                {
                    BeginDeathFallback();
                }
            }
            return;
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

    private void BeginDeathFallback()
    {
        if (deathFallbackRoutine != null)
            return;

        deathFallbackRoutine = StartCoroutine(DeathFallbackRoutine());
    }

    private IEnumerator DeathFallbackRoutine()
    {
        PlayDieAnim();
        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        yield return new WaitForSeconds(3f);

        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance.gameObject);
            healthBarInstance = null;
        }

        CleanupExternalHelpers();
        deathFallbackRoutine = null;
        Destroy(gameObject);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (enemyAI == null)
            return;

        if (other.CompareTag("Player"))
        {
            EnsurePlayerTargetReference(other.transform);
            Debug.Log($"{gameObject.name} OnTriggerEnter with Player! Collider: {other.name}");
            
            // Check which collider is currently triggering this event
            // This works because OnTriggerEnter is called for each trigger collider on the GameObject
            // Use Physics.OverlapSphere or OverlapBox if you need to check proximity

            // Check if this is the detection collider
            if (detectionCollider != null && detectionCollider.enabled && detectionCollider.bounds.Contains(other.transform.position))
            {
                Debug.Log($"{gameObject.name} Player is inside detection bounds! Trying to fire SeePlayer trigger...");
                if (TryFireTriggerByName("SeePlayer"))
                {
                    Debug.Log($"{gameObject.name} detected player (detection collider) - State should change to Chase!");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name} FAILED to fire SeePlayer trigger! Current state: {enemyAI.State}");
                }
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} Player entered trigger but NOT in detection bounds. Detection enabled: {detectionCollider?.enabled}, Player pos: {other.transform.position}");
            }
            
            // Check if this is the attack collider
            if (attackCollider != null && attackCollider.enabled && attackCollider.bounds.Contains(other.transform.position))
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
            EnsurePlayerTargetReference(other.transform);
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
            boxCenter += Vector3.up * attackBoxHeightOffset;
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
        var detectionRef = detectionCollider != null ? detectionCollider : detectionColliderOverride;
        if (detectionRef != null)
            detectionRef.radius = detectionRange;

        var attackRef = attackCollider != null ? attackCollider : attackColliderOverride;
        if (attackRef != null)
        {
            attackRef.size = attackBoxSize;
            attackRef.center = new Vector3(0f, attackBoxHeightOffset, attackBoxDistance);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlayerTargetReference();
    }
}

#region States and Triggers
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

    Recover,        // Recover is when the enemy is regaining health passively while idle.

    Death           // Death is when the enemy has been defeated and is no longer active.
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

    Attacked,           // The enemy has been attacked by the player

    Die                 // The enemy has been defeated
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
            //.Permit(EnemyTrigger.LowHealth, EnemyState.Flee);  // If low on health, it flees
            // Commented out until fleeing behavior is functional, or if we even want to use it at all

        sm.Configure(EnemyState.Flee) // This state can be used for unique fleeing behavior like calling for reinforcements or defensive manuevers
            .Permit(EnemyTrigger.OutOfAttackRange, EnemyState.Fled); // Once out of range, it goes to Fled

        sm.Configure(EnemyState.Fled)
            .Permit(EnemyTrigger.InAttackRange, EnemyState.Flee) // If the player comes back into range, it goes back to Flee
            .Permit(EnemyTrigger.LosePlayer, EnemyState.Relocate); // If it loses the player while fleeing, it relocates

        sm.Configure(EnemyState.Recover)  // Essentially the same as Idle but with health regen, maybe no movement at all
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
