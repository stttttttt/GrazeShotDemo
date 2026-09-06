using ShotGame.Gameplay.Entity;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Weapon
{
    /// <summary>一次有效扳机行为的不可变结算数据；一组散弹也只对应一个包。</summary>
    public readonly struct ShotPackage
    {
        public ShotPackage(GameEntityId sourceId, EntityTeam sourceTeam, Vector2 origin, Vector2 direction,
            GameObject projectilePrefab, int projectileCount, float spreadAngle, float damage,
            float projectileSpeed, float projectileLifetime, float projectileRadius, float recoilImpulse,
            LayerMask targetMask, LayerMask wallMask)
        {
            SourceId = sourceId;
            SourceTeam = sourceTeam;
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
        }

        public GameEntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
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
    }
}
