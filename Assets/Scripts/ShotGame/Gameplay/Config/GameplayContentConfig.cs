using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Weapon;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [CreateAssetMenu(fileName = "GameplayContentConfig", menuName = "Shot Game/Gameplay Content Config")]
    public sealed class GameplayContentConfig : ScriptableObject
    {
        [Header("实体预制体")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _testEnemyPrefab;

        [Header("第三阶段测试敌人")]
        [SerializeField] private bool _spawnTestEnemy = true;
        [Min(0)] [SerializeField] private int _testEnemyCount = 1;
        [Min(0.1f)] [SerializeField] private float _testEnemyAttackRange = 5f;

        [Header("第四阶段武器")]
        [SerializeField] private WeaponConfig[] _playerInitialWeapons = Array.Empty<WeaponConfig>();
        [SerializeField] private WeaponConfig _testEnemyWeapon;

        [Header("玩家后坐移动")]
        [Min(0.01f)] [SerializeField] private float _playerMaxRecoilSpeed = 12f;
        [Min(0.01f)] [SerializeField] private float _playerRecoilRecovery = 8f;

        [Header("玩家冲刺")]
        [Min(0.01f)] [SerializeField] private float _playerDashDistance = 2.4f;
        [Min(0.01f)] [SerializeField] private float _playerDashDuration = 0.12f;
        [Min(0.01f)] [SerializeField] private float _playerDashCooldown = 0.65f;

        [Header("Gameplay 物理查询")]
        [SerializeField] private LayerMask _playerTargetMask;
        [SerializeField] private LayerMask _enemyTargetMask;
        [SerializeField] private LayerMask _grazeProjectileMask;
        [SerializeField] private LayerMask _wallMask;

        [Header("第五阶段主动擦弹")]
        [SerializeField] private GrazeConfig _grazeConfig;

        [Header("第六阶段正式单局")]
        [SerializeField] private RunDefinition _runDefinition;

        [Header("第七阶段手感表现")]
        [SerializeField] private GameplayFeelConfig _feelConfig;

        public GameObject PlayerPrefab => _playerPrefab;
        public GameObject TestEnemyPrefab => _testEnemyPrefab;
        public bool SpawnTestEnemy => _spawnTestEnemy;
        public int TestEnemyCount => _testEnemyCount;
        public float TestEnemyAttackRange => _testEnemyAttackRange;
        public IReadOnlyList<WeaponConfig> PlayerInitialWeapons => _playerInitialWeapons;
        public WeaponConfig TestEnemyWeapon => _testEnemyWeapon;
        public float PlayerMaxRecoilSpeed => _playerMaxRecoilSpeed;
        public float PlayerRecoilRecovery => _playerRecoilRecovery;
        public float PlayerDashDistance => _playerDashDistance;
        public float PlayerDashDuration => _playerDashDuration;
        public float PlayerDashCooldown => _playerDashCooldown;
        public LayerMask PlayerTargetMask => _playerTargetMask;
        public LayerMask EnemyTargetMask => _enemyTargetMask;
        public LayerMask GrazeProjectileMask => _grazeProjectileMask;
        public LayerMask WallMask => _wallMask;
        public GrazeConfig GrazeConfig => _grazeConfig;
        public RunDefinition RunDefinition => _runDefinition;
        public GameplayFeelConfig FeelConfig => _feelConfig;

        public void Validate()
        {
            if (_playerPrefab == null) throw new InvalidOperationException("GameplayContentConfig 缺少 PlayerPrefab。");
            if (_spawnTestEnemy && _testEnemyCount > 0 && _testEnemyPrefab == null)
                throw new InvalidOperationException("GameplayContentConfig 开启了测试敌人，但缺少 TestEnemyPrefab。");
            if (_playerInitialWeapons == null || _playerInitialWeapons.Length < 1 || _playerInitialWeapons.Length > 3)
                throw new InvalidOperationException("GameplayContentConfig 需要配置 1 至 3 把玩家初始武器。");
            var weapons = new HashSet<WeaponConfig>();
            for (var i = 0; i < _playerInitialWeapons.Length; i++)
            {
                var weapon = _playerInitialWeapons[i];
                if (weapon == null) throw new InvalidOperationException($"玩家初始武器槽 {i + 1} 为空。");
                if (!weapons.Add(weapon)) throw new InvalidOperationException($"玩家初始武器重复：{weapon.name}。");
                weapon.Validate();
            }
            if (_spawnTestEnemy && _testEnemyCount > 0)
            {
                if (_testEnemyWeapon == null) throw new InvalidOperationException("测试敌人缺少武器配置。");
                _testEnemyWeapon.Validate();
            }
            if (_playerMaxRecoilSpeed <= 0f || _playerRecoilRecovery <= 0f)
                throw new InvalidOperationException("玩家后坐最大速度和恢复速度必须大于 0。");
            if (_playerDashDistance <= 0f || _playerDashDuration <= 0f ||
                _playerDashCooldown < _playerDashDuration)
                throw new InvalidOperationException("玩家冲刺距离、持续时间或冷却配置无效。");
            if (_grazeConfig == null) throw new InvalidOperationException("GameplayContentConfig 缺少 GrazeConfig。");
            _grazeConfig.Validate();
            if (_runDefinition == null) throw new InvalidOperationException("GameplayContentConfig 缺少 RunDefinition。");
            _runDefinition.Validate();
            if (_feelConfig == null) throw new InvalidOperationException("GameplayContentConfig 缺少 GameplayFeelConfig。");
            _feelConfig.Validate();
        }
    }
}
