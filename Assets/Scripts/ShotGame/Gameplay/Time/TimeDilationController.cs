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
        public bool IsHitStopped => HitStopRemaining > 0f;
        public float CurrentScale { get; private set; } = 1f;
        public float RemainingDuration { get; private set; }
        public float HitStopRemaining { get; private set; }
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
            ApplyScale();
            Publish();
        }

        public void RequestHitStop(float duration)
        {
            ThrowIfDisposed();
            if (duration <= 0f) return;
            HitStopRemaining = Mathf.Max(HitStopRemaining, duration);
            ApplyScale();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (_disposed || unscaledDeltaTime <= 0f) return;
            if (IsHitStopped)
            {
                HitStopRemaining = Mathf.Max(0f, HitStopRemaining - unscaledDeltaTime);
                ApplyScale();
                return;
            }
            if (!IsActive) return;
            RemainingDuration = Mathf.Max(0f, RemainingDuration - unscaledDeltaTime);
            if (RemainingDuration <= 0f) Clear();
            else Publish();
        }

        public void Clear()
        {
            if (_disposed) return;
            var changed = IsActive || CurrentScale != 1f;
            RemainingDuration = 0f;
            HitStopRemaining = 0f;
            CurrentScale = 1f;
            ApplyScale();
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

        private void ApplyScale() => _time.SetTimeScale(IsHitStopped ? 0f : CurrentScale);

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TimeDilationController));
        }
    }
}
