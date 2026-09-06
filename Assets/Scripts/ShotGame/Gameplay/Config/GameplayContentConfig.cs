using System;
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

        [Header("Gameplay 物理查询")]
        [SerializeField] private LayerMask _playerTargetMask;
        [SerializeField] private LayerMask _enemyTargetMask;
        [SerializeField] private LayerMask _grazeProjectileMask;
        [SerializeField] private LayerMask _wallMask;

        public GameObject PlayerPrefab => _playerPrefab;
        public GameObject TestEnemyPrefab => _testEnemyPrefab;
        public bool SpawnTestEnemy => _spawnTestEnemy;
        public int TestEnemyCount => _testEnemyCount;
        public float TestEnemyAttackRange => _testEnemyAttackRange;
        public LayerMask PlayerTargetMask => _playerTargetMask;
        public LayerMask EnemyTargetMask => _enemyTargetMask;
        public LayerMask GrazeProjectileMask => _grazeProjectileMask;
        public LayerMask WallMask => _wallMask;

        public void Validate()
        {
            if (_playerPrefab == null) throw new InvalidOperationException("GameplayContentConfig 缺少 PlayerPrefab。");
            if (_spawnTestEnemy && _testEnemyCount > 0 && _testEnemyPrefab == null)
                throw new InvalidOperationException("GameplayContentConfig 开启了测试敌人，但缺少 TestEnemyPrefab。");
        }
    }
}
