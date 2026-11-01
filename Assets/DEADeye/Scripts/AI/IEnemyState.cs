namespace DEADeye
{
    public enum HostileStateType
    {
        Spawn,
        Patrol,
        Suspicious,
        Alerted,
        Escape,
        Eliminated
    }

    public interface IEnemyState
    {
        HostileStateType StateType { get; }
        void Enter(DEADeyeHostileAI hostile);
        void UpdateState(DEADeyeHostileAI hostile);
        void Exit(DEADeyeHostileAI hostile);
    }
}

