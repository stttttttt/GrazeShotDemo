using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Run;
using ShotGame.Presentation.UI;
using UnityEngine;
using EntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>一局内的视听反馈总入口，由 GameAppFlow 统一 Tick。</summary>
    public sealed class GameplayFeedbackController : IDisposable
    {
        private readonly GameplaySession _session;
        private readonly GameplayFeelConfig _config;
        private readonly CameraFeedbackController _camera;
        private readonly AudioFeedbackController _audio;
        private readonly FeedbackObjectPool<WorldEffectView> _effectPool;
        private readonly GameplayUIFeedbackController _ui;
        private readonly Dictionary<EntityId, EntityVisualState> _entities =
            new Dictionary<EntityId, EntityVisualState>();
        private readonly Dictionary<EntityId, GameplayFeelConfig.WeaponFeedbackEntry> _lastWeapon =
            new Dictionary<EntityId, GameplayFeelConfig.WeaponFeedbackEntry>();
        private readonly Dictionary<EntityId, float> _hitStopCooldowns = new Dictionary<EntityId, float>();
        private readonly List<EffectState> _effects = new List<EffectState>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<EntityId> _cooldownKeys = new List<EntityId>();
        private bool _disposed;

        public GameplayFeedbackController(GameplaySession session, GameplayContentConfig content,
            GameplayScreen screen)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (content == null) throw new ArgumentNullException(nameof(content));
            _config = content.FeelConfig != null ? content.FeelConfig : throw new ArgumentNullException(nameof(content.FeelConfig));
            var root = session.SceneContext.PresentationRoot;
            var bindings = root.GetComponent<GameplayPresentationBindings>();
            var audioSource = bindings != null ? bindings.AudioSource : root.GetComponent<AudioSource>();
            _camera = new CameraFeedbackController(session.SceneContext.GameplayCamera, _config);
            _audio = new AudioFeedbackController(audioSource);
            var effectPrefab = _config.WorldEffectPrefab != null
                ? _config.WorldEffectPrefab.GetComponent<WorldEffectView>() : null;
            _effectPool = new FeedbackObjectPool<WorldEffectView>(effectPrefab,
                bindings != null ? bindings.TemporaryEffectRoot : root, 40);
            _ui = screen != null
                ? new GameplayUIFeedbackController(session, screen, _config, content.GrazeConfig)
                : null;
            Subscribe();
            RegisterEntity(session.PlayerEntityId);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_disposed || unscaledDeltaTime <= 0f) return;
            TickCooldowns(unscaledDeltaTime);
            TickEntities(unscaledDeltaTime);
            TickEffects(unscaledDeltaTime);
            _camera.Tick(unscaledDeltaTime);
            _ui?.Tick(unscaledDeltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (var i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
            foreach (var pair in _entities) pair.Value.View?.ResetVisual();
            _entities.Clear();
            _cooldownKeys.Clear();
            for (var i = 0; i < _effects.Count; i++) _effectPool.Return(_effects[i].View);
            _effects.Clear();
            _ui?.Dispose();
            _effectPool.Dispose();
            _camera.Reset();
            _audio.Stop();
        }

        private void Subscribe()
        {
            _subscriptions.Add(_session.Facts.Subscribe<EntitySpawnedFact>(fact => RegisterEntity(fact.EntityId)));
            _subscriptions.Add(_session.Facts.Subscribe<EntityDespawnedFact>(fact => UnregisterEntity(fact.EntityId)));
            _subscriptions.Add(_session.Facts.Subscribe<ShotFiredFact>(OnShotFired));
            _subscriptions.Add(_session.Facts.Subscribe<ProjectileHitFact>(OnProjectileHit));
            _subscriptions.Add(_session.Facts.Subscribe<ProjectileWallHitFact>(OnWallHit));
            _subscriptions.Add(_session.Facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged));
            _subscriptions.Add(_session.Facts.Subscribe<CharacterDiedFact>(OnCharacterDied));
            _subscriptions.Add(_session.Facts.Subscribe<GrazeSucceededFact>(OnGrazeSucceeded));
        }

        private void RegisterEntity(EntityId id)
        {
            if (!id.IsValid || _entities.ContainsKey(id) || !_session.World.TryGetEntity(id, out var entity) ||
                entity.UnityObject.GameObject == null) return;
            var view = entity.UnityObject.GameObject.GetComponent<EntityFeedbackView>();
            if (view == null) return;
            view.CaptureDefaults();
            var state = new EntityVisualState(view);
            if (entity.Category == EntityCategory.Enemy)
            {
                state.Scale = 0.6f;
                state.ScaleRemaining = 0.18f;
                view.SetScale(0.6f);
            }
            _entities.Add(id, state);
        }

        private void UnregisterEntity(EntityId id)
        {
            if (!_entities.TryGetValue(id, out var state)) return;
            state.View?.ResetVisual();
            _entities.Remove(id);
            _lastWeapon.Remove(id);
            _hitStopCooldowns.Remove(id);
        }

        private void OnShotFired(ShotFiredFact fact)
        {
            var profile = _config.FindWeapon(fact.WeaponConfig);
            if (profile != null) _lastWeapon[fact.SourceId] = profile;
            var size = profile?.MuzzleSize ?? 0.42f;
            var color = profile?.MuzzleColor ?? new Color(1f, 0.82f, 0.25f, 1f);
            if (fact.EmpowerLevel > 0) color = _config.EmpoweredDamageNumberColor;
            SpawnEffect(fact.Origin, fact.Direction, color, size, 0.07f);
            _audio.Play(profile?.FireAudio, fact.SourceTeam == EntityTeam.Player ? 1f : 0.55f, 0.025f);
            if (_entities.TryGetValue(fact.SourceId, out var entity))
            {
                entity.Scale = 1.16f;
                entity.ScaleRemaining = 0.1f;
            }
            if (fact.SourceTeam == EntityTeam.Player && profile != null)
            {
                _camera.AddKick(fact.Direction, profile.CameraKick);
                _camera.AddShake(profile.ShakeStrength, profile.ShakeDuration);
            }
        }

        private void OnProjectileHit(ProjectileHitFact fact)
        {
            if (fact.DamageResult.AppliedDamage <= 0f) return;
            var color = fact.EmpowerLevel > 0 ? _config.EmpoweredDamageNumberColor : _config.HitFlashColor;
            SpawnEffect(fact.HitPosition, fact.HitNormal, color,
                fact.DamageResult.Killed ? 0.55f : 0.32f, fact.DamageResult.Killed ? 0.18f : 0.1f);
            _audio.Play(_config.HitAudio, 0.7f, 0.05f);
            Flash(fact.TargetId, color, _config.FlashDuration, fact.DamageResult.Killed ? 1.28f : 1.12f);

            if (_lastWeapon.TryGetValue(fact.SourceId, out var profile) && profile.HitStopDuration > 0f &&
                (!_hitStopCooldowns.TryGetValue(fact.SourceId, out var cooldown) || cooldown <= 0f))
            {
                _session.TimeDilation.RequestHitStop(profile.HitStopDuration);
                _hitStopCooldowns[fact.SourceId] = profile.HitStopCooldown;
            }
        }

        private void OnWallHit(ProjectileWallHitFact fact)
        {
            SpawnEffect(fact.HitPosition, fact.HitNormal, new Color(1f, 0.65f, 0.25f, 1f),
                0.2f, 0.08f);
            _audio.Play(_config.WallHitAudio, 0.45f, 0.08f);
        }

        private void OnCharacterDamaged(CharacterDamagedFact fact)
        {
            if (fact.TargetId != _session.PlayerEntityId || fact.Result.AppliedDamage <= 0f) return;
            Flash(fact.TargetId, _config.PlayerDamageColor, _config.FlashDuration * 1.5f, 1.2f);
            _camera.AddShake(_config.DamageShakeStrength, _config.DamageShakeDuration);
            _audio.Play(_config.PlayerDamageAudio);
        }

        private void OnCharacterDied(CharacterDiedFact fact)
        {
            if (_session.World.TryGetEntity(fact.EntityId, out var entity))
                SpawnEffect(entity.UnityObject.Transform.position, Vector2.up,
                    fact.Category == EntityCategory.Player ? _config.PlayerDamageColor : _config.LethalDamageNumberColor,
                    fact.Category == EntityCategory.Player ? 1.1f : 0.8f, 0.3f);
            _camera.AddShake(_config.DeathShakeStrength, _config.DeathShakeDuration);
            _audio.Play(_config.DeathAudio);
        }

        private void OnGrazeSucceeded(GrazeSucceededFact fact)
        {
            var perfect = fact.ResultType == GrazeResultType.PerfectMomentum ||
                          fact.ResultType == GrazeResultType.PerfectDefensive;
            if (_session.World.TryGetEntity(fact.PlayerId, out var player))
                SpawnEffect(player.UnityObject.Transform.position, Vector2.up,
                    perfect ? new Color(0.35f, 1f, 1f, 1f) : new Color(0.35f, 1f, 0.55f, 1f),
                    perfect ? 1.15f : 0.8f, perfect ? 0.28f : 0.18f);
            _audio.Play(perfect ? _config.PerfectGrazeAudio : _config.GrazeAudio);
            if (perfect) _camera.AddShake(0.07f, 0.1f);
        }

        private void Flash(EntityId id, Color color, float duration, float scale)
        {
            RegisterEntity(id);
            if (!_entities.TryGetValue(id, out var state)) return;
            state.FlashColor = color;
            state.FlashRemaining = Mathf.Max(state.FlashRemaining, duration);
            state.Scale = Mathf.Max(state.Scale, scale);
            state.ScaleRemaining = Mathf.Max(state.ScaleRemaining, duration * 2f);
        }

        private void SpawnEffect(Vector2 position, Vector2 direction, Color color, float size, float duration)
        {
            var view = _effectPool.Rent();
            if (view == null) return;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            view.Show(position, angle, color, size);
            _effects.Add(new EffectState(view, color, size, duration));
        }

        private void TickEntities(float deltaTime)
        {
            foreach (var pair in _entities)
            {
                var state = pair.Value;
                if (state.View == null) continue;
                if (state.FlashRemaining > 0f)
                {
                    state.FlashRemaining = Mathf.Max(0f, state.FlashRemaining - deltaTime);
                    if (state.FlashRemaining > 0f) state.View.SetFlash(state.FlashColor);
                    else state.View.ResetVisual();
                }
                if (state.ScaleRemaining > 0f)
                {
                    state.ScaleRemaining = Mathf.Max(0f, state.ScaleRemaining - deltaTime);
                    var t = state.ScaleRemaining > 0f ? state.ScaleRemaining / 0.12f : 0f;
                    state.View.SetScale(Mathf.Lerp(1f, state.Scale, Mathf.Clamp01(t)));
                }
                else state.View.SetScale(1f);
            }
        }

        private void TickEffects(float deltaTime)
        {
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                var state = _effects[i];
                state.Elapsed += deltaTime;
                var t = Mathf.Clamp01(state.Elapsed / state.Duration);
                var color = state.Color;
                color.a = 1f - t;
                state.View.SetVisual(color, Mathf.Lerp(state.Size, state.Size * 1.8f, t));
                if (t < 1f) continue;
                _effects.RemoveAt(i);
                _effectPool.Return(state.View);
            }
        }

        private void TickCooldowns(float deltaTime)
        {
            if (_hitStopCooldowns.Count == 0) return;
            _cooldownKeys.Clear();
            _cooldownKeys.AddRange(_hitStopCooldowns.Keys);
            for (var i = 0; i < _cooldownKeys.Count; i++)
                _hitStopCooldowns[_cooldownKeys[i]] = Mathf.Max(0f,
                    _hitStopCooldowns[_cooldownKeys[i]] - deltaTime);
        }

        private sealed class EntityVisualState
        {
            public EntityVisualState(EntityFeedbackView view) => View = view;
            public EntityFeedbackView View;
            public Color FlashColor;
            public float FlashRemaining;
            public float Scale = 1f;
            public float ScaleRemaining;
        }

        private sealed class EffectState
        {
            public EffectState(WorldEffectView view, Color color, float size, float duration)
            {
                View = view;
                Color = color;
                Size = size;
                Duration = Mathf.Max(0.01f, duration);
            }
            public readonly WorldEffectView View;
            public readonly Color Color;
            public readonly float Size;
            public readonly float Duration;
            public float Elapsed;
        }
    }
}
