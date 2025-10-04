namespace Behaviors
{
    public interface IEnemyStateBehavior
    {
        void OnEnter(BaseEnemy<EnemyState, EnemyTrigger> enemy);
        void OnExit(BaseEnemy<EnemyState, EnemyTrigger> enemy);
    }
}