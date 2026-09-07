using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShotGame.Gameplay.Scene
{
    /// <summary>Gameplay 场景到纯 C# 运行时的唯一引用入口，不包含 Update 和业务逻辑。</summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySceneBindings : MonoBehaviour
    {
        [Header("根节点")]
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Transform _presentationRoot;
        [SerializeField] private Transform _dropRoot;

        [Header("出生点")]
        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private List<Transform> _enemySpawns = new List<Transform>();
        [SerializeField] private List<EnemySpawnPoint> _enemySpawnPoints = new List<EnemySpawnPoint>();

        [Header("战场")]
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private BoxCollider2D _arenaBounds;

        [Header("预摆放场景实体")]
        [SerializeField] private List<SceneEntityAuthoring> _sceneEntities = new List<SceneEntityAuthoring>();

        public GameplaySceneContext CreateContext()
        {
            Validate();
            var spawnPoints = new EnemySpawnPointData[_enemySpawnPoints.Count];
            for (var i = 0; i < _enemySpawnPoints.Count; i++) spawnPoints[i] = _enemySpawnPoints[i].CreateData();
            return new GameplaySceneContext(gameObject.scene, _worldRoot, _presentationRoot,
                _playerSpawn, spawnPoints, _dropRoot, _arenaBounds,
                _gameplayCamera, _sceneEntities.ToArray());
        }

        public void Validate()
        {
            Require(_worldRoot, nameof(_worldRoot));
            Require(_presentationRoot, nameof(_presentationRoot));
            Require(_dropRoot, nameof(_dropRoot));
            Require(_playerSpawn, nameof(_playerSpawn));
            Require(_gameplayCamera, nameof(_gameplayCamera));
            Require(_arenaBounds, nameof(_arenaBounds));
            if (_enemySpawnPoints == null || _enemySpawnPoints.Count == 0)
                throw new InvalidOperationException("GameplaySceneBindings 至少需要一个 EnemySpawnPoint。");
            var spawnIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _enemySpawnPoints.Count; i++)
            {
                Require(_enemySpawnPoints[i], $"_enemySpawnPoints[{i}]");
                var data = _enemySpawnPoints[i].CreateData();
                if (!spawnIds.Add(data.SpawnId)) throw new InvalidOperationException($"场景中存在重复 SpawnId：{data.SpawnId}。");
                EnsureBelongsToScene(data.Transform);
                if (!_arenaBounds.bounds.Contains(data.Transform.position))
                    throw new InvalidOperationException($"敌人出生点 {data.SpawnId} 位于 ArenaBounds 外。");
            }
            EnsureBelongsToScene(_worldRoot);
            EnsureBelongsToScene(_presentationRoot);
            EnsureBelongsToScene(_dropRoot);
            EnsureBelongsToScene(_playerSpawn);
            EnsureBelongsToScene(_gameplayCamera.transform);
            EnsureBelongsToScene(_arenaBounds.transform);
        }

        private static void Require(UnityEngine.Object value, string fieldName)
        {
            if (value == null) throw new InvalidOperationException($"GameplaySceneBindings 缺少引用：{fieldName}");
        }

        private void EnsureBelongsToScene(Component component) => EnsureBelongsToScene(component.transform);

        private void EnsureBelongsToScene(Transform target)
        {
            if (target.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException($"场景引用 {target.name} 不属于 {gameObject.scene.name}。");
        }
    }
}
