using ShotGame.Gameplay.Entity;

namespace ShotGame.Gameplay.Combat
{
    public interface IDamageSource
    {
        EntityId OwnerId { get; }
        EntityTeam Team { get; }
        DamagePayload CreateDamagePayload();
    }
}
