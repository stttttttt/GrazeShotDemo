using ShotGame.Gameplay.Combat;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Run;
using ShotGame.Gameplay.Config;
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
        public ShotFiredFact(GameEntityId sourceId, Vector2 origin, Vector2 direction,
            float recoilImpulse, int empowerLevel, WeaponConfig weaponConfig, EntityTeam sourceTeam)
        {
            SourceId = sourceId;
            Origin = origin;
            Direction = direction;
            RecoilImpulse = recoilImpulse;
            EmpowerLevel = empowerLevel;
            WeaponConfig = weaponConfig;
            SourceTeam = sourceTeam;
        }
        public GameEntityId SourceId { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public float RecoilImpulse { get; }
        public int EmpowerLevel { get; }
        public WeaponConfig WeaponConfig { get; }
        public EntityTeam SourceTeam { get; }
    }

    public readonly struct ProjectileHitFact
    {
        public ProjectileHitFact(GameEntityId projectileId, GameEntityId sourceId, GameEntityId targetId,
            Vector2 hitPosition, Vector2 hitNormal, Vector2 direction, DamageResult damageResult,
            int empowerLevel, EntityTeam sourceTeam)
        {
            ProjectileId = projectileId;
            SourceId = sourceId;
            TargetId = targetId;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            Direction = direction;
            DamageResult = damageResult;
            EmpowerLevel = empowerLevel;
            SourceTeam = sourceTeam;
        }

        public GameEntityId ProjectileId { get; }
        public GameEntityId SourceId { get; }
        public GameEntityId TargetId { get; }
        public Vector2 HitPosition { get; }
        public Vector2 HitNormal { get; }
        public Vector2 Direction { get; }
        public DamageResult DamageResult { get; }
        public int EmpowerLevel { get; }
        public EntityTeam SourceTeam { get; }
    }

    public readonly struct ProjectileWallHitFact
    {
        public ProjectileWallHitFact(GameEntityId projectileId, GameEntityId sourceId,
            Vector2 hitPosition, Vector2 hitNormal, Vector2 direction, int empowerLevel,
            EntityTeam sourceTeam)
        {
            ProjectileId = projectileId;
            SourceId = sourceId;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            Direction = direction;
            EmpowerLevel = empowerLevel;
            SourceTeam = sourceTeam;
        }

        public GameEntityId ProjectileId { get; }
        public GameEntityId SourceId { get; }
        public Vector2 HitPosition { get; }
        public Vector2 HitNormal { get; }
        public Vector2 Direction { get; }
        public int EmpowerLevel { get; }
        public EntityTeam SourceTeam { get; }
    }

    public readonly struct GrazePhaseChangedFact
    {
        public GrazePhaseChangedFact(GameEntityId playerId, GrazePhase previous, GrazePhase current)
        { PlayerId = playerId; Previous = previous; Current = current; }
        public GameEntityId PlayerId { get; }
        public GrazePhase Previous { get; }
        public GrazePhase Current { get; }
    }

    public readonly struct GrazeSucceededFact
    {
        public GrazeSucceededFact(GameEntityId playerId, GameEntityId projectileId,
            GrazeResultType resultType, int combo, int chargeLevel)
        { PlayerId = playerId; ProjectileId = projectileId; ResultType = resultType; Combo = combo; ChargeLevel = chargeLevel; }
        public GameEntityId PlayerId { get; }
        public GameEntityId ProjectileId { get; }
        public GrazeResultType ResultType { get; }
        public int Combo { get; }
        public int ChargeLevel { get; }
    }

    public readonly struct ChargeChangedFact
    {
        public ChargeChangedFact(GameEntityId playerId, int previousLevel, int currentLevel,
            int combo, float remainingDuration)
        { PlayerId = playerId; PreviousLevel = previousLevel; CurrentLevel = currentLevel; Combo = combo; RemainingDuration = remainingDuration; }
        public GameEntityId PlayerId { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int Combo { get; }
        public float RemainingDuration { get; }
    }

    public readonly struct TimeDilationChangedFact
    {
        public TimeDilationChangedFact(float timeScale, float remainingDuration, bool isActive)
        { TimeScale = timeScale; RemainingDuration = remainingDuration; IsActive = isActive; }
        public float TimeScale { get; }
        public float RemainingDuration { get; }
        public bool IsActive { get; }
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

    public readonly struct RunCountdownChangedFact
    {
        public RunCountdownChangedFact(int secondsRemaining) => SecondsRemaining = secondsRemaining;
        public int SecondsRemaining { get; }
    }

    public readonly struct WaveStartedFact
    {
        public WaveStartedFact(int waveIndex, int totalWaves, string displayName,
            WaveObjectiveType objectiveType)
        { WaveIndex = waveIndex; TotalWaves = totalWaves; DisplayName = displayName; ObjectiveType = objectiveType; }
        public int WaveIndex { get; }
        public int TotalWaves { get; }
        public string DisplayName { get; }
        public WaveObjectiveType ObjectiveType { get; }
    }

    public readonly struct WaveEnemySpawnedFact
    {
        public WaveEnemySpawnedFact(int waveIndex, string enemyId, GameEntityId entityId, bool isKeyTarget)
        { WaveIndex = waveIndex; EnemyId = enemyId; EntityId = entityId; IsKeyTarget = isKeyTarget; }
        public int WaveIndex { get; }
        public string EnemyId { get; }
        public GameEntityId EntityId { get; }
        public bool IsKeyTarget { get; }
    }

    public readonly struct WaveProgressChangedFact
    {
        public WaveProgressChangedFact(int waveIndex, int spawned, int pending, int alive)
        { WaveIndex = waveIndex; Spawned = spawned; Pending = pending; Alive = alive; }
        public int WaveIndex { get; }
        public int Spawned { get; }
        public int Pending { get; }
        public int Alive { get; }
    }

    public readonly struct WaveCompletedFact
    {
        public WaveCompletedFact(int waveIndex, float elapsedTime)
        { WaveIndex = waveIndex; ElapsedTime = elapsedTime; }
        public int WaveIndex { get; }
        public float ElapsedTime { get; }
    }

    public readonly struct WaveIntervalStartedFact
    {
        public WaveIntervalStartedFact(int nextWaveIndex, float duration)
        { NextWaveIndex = nextWaveIndex; Duration = duration; }
        public int NextWaveIndex { get; }
        public float Duration { get; }
    }

    public readonly struct GameplaySettledFact
    {
        public GameplaySettledFact(GameplayResult result) => Result = result;
        public GameplayResult Result { get; }
    }

    public readonly struct VictoryRequestedFact { }
    public readonly struct DefeatRequestedFact { }
}
