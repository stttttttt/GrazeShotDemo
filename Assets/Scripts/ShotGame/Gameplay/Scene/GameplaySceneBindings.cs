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

        [Header("战场")]
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private BoxCollider2D _arenaBounds;

        [Header("预摆放场景实体")]
        [SerializeField] private List<SceneEntityAuthoring> _sceneEntities = new List<SceneEntityAuthoring>();

        public GameplaySceneContext CreateContext()
        {
            Validate();
            return new GameplaySceneContext(gameObject.scene, _worldRoot, _presentationRoot,
                _playerSpawn, _enemySpawns.ToArray(), _dropRoot, _arenaBounds,
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
            if (_enemySpawns == null || _enemySpawns.Count == 0)
                throw new InvalidOperationException("GameplaySceneBindings 至少需要一个 EnemySpawn。");
            for (var i = 0; i < _enemySpawns.Count; i++) Require(_enemySpawns[i], $"_enemySpawns[{i}]");
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
