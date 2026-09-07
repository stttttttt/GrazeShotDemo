using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Run;
using ShotGame.Presentation.Feedback;
using UnityEngine;
using GameEntity = ShotGame.Gameplay.Entity.Entity;
using EntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Presentation.UI
{
    /// <summary>固定 HUD 与世界 UI 的统一纯 C# 驱动器。</summary>
    public sealed class GameplayUIFeedbackController : IDisposable
    {
        private readonly GameplaySession _session;
        private readonly GameplayScreen _screen;
        private readonly GameplayFeelConfig _config;
        private readonly GrazeConfig _grazeConfig;
        private readonly Camera _camera;
        private readonly FeedbackObjectPool<DamageNumberView> _damagePool;
        private readonly FeedbackObjectPool<WorldHealthBarView> _healthBarPool;
        private readonly Dictionary<EntityId, PendingDamage> _pendingDamage =
            new Dictionary<EntityId, PendingDamage>();
        private readonly Dictionary<EntityId, HealthBarBinding> _healthBars =
            new Dictionary<EntityId, HealthBarBinding>();
        private readonly List<DamageNumberView> _activeDamageNumbers = new List<DamageNumberView>();
        private readonly Queue<PendingHealing> _pendingHealing = new Queue<PendingHealing>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<EntityId> _staleHealthBars = new List<EntityId>();
        private bool _disposed;

        public GameplayUIFeedbackController(GameplaySession session, GameplayScreen screen,
            GameplayFeelConfig config, GrazeConfig grazeConfig)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _screen = screen != null ? screen : throw new ArgumentNullException(nameof(screen));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _grazeConfig = grazeConfig != null ? grazeConfig : throw new ArgumentNullException(nameof(grazeConfig));
            _camera = session.SceneContext.GameplayCamera;
            _damagePool = new FeedbackObjectPool<DamageNumberView>(screen.DamageNumberPrefab,
                screen.WorldUiRoot, config.DamageNumberLimit);
            _healthBarPool = new FeedbackObjectPool<WorldHealthBarView>(screen.WorldHealthBarPrefab,
                screen.WorldUiRoot, config.WorldHealthBarLimit);
            Subscribe();
            InitializePlayerHud();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_disposed || unscaledDeltaTime <= 0f) return;
            FlushDamageNumbers();
            TickDamageNumbers(unscaledDeltaTime);
            TickHealthBars(unscaledDeltaTime);
            TickPlayerHud(unscaledDeltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (var i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
            foreach (var pair in _healthBars) _healthBarPool.Return(pair.Value.View);
            _healthBars.Clear();
            for (var i = 0; i < _activeDamageNumbers.Count; i++)
                _damagePool.Return(_activeDamageNumbers[i]);
            _activeDamageNumbers.Clear();
            _pendingDamage.Clear();
            _pendingHealing.Clear();
            _staleHealthBars.Clear();
            _damagePool.Dispose();
            _healthBarPool.Dispose();
        }

        private void Subscribe()
        {
            _subscriptions.Add(_session.Facts.Subscribe<ProjectileHitFact>(OnProjectileHit));
            _subscriptions.Add(_session.Facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged));
            _subscriptions.Add(_session.Facts.Subscribe<CharacterHealedFact>(OnCharacterHealed));
            _subscriptions.Add(_session.Facts.Subscribe<CharacterDiedFact>(fact => RemoveHealthBar(fact.EntityId)));
            _subscriptions.Add(_session.Facts.Subscribe<EntityDespawnedFact>(fact => RemoveHealthBar(fact.EntityId)));
            _subscriptions.Add(_session.Facts.Subscribe<GrazeShockwaveReleasedFact>(OnShockwaveReleased));
        }

        private void InitializePlayerHud()
        {
            if (!_session.World.TryGetEntity(_session.PlayerEntityId, out var player)) return;
            var attributes = player.GetComponent<AttributeComponent>();
            if (attributes == null) return;
            _screen.PlayerHealthView?.Initialize(attributes.GetCurrent(AttributeType.Health),
                attributes.GetCurrent(AttributeType.MaxHealth), _config);
            GetOrCreateHealthBar(player, attributes.GetCurrent(AttributeType.MaxHealth));
            var graze = player.GetComponent<GrazeComponent>();
            _screen.GrazeIndicatorView?.SetShockwaveCharge(false, 0f,
                graze?.PreviewRadius ?? _grazeConfig.MinimumShockwaveRadius);
        }

        private void OnProjectileHit(ProjectileHitFact fact)
        {
            if (fact.SourceTeam != EntityTeam.Player || fact.DamageResult.AppliedDamage <= 0f) return;
            if (_pendingDamage.TryGetValue(fact.TargetId, out var pending))
            {
                pending.Damage += fact.DamageResult.AppliedDamage;
                pending.Position = fact.HitPosition;
                pending.Lethal |= fact.DamageResult.Killed;
                pending.EmpowerLevel = Mathf.Max(pending.EmpowerLevel, fact.EmpowerLevel);
            }
            else
            {
                _pendingDamage.Add(fact.TargetId, new PendingDamage
                {
                    Damage = fact.DamageResult.AppliedDamage,
                    Position = fact.HitPosition,
                    Lethal = fact.DamageResult.Killed,
                    EmpowerLevel = fact.EmpowerLevel
                });
            }
        }

        private void OnCharacterDamaged(CharacterDamagedFact fact)
        {
            if (!_session.World.TryGetEntity(fact.TargetId, out var target)) return;
            var attributes = target.GetComponent<AttributeComponent>();
            if (attributes == null) return;
            var maximum = attributes.GetCurrent(AttributeType.MaxHealth);
            if (fact.TargetId == _session.PlayerEntityId)
            {
                _screen.PlayerHealthView?.SetImmediate(fact.Result.HealthAfterDamage, maximum, _config);
                var playerBar = GetOrCreateHealthBar(target, maximum);
                playerBar?.View.SetHealth(maximum > 0f ? fact.Result.HealthAfterDamage / maximum : 0f,
                    _config);
                return;
            }
            if (target.Category != EntityCategory.Enemy) return;
            var binding = GetOrCreateHealthBar(target, maximum);
            binding?.View.SetHealth(maximum > 0f ? fact.Result.HealthAfterDamage / maximum : 0f, _config);
        }

        private void OnShockwaveReleased(GrazeShockwaveReleasedFact fact)
        {
            if (fact.PlayerId != _session.PlayerEntityId) return;
            _screen.GrazeIndicatorView?.ShowShockwaveResult(fact.AbsorbedProjectiles, fact.AmmoReward,
                fact.RestoredHealth);
        }

        private void OnCharacterHealed(CharacterHealedFact fact)
        {
            if (!_session.World.TryGetEntity(fact.TargetId, out var target)) return;
            var attributes = target.GetComponent<AttributeComponent>();
            if (attributes == null) return;
            var maximum = attributes.GetCurrent(AttributeType.MaxHealth);
            if (fact.TargetId == _session.PlayerEntityId)
            {
                _screen.PlayerHealthView?.SetImmediate(fact.HealthAfterHealing, maximum, _config);
                GetOrCreateHealthBar(target, maximum)?.View.SetHealth(
                    maximum > 0f ? fact.HealthAfterHealing / maximum : 0f, _config);
            }
            var anchor = target.UnityObject.GameObject.GetComponent<EntityFeedbackView>()?.HealthBarAnchor;
            _pendingHealing.Enqueue(new PendingHealing
            {
                Value = fact.RestoredHealth,
                Position = anchor != null ? anchor.position : target.UnityObject.Transform.position
            });
        }

        private void FlushDamageNumbers()
        {
            if (_screen.WorldUiRoot == null || _screen.DamageNumberPrefab == null)
            {
                _pendingDamage.Clear();
                _pendingHealing.Clear();
                return;
            }
            foreach (var pair in _pendingDamage)
            {
                var view = _damagePool.Rent();
                if (view == null) continue;
                var data = pair.Value;
                var color = data.Lethal ? _config.LethalDamageNumberColor :
                    data.EmpowerLevel > 0 ? _config.EmpoweredDamageNumberColor : _config.EnemyDamageNumberColor;
                var screenPosition = (Vector2)_camera.WorldToScreenPoint(data.Position);
                screenPosition.x += ((pair.Key.GetHashCode() & 3) - 1.5f) * 7f;
                view.Show(screenPosition, data.Damage, color, _config.DamageNumberDuration, data.Lethal);
                _activeDamageNumbers.Add(view);
            }
            _pendingDamage.Clear();
            while (_pendingHealing.Count > 0)
            {
                var data = _pendingHealing.Dequeue();
                var view = _damagePool.Rent();
                if (view == null) continue;
                view.ShowHealing(_camera.WorldToScreenPoint(data.Position), data.Value,
                    _config.HealingNumberColor, _config.DamageNumberDuration);
                _activeDamageNumbers.Add(view);
            }
        }

        private void TickDamageNumbers(float deltaTime)
        {
            for (var i = _activeDamageNumbers.Count - 1; i >= 0; i--)
            {
                var view = _activeDamageNumbers[i];
                view.Tick(deltaTime);
                if (!view.IsFinished) continue;
                _activeDamageNumbers.RemoveAt(i);
                _damagePool.Return(view);
            }
        }

        private void TickHealthBars(float deltaTime)
        {
            _staleHealthBars.Clear();
            foreach (var pair in _healthBars)
            {
                if (!_session.World.TryGetEntity(pair.Key, out var entity) || !entity.IsAlive ||
                    entity.UnityObject.GameObject == null)
                {
                    _staleHealthBars.Add(pair.Key);
                    continue;
                }
                var anchor = pair.Value.Anchor != null ? pair.Value.Anchor : entity.UnityObject.Transform;
                pair.Value.View.SetScreenPosition(_camera.WorldToScreenPoint(anchor.position));
                pair.Value.View.Tick(deltaTime, _config);
            }
            for (var i = 0; i < _staleHealthBars.Count; i++) RemoveHealthBar(_staleHealthBars[i]);
        }

        private void TickPlayerHud(float deltaTime)
        {
            _screen.PlayerHealthView?.Tick(deltaTime, _config);
            _screen.GrazeIndicatorView?.Tick(deltaTime);
            if (!_session.World.TryGetEntity(_session.PlayerEntityId, out var player)) return;
            var graze = player.GetComponent<GrazeComponent>();
            if (graze != null) _screen.GrazeIndicatorView?.SetShockwaveCharge(graze.IsCharging,
                graze.Charge01, graze.PreviewRadius);
        }

        private HealthBarBinding GetOrCreateHealthBar(GameEntity entity, float maximum)
        {
            if (_healthBars.TryGetValue(entity.Id, out var existing)) return existing;
            if (_screen.WorldUiRoot == null || _screen.WorldHealthBarPrefab == null) return null;
            var view = _healthBarPool.Rent();
            if (view == null) return null;
            var attributes = entity.GetComponent<AttributeComponent>();
            var current = attributes?.GetCurrent(AttributeType.Health) ?? maximum;
            var feedbackView = entity.UnityObject.GameObject.GetComponent<EntityFeedbackView>();
            var binding = new HealthBarBinding(view,
                feedbackView != null ? feedbackView.HealthBarAnchor : entity.UnityObject.Transform);
            if (entity.Category == EntityCategory.Player)
                view.SetStyle(_config.PlayerHealthBarColor, _config.PlayerDelayedHealthBarColor,
                    _config.PlayerHealthBarSize);
            else
                view.SetStyle(_config.EnemyHealthBarColor, _config.EnemyDelayedHealthBarColor,
                    _config.EnemyHealthBarSize);
            view.Show(maximum > 0f ? current / maximum : 0f, true);
            _healthBars.Add(entity.Id, binding);
            return binding;
        }

        private void RemoveHealthBar(EntityId id)
        {
            if (!_healthBars.TryGetValue(id, out var binding)) return;
            _healthBars.Remove(id);
            _healthBarPool.Return(binding.View);
        }

        private sealed class PendingDamage
        {
            public float Damage;
            public Vector2 Position;
            public bool Lethal;
            public int EmpowerLevel;
        }

        private sealed class PendingHealing
        {
            public float Value;
            public Vector3 Position;
        }

        private sealed class HealthBarBinding
        {
            public HealthBarBinding(WorldHealthBarView view, Transform anchor)
            {
                View = view;
                Anchor = anchor;
            }
            public WorldHealthBarView View { get; }
            public Transform Anchor { get; }
        }
    }
}
