using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShotGame.Gameplay.Scene
{
    public sealed class GameplaySceneContext
    {
        public GameplaySceneContext(UnityEngine.SceneManagement.Scene scene, Transform worldRoot,
            Transform presentationRoot, Transform playerSpawn, Transform[] enemySpawns,
            Transform dropRoot, BoxCollider2D arenaBounds, Camera gameplayCamera,
            SceneEntityAuthoring[] sceneEntities)
        {
            Scene = scene;
            WorldRoot = worldRoot ?? throw new ArgumentNullException(nameof(worldRoot));
            PresentationRoot = presentationRoot ?? throw new ArgumentNullException(nameof(presentationRoot));
            PlayerSpawn = playerSpawn ?? throw new ArgumentNullException(nameof(playerSpawn));
            EnemySpawns = enemySpawns ?? throw new ArgumentNullException(nameof(enemySpawns));
            DropRoot = dropRoot ?? throw new ArgumentNullException(nameof(dropRoot));
            ArenaBounds = arenaBounds != null ? arenaBounds : throw new ArgumentNullException(nameof(arenaBounds));
            GameplayCamera = gameplayCamera != null ? gameplayCamera : throw new ArgumentNullException(nameof(gameplayCamera));
            SceneEntities = sceneEntities ?? Array.Empty<SceneEntityAuthoring>();
        }

        public UnityEngine.SceneManagement.Scene Scene { get; }
        public Transform WorldRoot { get; }
        public Transform PresentationRoot { get; }
        public Transform PlayerSpawn { get; }
        public IReadOnlyList<Transform> EnemySpawns { get; }
        public Transform DropRoot { get; }
        public BoxCollider2D ArenaBounds { get; }
        public Camera GameplayCamera { get; }
        public IReadOnlyList<SceneEntityAuthoring> SceneEntities { get; }
    }
}
