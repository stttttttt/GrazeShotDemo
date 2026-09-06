using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Entity;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    public sealed class AttributeComponent : EntityComponent
    {
        private readonly Dictionary<AttributeType, AttributeValue> _values =
            new Dictionary<AttributeType, AttributeValue>();

        public AttributeComponent(float maxHealth = 100f, float moveSpeed = 4f)
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            Add(AttributeType.MaxHealth, maxHealth, 1f, float.MaxValue);
            Add(AttributeType.Health, maxHealth, 0f, maxHealth);
            Add(AttributeType.MaxMoveSpeed, moveSpeed, 0f, float.MaxValue);
            Add(AttributeType.MoveSpeed, moveSpeed, 0f, moveSpeed);
            Add(AttributeType.DamageMultiplier, 1f, 0f, 100f);
            Add(AttributeType.DamageReduction, 0f, 0f, 0.95f);
        }

        public float GetCurrent(AttributeType type) => Get(type).CurrentValue;
        public float GetBase(AttributeType type) => Get(type).BaseValue;

        public void SetCurrent(AttributeType type, float value) => Get(type).SetCurrent(value);

        public void SetMaxHealth(float value, bool fillHealth)
        {
            var maxHealth = Mathf.Max(1f, value);
            Get(AttributeType.MaxHealth).SetCurrent(maxHealth);
            Get(AttributeType.MaxHealth).SetBase(maxHealth);
            Get(AttributeType.Health).SetMaximum(maxHealth);
            if (fillHealth) Get(AttributeType.Health).SetCurrent(maxHealth);
        }

        public float ChangeHealth(float delta)
        {
            var health = Get(AttributeType.Health);
            var before = health.CurrentValue;
            health.SetCurrent(before + delta);
            return health.CurrentValue - before;
        }

        private void Add(AttributeType type, float value, float min, float max) =>
            _values.Add(type, new AttributeValue(value, min, max));

        private AttributeValue Get(AttributeType type)
        {
            if (!_values.TryGetValue(type, out var value))
                throw new ArgumentOutOfRangeException(nameof(type), type, "未配置该角色属性。");
            return value;
        }
    }
}
