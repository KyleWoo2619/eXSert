using UnityEngine;
using System.Collections;
using Behaviors;

public class BoxerEnemy : BaseEnemy<EnemyState, EnemyTrigger>
{
    private IEnemyStateBehavior idleBehavior;
    private IEnemyStateBehavior relocateBehavior;
    private IEnemyStateBehavior chaseBehavior;
    private IEnemyStateBehavior attackBehavior;
    private IEnemyStateBehavior recoverBehavior;
    private IEnemyStateBehavior deathBehavior;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Animation parameter names (match your Animator Controller)
    private const string MOVE_TRIGGER = "Move";
    private const string ATTACK_TRIGGER = "Attack";
    private const string IS_MOVING = "IsMoving";
    private const string IS_ATTACKING = "IsAttacking";

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

        idleBehavior = new IdleBehavior();
        relocateBehavior = new RelocateBehavior();
        recoverBehavior = new RecoverBehavior();
        chaseBehavior = new ChaseBehavior();
        attackBehavior = new AttackBehavior();
        deathBehavior = new DeathBehavior();

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

        // Initialize health bar if prefab is assigned
        if (healthBarPrefab)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform); // parent to enemy
            healthBarInstance.transform.localPosition = Vector3.zero;    // script adds offset
            healthBarInstance.SetEnemy(this);
            Debug.Log($"{gameObject.name}: Health bar successfully initialized and parented.");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: healthBarPrefab not assigned.");
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
                PlayMoveAnimation();
                chaseBehavior.OnEnter(this);
            })
            .OnExit(() => {
                StopMoveAnimation();
                chaseBehavior.OnExit(this);
            })
            .Ignore(EnemyTrigger.SeePlayer);

        // --- ATTACK STATE ---
        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => {
                // Don't play animation here - it's triggered per attack cycle in AttackBehavior
                attackBehavior.OnEnter(this);
            })
            .OnExit(() => {
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
        
        animator.SetBool(IS_MOVING, true);
        animator.SetBool(IS_ATTACKING, false);
        animator.SetTrigger(MOVE_TRIGGER);
        Debug.Log($"{gameObject.name}: Playing Move Animation");
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
        
        animator.SetBool(IS_ATTACKING, true);
        animator.SetBool(IS_MOVING, false);
        animator.SetTrigger(ATTACK_TRIGGER);
        Debug.Log($"{gameObject.name}: Playing Attack Animation");
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

    protected override void Update()
    {
        base.Update();
    }
}
