using System;
using System.Threading;
using System.Threading.Tasks;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Service.Scene
{
    /// <summary>
    /// 纯 C# 应用单例。所有调用都由 GameEntry 发起，不自行创建 MonoBehaviour 驱动器。
    /// </summary>
    public sealed class SceneService : ISceneService, IAppService
    {
        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private bool _initialized;

        private SceneService()
        {
        }

        public static SceneService Instance { get; } = new SceneService();

        public string Name => "场景服务";

        public Task InitializeAsync()
        {
            if (_initialized) throw new InvalidOperationException("场景服务不能重复初始化。");
            _initialized = true;
            return Task.CompletedTask;
        }

        public bool IsLoaded(SceneId sceneId)
        {
            if (!sceneId.IsValid) return false;
            var scene = SceneManager.GetSceneByName(sceneId.Value);
            return scene.IsValid() && scene.isLoaded;
        }

        public async Task LoadAdditiveAsync(SceneId sceneId, bool setActive = true)
        {
            EnsureInitialized();
            EnsureValid(sceneId);

            await _operationGate.WaitAsync();
            try
            {
                if (!IsLoaded(sceneId))
                {
                    var operation = SceneManager.LoadSceneAsync(sceneId.Value, LoadSceneMode.Additive);
                    if (operation == null)
                        throw new InvalidOperationException($"Unity 未能创建场景加载操作：{sceneId}");
                    await WaitForAsyncOperation(operation);
                }

                var loadedScene = SceneManager.GetSceneByName(sceneId.Value);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                    throw new InvalidOperationException($"场景加载完成后仍不可用：{sceneId}");

                if (setActive && !SceneManager.SetActiveScene(loadedScene))
                    throw new InvalidOperationException($"无法将场景设为 Active Scene：{sceneId}");
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public async Task UnloadAsync(SceneId sceneId)
        {
            EnsureInitialized();
            EnsureValid(sceneId);

            await _operationGate.WaitAsync();
            try
            {
                var scene = SceneManager.GetSceneByName(sceneId.Value);
                if (scene.IsValid() && scene.isLoaded)
                {
                    var operation = SceneManager.UnloadSceneAsync(scene);
                    if (operation == null)
                        throw new InvalidOperationException($"Unity 未能创建场景卸载操作：{sceneId}");
                    await WaitForAsyncOperation(operation);
                }

            }
            finally
            {
                _operationGate.Release();
            }
        }

        public void Shutdown()
        {
            _initialized = false;
        }

        private static async Task WaitForAsyncOperation(AsyncOperation operation)
        {
            while (!operation.isDone) await Task.Yield();
        }

        private static void EnsureValid(SceneId sceneId)
        {
            if (!sceneId.IsValid) throw new ArgumentException("SceneId 不能为空。", nameof(sceneId));
        }

        private void EnsureInitialized()
        {
            if (!_initialized) throw new InvalidOperationException("场景服务尚未初始化。");
        }
    }
}
