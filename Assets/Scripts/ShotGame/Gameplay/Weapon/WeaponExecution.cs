using System;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Projectile;
using ShotGame.Gameplay.World;
using UnityEngine;

namespace ShotGame.Gameplay.Weapon
{
    /// <summary>把 ShotPackage 落地为弹丸、后坐冲量和表现事实。</summary>
    public sealed class WeaponExecution
    {
        private readonly EntitySpawner _spawner;
        private readonly MovementComponent _movement;
        private readonly GameplayFactHub _facts;
        private readonly Transform _projectileRoot;

        public WeaponExecution(EntitySpawner spawner, MovementComponent movement,
            GameplayFactHub facts, Transform projectileRoot)
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _movement = movement ?? throw new ArgumentNullException(nameof(movement));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _projectileRoot = projectileRoot != null
                ? projectileRoot
                : throw new ArgumentNullException(nameof(projectileRoot));
        }

        public bool Execute(in ShotPackage package)
        {
            for (var i = 0; i < package.ProjectileCount; i++)
            {
                var angle = GetSpreadAngle(i, package.ProjectileCount, package.SpreadAngle);
                var direction = Quaternion.Euler(0f, 0f, angle) * package.Direction;
                var data = new ProjectileSpawnData(package.ProjectilePrefab, package.Origin, direction,
                    package.SourceId, package.SourceTeam, package.Damage, package.ProjectileSpeed,
                    package.ProjectileLifetime, package.ProjectileRadius, package.TargetMask,
                    package.WallMask, _projectileRoot, package.EmpowerLevel,
                    package.RemainingPenetrations);
                _spawner.SpawnProjectile(data);
            }

            if (package.RecoilImpulse > 0f)
                _movement.AddImpulse(-package.Direction * package.RecoilImpulse);
            _facts.Publish(new ShotFiredFact(package.SourceId, package.Origin,
                package.Direction, package.RecoilImpulse, package.EmpowerLevel));
            return true;
        }

        private static float GetSpreadAngle(int index, int count, float totalAngle)
        {
            if (count <= 1) return 0f;
            return Mathf.Lerp(-totalAngle * 0.5f, totalAngle * 0.5f, index / (float)(count - 1));
        }
    }
}
