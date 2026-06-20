namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianStateMachine
    {
        public PedestrianState CurrentState { get; private set; }

        public void ChangeState(PedestrianState nextState)
        {
            if (nextState == null || CurrentState == nextState)
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}
