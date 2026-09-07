using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Weapon;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [CreateAssetMenu(fileName = "GameplayFeelConfig", menuName = "Shot Game/Gameplay Feel Config")]
    public sealed class GameplayFeelConfig : ScriptableObject
    {
        [Serializable]
        public sealed class WeaponFeedbackEntry
        {
            [SerializeField] private WeaponConfig _weapon;
            [SerializeField] private AudioClip _fireAudio;
            [SerializeField] private Color _muzzleColor = new Color(1f, 0.82f, 0.25f, 1f);
            [Min(0.01f)] [SerializeField] private float _muzzleSize = 0.55f;
            [Min(0f)] [SerializeField] private float _cameraKick = 0.08f;
            [Min(0f)] [SerializeField] private float _shakeStrength = 0.05f;
            [Min(0f)] [SerializeField] private float _shakeDuration = 0.08f;
            [Min(0f)] [SerializeField] private float _hitStopDuration = 0.01f;
            [Min(0f)] [SerializeField] private float _hitStopCooldown = 0.06f;

            public WeaponConfig Weapon => _weapon;
            public AudioClip FireAudio => _fireAudio;
            public Color MuzzleColor => _muzzleColor;
            public float MuzzleSize => _muzzleSize;
            public float CameraKick => _cameraKick;
            public float ShakeStrength => _shakeStrength;
            public float ShakeDuration => _shakeDuration;
            public float HitStopDuration => _hitStopDuration;
            public float HitStopCooldown => _hitStopCooldown;
        }

        [Header("武器反馈")]
        [SerializeField] private WeaponFeedbackEntry[] _weaponFeedback = Array.Empty<WeaponFeedbackEntry>();

        [Header("命中与角色")]
        [SerializeField] private GameObject _worldEffectPrefab;
        [SerializeField] private Color _hitFlashColor = Color.white;
        [SerializeField] private Color _playerDamageColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _enemyDamageNumberColor = new Color(1f, 0.88f, 0.3f, 1f);
        [SerializeField] private Color _empoweredDamageNumberColor = new Color(0.3f, 1f, 1f, 1f);
        [SerializeField] private Color _lethalDamageNumberColor = new Color(1f, 0.35f, 0.2f, 1f);
        [Min(0.01f)] [SerializeField] private float _flashDuration = 0.055f;
        [Min(0.01f)] [SerializeField] private float _damageNumberDuration = 0.6f;
        [Min(1)] [SerializeField] private int _damageNumberLimit = 24;
        [Min(1)] [SerializeField] private int _worldHealthBarLimit = 12;
        [Range(0.05f, 0.9f)] [SerializeField] private float _lowHealthThreshold = 0.3f;

        [Header("头顶血条样式")]
        [SerializeField] private Color _playerHealthBarColor = new Color(0.18f, 0.9f, 0.34f, 1f);
        [SerializeField] private Color _playerDelayedHealthBarColor = new Color(0.7f, 1f, 0.35f, 1f);
        [SerializeField] private Vector2 _playerHealthBarSize = new Vector2(112f, 14f);
        [SerializeField] private Color _enemyHealthBarColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _enemyDelayedHealthBarColor = new Color(1f, 0.72f, 0.18f, 1f);
        [SerializeField] private Vector2 _enemyHealthBarSize = new Vector2(76f, 10f);

        [Header("生命条动画")]
        [Min(0.01f)] [SerializeField] private float _healthImmediateDuration = 0.08f;
        [Min(0f)] [SerializeField] private float _healthDamageHold = 0.22f;
        [Min(0.01f)] [SerializeField] private float _healthDelayedDuration = 0.32f;

        [Header("镜头")]
        [Min(0f)] [SerializeField] private float _damageShakeStrength = 0.13f;
        [Min(0f)] [SerializeField] private float _damageShakeDuration = 0.16f;
        [Min(0f)] [SerializeField] private float _deathShakeStrength = 0.18f;
        [Min(0f)] [SerializeField] private float _deathShakeDuration = 0.2f;
        [Min(0f)] [SerializeField] private float _maxShakeOffset = 0.22f;
        [Min(0f)] [SerializeField] private float _maxCameraKick = 0.18f;
        [Min(0.01f)] [SerializeField] private float _cameraRecovery = 12f;

        [Header("通用声音")]
        [SerializeField] private AudioClip _hitAudio;
        [SerializeField] private AudioClip _wallHitAudio;
        [SerializeField] private AudioClip _playerDamageAudio;
        [SerializeField] private AudioClip _deathAudio;
        [SerializeField] private AudioClip _grazeAudio;
        [SerializeField] private AudioClip _perfectGrazeAudio;

        public IReadOnlyList<WeaponFeedbackEntry> WeaponFeedback => _weaponFeedback;
        public Color HitFlashColor => _hitFlashColor;
        public GameObject WorldEffectPrefab => _worldEffectPrefab;
        public Color PlayerDamageColor => _playerDamageColor;
        public Color EnemyDamageNumberColor => _enemyDamageNumberColor;
        public Color EmpoweredDamageNumberColor => _empoweredDamageNumberColor;
        public Color LethalDamageNumberColor => _lethalDamageNumberColor;
        public float FlashDuration => _flashDuration;
        public float DamageNumberDuration => _damageNumberDuration;
        public int DamageNumberLimit => _damageNumberLimit;
        public int WorldHealthBarLimit => _worldHealthBarLimit;
        public float LowHealthThreshold => _lowHealthThreshold;
        public Color PlayerHealthBarColor => _playerHealthBarColor;
        public Color PlayerDelayedHealthBarColor => _playerDelayedHealthBarColor;
        public Vector2 PlayerHealthBarSize => _playerHealthBarSize;
        public Color EnemyHealthBarColor => _enemyHealthBarColor;
        public Color EnemyDelayedHealthBarColor => _enemyDelayedHealthBarColor;
        public Vector2 EnemyHealthBarSize => _enemyHealthBarSize;
        public float HealthImmediateDuration => _healthImmediateDuration;
        public float HealthDamageHold => _healthDamageHold;
        public float HealthDelayedDuration => _healthDelayedDuration;
        public float DamageShakeStrength => _damageShakeStrength;
        public float DamageShakeDuration => _damageShakeDuration;
        public float DeathShakeStrength => _deathShakeStrength;
        public float DeathShakeDuration => _deathShakeDuration;
        public float MaxShakeOffset => _maxShakeOffset;
        public float MaxCameraKick => _maxCameraKick;
        public float CameraRecovery => _cameraRecovery;
        public AudioClip HitAudio => _hitAudio;
        public AudioClip WallHitAudio => _wallHitAudio;
        public AudioClip PlayerDamageAudio => _playerDamageAudio;
        public AudioClip DeathAudio => _deathAudio;
        public AudioClip GrazeAudio => _grazeAudio;
        public AudioClip PerfectGrazeAudio => _perfectGrazeAudio;

        public WeaponFeedbackEntry FindWeapon(WeaponConfig weapon)
        {
            for (var i = 0; i < _weaponFeedback.Length; i++)
                if (_weaponFeedback[i] != null && _weaponFeedback[i].Weapon == weapon)
                    return _weaponFeedback[i];
            return null;
        }

        public void Validate()
        {
            if (_damageNumberLimit <= 0 || _worldHealthBarLimit <= 0)
                throw new InvalidOperationException("GameplayFeelConfig 的 UI 对象上限必须大于 0。");
            if (_healthImmediateDuration <= 0f || _healthDelayedDuration <= 0f || _cameraRecovery <= 0f)
                throw new InvalidOperationException("GameplayFeelConfig 包含非法动画时长。");
        }
    }
}
