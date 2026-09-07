using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Scene;
using ShotGame.Gameplay.World;
using ShotGame.Gameplay.Time;

namespace ShotGame.Gameplay.Run
{
    /// <summary>一局 Gameplay 的生命周期根，所有局内对象都随 Session 创建和释放。</summary>
    public sealed class GameplaySession : IDisposable
    {
        private readonly IGameTimeService _time;
        private GameplayInputAdapter _input;
        private bool _disposed;

        public GameplaySession(IGameTimeService time)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public bool IsInitialized { get; private set; }
        public GameplayFactHub Facts { get; private set; }
        public GameplayWorld World { get; private set; }
        public EntitySpawner Spawner { get; private set; }
        public GameplayRun Run { get; private set; }
        public GameplaySceneContext SceneContext { get; private set; }
        public EntityId PlayerEntityId { get; private set; }
        public TimeDilationController TimeDilation { get; private set; }
        public WavePlan WavePlan { get; private set; }

        public Task InitializeAsync(GameplaySceneContext sceneContext, GameplayContentConfig contentConfig,
            GameplayInputAdapter input)
        {
            ThrowIfDisposed();
            if (IsInitialized) throw new InvalidOperationException("GameplaySession 不能重复初始化。");

            try
            {
                SceneContext = sceneContext ?? throw new ArgumentNullException(nameof(sceneContext));
                contentConfig = contentConfig != null
                    ? contentConfig
                    : throw new ArgumentNullException(nameof(contentConfig));
                _input = input ?? throw new ArgumentNullException(nameof(input));
                contentConfig.Validate();
                Facts = new GameplayFactHub();
                World = new GameplayWorld(Facts);
                Spawner = new EntitySpawner(World, Facts, new EntityIdGenerator());
                TimeDilation = new TimeDilationController(_time, contentConfig.GrazeConfig, Facts);
                WavePlan = WavePlanBuilder.Build(contentConfig.RunDefinition, SceneContext.EnemySpawnPoints);

                for (var i = 0; i < SceneContext.SceneEntities.Count; i++)
                    Spawner.RegisterSceneEntity(SceneContext.SceneEntities[i]);

                var player = Spawner.SpawnPlayer(contentConfig.PlayerPrefab,
                    SceneContext.PlayerSpawn.position, SceneContext.PlayerSpawn.rotation,
                    contentConfig.PlayerInitialWeapons, contentConfig.PlayerTargetMask, contentConfig.WallMask,
                    contentConfig.GrazeProjectileMask,
                    contentConfig.PlayerMaxRecoilSpeed, contentConfig.PlayerRecoilRecovery,
                    contentConfig.GrazeConfig, TimeDilation, SceneContext.MovementBounds,
                    SceneContext.WorldRoot);
                PlayerEntityId = player.Id;
                _input.Bind(player.GetComponent<PlayerInputComponent>(), player.UnityObject.Transform,
                    SceneContext.GameplayCamera);

                var waveController = new WaveController(Spawner, SceneContext, Facts, PlayerEntityId,
                    contentConfig.EnemyTargetMask, contentConfig.WallMask);
                Run = new GameplayRun(World, Facts, _time, WavePlan, waveController,
                    TimeDilation, PlayerEntityId);

                Run.Start();
                IsInitialized = true;
                return Task.CompletedTask;
            }
            catch
            {
                DisposeObjects();
                throw;
            }
        }

        public void Tick()
        {
            ThrowIfDisposed();
            if (!IsInitialized || _time.IsPaused) 
                return;

            TimeDilation.Tick(_time.UnscaledDeltaTime);
            World.UnscaledTick(_time.UnscaledDeltaTime);
            Run.Tick(_time.DeltaTime);
            World.Tick(_time.DeltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ThrowIfDisposed();
            if (!IsInitialized || _time.IsPaused || fixedDeltaTime <= 0f) return;

            World.FixedTick(fixedDeltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IsInitialized = false;
            DisposeObjects();
        }

        private void DisposeObjects()
        {
            // 输入由应用级适配器持有，Session 只解除本局玩家绑定。
            _input?.Unbind();
            _input = null;
            Run?.Dispose();
            Run = null;
            TimeDilation?.Dispose();
            TimeDilation = null;
            Spawner?.Dispose();
            Spawner = null;
            World?.Dispose();
            World = null;
            Facts?.Dispose();
            Facts = null;
            SceneContext = null;
            WavePlan = null;
            PlayerEntityId = default;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplaySession));
        }
    }
}
