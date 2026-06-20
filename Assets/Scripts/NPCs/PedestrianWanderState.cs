namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianWanderState : PedestrianState
    {
        private readonly PedestrianController controller;

        public PedestrianWanderState(PedestrianController controller)
        {
            this.controller = controller;
        }

        public override void Enter()
        {
            if (controller.IsAgentReady)
            {
                controller.TryPickDestination();
            }
        }

        public override void Tick()
        {
            if (!controller.IsAgentReady)
            {
                return;
            }

            if (controller.ShouldPauseAtDestination())
            {
                controller.EnterPauseState();
                return;
            }

            if (controller.IsStuck())
            {
                controller.ResetRouteDirection();
                controller.EnterPauseState();
            }
        }
    }
}
