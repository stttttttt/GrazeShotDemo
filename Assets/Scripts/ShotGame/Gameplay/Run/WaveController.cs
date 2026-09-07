using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Scene;
using ShotGame.Gameplay.World;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Run
{
    /// <summary>执行单个波次的生成队列，并以 EntityId 追踪完成目标。</summary>
    public sealed class WaveController : IDisposable
    {
        private readonly EntitySpawner _spawner;
        private readonly GameplaySceneContext _scene;
        private readonly GameplayFactHub _facts;
        private readonly GameEntityId _playerId;
        private readonly LayerMask _targetMask;
        private readonly LayerMask _wallMask;
        private readonly HashSet<GameEntityId> _aliveEnemyIds = new HashSet<GameEntityId>();
        private readonly HashSet<GameEntityId> _requiredEnemyIds = new HashSet<GameEntityId>();
        private readonly IDisposable _deathSubscription;
        private WavePlanEntry _current;
        private GameEntityId _keyTargetId;
        private int _nextSpawnIndex;
        private int _spawnedCount;
        private float _spawnElapsed;
        private bool _keyTargetKilled;
        private bool _stopped;
        private bool _disposed;

        public WaveController(EntitySpawner spawner, GameplaySceneContext scene, GameplayFactHub facts,
            GameEntityId playerId, LayerMask targetMask, LayerMask wallMask)
        {
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _playerId = playerId.IsValid ? playerId : throw new ArgumentException("玩家 EntityId 无效。", nameof(playerId));
            _targetMask = targetMask;
            _wallMask = wallMask;
            _deathSubscription = facts.Subscribe<CharacterDiedFact>(OnCharacterDied);
        }

        public bool IsActive => _current != null && !IsCompleted && !_stopped;
        public bool IsCompleted { get; private set; }
        public int AliveCount => _aliveEnemyIds.Count;
        public int PendingCount => _current == null ? 0 : _current.Spawns.Count - _nextSpawnIndex;
        public int CurrentWaveIndex => _current?.WaveIndex ?? -1;

        public void StartWave(WavePlanEntry wave, int totalWaves)
        {
            ThrowIfDisposed();
            if (IsActive) throw new InvalidOperationException("同一时刻不能启动两个波次。");
            _current = wave ?? throw new ArgumentNullException(nameof(wave));
            _aliveEnemyIds.Clear();
            _requiredEnemyIds.Clear();
            _keyTargetId = default;
            _keyTargetKilled = false;
            _nextSpawnIndex = 0;
            _spawnedCount = 0;
            _spawnElapsed = 0f;
            IsCompleted = false;
            _stopped = false;
            _facts.Publish(new WaveStartedFact(wave.WaveIndex + 1, totalWaves,
                wave.DisplayName, wave.ObjectiveType));
            PublishProgress();
            TryComplete();
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f) return;
            _spawnElapsed += deltaTime;
            while (_nextSpawnIndex < _current.Spawns.Count &&
                   _aliveEnemyIds.Count < _current.MaxAliveEnemies)
            {
                var spawn = _current.Spawns[_nextSpawnIndex];
                if (spawn.SpawnTime > _spawnElapsed) break;
                Spawn(spawn);
                _nextSpawnIndex++;
            }
            TryComplete();
        }

        public void Stop()
        {
            _stopped = true;
            _nextSpawnIndex = _current?.Spawns.Count ?? 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _deathSubscription.Dispose();
            _aliveEnemyIds.Clear();
            _requiredEnemyIds.Clear();
            _current = null;
        }

        private void Spawn(EnemySpawnPlan spawn)
        {
            if (!_scene.TryGetEnemySpawn(spawn.SpawnId, out var point))
                throw new InvalidOperationException($"WavePlan 引用了不存在的 SpawnId：{spawn.SpawnId}。");
            var enemy = _spawner.SpawnEnemy(spawn.Enemy, point.Transform.position,
                point.Transform.rotation, _playerId, _targetMask, _wallMask,
                _scene.MovementBounds, _scene.WorldRoot);
            _aliveEnemyIds.Add(enemy.Id);
            if (spawn.RequiredForClear) _requiredEnemyIds.Add(enemy.Id);
            if (spawn.IsKeyTarget) _keyTargetId = enemy.Id;
            _spawnedCount++;
            _facts.Publish(new WaveEnemySpawnedFact(_current.WaveIndex + 1,
                spawn.Enemy.EnemyId, enemy.Id, spawn.IsKeyTarget));
            PublishProgress();
        }

        private void OnCharacterDied(CharacterDiedFact fact)
        {
            if (_disposed || _current == null || fact.Category != EntityCategory.Enemy ||
                !_aliveEnemyIds.Remove(fact.EntityId)) return;
            _requiredEnemyIds.Remove(fact.EntityId);
            if (fact.EntityId == _keyTargetId)
            {
                _keyTargetKilled = true;
                if (_current.ObjectiveType == WaveObjectiveType.KeyTarget)
                    _nextSpawnIndex = _current.Spawns.Count;
            }
            PublishProgress();
            TryComplete();
        }

        private void TryComplete()
        {
            if (_current == null || IsCompleted || _stopped) return;
            var queueExhausted = _nextSpawnIndex >= _current.Spawns.Count;
            var completed = _current.ObjectiveType == WaveObjectiveType.KeyTarget
                ? _keyTargetKilled && _aliveEnemyIds.Count == 0
                : queueExhausted && _requiredEnemyIds.Count == 0;
            if (!completed) return;
            IsCompleted = true;
            _facts.Publish(new WaveCompletedFact(_current.WaveIndex + 1, _spawnElapsed));
        }

        private void PublishProgress() => _facts.Publish(new WaveProgressChangedFact(
            _current.WaveIndex + 1, _spawnedCount, PendingCount, _aliveEnemyIds.Count));

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WaveController));
        }
    }
}
