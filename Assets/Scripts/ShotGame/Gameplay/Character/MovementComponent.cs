using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Time;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    /// <summary>统一拥有主动移动和持续后坐速度。</summary>
    public sealed class MovementComponent : EntityComponent, IEntityFixedTickable
    {
        private const float RecoilMovingThreshold = 0.05f;
        private readonly AttributeComponent _attributes;
        private readonly float _maxRecoilSpeed;
        private readonly float _recoilRecovery;
        private readonly TimeDilationController _timeDilation;
        private readonly Bounds _movementBounds;
        private readonly bool _hasMovementBounds;
        private Vector2 _moveDirection;
        private Vector2 _recoilVelocity;

        public MovementComponent(AttributeComponent attributes, float maxRecoilSpeed = 12f,
            float recoilRecovery = 8f, TimeDilationController timeDilation = null,
            Bounds movementBounds = default)
        {
            _attributes = attributes;
            _maxRecoilSpeed = Mathf.Max(0.01f, maxRecoilSpeed);
            _recoilRecovery = Mathf.Max(0.01f, recoilRecovery);
            _timeDilation = timeDilation;
            _movementBounds = movementBounds;
            _hasMovementBounds = movementBounds.size.x > 0f && movementBounds.size.y > 0f;
        }

        public Vector2 RecoilVelocity => _recoilVelocity;
        public bool IsRecoilMoving => _recoilVelocity.sqrMagnitude >
                                      RecoilMovingThreshold * RecoilMovingThreshold;

        public void SetMoveDirection(Vector2 direction) =>
            _moveDirection = Vector2.ClampMagnitude(direction, 1f);

        public void AddImpulse(Vector2 impulse) =>
            _recoilVelocity = Vector2.ClampMagnitude(_recoilVelocity + impulse, _maxRecoilSpeed);

        public void FixedTick(float fixedDeltaTime)
        {
            if (_timeDilation != null && _timeDilation.IsActive)
            {
                var worldScale = Mathf.Max(0.0001f, _timeDilation.CurrentScale);
                fixedDeltaTime *= _timeDilation.PlayerMotionScale / worldScale;
            }
            var moveSpeed = _attributes.GetCurrent(AttributeType.MoveSpeed);
            var velocity = _moveDirection * moveSpeed + _recoilVelocity;
            var rigidbody = Owner.UnityObject.Rigidbody;
            var currentPosition = rigidbody != null
                ? rigidbody.position
                : (Vector2)Owner.UnityObject.Transform.position;
            var targetPosition = currentPosition + velocity * fixedDeltaTime;
            if (_hasMovementBounds) targetPosition = ClampToMovementBounds(targetPosition);
            if (rigidbody != null) rigidbody.MovePosition(targetPosition);
            else Owner.UnityObject.Transform.position = targetPosition;

            _recoilVelocity = Vector2.MoveTowards(_recoilVelocity, Vector2.zero,
                _recoilRecovery * fixedDeltaTime);
        }

        private Vector2 ClampToMovementBounds(Vector2 position)
        {
            var colliderExtents = GetColliderExtents();
            var min = (Vector2)_movementBounds.min + colliderExtents;
            var max = (Vector2)_movementBounds.max - colliderExtents;
            var clamped = new Vector2(
                ClampAxis(position.x, min.x, max.x),
                ClampAxis(position.y, min.y, max.y));

            // 撞到边界时清除朝边界外的后坐分量，避免持续在墙边积累速度。
            if (!Mathf.Approximately(clamped.x, position.x) &&
                Mathf.Sign(_recoilVelocity.x) == Mathf.Sign(position.x - clamped.x))
                _recoilVelocity.x = 0f;
            if (!Mathf.Approximately(clamped.y, position.y) &&
                Mathf.Sign(_recoilVelocity.y) == Mathf.Sign(position.y - clamped.y))
                _recoilVelocity.y = 0f;
            return clamped;
        }

        private Vector2 GetColliderExtents()
        {
            var extents = Vector2.zero;
            var colliders = Owner.UnityObject.Colliders;
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled) continue;
                var boundsExtents = collider.bounds.extents;
                extents.x = Mathf.Max(extents.x, boundsExtents.x);
                extents.y = Mathf.Max(extents.y, boundsExtents.y);
            }
            return extents;
        }

        private static float ClampAxis(float value, float min, float max) =>
            min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
    }
}
