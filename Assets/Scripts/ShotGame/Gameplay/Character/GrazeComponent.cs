using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Projectile;
using ShotGame.Gameplay.Time;
using ShotGame.Gameplay.World;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Character
{
    /// <summary>主动擦弹窗口、敌弹候选和擦弹结果的唯一判定入口。</summary>
    public sealed class GrazeComponent : EntityComponent, IEntityUnscaledTickable, IEntityFixedTickable
    {
        private readonly GameplayWorld _world;
        private readonly MovementComponent _movement;
        private readonly ChargeComponent _charge;
        private readonly TimeDilationController _timeDilation;
        private readonly GameplayFactHub _facts;
        private readonly GrazeConfig _config;
        private readonly LayerMask _projectileMask;
        private readonly Dictionary<GameEntityId, GrazeCandidate> _candidates = new Dictionary<GameEntityId, GrazeCandidate>();
        private readonly HashSet<GameEntityId> _resolvedProjectileIds = new HashSet<GameEntityId>();
        private readonly HashSet<GameEntityId> _seenProjectileIds = new HashSet<GameEntityId>();
        private readonly List<GameEntityId> _candidateIds = new List<GameEntityId>();
        private float _totalElapsed;

        public GrazeComponent(GameplayWorld world, MovementComponent movement, ChargeComponent charge,
            TimeDilationController timeDilation, GameplayFactHub facts, GrazeConfig config,
            LayerMask projectileMask)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _movement = movement ?? throw new ArgumentNullException(nameof(movement));
            _charge = charge ?? throw new ArgumentNullException(nameof(charge));
            _timeDilation = timeDilation ?? throw new ArgumentNullException(nameof(timeDilation));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _projectileMask = projectileMask;
        }

        public GrazePhase Phase { get; private set; } = GrazePhase.Idle;
        public bool IsEffectiveWindow => Phase == GrazePhase.Perfect || Phase == GrazePhase.Active;

        public void ApplyIntent(bool pressed)
        {
            if (!pressed || Phase != GrazePhase.Idle) return;
            _totalElapsed = 0f;
            ChangePhase(GrazePhase.Startup);
        }

        public void UnscaledTick(float unscaledDeltaTime)
        {
            if (Phase == GrazePhase.Idle || unscaledDeltaTime <= 0f) return;
            _totalElapsed += unscaledDeltaTime;
            var next = GetPhase(_totalElapsed);
            if (next != Phase) ChangePhase(next);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            _seenProjectileIds.Clear();
            var position = (Vector2)Owner.UnityObject.Transform.position;
            var colliders = Physics2D.OverlapCircleAll(position, _config.GrazeSensorRadius, _projectileMask);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!_world.TryGetEntity(colliders[i], out var entity) || !IsEnemyProjectile(entity)) continue;
                if (!_seenProjectileIds.Add(entity.Id) || _resolvedProjectileIds.Contains(entity.Id)) continue;
                if (!_candidates.TryGetValue(entity.Id, out var candidate))
                {
                    candidate = new GrazeCandidate();
                    _candidates.Add(entity.Id, candidate);
                }
                if (IsEffectiveWindow) candidate.OverlappedActiveWindow = true;
            }

            _candidateIds.Clear();
            _candidateIds.AddRange(_candidates.Keys);
            for (var i = 0; i < _candidateIds.Count; i++)
            {
                var projectileId = _candidateIds[i];
                if (_seenProjectileIds.Contains(projectileId)) continue;
                var candidate = _candidates[projectileId];
                if (_world.TryGetEntity(projectileId, out var projectile) && projectile.IsAlive &&
                    candidate.OverlappedActiveWindow && !candidate.HitHurtbox)
                    Resolve(projectileId, false);
                _candidates.Remove(projectileId);
            }
        }

        public bool TryInterceptProjectile(GameEntityId projectileId)
        {
            if (!projectileId.IsValid || !_world.TryGetEntity(projectileId, out var projectile) ||
                !IsEnemyProjectile(projectile)) return false;
            if (Phase == GrazePhase.Perfect)
            {
                if (_resolvedProjectileIds.Add(projectileId)) Resolve(projectileId, true);
                _candidates.Remove(projectileId);
                return true;
            }
            NotifyProjectileHit(projectileId);
            return false;
        }

        public void NotifyProjectileHit(GameEntityId projectileId)
        {
            if (!projectileId.IsValid || _resolvedProjectileIds.Contains(projectileId)) return;
            if (!_candidates.TryGetValue(projectileId, out var candidate))
            {
                candidate = new GrazeCandidate();
                _candidates.Add(projectileId, candidate);
            }
            candidate.HitHurtbox = true;
        }

        public float GetIncomingDamageMultiplier(GameEntityId projectileId)
        {
            if (!projectileId.IsValid || !_world.TryGetEntity(projectileId, out var projectile) ||
                !IsEnemyProjectile(projectile)) return 1f;
            if (!_movement.IsRecoilMoving || IsEffectiveWindow) return 1f;
            return 1f - _config.RecoilDamageReduction;
        }

        public void NotifyOwnerDied()
        {
            _candidates.Clear();
            _resolvedProjectileIds.Clear();
            _charge.Clear();
            _timeDilation.Clear();
        }

        public override void Dispose()
        {
            _candidates.Clear();
            _resolvedProjectileIds.Clear();
            _seenProjectileIds.Clear();
            _candidateIds.Clear();
        }

        private GrazePhase GetPhase(float elapsed)
        {
            if (elapsed < _config.StartupDuration) return GrazePhase.Startup;
            if (elapsed < _config.StartupDuration + _config.PerfectDuration) return GrazePhase.Perfect;
            if (elapsed < _config.StartupDuration + _config.PerfectDuration + _config.ActiveDuration)
                return GrazePhase.Active;
            return elapsed < _config.TotalCooldown ? GrazePhase.Cooldown : GrazePhase.Idle;
        }

        private bool IsEnemyProjectile(ShotGame.Gameplay.Entity.Entity entity)
        {
            if (!entity.IsAlive || entity.Category != EntityCategory.Projectile) return false;
            var projectile = entity.GetComponent<ProjectileComponent>();
            return projectile != null && projectile.SourceId.IsValid &&
                   (Owner.Team == EntityTeam.Neutral || projectile.SourceTeam != Owner.Team);
        }

        private void Resolve(GameEntityId projectileId, bool perfect)
        {
            _resolvedProjectileIds.Add(projectileId);
            var momentum = _movement.IsRecoilMoving;
            var result = perfect
                ? (momentum ? GrazeResultType.PerfectMomentum : GrazeResultType.PerfectDefensive)
                : (momentum ? GrazeResultType.Momentum : GrazeResultType.Defensive);
            if (momentum)
            {
                _charge.AddMomentum(perfect);
                _timeDilation.RequestMomentum(perfect);
            }
            _facts.Publish(new GrazeSucceededFact(Owner.Id, projectileId, result,
                _charge.ComboCount, _charge.ChargeLevel));
        }

        private void ChangePhase(GrazePhase next)
        {
            var previous = Phase;
            Phase = next;
            _facts.Publish(new GrazePhaseChangedFact(Owner.Id, previous, next));
        }

        private sealed class GrazeCandidate
        {
            public bool OverlappedActiveWindow;
            public bool HitHurtbox;
        }
    }
}
