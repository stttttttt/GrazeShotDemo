using System;
using GameFoundation.Core;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Facts;
using UnityEngine;

namespace ShotGame.Gameplay.Time
{
    /// <summary>一局内唯一的子弹时间控制器，负责叠加、倒计时和恢复。</summary>
    public sealed class TimeDilationController : IDisposable
    {
        private readonly IGameTimeService _time;
        private readonly GrazeConfig _config;
        private readonly GameplayFactHub _facts;
        private bool _disposed;

        public TimeDilationController(IGameTimeService time, GrazeConfig config, GameplayFactHub facts)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        }

        public bool IsActive => RemainingDuration > 0f;
        public float CurrentScale { get; private set; } = 1f;
        public float RemainingDuration { get; private set; }
        public float PlayerMotionScale => IsActive
            ? Mathf.Max(CurrentScale, _config.PlayerMotionScaleDuringSlowTime)
            : 1f;

        public void RequestMomentum(bool perfect)
        {
            ThrowIfDisposed();
            var requestedScale = perfect ? _config.PerfectTimeScale : _config.MomentumTimeScale;
            var requestedDuration = perfect ? _config.PerfectSlowTimeDuration : _config.MomentumDuration;
            CurrentScale = IsActive ? Mathf.Min(CurrentScale, requestedScale) : requestedScale;
            RemainingDuration = Mathf.Min(_config.MaxAccumulatedDuration,
                RemainingDuration + requestedDuration);
            _time.SetTimeScale(CurrentScale);
            Publish();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_disposed || !IsActive || unscaledDeltaTime <= 0f) return;
            RemainingDuration = Mathf.Max(0f, RemainingDuration - unscaledDeltaTime);
            if (RemainingDuration <= 0f) Clear();
            else Publish();
        }

        public void Clear()
        {
            if (_disposed) return;
            var changed = IsActive || CurrentScale != 1f;
            RemainingDuration = 0f;
            CurrentScale = 1f;
            _time.SetTimeScale(1f);
            if (changed) Publish();
        }

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
        }

        private void Publish() => _facts.Publish(new TimeDilationChangedFact(
            CurrentScale, RemainingDuration, IsActive));

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TimeDilationController));
        }
    }
}
