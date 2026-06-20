namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerController player) : base(player)
        {
        }

        public override void Tick()
        {
            if (Player.ConsumeJumpInput())
            {
                Player.TryJump();
                return;
            }

            if (Player.HasMoveInput)
            {
                Player.ChangeState(Player.MoveState);
            }
        }

        public override void FixedTick()
        {
            Player.ApplyHorizontalMovement();
        }
    }
}
