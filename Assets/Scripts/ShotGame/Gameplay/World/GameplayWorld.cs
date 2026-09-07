using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using UnityEngine;
using GameplayEntity = ShotGame.Gameplay.Entity.Entity;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.World
{
    public sealed class GameplayWorld : IDisposable
    {
        private readonly GameplayFactHub _facts;
        private readonly List<GameplayEntity> _entities = new List<GameplayEntity>();
        private readonly Dictionary<GameEntityId, GameplayEntity> _entitiesById = new Dictionary<GameEntityId, GameplayEntity>();
        private readonly List<GameplayEntity> _pendingAdd = new List<GameplayEntity>();
        private readonly HashSet<GameEntityId> _pendingRemove = new HashSet<GameEntityId>();
        private bool _isUpdating;
        private bool _disposed;

        public GameplayWorld(GameplayFactHub facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            ColliderMap = new ColliderEntityMap();
        }

        public ColliderEntityMap ColliderMap { get; }
        public bool IsRunning { get; private set; } = true;
        public bool IsSimulationEnabled { get; private set; }
        public int EntityCount => _entities.Count + _pendingAdd.Count;

        public bool TryGetEntity(GameEntityId entityId, out GameplayEntity entity)
        {
            if (_entitiesById.TryGetValue(entityId, out entity)) return true;
            for (var i = 0; i < _pendingAdd.Count; i++)
                if (_pendingAdd[i].Id == entityId) { entity = _pendingAdd[i]; return true; }
            entity = null;
            return false;
        }

        public bool TryGetEntity(Collider2D collider, out GameplayEntity entity)
        {
            if (ColliderMap.TryGetEntityId(collider, out var id)) return TryGetEntity(id, out entity);
            entity = null;
            return false;
        }

        public bool TryRaycastTarget(Vector2 origin, Vector2 direction, float distance, LayerMask layerMask,
            EntityTeam sourceTeam, out GameEntityId targetId)
        {
            var hits = Physics2D.RaycastAll(origin, direction, distance, layerMask);
            for (var i = 0; i < hits.Length; i++)
            {
                if (!TryGetEntity(hits[i].collider, out var entity) || !entity.IsAlive) continue;
                if (sourceTeam != EntityTeam.Neutral && entity.Team == sourceTeam) continue;
                targetId = entity.Id;
                return true;
            }
            targetId = default;
            return false;
        }

        public void GetEntitiesInRange(Vector2 center, float radius, LayerMask layerMask,
            EntityTeam sourceTeam, List<GameEntityId> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            var found = new HashSet<GameEntityId>();
            var colliders = Physics2D.OverlapCircleAll(center, radius, layerMask);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!TryGetEntity(colliders[i], out var entity) || !entity.IsAlive) continue;
                if (sourceTeam != EntityTeam.Neutral && entity.Team == sourceTeam) continue;
                if (found.Add(entity.Id)) results.Add(entity.Id);
            }
        }

        public void Register(GameplayEntity entity)
        {
            ThrowIfDisposed();
            if (!IsRunning) throw new InvalidOperationException("GameplayWorld 已停止，不能再注册 Entity。");
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (TryGetEntity(entity.Id, out _)) throw new InvalidOperationException($"EntityId {entity.Id} 已存在。");
            if (!entity.IsInitialized) entity.Initialize();
            if (_isUpdating) _pendingAdd.Add(entity);
            else AddNow(entity);
        }

        public void Despawn(GameEntityId entityId)
        {
            if (_disposed || !entityId.IsValid) return;
            if (_isUpdating) _pendingRemove.Add(entityId);
            else RemoveNow(entityId);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || _disposed) return;
            FlushChanges();
            if (!IsSimulationEnabled) return;
            _isUpdating = true;
            for (var i = 0; i < _entities.Count && IsRunning; i++)
            {
                var entity = _entities[i];
                entity.Tick(deltaTime);
                if (!entity.IsAlive) _pendingRemove.Add(entity.Id);
            }
            _isUpdating = false;
            FlushChanges();
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsRunning || !IsSimulationEnabled || _disposed) return;
            _isUpdating = true;
            for (var i = 0; i < _entities.Count && IsRunning; i++) _entities[i].FixedTick(fixedDeltaTime);
            _isUpdating = false;
            FlushChanges();
        }

        public void UnscaledTick(float unscaledDeltaTime)
        {
            if (!IsRunning || !IsSimulationEnabled || _disposed) return;
            _isUpdating = true;
            for (var i = 0; i < _entities.Count && IsRunning; i++)
                _entities[i].UnscaledTick(unscaledDeltaTime);
            _isUpdating = false;
            FlushChanges();
        }

        public void SetSimulationEnabled(bool enabled)
        {
            if (IsRunning) IsSimulationEnabled = enabled;
        }

        public void Stop()
        {
            IsSimulationEnabled = false;
            IsRunning = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IsRunning = false;
            _pendingRemove.Clear();
            for (var i = _pendingAdd.Count - 1; i >= 0; i--) _pendingAdd[i].Dispose();
            _pendingAdd.Clear();
            for (var i = _entities.Count - 1; i >= 0; i--)
            {
                ColliderMap.Unregister(_entities[i]);
                _entities[i].Dispose();
            }
            _entities.Clear();
            _entitiesById.Clear();
            ColliderMap.Clear();
        }

        private void AddNow(GameplayEntity entity)
        {
            _entities.Add(entity);
            _entitiesById.Add(entity.Id, entity);
            ColliderMap.Register(entity);
            _facts.Publish(new EntitySpawnedFact(entity.Id, entity.Category));
        }

        private void RemoveNow(GameEntityId id)
        {
            if (!_entitiesById.TryGetValue(id, out var entity))
            {
                var pendingIndex = _pendingAdd.FindIndex(item => item.Id == id);
                if (pendingIndex >= 0) { _pendingAdd[pendingIndex].Dispose(); _pendingAdd.RemoveAt(pendingIndex); }
                return;
            }
            ColliderMap.Unregister(entity);
            _entitiesById.Remove(id);
            _entities.Remove(entity);
            _facts.Publish(new EntityDespawnedFact(entity.Id, entity.Category));
            entity.Dispose();
        }

        private void FlushChanges()
        {
            if (_pendingRemove.Count > 0)
            {
                var ids = new GameEntityId[_pendingRemove.Count];
                _pendingRemove.CopyTo(ids);
                _pendingRemove.Clear();
                for (var i = 0; i < ids.Length; i++) RemoveNow(ids[i]);
            }
            if (_pendingAdd.Count == 0 || !IsRunning) return;
            var entities = _pendingAdd.ToArray();
            _pendingAdd.Clear();
            for (var i = 0; i < entities.Length; i++) AddNow(entities[i]);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayWorld));
        }
    }
}
