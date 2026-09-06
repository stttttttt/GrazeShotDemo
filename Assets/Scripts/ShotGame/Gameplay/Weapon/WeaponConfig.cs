using System;
using UnityEngine;

namespace ShotGame.Gameplay.Weapon
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Shot Game/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Header("标识")]
        [SerializeField] private string _displayName = "Weapon";

        [Header("开火")]
        [SerializeField] private WeaponFireMode _fireMode = WeaponFireMode.SemiAutomatic;
        [Min(1)] [SerializeField] private int _magazineSize = 6;
        [Min(0)] [SerializeField] private int _initialReserveAmmo = 48;
        [Min(0.01f)] [SerializeField] private float _fireInterval = 0.45f;
        [Min(0.01f)] [SerializeField] private float _reloadDuration = 1.2f;

        [Header("弹丸")]
        [SerializeField] private GameObject _projectilePrefab;
        [Min(1)] [SerializeField] private int _projectileCount = 1;
        [Range(0f, 180f)] [SerializeField] private float _spreadAngle;
        [Min(0f)] [SerializeField] private float _damage = 10f;
        [Min(0.01f)] [SerializeField] private float _projectileSpeed = 16f;
        [Min(0.01f)] [SerializeField] private float _projectileLifetime = 2f;
        [Min(0f)] [SerializeField] private float _projectileRadius = 0.08f;

        [Header("射击位移")]
        [Min(0f)] [SerializeField] private float _recoilImpulse = 5f;
        [Min(0f)] [SerializeField] private float _muzzleOffset = 0.5f;

        public string DisplayName => _displayName;
        public WeaponFireMode FireMode => _fireMode;
        public int MagazineSize => _magazineSize;
        public int InitialReserveAmmo => _initialReserveAmmo;
        public float FireInterval => _fireInterval;
        public float ReloadDuration => _reloadDuration;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public int ProjectileCount => _projectileCount;
        public float SpreadAngle => _spreadAngle;
        public float Damage => _damage;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float ProjectileRadius => _projectileRadius;
        public float RecoilImpulse => _recoilImpulse;
        public float MuzzleOffset => _muzzleOffset;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_displayName))
                throw new InvalidOperationException($"WeaponConfig {name} 缺少显示名称。");
            if (_magazineSize <= 0) throw new InvalidOperationException($"WeaponConfig {name} 弹匣容量必须大于 0。");
            if (_initialReserveAmmo < 0) throw new InvalidOperationException($"WeaponConfig {name} 备弹不能小于 0。");
            if (_fireInterval <= 0f) throw new InvalidOperationException($"WeaponConfig {name} 开火间隔必须大于 0。");
            if (_reloadDuration <= 0f) throw new InvalidOperationException($"WeaponConfig {name} 换弹时间必须大于 0。");
            if (_projectilePrefab == null) throw new InvalidOperationException($"WeaponConfig {name} 缺少弹丸 Prefab。");
            if (_projectilePrefab.GetComponentInChildren<Collider2D>(true) == null)
                throw new InvalidOperationException($"WeaponConfig {name} 的弹丸 Prefab 缺少 Collider2D。");
            if (_projectileCount <= 0) throw new InvalidOperationException($"WeaponConfig {name} 弹丸数量必须大于 0。");
            if (_damage < 0f || _projectileSpeed <= 0f || _projectileLifetime <= 0f ||
                _projectileRadius < 0f || _recoilImpulse < 0f || _muzzleOffset < 0f)
                throw new InvalidOperationException($"WeaponConfig {name} 包含非法负数或零值。");
        }
    }
}
