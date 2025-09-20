// PLEASE DO NOT DELETE THIS SCRIPT
// USE THIS AS A TEMPLATE/REFERENCE FOR CREATING NEW ENEMY TYPES
// AND/OR TO TEST OUT NEW BEHAVIORAL FEATURES
// IF AN ENEMY TYPE WILL BE USING DIFFERENT OR MORE/LESS STATES/TRIGGERS THAN THE BASE ENEMY CLASS
// THEN YOU WILL NEED TO CREATE NEW STATE AND TRIGGER ENUMS IN THAT ENEMY SCRIPT

using UnityEngine;

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
    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"{gameObject.name} Awake called");
        InitializeStateMachine(EnemyState.Idle);
        ConfigureStateMachine();
        Debug.Log($"{gameObject.name} State machine initialized");

        // Manually call OnEnterIdle if starting in Idle
        if (enemyAI.State.Equals(EnemyState.Idle))
        {
            Debug.Log($"{gameObject.name} Manually calling OnEnterIdle for initial Idle state");
            OnEnterIdle();
        }
    }

    protected override void ConfigureStateMachine()
    {
        Debug.Log($"{gameObject.name} ConfigureStateMachine called");
        EnemyStateMachineConfig.ConfigureBasic(enemyAI); // ConfigureBasic is a static helper method to set up the default states and triggers
                                                         // It would not be used again in this derived class if you had custom states/triggers
        // Add more transitions specific to this enemy if needed

        enemyAI.Configure(EnemyState.Idle)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry lambda for Idle called");
                OnEnterIdle();
            })
            .OnExit(() => {
                if (idleTimerCoroutine != null)
                {
                    StopCoroutine(idleTimerCoroutine);
                    idleTimerCoroutine = null; // The purpose of setting idleTimerCoroutine to null is to indicate that the coroutine is no longer running
                }
                if (idleWanderCoroutine != null)
                {
                    StopCoroutine(idleWanderCoroutine);
                    idleWanderCoroutine = null;
                }
            });
        enemyAI.Configure(EnemyState.Relocate)
            .OnEntry(() => OnEnterRelocate())
            .OnExit(() => {
                StopCoroutine(zoneArrivalCoroutine);
                zoneArrivalCoroutine = null;
            });
        enemyAI.Configure(EnemyState.Recover)
        .OnEntry(() => OnEnterRecover())
        .OnExit(() => {
            if (recoverCoroutine != null)
            {
                StopCoroutine(recoverCoroutine);
                recoverCoroutine = null;
            }
        });
    }
}
