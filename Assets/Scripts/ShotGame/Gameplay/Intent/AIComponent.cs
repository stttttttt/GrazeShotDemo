using System;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.World;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Intent
{
    /// <summary>Demo 敌人三态 AI：追击目标，进入范围后攻击，角色死亡后停止。</summary>
    public sealed class AIComponent : EntityComponent, IEntityTickable, IPawnIntentSource
    {
        private readonly GameplayWorld _world;
        private readonly float _attackRange;
        private float _attackDelayRemaining;
        private PawnIntent _intent;

        public AIComponent(GameplayWorld world, GameEntityId targetId, float attackRange = 5f,
            float initialAttackDelay = 0f)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            TargetId = targetId;
            _attackRange = Mathf.Max(0.1f, attackRange);
            _attackDelayRemaining = Mathf.Max(0f, initialAttackDelay);
        }

        public GameEntityId TargetId { get; private set; }
        public AIState State { get; private set; } = AIState.Chase;

        public void SetTarget(GameEntityId targetId) => TargetId = targetId;

        public void Tick(float deltaTime)
        {
            if (State == AIState.Dead) return;
            _attackDelayRemaining = Mathf.Max(0f, _attackDelayRemaining - Mathf.Max(0f, deltaTime));
            if (!_world.TryGetEntity(TargetId, out var target) || !target.IsAlive)
            {
                ClearContinuousIntent();
                State = AIState.Chase;
                return;
            }

            var offset = target.UnityObject.Transform.position - Owner.UnityObject.Transform.position;
            var direction = ((Vector2)offset).normalized;
            var inAttackRange = offset.sqrMagnitude <= _attackRange * _attackRange;
            State = inAttackRange ? AIState.Attack : AIState.Chase;
            _intent.AimDirection = direction;
            _intent.MoveDirection = inAttackRange ? Vector2.zero : direction;
            SetFire(inAttackRange && _attackDelayRemaining <= 0f);
        }

        public PawnIntent GetIntent() => _intent;

        public void ClearFrameIntent()
        {
            _intent.FirePressed = false;
            _intent.FireReleased = false;
        }

        public void MarkDead()
        {
            State = AIState.Dead;
            ClearContinuousIntent();
        }

        private void SetFire(bool held)
        {
            if (held && !_intent.FireHeld) _intent.FirePressed = true;
            if (!held && _intent.FireHeld) _intent.FireReleased = true;
            _intent.FireHeld = held;
        }

        private void ClearContinuousIntent()
        {
            _intent.MoveDirection = Vector2.zero;
            _intent.AimDirection = Vector2.zero;
            SetFire(false);
        }
    }
}
