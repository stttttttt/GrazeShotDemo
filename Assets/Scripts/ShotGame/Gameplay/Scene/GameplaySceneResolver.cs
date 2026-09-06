using System;
using System.Collections.Generic;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShotGame.Gameplay.Scene
{
    public static class GameplaySceneResolver
    {
        public static GameplaySceneContext Resolve(SceneId sceneId)
        {
            if (!sceneId.IsValid) throw new ArgumentException("Gameplay SceneId 无效。", nameof(sceneId));
            var scene = SceneManager.GetSceneByName(sceneId.Value);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Gameplay 场景尚未加载：{sceneId}");

            var found = new List<GameplaySceneBindings>();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                found.AddRange(roots[i].GetComponentsInChildren<GameplaySceneBindings>(true));

            if (found.Count != 1)
                throw new InvalidOperationException(
                    $"Gameplay 场景 {sceneId} 必须且只能包含一个 GameplaySceneBindings，当前数量：{found.Count}。");
            return found[0].CreateContext();
        }
    }
}
