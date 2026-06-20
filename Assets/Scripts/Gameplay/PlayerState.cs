namespace FriendOfOurs.Gameplay
{
    public abstract class PlayerState
    {
        protected readonly PlayerController Player;

        protected PlayerState(PlayerController player)
        {
            Player = player;
        }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public abstract void Tick();

        public abstract void FixedTick();
    }
}
