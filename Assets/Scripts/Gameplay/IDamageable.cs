namespace FriendOfOurs.Gameplay
{
    public interface IDamageable
    {
        bool IsDead { get; }

        void TakeDamage(DamageInfo damageInfo);
    }
}
