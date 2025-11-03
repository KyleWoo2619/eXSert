namespace Behaviors
{
    public interface IEnemyStateBehavior<TState, TTrigger>
        where TState : struct, System.Enum
        where TTrigger : struct, System.Enum
    {
        void OnEnter(BaseEnemy<TState, TTrigger> enemy);
        void OnExit(BaseEnemy<TState, TTrigger> enemy);
        void Tick(BaseEnemy<TState, TTrigger> enemy);
    }
}