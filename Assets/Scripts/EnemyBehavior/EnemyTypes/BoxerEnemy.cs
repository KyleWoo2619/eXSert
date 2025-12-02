using UnityEngine;
using System.Collections;
using Behaviors;

public class BoxerEnemy : BaseEnemy<EnemyState, EnemyTrigger>
{
    private IEnemyStateBehavior<EnemyState, EnemyTrigger> idleBehavior, relocateBehavior, chaseBehavior, attackBehavior, recoverBehavior, deathBehavior;

    protected Coroutine lookAtPlayerCoroutine;
    protected Coroutine chaseCoroutine;
    protected Coroutine attackRangeMonitorCoroutine;

    protected override void Awake()
    {
        base.Awake();

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
        
        EnsureHealthBarBinding();
    }

    protected override void ConfigureStateMachine()
    {
        base.ConfigureStateMachine();

        Debug.Log($"{gameObject.name} ConfigureStateMachine called");
        EnemyStateMachineConfig.ConfigureBasic(enemyAI);

        enemyAI.Configure(EnemyState.Idle)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry lambda for Idle called");
                PlayIdleAnim();
                idleBehavior.OnEnter(this);
            })
            .OnExit(() => {
                idleBehavior.OnExit(this);
            });

        enemyAI.Configure(EnemyState.Relocate)
            .OnEntry(() => {
                PlayLocomotionAnim(agent != null ? agent.velocity.magnitude : 1f);
                relocateBehavior.OnEnter(this);
            })
            .OnExit(() => {
                PlayIdleAnim();
                relocateBehavior.OnExit(this);
            });

        enemyAI.Configure(EnemyState.Recover)
            .OnEntry(() => {
                PlayIdleAnim(); // or create a recover animation
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
                PlayLocomotionAnim(agent != null ? agent.velocity.magnitude : 1f);
                chaseBehavior.OnEnter(this);
            })
            .OnExit(() => {
                Debug.Log($"{gameObject.name}: EXITING Chase state");
                PlayIdleAnim();
                chaseBehavior.OnExit(this);
            })
            .Ignore(EnemyTrigger.SeePlayer);

        // --- ATTACK STATE ---
        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name}: ENTERING Attack state");
                // Play attack animation immediately when entering attack state
                PlayAttackAnim();
                attackBehavior.OnEnter(this);
            })
            .OnExit(() => {
                Debug.Log($"{gameObject.name}: EXITING Attack state");
                PlayIdleAnim();
                attackBehavior.OnExit(this);
            })
            .Ignore(EnemyTrigger.SeePlayer);

        // --- DEATH STATE ---
        enemyAI.Configure(EnemyState.Death)
            .OnEntry(() => {
                PlayDieAnim();
                deathBehavior.OnEnter(this);
            })
            .Ignore(EnemyTrigger.SeePlayer)
            .Ignore(EnemyTrigger.LowHealth);
    }
}


