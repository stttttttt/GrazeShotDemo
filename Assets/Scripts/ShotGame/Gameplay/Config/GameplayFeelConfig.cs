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
            [Min(0.01f)] [SerializeField] private float _muzzleSize = 0.55f;
            [Min(0f)] [SerializeField] private float _cameraKick = 0.08f;
            [Min(0f)] [SerializeField] private float _shakeStrength = 0.05f;
            [Min(0f)] [SerializeField] private float _shakeDuration = 0.08f;
            [Min(0f)] [SerializeField] private float _hitStopDuration = 0.01f;
            [Min(0f)] [SerializeField] private float _hitStopCooldown = 0.06f;

            public WeaponConfig Weapon => _weapon;
            public AudioClip FireAudio => _fireAudio;
            public float MuzzleSize => _muzzleSize;
            public float CameraKick => _cameraKick;
            public float ShakeStrength => _shakeStrength;
            public float ShakeDuration => _shakeDuration;
            public float HitStopDuration => _hitStopDuration;
            public float HitStopCooldown => _hitStopCooldown;
        }

        [Header("武器反馈")]
        [SerializeField] private WeaponFeedbackEntry[] _weaponFeedback = Array.Empty<WeaponFeedbackEntry>();

        [Header("枪口焰序列帧与弹壳")]
        [SerializeField] private Sprite[] _muzzleFrames = Array.Empty<Sprite>();
        [Min(0.01f)] [SerializeField] private float _muzzleFrameDuration = 0.035f;
        [SerializeField] private Color _shellColor = new Color(0.9f, 0.62f, 0.18f, 1f);
        [SerializeField] private Vector2 _shellSize = new Vector2(0.26f, 0.09f);
        [SerializeField] private Vector2 _shellEjectSpeedRange = new Vector2(1.5f, 2.8f);
        [Min(0f)] [SerializeField] private float _shellGravity = 4f;
        [Min(0.01f)] [SerializeField] private float _shellLifetime = 0.7f;

        [Header("命中表现")]
        [SerializeField] private Sprite[] _hitEffectFrames = Array.Empty<Sprite>();
        [Min(0.01f)] [SerializeField] private float _hitEffectFrameDuration = 0.055f;
        [Min(0.01f)] [SerializeField] private float _hitEffectSize = 0.42f;
        [Min(0.01f)] [SerializeField] private float _lethalHitEffectSize = 0.72f;
        [Min(0f)] [SerializeField] private float _enemyHitShakeDistance = 0.1f;
        [Min(0.01f)] [SerializeField] private float _enemyHitShakeDuration = 0.12f;

        [Header("命中与角色")]
        [SerializeField] private GameObject _worldEffectPrefab;
        [SerializeField] private Color _hitFlashColor = Color.white;
        [SerializeField] private Color _playerDamageColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _enemyDamageNumberColor = new Color(1f, 0.88f, 0.3f, 1f);
        [SerializeField] private Color _empoweredDamageNumberColor = new Color(0.3f, 1f, 1f, 1f);
        [SerializeField] private Color _lethalDamageNumberColor = new Color(1f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color _healingNumberColor = new Color(0.35f, 1f, 0.45f, 1f);
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
        [Min(0.01f)] [SerializeField] private float _healthBarHitDuration = 0.16f;
        [Min(0f)] [SerializeField] private float _healthBarHitShakeDistance = 5f;
        [Min(0f)] [SerializeField] private float _healthBarHitPulseScale = 0.12f;

        [Header("敌人死亡溶解")]
        [SerializeField] private Material _enemyDissolveMaterial;
        [Min(0.01f)] [SerializeField] private float _enemyDissolveDuration = 0.72f;
        [SerializeField] private Color _enemyDissolveEdgeColor = new Color(1f, 0.35f, 0.05f, 1f);
        [Range(0.001f, 0.3f)] [SerializeField] private float _enemyDissolveEdgeWidth = 0.08f;
        [Min(1)] [SerializeField] private int _enemyDissolvePoolLimit = 12;

        [Header("擦弹窗口表现")]
        [SerializeField] private Material _grazeRingMaterial;
        [SerializeField] private Color _grazeStartupColor = new Color(0.25f, 0.7f, 1f, 1f);
        [SerializeField] private Color _grazePerfectColor = new Color(0.35f, 1f, 0.72f, 1f);
        [SerializeField] private Color _grazeActiveColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _grazeCooldownColor = new Color(0.3f, 0.38f, 0.48f, 1f);
        [Min(1f)] [SerializeField] private float _grazeStartupScale = 1.45f;
        [Min(0.01f)] [SerializeField] private float _grazeStartupVisualDuration = 0.16f;
        [Range(0.005f, 0.15f)] [SerializeField] private float _grazeRingThickness = 0.018f;
        [Range(0.001f, 0.08f)] [SerializeField] private float _grazeRingSoftness = 0.006f;
        [Range(0.05f, 1f)] [SerializeField] private float _grazeActiveAlpha = 0.3f;
        [Min(1f)] [SerializeField] private float _grazePerfectBrightness = 2.5f;
        [Min(1f)] [SerializeField] private float _grazePerfectPulseScale = 1.8f;
        [Min(0.01f)] [SerializeField] private float _grazePerfectPulseDuration = 0.28f;

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
        [SerializeField] private AudioClip _perfectReadyAudio;

        public IReadOnlyList<WeaponFeedbackEntry> WeaponFeedback => _weaponFeedback;
        public IReadOnlyList<Sprite> MuzzleFrames => _muzzleFrames;
        public float MuzzleFrameDuration => _muzzleFrameDuration;
        public Color ShellColor => _shellColor;
        public Vector2 ShellSize => _shellSize;
        public Vector2 ShellEjectSpeedRange => _shellEjectSpeedRange;
        public float ShellGravity => _shellGravity;
        public float ShellLifetime => _shellLifetime;
        public IReadOnlyList<Sprite> HitEffectFrames => _hitEffectFrames;
        public float HitEffectFrameDuration => _hitEffectFrameDuration;
        public float HitEffectSize => _hitEffectSize;
        public float LethalHitEffectSize => _lethalHitEffectSize;
        public float EnemyHitShakeDistance => _enemyHitShakeDistance;
        public float EnemyHitShakeDuration => _enemyHitShakeDuration;
        public Color HitFlashColor => _hitFlashColor;
        public GameObject WorldEffectPrefab => _worldEffectPrefab;
        public Color PlayerDamageColor => _playerDamageColor;
        public Color EnemyDamageNumberColor => _enemyDamageNumberColor;
        public Color EmpoweredDamageNumberColor => _empoweredDamageNumberColor;
        public Color LethalDamageNumberColor => _lethalDamageNumberColor;
        public Color HealingNumberColor => _healingNumberColor;
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
        public float HealthBarHitDuration => _healthBarHitDuration;
        public float HealthBarHitShakeDistance => _healthBarHitShakeDistance;
        public float HealthBarHitPulseScale => _healthBarHitPulseScale;
        public Material EnemyDissolveMaterial => _enemyDissolveMaterial;
        public float EnemyDissolveDuration => _enemyDissolveDuration;
        public Color EnemyDissolveEdgeColor => _enemyDissolveEdgeColor;
        public float EnemyDissolveEdgeWidth => _enemyDissolveEdgeWidth;
        public int EnemyDissolvePoolLimit => _enemyDissolvePoolLimit;
        public Material GrazeRingMaterial => _grazeRingMaterial;
        public Color GrazeStartupColor => _grazeStartupColor;
        public Color GrazePerfectColor => _grazePerfectColor;
        public Color GrazeActiveColor => _grazeActiveColor;
        public Color GrazeCooldownColor => _grazeCooldownColor;
        public float GrazeStartupScale => _grazeStartupScale;
        public float GrazeStartupVisualDuration => _grazeStartupVisualDuration;
        public float GrazeRingThickness => _grazeRingThickness;
        public float GrazeRingSoftness => _grazeRingSoftness;
        public float GrazeActiveAlpha => _grazeActiveAlpha;
        public float GrazePerfectBrightness => _grazePerfectBrightness;
        public float GrazePerfectPulseScale => _grazePerfectPulseScale;
        public float GrazePerfectPulseDuration => _grazePerfectPulseDuration;
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
        public AudioClip PerfectReadyAudio => _perfectReadyAudio;

        public WeaponFeedbackEntry FindWeapon(WeaponConfig weapon)
        {
            for (var i = 0; i < _weaponFeedback.Length; i++)
                if (_weaponFeedback[i] != null && _weaponFeedback[i].Weapon == weapon)
                    return _weaponFeedback[i];
            return null;
        }

        public void Validate()
        {
            if (_muzzleFrames == null || _muzzleFrames.Length == 0 || _muzzleFrameDuration <= 0f ||
                _shellSize.x <= 0f || _shellSize.y <= 0f || _shellLifetime <= 0f ||
                _shellEjectSpeedRange.x < 0f || _shellEjectSpeedRange.y < _shellEjectSpeedRange.x)
                throw new InvalidOperationException("GameplayFeelConfig 的枪口焰或弹壳配置无效。");
            if (_hitEffectFrames == null || _hitEffectFrames.Length == 0 ||
                _hitEffectFrameDuration <= 0f || _hitEffectSize <= 0f || _lethalHitEffectSize <= 0f ||
                _enemyHitShakeDistance < 0f || _enemyHitShakeDuration <= 0f)
                throw new InvalidOperationException("GameplayFeelConfig 的命中表现配置无效。");
            if (_damageNumberLimit <= 0 || _worldHealthBarLimit <= 0)
                throw new InvalidOperationException("GameplayFeelConfig 的 UI 对象上限必须大于 0。");
            if (_healthImmediateDuration <= 0f || _healthDelayedDuration <= 0f ||
                _healthBarHitDuration <= 0f || _healthBarHitShakeDistance < 0f ||
                _healthBarHitPulseScale < 0f || _cameraRecovery <= 0f)
                throw new InvalidOperationException("GameplayFeelConfig 包含非法动画时长。");
            if (_enemyDissolveMaterial == null || _enemyDissolveDuration <= 0f ||
                _enemyDissolveEdgeWidth <= 0f || _enemyDissolvePoolLimit <= 0)
                throw new InvalidOperationException("GameplayFeelConfig 的敌人死亡溶解配置无效。");
            if (_grazeStartupScale < 1f || _grazeStartupVisualDuration <= 0f ||
                _grazeRingThickness <= 0f || _grazeRingSoftness <= 0f ||
                _grazePerfectPulseScale < 1f || _grazePerfectPulseDuration <= 0f)
                throw new InvalidOperationException("GameplayFeelConfig 包含非法擦弹表现参数。");
            if (_grazeRingMaterial == null)
                throw new InvalidOperationException("GameplayFeelConfig 缺少擦弹圆环材质。");
        }
    }
}
