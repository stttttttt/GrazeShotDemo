using System;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [CreateAssetMenu(fileName = "GrazeConfig", menuName = "Shot Game/Graze Config")]
    public sealed class GrazeConfig : ScriptableObject
    {
        [Header("蓄力冲击波（真实秒）")]
        [Min(0.05f)] [SerializeField] private float _shockwaveChargeDuration = 0.9f;
        [Min(0.1f)] [SerializeField] private float _minimumShockwaveRadius = 1.1f;
        [Min(0.1f)] [SerializeField] private float _maximumShockwaveRadius = 3.4f;
        [Tooltip("冲击波每成功消除一枚敌弹时恢复的生命。")]
        [Min(0f)] [SerializeField] private float _healthPerAbsorbedProjectile = 1f;

        [Header("擦弹窗口（真实秒）")]
        [Min(0f)] [SerializeField] private float _startupDuration = 0.03f;
        [Min(0.01f)] [SerializeField] private float _perfectDuration = 0.12f;
        [Min(0.01f)] [SerializeField] private float _activeDuration = 0.22f;
        [Min(0.01f)] [SerializeField] private float _totalCooldown = 0.6f;
        [Min(0.01f)] [SerializeField] private float _grazeSensorRadius = 0.9f;
        [Range(0f, 0.95f)] [SerializeField] private float _recoilDamageReduction = 0.35f;

        [Header("充能与连段（真实秒）")]
        [Range(1, 3)] [SerializeField] private int _maxChargeLevel = 3;
        [Min(0.01f)] [SerializeField] private float _chargeDuration = 6f;
        [Min(0.01f)] [SerializeField] private float _comboGracePeriod = 1.4f;
        [SerializeField] private Vector3 _damageMultipliers = new Vector3(1.25f, 1.5f, 2f);
        [SerializeField] private Vector3 _radiusMultipliers = new Vector3(1.15f, 1.3f, 1.6f);
        [SerializeField] private Vector3Int _penetrations = new Vector3Int(0, 1, 2);

        [Header("子弹时间（真实秒）")]
        [Range(0.01f, 1f)] [SerializeField] private float _momentumTimeScale = 0.35f;
        [Min(0.01f)] [SerializeField] private float _momentumDuration = 0.18f;
        [Range(0.01f, 1f)] [SerializeField] private float _perfectTimeScale = 0.2f;
        [Min(0.01f)] [SerializeField] private float _perfectSlowTimeDuration = 0.26f;
        [Min(0.01f)] [SerializeField] private float _maxAccumulatedDuration = 0.5f;
        [Range(0.01f, 1f)] [SerializeField] private float _playerMotionScaleDuringSlowTime = 0.75f;

        public float StartupDuration => _startupDuration;
        public float ShockwaveChargeDuration => _shockwaveChargeDuration;
        public float MinimumShockwaveRadius => _minimumShockwaveRadius;
        public float MaximumShockwaveRadius => _maximumShockwaveRadius;
        public float HealthPerAbsorbedProjectile => _healthPerAbsorbedProjectile;
        public float PerfectDuration => _perfectDuration;
        public float ActiveDuration => _activeDuration;
        public float TotalCooldown => _totalCooldown;
        public float GrazeSensorRadius => _grazeSensorRadius;
        public float RecoilDamageReduction => _recoilDamageReduction;
        public int MaxChargeLevel => _maxChargeLevel;
        public float ChargeDuration => _chargeDuration;
        public float ComboGracePeriod => _comboGracePeriod;
        public float MomentumTimeScale => _momentumTimeScale;
        public float MomentumDuration => _momentumDuration;
        public float PerfectTimeScale => _perfectTimeScale;
        public float PerfectSlowTimeDuration => _perfectSlowTimeDuration;
        public float MaxAccumulatedDuration => _maxAccumulatedDuration;
        public float PlayerMotionScaleDuringSlowTime => _playerMotionScaleDuringSlowTime;

        public float GetDamageMultiplier(int level) => GetLevelValue(_damageMultipliers, level, 1f);
        public float GetRadiusMultiplier(int level) => GetLevelValue(_radiusMultipliers, level, 1f);
        public int GetPenetrations(int level) => level <= 0 ? 0 : _penetrations[Mathf.Clamp(level - 1, 0, 2)];

        public void Validate()
        {
            if (_shockwaveChargeDuration <= 0f || _minimumShockwaveRadius <= 0f ||
                _maximumShockwaveRadius < _minimumShockwaveRadius || _healthPerAbsorbedProjectile < 0f)
                throw new InvalidOperationException("蓄力冲击波配置无效。");
            if (_startupDuration < 0f || _perfectDuration <= 0f || _activeDuration <= 0f)
                throw new InvalidOperationException("擦弹窗口时长配置无效。");
            var effectiveEnd = _startupDuration + _perfectDuration + _activeDuration;
            if (_totalCooldown < effectiveEnd)
                throw new InvalidOperationException("擦弹总冷却不能短于 Startup、Perfect 与 Active 之和。");
            if (_grazeSensorRadius <= 0f || _chargeDuration <= 0f || _comboGracePeriod <= 0f)
                throw new InvalidOperationException("擦弹半径、充能和连段时长必须大于 0。");
            if (_maxChargeLevel < 1 || _maxChargeLevel > 3)
                throw new InvalidOperationException("当前 Demo 的最大充能等级必须在 1 至 3 之间。");
            ValidatePositive(_damageMultipliers, "伤害倍率");
            ValidatePositive(_radiusMultipliers, "弹丸半径倍率");
            if (_momentumTimeScale <= 0f || _perfectTimeScale <= 0f || _maxAccumulatedDuration <= 0f)
                throw new InvalidOperationException("子弹时间配置无效。");
        }

        private static float GetLevelValue(Vector3 values, int level, float defaultValue) =>
            level <= 0 ? defaultValue : values[Mathf.Clamp(level - 1, 0, 2)];

        private static void ValidatePositive(Vector3 values, string name)
        {
            if (values.x <= 0f || values.y <= 0f || values.z <= 0f)
                throw new InvalidOperationException($"{name}必须大于 0。");
        }
    }
}
