using UnityEngine;

namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianFleeState : PedestrianState
    {
        private readonly PedestrianController controller;
        private float earliestExitTime;
        private float nextRepathTime;

        public PedestrianFleeState(PedestrianController controller)
        {
            this.controller = controller;
        }

        public override void Enter()
        {
            earliestExitTime = Time.time + controller.MinimumFleeDuration;
            nextRepathTime = 0f;
            controller.BeginFleeMovement();
            controller.TryFleeFromThreat();
        }

        public override void Tick()
        {
            if (!controller.IsAgentReady)
            {
                return;
            }

            if (controller.HasThreat && !controller.IsThreatAlive())
            {
                controller.ClearThreat();
                controller.BeginWalkMovement();
                controller.EnterWanderState();
                return;
            }

            if (!controller.HasThreat)
            {
                controller.BeginWalkMovement();
                controller.EnterWanderState();
                return;
            }

            if (Time.time >= earliestExitTime && controller.DistanceToThreat() >= controller.FleeSafeDistance)
            {
                controller.ClearThreat();
                controller.BeginWalkMovement();
                controller.EnterWanderState();
                return;
            }

            if (Time.time >= nextRepathTime || controller.ShouldPauseAtDestination() || controller.IsStuck())
            {
                controller.TryFleeFromThreat();
                nextRepathTime = Time.time + controller.FleeRepathInterval;
            }
        }

        public override void Exit()
        {
            controller.BeginWalkMovement();
        }
    }
}
