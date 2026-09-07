using ShotGame.Gameplay.Entity;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Projectile
{
    public readonly struct ProjectileSpawnData
    {
        public ProjectileSpawnData(GameObject prefab, Vector2 position, Vector2 direction,
            GameEntityId sourceId, EntityTeam sourceTeam, float damage, float speed, float lifetime,
            float radius, LayerMask targetMask, LayerMask wallMask, Transform parent,
            int empowerLevel = 0, int remainingPenetrations = 0)
        {
            Prefab = prefab;
            Position = position;
            Direction = direction.normalized;
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            Radius = radius;
            TargetMask = targetMask;
            WallMask = wallMask;
            Parent = parent;
            EmpowerLevel = empowerLevel;
            RemainingPenetrations = remainingPenetrations;
        }

        public GameObject Prefab { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public GameEntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
        public float Damage { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public float Radius { get; }
        public LayerMask TargetMask { get; }
        public LayerMask WallMask { get; }
        public Transform Parent { get; }
        public int EmpowerLevel { get; }
        public int RemainingPenetrations { get; }
    }
}
