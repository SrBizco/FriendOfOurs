namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerMoveState : PlayerState
    {
        public PlayerMoveState(PlayerController player) : base(player)
        {
        }

        public override void Tick()
        {
            if (Player.ConsumeJumpInput())
            {
                Player.TryJump();
                return;
            }

            if (!Player.HasMoveInput)
            {
                Player.ChangeState(Player.IdleState);
            }
        }

        public override void FixedTick()
        {
            Player.ApplyHorizontalMovement();
            Player.ApplyMovementRotation();
        }
    }
}
