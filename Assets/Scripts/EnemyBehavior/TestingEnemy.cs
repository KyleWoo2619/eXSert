// PLEASE DO NOT DELETE THIS SCRIPT
// USE THIS AS A TEMPLATE/REFERENCE FOR CREATING NEW ENEMY TYPES
// AND/OR TO TEST OUT NEW BEHAVIORAL FEATURES
// IF AN ENEMY TYPE WILL BE USING DIFFERENT OR MORE/LESS STATES/TRIGGERS THAN THE BASE ENEMY CLASS
// THEN YOU WILL NEED TO CREATE NEW STATE AND TRIGGER ENUMS IN THAT ENEMY SCRIPT

using UnityEngine;
using System.Collections;
using Behaviors;

// This is an example of how to create custom states and triggers for a specific enemy type
// You would still need to re-implement all the reused states/triggers in the new enums
/*
public enum TestingEnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    SpecialMove
}

public enum TestingEnemyTrigger
{
    SeePlayer,
    LosePlayer,
    LowHealth,
    RecoveredHealth,
    PlayerInRange,
    PlayerOutOfRange,
    PlayerLowHealth
}
*/

// Example derived enemy class with custom states and triggers
// You would replace EnemyState and/or EnemyTrigger with your custom enums if needed
// This also means that you would need to implement all of the state machine configurations in this class
public class TestingEnemy : BaseEnemy<EnemyState, EnemyTrigger>
{
    private IEnemyStateBehavior idleBehavior;
    private IEnemyStateBehavior relocateBehavior;
    private IEnemyStateBehavior chaseBehavior;
    private IEnemyStateBehavior attackBehavior;
    private IEnemyStateBehavior recoverBehavior;
    private IEnemyStateBehavior deathBehavior;

    [SerializeField]
    private GameObject healthBarPrefab; // Reference to the health bar prefab

    // Reference to the player (set this appropriately in your game)

    protected Coroutine lookAtPlayerCoroutine;
    protected Coroutine chaseCoroutine;
    protected Coroutine attackRangeMonitorCoroutine;

    protected override void Awake()
    {
        base.Awake();
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

        if (healthBarPrefab != null)
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"{gameObject.name}: No Canvas found in the scene for health bar instantiation.");
                return;
            }
            var healthBarObj = Instantiate(healthBarPrefab, canvas.transform);
            var healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                Debug.LogError($"{gameObject.name}: healthBarPrefab does not have an EnemyHealthBar component.");
                return;
            }
            healthBarInstance = healthBar;
            healthBarInstance.SetEnemy(this);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: healthBarPrefab is not assigned in the Inspector.");
        }
    }

    protected override void ConfigureStateMachine()
    {
        base.ConfigureStateMachine();

        Debug.Log($"{gameObject.name} ConfigureStateMachine called");
        EnemyStateMachineConfig.ConfigureBasic(enemyAI); // ConfigureBasic is a static helper method to set up the default states and triggers
                                                         // It would not be used again in this derived class if you had custom states/triggers
        // Add more transitions specific to this enemy if needed

        enemyAI.Configure(EnemyState.Idle)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry lambda for Idle called");
                idleBehavior.OnEnter(this);
            })
            .OnExit(() => {
                idleBehavior.OnExit(this);
            });

        enemyAI.Configure(EnemyState.Relocate)
            .OnEntry(() => relocateBehavior.OnEnter(this))
            .OnExit(() => relocateBehavior.OnExit(this));

        enemyAI.Configure(EnemyState.Recover)
            .OnEntry(() => recoverBehavior.OnEnter(this))
            .OnExit(() => recoverBehavior.OnExit(this));

        // --- CHASE STATE ---
        enemyAI.Configure(EnemyState.Chase)
            .OnEntry(() => chaseBehavior.OnEnter(this))
            .OnExit(() => chaseBehavior.OnExit(this))
            .Ignore(EnemyTrigger.SeePlayer);

        // --- ATTACK STATE ---
        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => attackBehavior.OnEnter(this))
            .OnExit(() => attackBehavior.OnExit(this))
            .Ignore(EnemyTrigger.SeePlayer); // Ignore SeePlayer trigger in Attack state

        // --- DEATH STATE ---
        enemyAI.Configure(EnemyState.Death)
            .OnEntry(() => deathBehavior.OnEnter(this))
            .Ignore(EnemyTrigger.SeePlayer)
            .Ignore(EnemyTrigger.LowHealth);
    }

    protected override void Update()
    {
        base.Update();
    }
}
