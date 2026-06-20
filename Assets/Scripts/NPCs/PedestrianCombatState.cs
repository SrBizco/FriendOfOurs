using UnityEngine;

namespace FriendOfOurs.NPCs
{
    public sealed class PedestrianCombatState : PedestrianState
    {
        private readonly PedestrianController controller;
        private float nextAttackTime;

        public PedestrianCombatState(PedestrianController controller)
        {
            this.controller = controller;
        }

        public override void Enter()
        {
            controller.BeginCombatMovement();
            nextAttackTime = Time.time;
        }

        public override void Tick()
        {
            if (!controller.HasThreat || !controller.IsAgentReady)
            {
                controller.EnterWanderState();
                return;
            }

            if (!controller.IsThreatAlive())
            {
                controller.ClearThreat();
                controller.EnterWanderState();
                return;
            }

            float distance = controller.DistanceToThreat();
            if (distance > controller.CombatGiveUpDistance)
            {
                controller.EnterFleeState();
                return;
            }

            if (!controller.IsThreatInAttackRange())
            {
                controller.MoveToThreat();
                return;
            }

            controller.StopAgent();
            controller.FaceThreat();
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + controller.CombatAttackCooldown;
                controller.PunchThreat();
            }
        }

        public override void Exit()
        {
            controller.BeginWalkMovement();
        }
    }
}
