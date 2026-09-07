using ShotGame.Gameplay.Entity;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    /// <summary>玩家位移冲刺；只负责移动和冷却，不提供无敌或伤害免疫。</summary>
    public sealed class DashComponent : EntityComponent, IEntityTickable
    {
        private readonly MovementComponent _movement;
        private readonly float _distance;
        private readonly float _duration;
        private readonly float _cooldown;
        private float _cooldownRemaining;

        public DashComponent(MovementComponent movement, float distance, float duration, float cooldown)
        {
            _movement = movement;
            _distance = Mathf.Max(0.01f, distance);
            _duration = Mathf.Max(0.01f, duration);
            _cooldown = Mathf.Max(_duration, cooldown);
        }

        public bool IsReady => _cooldownRemaining <= 0f;

        public void Tick(float deltaTime) =>
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

        public bool TryDash(bool pressed, Vector2 moveDirection, Vector2 aimDirection)
        {
            if (!pressed || !IsReady || _movement == null) return false;
            var direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection : aimDirection;
            if (direction.sqrMagnitude <= 0.0001f) return false;
            _movement.StartDash(direction, _distance, _duration);
            _cooldownRemaining = _cooldown;
            return true;
        }
    }
}
