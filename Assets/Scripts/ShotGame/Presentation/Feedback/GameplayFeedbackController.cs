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
        private readonly GrazeConfig _grazeConfig;
        private readonly CameraFeedbackController _camera;
        private readonly AudioFeedbackController _audio;
        private readonly FeedbackObjectPool<WorldEffectView> _effectPool;
        private readonly FeedbackObjectPool<DeathDissolveView> _deathDissolvePool;
        private readonly DeathDissolveView _deathDissolveTemplate;
        private readonly GameplayUIFeedbackController _ui;
        private readonly Dictionary<EntityId, EntityVisualState> _entities =
            new Dictionary<EntityId, EntityVisualState>();
        private readonly Dictionary<EntityId, GameplayFeelConfig.WeaponFeedbackEntry> _lastWeapon =
            new Dictionary<EntityId, GameplayFeelConfig.WeaponFeedbackEntry>();
        private readonly Dictionary<EntityId, float> _hitStopCooldowns = new Dictionary<EntityId, float>();
        private readonly List<EffectState> _effects = new List<EffectState>();
        private readonly List<MuzzleFlashState> _muzzleFlashes = new List<MuzzleFlashState>();
        private readonly List<MuzzleFlashState> _hitEffects = new List<MuzzleFlashState>();
        private readonly List<ShellCasingState> _shellCasings = new List<ShellCasingState>();
        private readonly List<DeathDissolveState> _deathDissolves = new List<DeathDissolveState>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<EntityId> _cooldownKeys = new List<EntityId>();
        private PlayerGrazeEffectView _playerGrazeView;
        private GrazeComponent _playerGraze;
        private bool _disposed;

        public GameplayFeedbackController(GameplaySession session, GameplayContentConfig content,
            GameplayScreen screen)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (content == null) throw new ArgumentNullException(nameof(content));
            _config = content.FeelConfig != null ? content.FeelConfig : throw new ArgumentNullException(nameof(content.FeelConfig));
            _grazeConfig = content.GrazeConfig != null
                ? content.GrazeConfig
                : throw new ArgumentNullException(nameof(content.GrazeConfig));
            var root = session.SceneContext.PresentationRoot;
            var bindings = root.GetComponent<GameplayPresentationBindings>();
            var audioSource = bindings != null ? bindings.AudioSource : root.GetComponent<AudioSource>();
            _camera = new CameraFeedbackController(session.SceneContext.GameplayCamera, _config);
            _audio = new AudioFeedbackController(audioSource);
            var effectPrefab = _config.WorldEffectPrefab != null
                ? _config.WorldEffectPrefab.GetComponent<WorldEffectView>() : null;
            _effectPool = new FeedbackObjectPool<WorldEffectView>(effectPrefab,
                bindings != null ? bindings.TemporaryEffectRoot : root, 40);
            var deathRoot = bindings != null ? bindings.PersistentEffectRoot : root;
            var deathTemplateObject = new GameObject("DeathDissolveTemplate", typeof(DeathDissolveView));
            deathTemplateObject.transform.SetParent(deathRoot, false);
            deathTemplateObject.SetActive(false);
            _deathDissolveTemplate = deathTemplateObject.GetComponent<DeathDissolveView>();
            _deathDissolvePool = new FeedbackObjectPool<DeathDissolveView>(_deathDissolveTemplate,
                deathRoot, _config.EnemyDissolvePoolLimit);
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
            TickGrazeEffect(unscaledDeltaTime);
            TickEntities(unscaledDeltaTime);
            TickEffects(unscaledDeltaTime);
            TickMuzzleFlashes(unscaledDeltaTime);
            TickAnimatedEffects(unscaledDeltaTime, _hitEffects);
            TickShellCasings(unscaledDeltaTime);
            TickDeathDissolves(unscaledDeltaTime);
            _camera.Tick(unscaledDeltaTime);
            _ui?.Tick(unscaledDeltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (var i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
            _playerGrazeView?.ResetVisual();
            _playerGrazeView = null;
            _playerGraze = null;
            foreach (var pair in _entities) pair.Value.View?.ResetVisual();
            _entities.Clear();
            _cooldownKeys.Clear();
            for (var i = 0; i < _effects.Count; i++) _effectPool.Return(_effects[i].View);
            _effects.Clear();
            for (var i = 0; i < _muzzleFlashes.Count; i++) _effectPool.Return(_muzzleFlashes[i].View);
            _muzzleFlashes.Clear();
            for (var i = 0; i < _hitEffects.Count; i++) _effectPool.Return(_hitEffects[i].View);
            _hitEffects.Clear();
            for (var i = 0; i < _shellCasings.Count; i++) _effectPool.Return(_shellCasings[i].View);
            _shellCasings.Clear();
            for (var i = 0; i < _deathDissolves.Count; i++)
                _deathDissolvePool.Return(_deathDissolves[i].View);
            _deathDissolves.Clear();
            _ui?.Dispose();
            _effectPool.Dispose();
            _deathDissolvePool.Dispose();
            if (_deathDissolveTemplate != null)
                UnityEngine.Object.Destroy(_deathDissolveTemplate.gameObject);
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
            _subscriptions.Add(_session.Facts.Subscribe<GrazeShockwaveReleasedFact>(OnShockwaveReleased));
        }

        private void RegisterEntity(EntityId id)
        {
            if (!id.IsValid || _entities.ContainsKey(id) || !_session.World.TryGetEntity(id, out var entity) ||
                entity.UnityObject.GameObject == null) return;
            if (id == _session.PlayerEntityId)
            {
                _playerGraze = entity.GetComponent<GrazeComponent>();
                _playerGrazeView = PlayerGrazeEffectView.GetOrCreate(entity.UnityObject.GameObject,
                    _config.GrazeRingMaterial);
                _playerGrazeView?.Initialize(_config, _grazeConfig.MaximumShockwaveRadius);
            }
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
            SpawnMuzzleFlash(fact.Origin, fact.Direction, size);
            if (fact.SourceTeam == EntityTeam.Player)
                SpawnShellCasing(fact.Origin, fact.Direction);
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
            SpawnHitEffect(fact.HitPosition, fact.HitNormal, fact.DamageResult.Killed);
            _audio.Play(_config.HitAudio, 0.7f, 0.05f);
            var hitShakeDirection = Vector2.zero;
            if (_session.World.TryGetEntity(fact.TargetId, out var target) &&
                target.Category == EntityCategory.Enemy)
                hitShakeDirection = -fact.Direction;
            Flash(fact.TargetId, color, _config.FlashDuration,
                fact.DamageResult.Killed ? 1.28f : 1.12f, hitShakeDirection);

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
            {
                if (fact.Category == EntityCategory.Enemy)
                    SpawnDeathDissolve(entity.UnityObject.GameObject.GetComponent<EntityFeedbackView>());
                SpawnEffect(entity.UnityObject.Transform.position, Vector2.up,
                    fact.Category == EntityCategory.Player ? _config.PlayerDamageColor : _config.LethalDamageNumberColor,
                    fact.Category == EntityCategory.Player ? 1.1f : 0.8f, 0.3f);
            }
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

        private void OnGrazePhaseChanged(GrazePhaseChangedFact fact)
        {
            if (fact.PlayerId != _session.PlayerEntityId) return;
            _playerGrazeView?.NotifyPhaseChanged(fact.Current);
            if (fact.Current == GrazePhase.Perfect)
                _audio.Play(_config.PerfectReadyAudio, 0.85f, 0.03f);
        }

        private void OnShockwaveReleased(GrazeShockwaveReleasedFact fact)
        {
            if (fact.PlayerId != _session.PlayerEntityId) return;
            _playerGrazeView?.PlayShockwave(fact.Radius, fact.Charge01);
            SpawnEffect(fact.Position, Vector2.up, Color.white,
                Mathf.Lerp(0.65f, 1.2f, fact.Charge01), 0.18f);
            _audio.Play(fact.Charge01 >= 0.99f ? _config.PerfectGrazeAudio : _config.GrazeAudio);
            _camera.AddShake(Mathf.Lerp(0.025f, 0.08f, fact.Charge01), 0.1f);
        }

        private void TickGrazeEffect(float unscaledDeltaTime)
        {
            if (_playerGrazeView == null || _playerGraze == null) return;
            _playerGrazeView.SetCharging(_playerGraze.IsCharging, _playerGraze.Charge01,
                _playerGraze.PreviewRadius);
            _playerGrazeView.Tick(unscaledDeltaTime);
        }

        private void Flash(EntityId id, Color color, float duration, float scale,
            Vector2 hitDirection = default)
        {
            RegisterEntity(id);
            if (!_entities.TryGetValue(id, out var state)) return;
            state.FlashColor = color;
            state.FlashRemaining = Mathf.Max(state.FlashRemaining, duration);
            state.Scale = Mathf.Max(state.Scale, scale);
            state.ScaleRemaining = Mathf.Max(state.ScaleRemaining, duration * 2f);
            if (hitDirection.sqrMagnitude > 0.0001f)
            {
                state.ShakeDirection = hitDirection.normalized;
                state.ShakeDuration = _config.EnemyHitShakeDuration;
                state.ShakeRemaining = _config.EnemyHitShakeDuration;
            }
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
                if (state.ShakeRemaining > 0f)
                {
                    state.ShakeRemaining = Mathf.Max(0f, state.ShakeRemaining - deltaTime);
                    var progress = 1f - state.ShakeRemaining / state.ShakeDuration;
                    var envelope = 1f - progress;
                    var forward = Mathf.Sin(progress * Mathf.PI * 5f);
                    var side = Mathf.Sin(progress * Mathf.PI * 8f) * 0.3f;
                    var perpendicular = new Vector2(-state.ShakeDirection.y, state.ShakeDirection.x);
                    state.View.SetHitOffset((state.ShakeDirection * forward + perpendicular * side) *
                                            (_config.EnemyHitShakeDistance * envelope));
                }
                else state.View.SetHitOffset(Vector2.zero);
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

        private void SpawnMuzzleFlash(Vector2 origin, Vector2 direction, float size)
        {
            var frames = _config.MuzzleFrames;
            if (frames == null || frames.Count == 0 || frames[0] == null) return;
            var view = _effectPool.Rent();
            if (view == null) return;
            var normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            // ShotFiredFact.Origin 就是弹丸生成点，枪口焰直接以此为中心，避免视觉脱离枪口。
            var angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
            view.ShowSprite(origin, angle, frames[0], size);
            _muzzleFlashes.Add(new MuzzleFlashState(view, frames, _config.MuzzleFrameDuration));
        }

        private void SpawnHitEffect(Vector2 position, Vector2 normal, bool lethal)
        {
            var frames = _config.HitEffectFrames;
            if (frames == null || frames.Count == 0 || frames[0] == null) return;
            var view = _effectPool.Rent();
            if (view == null) return;
            var direction = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.right;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            view.ShowSprite(position, angle, frames[0],
                lethal ? _config.LethalHitEffectSize : _config.HitEffectSize);
            _hitEffects.Add(new MuzzleFlashState(view, frames, _config.HitEffectFrameDuration));
        }

        private void SpawnDeathDissolve(EntityFeedbackView source)
        {
            if (source == null) return;
            var view = _deathDissolvePool.Rent();
            if (view == null) return;
            if (!view.Show(source, _config.EnemyDissolveMaterial))
            {
                _deathDissolvePool.Return(view);
                return;
            }
            view.SetDissolve(0f, _config.EnemyDissolveEdgeColor, _config.EnemyDissolveEdgeWidth);
            _deathDissolves.Add(new DeathDissolveState(view, _config.EnemyDissolveDuration));
        }

        private void SpawnShellCasing(Vector2 origin, Vector2 fireDirection)
        {
            var view = _effectPool.Rent();
            if (view == null) return;
            var direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : Vector2.right;
            var side = new Vector2(-direction.y, direction.x);
            var speed = UnityEngine.Random.Range(_config.ShellEjectSpeedRange.x,
                _config.ShellEjectSpeedRange.y);
            var velocity = side * speed + direction * UnityEngine.Random.Range(-0.65f, -0.15f) +
                           Vector2.up * UnityEngine.Random.Range(0.25f, 0.8f);
            var position = origin - direction * 0.18f + side * 0.12f;
            var angle = UnityEngine.Random.Range(0f, 360f);
            view.ShowRectangle(position, angle, _config.ShellColor, _config.ShellSize);
            _shellCasings.Add(new ShellCasingState(view, _config.ShellColor, _config.ShellSize,
                velocity, UnityEngine.Random.Range(-720f, 720f), _config.ShellLifetime));
        }

        private void TickMuzzleFlashes(float deltaTime)
        {
            TickAnimatedEffects(deltaTime, _muzzleFlashes);
        }

        private void TickAnimatedEffects(float deltaTime, List<MuzzleFlashState> effects)
        {
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                var state = effects[i];
                state.Elapsed += deltaTime;
                var frameIndex = Mathf.FloorToInt(state.Elapsed / state.FrameDuration);
                if (frameIndex < state.Frames.Count)
                {
                    var frame = state.Frames[frameIndex];
                    if (frame != null) state.View.SetSprite(frame);
                    continue;
                }
                effects.RemoveAt(i);
                _effectPool.Return(state.View);
            }
        }

        private void TickDeathDissolves(float deltaTime)
        {
            for (var i = _deathDissolves.Count - 1; i >= 0; i--)
            {
                var state = _deathDissolves[i];
                state.Elapsed += deltaTime;
                var progress = Mathf.Clamp01(state.Elapsed / state.Duration);
                state.View.SetDissolve(progress, _config.EnemyDissolveEdgeColor,
                    _config.EnemyDissolveEdgeWidth);
                if (progress < 1f) continue;
                _deathDissolves.RemoveAt(i);
                _deathDissolvePool.Return(state.View);
            }
        }

        private void TickShellCasings(float deltaTime)
        {
            for (var i = _shellCasings.Count - 1; i >= 0; i--)
            {
                var state = _shellCasings[i];
                state.Elapsed += deltaTime;
                var t = Mathf.Clamp01(state.Elapsed / state.Duration);
                state.Velocity += Vector2.down * (_config.ShellGravity * deltaTime);
                state.Velocity *= Mathf.Exp(-1.8f * deltaTime);
                state.View.transform.position += (Vector3)(state.Velocity * deltaTime);
                state.View.transform.Rotate(0f, 0f, state.AngularSpeed * deltaTime);
                var color = state.Color;
                color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
                state.View.SetRectangleVisual(color, state.Size * Mathf.Lerp(1f, 0.72f, t));
                if (t < 1f) continue;
                _shellCasings.RemoveAt(i);
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
            public Vector2 ShakeDirection;
            public float ShakeDuration;
            public float ShakeRemaining;
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

        private sealed class MuzzleFlashState
        {
            public MuzzleFlashState(WorldEffectView view, IReadOnlyList<Sprite> frames,
                float frameDuration)
            {
                View = view;
                Frames = frames;
                FrameDuration = Mathf.Max(0.01f, frameDuration);
            }
            public readonly WorldEffectView View;
            public readonly IReadOnlyList<Sprite> Frames;
            public readonly float FrameDuration;
            public float Elapsed;
        }

        private sealed class ShellCasingState
        {
            public ShellCasingState(WorldEffectView view, Color color, Vector2 size,
                Vector2 velocity, float angularSpeed, float duration)
            {
                View = view;
                Color = color;
                Size = size;
                Velocity = velocity;
                AngularSpeed = angularSpeed;
                Duration = Mathf.Max(0.01f, duration);
            }
            public readonly WorldEffectView View;
            public readonly Color Color;
            public readonly Vector2 Size;
            public readonly float AngularSpeed;
            public readonly float Duration;
            public Vector2 Velocity;
            public float Elapsed;
        }

        private sealed class DeathDissolveState
        {
            public DeathDissolveState(DeathDissolveView view, float duration)
            {
                View = view;
                Duration = Mathf.Max(0.01f, duration);
            }

            public readonly DeathDissolveView View;
            public readonly float Duration;
            public float Elapsed;
        }
    }
}
