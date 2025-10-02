// PLEASE DO NOT DELETE THIS SCRIPT
// USE THIS AS A TEMPLATE/REFERENCE FOR CREATING NEW ENEMY TYPES
// AND/OR TO TEST OUT NEW BEHAVIORAL FEATURES
// IF AN ENEMY TYPE WILL BE USING DIFFERENT OR MORE/LESS STATES/TRIGGERS THAN THE BASE ENEMY CLASS
// THEN YOU WILL NEED TO CREATE NEW STATE AND TRIGGER ENUMS IN THAT ENEMY SCRIPT

using UnityEngine;
using System.Collections;

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
    // Reference to the player (set this appropriately in your game)
    protected Transform playerTarget;

    protected Coroutine lookAtPlayerCoroutine;
    protected Coroutine chaseCoroutine;
    protected Coroutine attackRangeMonitorCoroutine;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log($"{gameObject.name} Awake called");
        InitializeStateMachine(EnemyState.Idle);
        ConfigureStateMachine();
        Debug.Log($"{gameObject.name} State machine initialized");

        // Find the player by tag (if not set elsewhere)
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }

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
                if (zoneArrivalCoroutine != null)
                {
                    StopCoroutine(zoneArrivalCoroutine);
                    zoneArrivalCoroutine = null;
                }
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

        // --- CHASE STATE ---
        enemyAI.Configure(EnemyState.Chase)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry for Chase called");
                OnEnterChase();
                if (playerTarget != null)
                {
                    if (chaseCoroutine != null)
                        StopCoroutine(chaseCoroutine);
                    chaseCoroutine = StartCoroutine(ChasePlayerLoop());
                }
            })
            .OnExit(() => {
                if (chaseCoroutine != null)
                {
                    StopCoroutine(chaseCoroutine);
                    chaseCoroutine = null;
                }
                agent.ResetPath();
            });

        // --- ATTACK STATE ---
        enemyAI.Configure(EnemyState.Attack)
            .OnEntry(() => {
                Debug.Log($"{gameObject.name} OnEntry for Attack called");
                OnEnterAttack();
            })
            .OnExit(() => {
                Debug.Log($"{gameObject.name} OnExit for Attack called");
                OnExitAttack();
            })
            .Ignore(EnemyTrigger.SeePlayer); // Ignore SeePlayer trigger in Attack state
    }

    protected override void Update()
    {
        base.Update();
    }

    protected virtual IEnumerator LookAtPlayerLoop()
    {
        while (enemyAI.State.Equals(EnemyState.Attack) && playerTarget != null)
        {
            if (!isAttackBoxActive) // Only rotate when attack box is disabled
            {
                Vector3 direction = (playerTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    float t = 0f;
                    Quaternion startRotation = transform.rotation;
                    while (t < 1f && !isAttackBoxActive)
                    {
                        t += Time.deltaTime;
                        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                        yield return null;
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    protected IEnumerator MonitorAttackRangeLoop()
    {
        while (enemyAI.State.Equals(EnemyState.Attack) && playerTarget != null)
        {
            float attackRange = (Mathf.Max(attackBoxSize.x, attackBoxSize.z) * 0.5f) + attackBoxDistance;
            float distance = Vector3.Distance(transform.position, playerTarget.position);

            if (distance > attackRange)
            {
                TryFireTriggerByName("OutOfAttackRange");
                yield break; // Stop monitoring when out of range
            }

            yield return new WaitForSeconds(0.1f); // Check 10 times per second
        }
    }

    protected override void OnEnterAttack()
    {
        base.OnEnterAttack();
        if (lookAtPlayerCoroutine != null)
            StopCoroutine(lookAtPlayerCoroutine);
        lookAtPlayerCoroutine = StartCoroutine(LookAtPlayerLoop());

        if (attackRangeMonitorCoroutine != null)
            StopCoroutine(attackRangeMonitorCoroutine);
        attackRangeMonitorCoroutine = StartCoroutine(MonitorAttackRangeLoop());
    }

    protected override void OnExitAttack()
    {
        base.OnExitAttack();
        if (lookAtPlayerCoroutine != null)
            StopCoroutine(lookAtPlayerCoroutine);
        lookAtPlayerCoroutine = null;

        if (attackRangeMonitorCoroutine != null)
            StopCoroutine(attackRangeMonitorCoroutine);
        attackRangeMonitorCoroutine = null;
    }

    protected IEnumerator ChasePlayerLoop()
    {
        while (enemyAI.State.Equals(EnemyState.Chase) && playerTarget != null)
        {
            float attackRange = (Mathf.Max(attackBoxSize.x, attackBoxSize.z) * 0.5f) + attackBoxDistance;
            float distance = Vector3.Distance(transform.position, playerTarget.position);

            MoveToAttackRange(playerTarget);

            if (distance <= attackRange)
            {
                TryFireTriggerByName("InAttackRange");
                yield break; // Stop coroutine when attack range is reached
            }

            yield return null; // Wait for next frame
        }
    }

    protected override void OnEnterChase()
    {
        base.OnEnterChase(); // Sets color to yellow
    }
}
