using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.World;

namespace ShotGame.Gameplay.Run
{
    /// <summary>一局 Gameplay 的生命周期根，所有局内对象都随 Session 创建和释放。</summary>
    public sealed class GameplaySession : IDisposable
    {
        private readonly IGameTimeService _time;
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

        public Task InitializeAsync()
        {
            ThrowIfDisposed();
            if (IsInitialized) throw new InvalidOperationException("GameplaySession 不能重复初始化。");

            try
            {
                Facts = new GameplayFactHub();
                World = new GameplayWorld(Facts);
                Spawner = new EntitySpawner(World, Facts, new EntityIdGenerator());
                Run = new GameplayRun(World, Facts, _time);
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
            Run?.Dispose();
            Run = null;
            Spawner?.Dispose();
            Spawner = null;
            World?.Dispose();
            World = null;
            Facts?.Dispose();
            Facts = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplaySession));
        }
    }
}
