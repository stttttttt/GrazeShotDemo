using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShotGame.Gameplay.Scene
{
    public sealed class GameplaySceneContext
    {
        public GameplaySceneContext(UnityEngine.SceneManagement.Scene scene, Transform worldRoot,
            Transform presentationRoot, Transform playerSpawn, EnemySpawnPointData[] enemySpawnPoints,
            Transform dropRoot, BoxCollider2D arenaBounds, Camera gameplayCamera,
            SceneEntityAuthoring[] sceneEntities)
        {
            Scene = scene;
            WorldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
            PresentationRoot = presentationRoot ?? throw new ArgumentNullException(nameof(presentationRoot));
            PlayerSpawn = playerSpawn ?? throw new ArgumentNullException(nameof(playerSpawn));
            EnemySpawnPoints = enemySpawnPoints ?? throw new ArgumentNullException(nameof(enemySpawnPoints));
            DropRoot = dropRoot ?? throw new ArgumentNullException(nameof(dropRoot));
            ArenaBounds = arenaBounds != null ? arenaBounds : throw new ArgumentNullException(nameof(arenaBounds));
            GameplayCamera = gameplayCamera != null ? gameplayCamera : throw new ArgumentNullException(nameof(gameplayCamera));
            SceneEntities = sceneEntities ?? Array.Empty<SceneEntityAuthoring>();
        }

        public UnityEngine.SceneManagement.Scene Scene { get; }
        public Transform WorldRoot { get; }
        public Transform PresentationRoot { get; }
        public Transform PlayerSpawn { get; }
        public IReadOnlyList<EnemySpawnPointData> EnemySpawnPoints { get; }
        public Transform DropRoot { get; }
        public BoxCollider2D ArenaBounds { get; }
        public Camera GameplayCamera { get; }
        public IReadOnlyList<SceneEntityAuthoring> SceneEntities { get; }

        /// <summary>场景战场与相机可见区域的交集，保证不同画面比例下角色也不会离开屏幕。</summary>
        public Bounds MovementBounds
        {
            get
            {
                var arena = ArenaBounds.bounds;
                if (!GameplayCamera.orthographic) return arena;

                var cameraPosition = GameplayCamera.transform.position;
                var halfHeight = GameplayCamera.orthographicSize;
                var halfWidth = halfHeight * GameplayCamera.aspect;
                var visibleMin = new Vector2(cameraPosition.x - halfWidth, cameraPosition.y - halfHeight);
                var visibleMax = new Vector2(cameraPosition.x + halfWidth, cameraPosition.y + halfHeight);
                var min = Vector2.Max(arena.min, visibleMin);
                var max = Vector2.Min(arena.max, visibleMax);
                if (min.x >= max.x || min.y >= max.y)
                    throw new InvalidOperationException("ArenaBounds 与 GameplayCamera 可见区域没有交集。");
                var center = (min + max) * 0.5f;
                var size = max - min;
                return new Bounds(new Vector3(center.x, center.y, arena.center.z),
                    new Vector3(size.x, size.y, arena.size.z));
            }
        }

        public bool TryGetEnemySpawn(string spawnId, out EnemySpawnPointData spawnPoint)
        {
            for (var i = 0; i < EnemySpawnPoints.Count; i++)
            {
                if (!string.Equals(EnemySpawnPoints[i].SpawnId, spawnId, StringComparison.Ordinal)) continue;
                spawnPoint = EnemySpawnPoints[i];
                return true;
            }
            spawnPoint = default;
            return false;
        }
    }
}
