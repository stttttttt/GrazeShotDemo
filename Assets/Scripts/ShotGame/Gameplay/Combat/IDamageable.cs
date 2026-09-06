namespace ShotGame.Gameplay.Combat
{
    public interface IDamageable
    {
        DamageResult TakeDamage(in DamageRequest request);
    }
}
