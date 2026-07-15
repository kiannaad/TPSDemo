namespace CGame
{
    public interface IDamageable
    {
        string EntityId { get; }
        bool IsAlive { get; }
        bool ApplyDamage(in DamageEvent damageEvent);
    }
}
