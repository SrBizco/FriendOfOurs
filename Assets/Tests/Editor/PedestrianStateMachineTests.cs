using FriendOfOurs.NPCs;
using NUnit.Framework;

namespace FriendOfOurs.Tests
{
    public sealed class PedestrianStateMachineTests
    {
        [Test]
        public void ChangeState_ExitsPreviousStateAndEntersNextState()
        {
            FakePedestrianState first = new FakePedestrianState();
            FakePedestrianState second = new FakePedestrianState();
            PedestrianStateMachine stateMachine = new PedestrianStateMachine();

            stateMachine.ChangeState(first);
            stateMachine.ChangeState(second);

            Assert.That(first.EnterCount, Is.EqualTo(1));
            Assert.That(first.ExitCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.EqualTo(1));
            Assert.That(second.ExitCount, Is.Zero);
            Assert.That(stateMachine.CurrentState, Is.SameAs(second));
        }

        [Test]
        public void Tick_UpdatesCurrentState()
        {
            FakePedestrianState state = new FakePedestrianState();
            PedestrianStateMachine stateMachine = new PedestrianStateMachine();

            stateMachine.ChangeState(state);
            stateMachine.Tick();

            Assert.That(state.TickCount, Is.EqualTo(1));
        }

        private sealed class FakePedestrianState : PedestrianState
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public int TickCount { get; private set; }

            public override void Enter()
            {
                EnterCount++;
            }

            public override void Exit()
            {
                ExitCount++;
            }

            public override void Tick()
            {
                TickCount++;
            }
        }
    }
}
