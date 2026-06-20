namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerDeadState : PlayerState
    {
        public PlayerDeadState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            Player.StopMovement();
            Player.PlayDeathAnimation();
        }

        public override void Tick()
        {
        }

        public override void FixedTick()
        {
        }
    }
}
