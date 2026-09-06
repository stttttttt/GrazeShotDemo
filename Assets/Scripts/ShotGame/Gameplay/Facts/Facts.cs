using ShotGame.Gameplay.Combat;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Run;
using ShotGame.Gameplay.Weapon;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Facts
{
    public readonly struct EntitySpawnedFact
    {
        public EntitySpawnedFact(GameEntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public GameEntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct EntityDespawnedFact
    {
        public EntityDespawnedFact(GameEntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public GameEntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct CharacterDamagedFact
    {
        public CharacterDamagedFact(GameEntityId targetId, GameEntityId sourceId, DamageResult result)
        { TargetId = targetId; SourceId = sourceId; Result = result; }
        public GameEntityId TargetId { get; }
        public GameEntityId SourceId { get; }
        public DamageResult Result { get; }
    }

    public readonly struct CharacterDiedFact
    {
        public CharacterDiedFact(GameEntityId entityId, EntityCategory category) { EntityId = entityId; Category = category; }
        public GameEntityId EntityId { get; }
        public EntityCategory Category { get; }
    }

    public readonly struct ShotFiredFact
    {
        public ShotFiredFact(GameEntityId sourceId, Vector2 origin, Vector2 direction, float recoilImpulse)
        { SourceId = sourceId; Origin = origin; Direction = direction; RecoilImpulse = recoilImpulse; }
        public GameEntityId SourceId { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public float RecoilImpulse { get; }
    }

    public readonly struct WeaponStateChangedFact
    {
        public WeaponStateChangedFact(GameEntityId ownerId, int weaponSlot, string displayName,
            WeaponState state, int magazineAmmo, int reserveAmmo)
        {
            OwnerId = ownerId;
            WeaponSlot = weaponSlot;
            DisplayName = displayName;
            State = state;
            MagazineAmmo = magazineAmmo;
            ReserveAmmo = reserveAmmo;
        }

        public GameEntityId OwnerId { get; }
        public int WeaponSlot { get; }
        public string DisplayName { get; }
        public WeaponState State { get; }
        public int MagazineAmmo { get; }
        public int ReserveAmmo { get; }
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
