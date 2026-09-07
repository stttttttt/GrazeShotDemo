using System;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Intent;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    public sealed class CharacterController : EntityComponent, IEntityTickable
    {
        private IPawnIntentSource _intentSource;
        private MovementComponent _movement;
        private WeaponUseComponent _weaponUse;
        private EquipmentComponent _equipment;
        private GrazeComponent _graze;
        private AimRotationComponent _aimRotation;
        private DashComponent _dash;

        public override void Initialize()
        {
            _intentSource = Owner.GetComponent<IPawnIntentSource>()
                ?? throw new InvalidOperationException($"角色 {Owner.Id} 缺少意图源。");
            _movement = Owner.GetComponent<MovementComponent>();
            _weaponUse = Owner.GetComponent<WeaponUseComponent>();
            _equipment = Owner.GetComponent<EquipmentComponent>();
            _graze = Owner.GetComponent<GrazeComponent>();
            _aimRotation = Owner.GetComponent<AimRotationComponent>();
            _dash = Owner.GetComponent<DashComponent>();
        }

        public void Tick(float deltaTime)
        {
            var intent = _intentSource.GetIntent();
            _movement?.SetMoveDirection(intent.MoveDirection);
            _aimRotation?.SetAimDirection(intent.AimDirection);
            _equipment?.ApplyIntent(intent);
            _weaponUse?.ApplyIntent(intent);
            _graze?.ApplyIntent(intent.GrazeHeld, intent.GrazeReleased);
            _dash?.TryDash(intent.DashPressed, intent.MoveDirection, intent.AimDirection);
            _intentSource.ClearFrameIntent();
        }
    }

    /// <summary>根据瞄准意图旋转角色视觉节点，不改变碰撞体和逻辑根节点。</summary>
    public sealed class AimRotationComponent : EntityComponent
    {
        // 当前玩家飞船贴图的机头默认朝向本地 +Y，代码瞄准角以 +X 为 0 度。
        private const float VisualAngleOffset = -90f;
        private readonly string _visualRootName;
        private Transform _rotationRoot;

        public AimRotationComponent(string visualRootName = "VisualRoot") =>
            _visualRootName = visualRootName;

        public override void Initialize()
        {
            var entityRoot = Owner.UnityObject.Transform;
            _rotationRoot = !string.IsNullOrWhiteSpace(_visualRootName)
                ? entityRoot.Find(_visualRootName)
                : null;
            if (_rotationRoot == null) _rotationRoot = entityRoot;
        }

        public void SetAimDirection(Vector2 direction)
        {
            if (_rotationRoot == null || direction.sqrMagnitude <= 0.0001f) return;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + VisualAngleOffset;
            _rotationRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
