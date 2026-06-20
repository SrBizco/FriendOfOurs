namespace FriendOfOurs.Gameplay
{
    public sealed class PlayerJumpState : PlayerState
    {
        public PlayerJumpState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            Player.ApplyJumpImpulse();
        }

        public override void Tick()
        {
            if (Player.ConsumeJumpInput())
            {
                Player.TryJump();
                return;
            }

            if (Player.CanFinishJump)
            {
                Player.ChangeState(Player.HasMoveInput ? Player.MoveState : Player.IdleState);
            }
        }

        public override void FixedTick()
        {
            Player.ApplyHorizontalMovement();
            Player.ApplyMovementRotation();
        }
    }
}
