using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Projectile;
using ShotGame.Gameplay.World;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Character
{
    /// <summary>右键蓄力与松开冲击波的玩法入口。冲击波会清除范围内敌弹并转化为备弹。</summary>
    public sealed class GrazeComponent : EntityComponent, IEntityUnscaledTickable
    {
        private readonly GameplayWorld _world;
        private readonly AmmoRewardComponent _ammoReward;
        private readonly GameplayFactHub _facts;
        private readonly GrazeConfig _config;
        private readonly LayerMask _projectileMask;
        private readonly HashSet<GameEntityId> _absorbedIds = new HashSet<GameEntityId>();
        private float _chargeElapsed;

        public GrazeComponent(GameplayWorld world, AmmoRewardComponent ammoReward, GameplayFactHub facts,
            GrazeConfig config, LayerMask projectileMask)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _ammoReward = ammoReward ?? throw new ArgumentNullException(nameof(ammoReward));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _projectileMask = projectileMask;
        }

        public bool IsCharging { get; private set; }
        public float Charge01 => Mathf.Clamp01(_chargeElapsed / _config.ShockwaveChargeDuration);
        public float PreviewRadius => Mathf.Lerp(_config.MinimumShockwaveRadius,
            _config.MaximumShockwaveRadius, Charge01);

        // 保留给旧结算展示读取，实际玩法不再使用阶段窗口。
        public GrazePhase Phase => IsCharging ? GrazePhase.Active : GrazePhase.Idle;
        public float NormalizedPhaseProgress => Charge01;

        public void ApplyIntent(bool held, bool released)
        {
            if (held && !IsCharging)
            {
                IsCharging = true;
                _chargeElapsed = 0f;
                _facts.Publish(new GrazeChargeStartedFact(Owner.Id));
            }
            if (IsCharging && released) ReleaseShockwave();
        }

        public void UnscaledTick(float unscaledDeltaTime)
        {
            if (!IsCharging || unscaledDeltaTime <= 0f) return;
            _chargeElapsed = Mathf.Min(_config.ShockwaveChargeDuration,
                _chargeElapsed + unscaledDeltaTime);
        }

        public void NotifyOwnerDied()
        {
            IsCharging = false;
            _chargeElapsed = 0f;
        }

        private void ReleaseShockwave()
        {
            var charge = Charge01;
            var radius = PreviewRadius;
            var position = (Vector2)Owner.UnityObject.Transform.position;
            var absorbed = AbsorbProjectiles(position, radius);
            var ammo = _ammoReward.AddFromAbsorbedProjectiles(absorbed);
            var healing = Owner is CharacterEntity character
                ? character.RestoreHealth(absorbed * _config.HealthPerAbsorbedProjectile)
                : 0f;
            IsCharging = false;
            _chargeElapsed = 0f;
            _facts.Publish(new GrazeShockwaveReleasedFact(Owner.Id, position, charge,
                radius, absorbed, ammo, healing));
        }

        private int AbsorbProjectiles(Vector2 center, float radius)
        {
            _absorbedIds.Clear();
            var colliders = Physics2D.OverlapCircleAll(center, radius, _projectileMask);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!_world.TryGetEntity(colliders[i], out var entity) ||
                    entity.Category != EntityCategory.Projectile || !entity.IsAlive) continue;
                var projectile = entity.GetComponent<ProjectileComponent>();
                if (projectile == null || projectile.SourceTeam == Owner.Team ||
                    !_absorbedIds.Add(entity.Id)) continue;
                _world.Despawn(entity.Id);
            }
            return _absorbedIds.Count;
        }

        public override void Dispose() => _absorbedIds.Clear();
    }
}
