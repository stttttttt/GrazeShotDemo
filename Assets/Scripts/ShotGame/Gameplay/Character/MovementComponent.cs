using ShotGame.Gameplay.Entity;
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
        private Vector2 _moveDirection;
        private Vector2 _recoilVelocity;

        public MovementComponent(AttributeComponent attributes, float maxRecoilSpeed = 12f,
            float recoilRecovery = 8f)
        {
            _attributes = attributes;
            _maxRecoilSpeed = Mathf.Max(0.01f, maxRecoilSpeed);
            _recoilRecovery = Mathf.Max(0.01f, recoilRecovery);
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
            var moveSpeed = _attributes.GetCurrent(AttributeType.MoveSpeed);
            var velocity = _moveDirection * moveSpeed + _recoilVelocity;
            var rigidbody = Owner.UnityObject.Rigidbody;
            if (rigidbody != null) rigidbody.MovePosition(rigidbody.position + velocity * fixedDeltaTime);
            else Owner.UnityObject.Transform.position += (Vector3)(velocity * fixedDeltaTime);

            _recoilVelocity = Vector2.MoveTowards(_recoilVelocity, Vector2.zero,
                _recoilRecovery * fixedDeltaTime);
        }
    }
}
