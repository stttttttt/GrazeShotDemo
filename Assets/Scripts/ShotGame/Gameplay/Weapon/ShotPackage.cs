using ShotGame.Gameplay.Entity;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Weapon
{
    /// <summary>一次有效扳机行为的不可变结算数据；一组散弹也只对应一个包。</summary>
    public readonly struct ShotPackage
    {
        public ShotPackage(GameEntityId sourceId, EntityTeam sourceTeam, WeaponConfig weaponConfig,
            Vector2 origin, Vector2 direction,
            GameObject projectilePrefab, int projectileCount, float spreadAngle, float damage,
            float projectileSpeed, float projectileLifetime, float projectileRadius, float recoilImpulse,
            LayerMask targetMask, LayerMask wallMask, int empowerLevel = 0, int remainingPenetrations = 0)
        {
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            WeaponConfig = weaponConfig;
            Origin = origin;
            Direction = direction.normalized;
            ProjectilePrefab = projectilePrefab;
            ProjectileCount = projectileCount;
            SpreadAngle = spreadAngle;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
            ProjectileLifetime = projectileLifetime;
            ProjectileRadius = projectileRadius;
            RecoilImpulse = recoilImpulse;
            TargetMask = targetMask;
            WallMask = wallMask;
            EmpowerLevel = empowerLevel;
            RemainingPenetrations = remainingPenetrations;
        }

        public GameEntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
        public WeaponConfig WeaponConfig { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public GameObject ProjectilePrefab { get; }
        public int ProjectileCount { get; }
        public float SpreadAngle { get; }
        public float Damage { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileLifetime { get; }
        public float ProjectileRadius { get; }
        public float RecoilImpulse { get; }
        public LayerMask TargetMask { get; }
        public LayerMask WallMask { get; }
        public int EmpowerLevel { get; }
        public int RemainingPenetrations { get; }

        public ShotPackage WithEmpowerment(int level, float damageMultiplier,
            float radiusMultiplier, int penetrations) =>
            new ShotPackage(SourceId, SourceTeam, WeaponConfig, Origin, Direction, ProjectilePrefab,
                ProjectileCount, SpreadAngle, Damage * Mathf.Max(0f, damageMultiplier),
                ProjectileSpeed, ProjectileLifetime, ProjectileRadius * Mathf.Max(0.01f, radiusMultiplier),
                RecoilImpulse, TargetMask, WallMask, Mathf.Max(0, level), Mathf.Max(0, penetrations));
    }
}
