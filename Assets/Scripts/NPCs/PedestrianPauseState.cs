using UnityEngine;

namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianPauseState : PedestrianState
    {
        private readonly PedestrianController controller;
        private float resumeTime;

        public PedestrianPauseState(PedestrianController controller)
        {
            this.controller = controller;
        }

        public override void Enter()
        {
            resumeTime = Time.time + controller.GetRandomPauseDuration();
        }

        public override void Tick()
        {
            if (Time.time >= resumeTime)
            {
                controller.EnterWanderState();
            }
        }
    }
}
