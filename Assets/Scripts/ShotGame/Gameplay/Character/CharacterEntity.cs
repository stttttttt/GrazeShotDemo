using ShotGame.Gameplay.Combat;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
using UnityEngine;
using GameEntityId = ShotGame.Gameplay.Entity.EntityId;

namespace ShotGame.Gameplay.Character
{
    public sealed class CharacterEntity : ShotGame.Gameplay.Entity.Entity, IDamageable
    {
        private readonly GameplayFactHub _facts;

        public CharacterEntity(GameEntityId id, EntityCategory category, EntityTeam team,
            EntityUnityObject unityObject, GameplayFactHub facts)
            : base(id, category, team, unityObject) => _facts = facts;

        public DamageResult TakeDamage(in DamageRequest request)
        {
            if (!IsAlive || request.Payload.Amount <= 0f) return new DamageResult(0f, CurrentHealth, false);
            if (Team != EntityTeam.Neutral && request.SourceTeam == Team)
                return new DamageResult(0f, CurrentHealth, false);

            var attributes = GetComponent<AttributeComponent>();
            if (attributes == null) return new DamageResult(0f, 0f, false);
            var reduction = Mathf.Clamp01(attributes.GetCurrent(AttributeType.DamageReduction));
            var applied = request.Payload.Amount * (1f - reduction);
            attributes.ChangeHealth(-applied);
            GetComponent<ChargeComponent>()?.NotifyDamaged();
            var killed = attributes.GetCurrent(AttributeType.Health) <= 0f;
            var result = new DamageResult(applied, attributes.GetCurrent(AttributeType.Health), killed);
            _facts?.Publish(new CharacterDamagedFact(Id, request.SourceId, result));
            if (killed)
            {
                GetComponent<GrazeComponent>()?.NotifyOwnerDied();
                GetComponent<AIComponent>()?.MarkDead();
                Kill();
                _facts?.Publish(new CharacterDiedFact(Id, Category, request.SourceId));
            }
            return result;
        }

        /// <summary>恢复生命并返回实际恢复值；生命已满或角色死亡时返回 0。</summary>
        public float RestoreHealth(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            var attributes = GetComponent<AttributeComponent>();
            if (attributes == null) return 0f;
            var restored = attributes.ChangeHealth(amount);
            if (restored > 0f)
                _facts?.Publish(new CharacterHealedFact(Id, restored,
                    attributes.GetCurrent(AttributeType.Health)));
            return restored;
        }

        private float CurrentHealth => GetComponent<AttributeComponent>()?.GetCurrent(AttributeType.Health) ?? 0f;
    }
}
