namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerStateMachine
    {
        public PlayerState CurrentState { get; private set; }

        public void ChangeState(PlayerState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState?.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }

        public void FixedTick()
        {
            CurrentState?.FixedTick();
        }
    }
}
