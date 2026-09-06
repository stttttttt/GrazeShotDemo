using ShotGame.Gameplay.Combat;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Run;

namespace ShotGame.Gameplay.Facts
{
    public readonly struct EntitySpawnedFact
    {
        public EntitySpawnedFact(EntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public EntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct EntityDespawnedFact
    {
        public EntityDespawnedFact(EntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public EntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct CharacterDamagedFact
    {
        public CharacterDamagedFact(EntityId targetId, EntityId sourceId, DamageResult result)
        { TargetId = targetId; SourceId = sourceId; Result = result; }
        public EntityId TargetId { get; }
        public EntityId SourceId { get; }
        public DamageResult Result { get; }
    }

    public readonly struct CharacterDiedFact
    {
        public CharacterDiedFact(EntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public EntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct RunStateChangedFact
    {
        public RunStateChangedFact(GameplayRunState previous, GameplayRunState current) { Previous = previous; Current = current; }
        public GameplayRunState Previous { get; }
        public GameplayRunState Current { get; }
    }

    public readonly struct VictoryRequestedFact { }
    public readonly struct DefeatRequestedFact { }
}
