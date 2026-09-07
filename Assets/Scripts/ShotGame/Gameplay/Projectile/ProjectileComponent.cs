using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Combat;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.World;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Projectile
{
    /// <summary>弹丸的移动、寿命、命中和伤害结算。</summary>
    public sealed class ProjectileComponent : EntityComponent, IEntityTickable, IEntityFixedTickable, IDamageSource
    {
        private readonly GameplayWorld _world;
        private readonly EntitySpawner _spawner;
        private readonly Vector2 _direction;
        private readonly float _speed;
        private readonly float _radius;
        private readonly DamagePayload _damage;
        private readonly LayerMask _queryMask;
        private readonly HashSet<GameEntityId> _hitEntityIds = new HashSet<GameEntityId>();
        private int _remainingPenetrations;
        private float _lifetimeRemaining;
        private bool _despawnRequested;

        public ProjectileComponent(GameplayWorld world, EntitySpawner spawner, in ProjectileSpawnData data)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            SourceId = data.SourceId;
            SourceTeam = data.SourceTeam;
            _direction = data.Direction;
            _speed = data.Speed;
            _lifetimeRemaining = data.Lifetime;
            _radius = data.Radius;
            _damage = new DamagePayload(data.Damage);
            _queryMask = data.TargetMask | data.WallMask;
            EmpowerLevel = data.EmpowerLevel;
            _remainingPenetrations = Mathf.Max(0, data.RemainingPenetrations);
        }

        public GameEntityId SourceId { get; }
        public EntityTeam SourceTeam { get; }
        public GameEntityId OwnerId => SourceId;
        public EntityTeam Team => SourceTeam;
        public int EmpowerLevel { get; }

        public DamagePayload CreateDamagePayload() => _damage;

        public override void Initialize()
        {
            if (!SourceId.IsValid) throw new InvalidOperationException("弹丸缺少有效 SourceId。");
            if (_direction.sqrMagnitude <= 0.0001f) throw new InvalidOperationException("弹丸方向无效。");
            if (_speed <= 0f || _lifetimeRemaining <= 0f) throw new InvalidOperationException("弹丸速度或寿命无效。");
        }

        public void Tick(float deltaTime)
        {
            if (_despawnRequested || deltaTime <= 0f) return;
            _lifetimeRemaining -= deltaTime;
            if (_lifetimeRemaining <= 0f) RequestDespawn();
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (_despawnRequested || fixedDeltaTime <= 0f) return;
            var start = (Vector2)Owner.UnityObject.Transform.position;
            var remainingDistance = _speed * fixedDeltaTime;
            const float safeOffset = 0.01f;
            const int maxHitsPerTick = 8;

            for (var hitIndex = 0; hitIndex < maxHitsPerTick && remainingDistance > 0f; hitIndex++)
            {
                if (!TryFindNearestHit(start, remainingDistance, out var target, out var hitDistance))
                {
                    Owner.UnityObject.Transform.position = start + _direction * remainingDistance;
                    return;
                }

                if (target.Category == EntityCategory.Wall)
                {
                    Owner.UnityObject.Transform.position = start + _direction * Mathf.Max(0f, hitDistance);
                    RequestDespawn();
                    return;
                }

                ResolveHit(target);
                if (_remainingPenetrations <= 0)
                {
                    RequestDespawn();
                    return;
                }

                _remainingPenetrations--;
                var advance = Mathf.Min(remainingDistance, Mathf.Max(0f, hitDistance) + safeOffset);
                start += _direction * advance;
                remainingDistance -= advance;
            }

            Owner.UnityObject.Transform.position = start + _direction * remainingDistance;
        }

        private bool TryFindNearestHit(Vector2 start, float distance,
            out ShotGame.Gameplay.Entity.Entity target, out float hitDistance)
        {
            target = null;
            hitDistance = 0f;
            var nearestDistance = float.MaxValue;
            var hits = Physics2D.CircleCastAll(start, _radius, _direction, distance, _queryMask);
            for (var i = 0; i < hits.Length; i++)
            {
                var collider = hits[i].collider;
                if (collider == null || !_world.TryGetEntity(collider, out var entity)) continue;
                if (!entity.IsAlive || entity.Id == Owner.Id || entity.Id == SourceId) continue;
                if (SourceTeam != EntityTeam.Neutral && entity.Team == SourceTeam) continue;
                if (entity.Category != EntityCategory.Wall && !(entity is IDamageable)) continue;
                if (entity.Category != EntityCategory.Wall && _hitEntityIds.Contains(entity.Id)) continue;
                if (hits[i].distance >= nearestDistance) continue;
                nearestDistance = hits[i].distance;
                target = entity;
            }
            hitDistance = nearestDistance;
            return target != null;
        }

        private void ResolveHit(ShotGame.Gameplay.Entity.Entity target)
        {
            if (target.Category == EntityCategory.Wall) return;
            if (target is IDamageable damageable)
            {
                _hitEntityIds.Add(target.Id);
                damageable.TakeDamage(new DamageRequest(SourceId, SourceTeam, _damage, Owner.Id));
            }
        }

        private void RequestDespawn()
        {
            if (_despawnRequested) return;
            _despawnRequested = true;
            _spawner.Despawn(Owner.Id);
        }
    }
}
