using UnityEngine;
using System.Collections;
using Behaviors;

public class BoxerEnemy : BaseEnemy<EnemyState, EnemyTrigger>
{
    private IEnemyStateBehavior<EnemyState, EnemyTrigger> idleBehavior, relocateBehavior, chaseBehavior, attackBehavior, recoverBehavior, deathBehavior;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Animation parameter names (match your Animator Controller)
    private const string MOVE_TRIGGER = "Move";
    private const string ATTACK_TRIGGER = "Attack";
    private const string IS_MOVING = "IsMoving";
    private const string IS_ATTACKING = "IsAttacking";
    
    // Animation timing
    [SerializeField] private float attackAnimationDuration = 2.0f; // Match your slow attack animation
    [SerializeField] private bool forceAnimationTransitions = true; // Force immediate animation changes

    protected Coroutine lookAtPlayerCoroutine;
    protected Coroutine chaseCoroutine;
    protected Coroutine attackRangeMonitorCoroutine;

    protected override void Awake()
    {
        base.Awake();
        
        // Get animator component
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (animator == null)
            Debug.LogError($"{gameObject.name}: No Animator component found!");

        idleBehavior = new IdleBehavior<EnemyState, EnemyTrigger>();
        relocateBehavior = new RelocateBehavior<EnemyState, EnemyTrigger>();
        recoverBehavior = new RecoverBehavior<EnemyState, EnemyTrigger>();
        chaseBehavior = new ChaseBehavior<EnemyState, EnemyTrigger>();
        attackBehavior = new AttackBehavior<EnemyState, EnemyTrigger>();
        deathBehavior = new DeathBehavior<EnemyState, EnemyTrigger>();

        Debug.Log($"{gameObject.name} Awake called");

        // Find the player by tag (if not set elsewhere)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            PlayerTarget = playerObj.transform;
    }

    protected virtual void Start()
    {
        InitializeStateMachine(EnemyState.Idle);
        ConfigureStateMachine();
        Debug.Log($"{gameObject.name} State machine initialized");
        
        if (enemyAI.State.Equals(EnemyState.Idle))
        {
            Debug.Log($"{gameObject.name} Manually calling OnEnterIdle for initial Idle state");
            idleBehavior.OnEnter(this);
        }

        // Initialize health system - ensure current health is set to max health
        currentHealth = maxHealth;
        
        // Initialize health bar using existing Canvas on this enemy
        healthBarInstance = GetComponentInChildren<EnemyHealthBar>();
        if (healthBarInstance != null)
        {
            // Use BaseEnemy's IHealthSystem implementation directly
            healthBarInstance.SetEnemy(this);
            Debug.Log($"{gameObject.name}: Health bar initialized with BaseEnemy IHealthSystem. Health: {currentHealth}/{maxHealth}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No EnemyHealthBar component found in children. Make sure there's a Canvas with EnemyHealthBar script attached.");
        }
    }

    protected override void ConfigureStateMachine()
    {
        base.ConfigureStateMachine();

        Debug.Log($"{gameObject.name} ConfigureStateMachine called");
        EnemyStateMachineConfig.ConfigureBasic(enemyAI);

        enemyAI.Configure(EnemyState.Idle)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry lambda for Idle called");
                PlayIdleAnimation();
                idleBehavior.OnEnter(this);
            })
            .OnExit(() => {
                idleBehavior.OnExit(this);
            });

        enemyAI.Configure(EnemyState.Relocate)
            .OnEntry(() => {
                PlayMoveAnimation();
                relocateBehavior.OnEnter(this);
            })
            .OnExit(() => {
                StopMoveAnimation();
                relocateBehavior.OnExit(this);
            });

        enemyAI.Configure(EnemyState.Recover)
            .OnEntry(() => {
                PlayIdleAnimation(); // or create a recover animation
                recoverBehavior.OnEnter(this);
            })
            .OnExit(() => {
                recoverBehavior.OnExit(this);
            });

        // --- CHASE STATE ---
        enemyAI.Configure(EnemyState.Chase)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name}: ENTERING Chase state");
                // Force immediate transition to move animation (especially when coming from Attack state)
                ForcePlayMoveAnimation();
                chaseBehavior.OnEnter(this);
            })
            .OnExit(() => {
                Debug.Log($"{gameObject.name}: EXITING Chase state");
                StopMoveAnimation();
                chaseBehavior.OnExit(this);
            })
            .Ignore(EnemyTrigger.SeePlayer);

        // --- ATTACK STATE ---
        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name}: ENTERING Attack state");
                // Play attack animation immediately when entering attack state
                PlayAttackAnimation();
                attackBehavior.OnEnter(this);
            })
            .OnExit(() => {
                Debug.Log($"{gameObject.name}: EXITING Attack state");
                StopAttackAnimation();
                attackBehavior.OnExit(this);
            })
            .Ignore(EnemyTrigger.SeePlayer);

        // --- DEATH STATE ---
        enemyAI.Configure(EnemyState.Death)
            .OnEntry(() => {
                PlayDeathAnimation(); // You'll need to add this animation later
                deathBehavior.OnEnter(this);
            })
            .Ignore(EnemyTrigger.SeePlayer)
            .Ignore(EnemyTrigger.LowHealth);
    }

    // Animation control methods
    private void PlayIdleAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(IS_MOVING, false);
        animator.SetBool(IS_ATTACKING, false);
        Debug.Log($"{gameObject.name}: Playing Idle Animation");
    }

    private void PlayMoveAnimation()
    {
        if (animator == null) return;
        
        // Force immediate transition if needed
        if (forceAnimationTransitions)
        {
            animator.Play("BoxingBot_Move", 0, 0f); // Play from beginning
        }
        
        animator.SetBool(IS_MOVING, true);
        animator.SetBool(IS_ATTACKING, false);
        animator.SetTrigger(MOVE_TRIGGER);
        Debug.Log($"{gameObject.name}: Playing Move Animation (BoxingBot_Move)");
    }

    private void StopMoveAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(IS_MOVING, false);
        Debug.Log($"{gameObject.name}: Stopping Move Animation");
    }

    private void PlayAttackAnimation()
    {
        if (animator == null) return;
        
        // Force immediate transition to attack animation (cancel whatever is playing)
        if (forceAnimationTransitions)
        {
            animator.Play("BoxingBot_Attack", 0, 0f); // Play from beginning immediately
        }
        
        animator.SetBool(IS_ATTACKING, true);
        animator.SetBool(IS_MOVING, false);
        animator.SetTrigger(ATTACK_TRIGGER);
        Debug.Log($"{gameObject.name}: Playing Attack Animation (BoxingBot_Attack) - Duration: {attackAnimationDuration}s");
    }

    private void StopAttackAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool(IS_ATTACKING, false);
        Debug.Log($"{gameObject.name}: Stopping Attack Animation");
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;
        
        // You'll add this when you have a death animation
        animator.SetBool(IS_MOVING, false);
        animator.SetBool(IS_ATTACKING, false);
        Debug.Log($"{gameObject.name}: Playing Death Animation (not implemented yet)");
    }

    // Public method to trigger attack animation from behavior classes
    public void TriggerAttackAnimation()
    {
        Debug.Log($"{gameObject.name}: TriggerAttackAnimation called");
        PlayAttackAnimation();
    }
    
    // Public method to get attack animation duration for behaviors
    public float GetAttackAnimationDuration()
    {
        return attackAnimationDuration;
    }
    
    // Public method to immediately switch to move animation (for quick transitions)
    public void ForcePlayMoveAnimation()
    {
        Debug.Log($"{gameObject.name}: ForcePlayMoveAnimation called - switching to BoxingBot_Move");
        PlayMoveAnimation();
    }

    // protected override void Update()
    // {
    //     base.Update();
        
    //     // Debug animation state (can be disabled in production)
    //     if (Input.GetKeyDown(KeyCode.B) && gameObject.name.Contains("Boxer")) // Press B to debug this boxer
    //     {
    //         DebugAnimationState();
    //     }
    // }
    
    [ContextMenu("Debug Animation State")]
    public void DebugAnimationState()
    {
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: No animator found!");
            return;
        }
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"{gameObject.name} Animation Debug:");
        Debug.Log($"  Current State: {stateInfo.shortNameHash}");
        Debug.Log($"  Is Moving: {animator.GetBool(IS_MOVING)}");
        Debug.Log($"  Is Attacking: {animator.GetBool(IS_ATTACKING)}");
        Debug.Log($"  AI State: {enemyAI.State}");
        Debug.Log($"  Attack Duration Setting: {attackAnimationDuration}s");
    }
}
