using System;
using System.Threading.Tasks;
using GameFoundation.Core;

namespace ShotGame.Gameplay.Run
{
    /// <summary>
    /// 一局 Gameplay 的生命周期根。后续 EntityWorld、胜负条件与波次系统都由它持有。
    /// </summary>
    public sealed class GameplaySession : IDisposable
    {
        private readonly IGameTimeService _time;
        private bool _disposed;

        public GameplaySession(IGameTimeService time)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            ThrowIfDisposed();
            if (IsInitialized) throw new InvalidOperationException("GameplaySession 不能重复初始化。");

            // 下一层在这里创建 GameplayRun 和 EntityWorld。
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public void Tick()
        {
            ThrowIfDisposed();
            if (!IsInitialized || _time.IsPaused) return;

            // 下一层在这里按固定顺序推进 World 中的纯逻辑系统。
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ThrowIfDisposed();
            if (!IsInitialized || _time.IsPaused || fixedDeltaTime <= 0f) return;

            // 下一层在这里推进需要固定步长的移动与物理适配逻辑。
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IsInitialized = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplaySession));
        }
    }
}
