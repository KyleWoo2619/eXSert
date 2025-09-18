using Stateless;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    private StateMachine<State, Trigger> enemyAI;
    private enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee
    }
    private enum Trigger
    {
        SeePlayer,
        LosePlayer,
        LowHealth,
        RecoveredHealth,
        InAttackRange,
        OutOfAttackRange
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        enemyAI = new StateMachine<State, Trigger>(State.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
