using System;

namespace GameFoundation.Core
{
    public enum TimerTimeMode
    {
        Scaled,
        Unscaled
    }

    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        public TimerHandle(int id) => Id = id;
        public int Id { get; }
        public bool IsValid => Id != 0;
        public bool Equals(TimerHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);
        public override int GetHashCode() => Id;
        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
    }

    /// <summary>归属明确 Scope 的定时任务调度器；不负责控制时间倍率。</summary>
    public interface ITimerScheduler : IDisposable
    {
        TimerHandle Schedule(
            float delaySeconds,
            Action callback,
            TimerTimeMode timeMode = TimerTimeMode.Scaled,
            bool repeating = false);

        bool Cancel(TimerHandle handle);
        void Tick();
        void Clear();
    }
}
