using System;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Time
{
    /// <summary>统一驱动纯逻辑时间与 Unity 物理时间，支持暂停和子弹时间。</summary>
    public sealed class GameTimeService : IGameTimeService, IDisposable
    {
        private readonly bool _syncUnityTimeScale;
        private bool _isDisposed;
        private float _gameplayTimeScale = 1f;

        public GameTimeService(bool syncUnityTimeScale = true)
        {
            _syncUnityTimeScale = syncUnityTimeScale;
            ApplyUnityTimeScale();
        }

        public float DeltaTime { get; private set; }
        public float UnscaledDeltaTime { get; private set; }
        public double ElapsedTime { get; private set; }
        public bool IsPaused { get; private set; }
        public float TimeScale => IsPaused ? 0f : _gameplayTimeScale;

        public void Tick(float unscaledDeltaTime)
        {
            ThrowIfDisposed();
            if (unscaledDeltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));

            UnscaledDeltaTime = unscaledDeltaTime;
            DeltaTime = unscaledDeltaTime * TimeScale;
            ElapsedTime += DeltaTime;
        }

        public void SetPaused(bool isPaused)
        {
            ThrowIfDisposed();
            IsPaused = isPaused;
            if (isPaused) DeltaTime = 0f;
            ApplyUnityTimeScale();
        }

        public void SetTimeScale(float timeScale)
        {
            ThrowIfDisposed();
            if (timeScale < 0f) throw new ArgumentOutOfRangeException(nameof(timeScale));
            _gameplayTimeScale = timeScale;
            ApplyUnityTimeScale();
        }

        public void Reset()
        {
            ThrowIfDisposed();
            DeltaTime = 0f;
            UnscaledDeltaTime = 0f;
            ElapsedTime = 0d;
            IsPaused = false;
            _gameplayTimeScale = 1f;
            ApplyUnityTimeScale();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            IsPaused = false;
            _gameplayTimeScale = 1f;
            ApplyUnityTimeScale();
            _isDisposed = true;
        }

        private void ApplyUnityTimeScale()
        {
            if (_syncUnityTimeScale) UnityEngine.Time.timeScale = TimeScale;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(GameTimeService));
        }
    }
}
