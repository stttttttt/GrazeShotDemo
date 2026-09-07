using System;
using ShotGame.Gameplay.Weapon;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Shot Game/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField] private string _enemyId = "enemy";
        [SerializeField] private string _displayName = "敌人";
        [SerializeField] private GameObject _prefab;
        [SerializeField] private WeaponConfig _weapon;
        [Min(1f)] [SerializeField] private float _maxHealth = 30f;
        [Min(0f)] [SerializeField] private float _moveSpeed = 2f;
        [Min(0.1f)] [SerializeField] private float _attackRange = 5f;
        [Min(0f)] [SerializeField] private float _initialAttackDelay = 0.8f;
        [Min(1)] [SerializeField] private int _budgetCost = 1;

        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public WeaponConfig Weapon => _weapon;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float InitialAttackDelay => _initialAttackDelay;
        public int BudgetCost => _budgetCost;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_enemyId)) throw new InvalidOperationException("EnemyConfig 缺少 EnemyId。");
            if (string.IsNullOrWhiteSpace(_displayName)) throw new InvalidOperationException($"敌人 {_enemyId} 缺少显示名称。");
            if (_prefab == null) throw new InvalidOperationException($"敌人 {_enemyId} 缺少 Prefab。");
            if (_weapon == null) throw new InvalidOperationException($"敌人 {_enemyId} 缺少武器。");
            if (_prefab.GetComponentInChildren<Collider2D>(true) == null)
                throw new InvalidOperationException($"敌人 {_enemyId} Prefab 缺少 Collider2D。");
            if (_maxHealth <= 0f || _moveSpeed < 0f || _attackRange <= 0f ||
                _initialAttackDelay < 0f || _budgetCost <= 0)
                throw new InvalidOperationException($"敌人 {_enemyId} 的数值配置无效。");
            _weapon.Validate();
        }
    }
}
