using ShotGame.Gameplay.Entity;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    public sealed class MovementComponent : EntityComponent, IEntityFixedTickable
    {
        private readonly AttributeComponent _attributes;
        private Vector2 _moveDirection;
        private Vector2 _impulse;

        public MovementComponent(AttributeComponent attributes) => _attributes = attributes;

        public void SetMoveDirection(Vector2 direction) => _moveDirection = Vector2.ClampMagnitude(direction, 1f);
        public void AddImpulse(Vector2 impulse) => _impulse += impulse;

        public void FixedTick(float fixedDeltaTime)
        {
            var speed = _attributes.GetCurrent(AttributeType.MoveSpeed);
            var velocity = _moveDirection * speed + _impulse;
            var rigidbody = Owner.UnityObject.Rigidbody;
            if (rigidbody != null) rigidbody.MovePosition(rigidbody.position + velocity * fixedDeltaTime);
            else Owner.UnityObject.Transform.position += (Vector3)(velocity * fixedDeltaTime);
            _impulse = Vector2.zero;
        }
    }
}
