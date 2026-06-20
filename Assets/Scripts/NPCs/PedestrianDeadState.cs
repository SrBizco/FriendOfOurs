namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianDeadState : PedestrianState
    {
        private readonly PedestrianController controller;

        public PedestrianDeadState(PedestrianController controller)
        {
            this.controller = controller;
        }

        public override void Enter()
        {
            controller.Die();
        }
    }
}
