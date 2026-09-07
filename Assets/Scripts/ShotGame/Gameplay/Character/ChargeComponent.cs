using System;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Weapon;
using UnityEngine;

namespace ShotGame.Gameplay.Character
{
    /// <summary>管理玩家级擦弹充能、连段，以及下一次有效射击的强化快照。</summary>
    public sealed class ChargeComponent : EntityComponent, IEntityUnscaledTickable
    {
        private readonly GrazeConfig _config;
        private readonly GameplayFactHub _facts;

        public ChargeComponent(GrazeConfig config, GameplayFactHub facts)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        }

        public int ChargeLevel { get; private set; }
        public int ComboCount { get; private set; }
        public float ChargeRemaining { get; private set; }
        public float ComboGraceRemaining { get; private set; }

        public void UnscaledTick(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime <= 0f) return;
            if (ComboGraceRemaining > 0f)
            {
                ComboGraceRemaining = Mathf.Max(0f, ComboGraceRemaining - unscaledDeltaTime);
                if (ComboGraceRemaining <= 0f && ComboCount != 0)
                {
                    var previousLevel = ChargeLevel;
                    ComboCount = 0;
                    Publish(previousLevel);
                }
            }

            if (ChargeRemaining <= 0f) return;
            ChargeRemaining = Mathf.Max(0f, ChargeRemaining - unscaledDeltaTime);
            if (ChargeRemaining <= 0f) SetState(0, 0);
        }

        public void AddMomentum(bool perfect)
        {
            var previousLevel = ChargeLevel;
            ComboCount++;
            ComboGraceRemaining = _config.ComboGracePeriod;
            var earnedLevel = perfect
                ? (ComboCount >= 3 ? 3 : 2)
                : (ComboCount >= 2 ? 2 : 1);
            ChargeLevel = Mathf.Clamp(Mathf.Max(ChargeLevel, earnedLevel), 0, _config.MaxChargeLevel);
            ChargeRemaining = _config.ChargeDuration;
            Publish(previousLevel);
        }

        public void NotifyDamaged()
        {
            var previousLevel = ChargeLevel;
            ComboCount = 0;
            ComboGraceRemaining = 0f;
            if (ChargeLevel >= 2)
            {
                ChargeLevel = 1;
                ChargeRemaining = _config.ChargeDuration;
            }
            Publish(previousLevel);
        }

        public ShotPackage CreateEmpoweredPackage(in ShotPackage basePackage, out int usedLevel)
        {
            usedLevel = ChargeLevel;
            if (usedLevel <= 0) return basePackage;
            return basePackage.WithEmpowerment(usedLevel,
                _config.GetDamageMultiplier(usedLevel),
                _config.GetRadiusMultiplier(usedLevel),
                _config.GetPenetrations(usedLevel));
        }

        public void Consume(int usedLevel)
        {
            if (usedLevel <= 0 || ChargeLevel <= 0) return;
            SetState(0, 0);
        }

        public void Clear() => SetState(0, 0);

        private void SetState(int level, int combo)
        {
            var previousLevel = ChargeLevel;
            ChargeLevel = level;
            ComboCount = combo;
            ChargeRemaining = 0f;
            ComboGraceRemaining = 0f;
            Publish(previousLevel);
        }

        private void Publish(int previousLevel)
        {
            if (Owner == null) return;
            _facts.Publish(new ChargeChangedFact(Owner.Id, previousLevel, ChargeLevel,
                ComboCount, ChargeRemaining));
        }
    }
}
