using System;
using System.Collections.Generic;
using GameFoundation.Core;

namespace GameFoundation.Service.Time
{
    /// <summary>按缩放或真实时间推进的局部定时任务调度器。</summary>
    public sealed class TimerScheduler : ITimerScheduler
    {
        private sealed class TimerItem
        {
            public int Id;
            public float Remaining;
            public float Interval;
            public TimerTimeMode TimeMode;
            public bool Repeating;
            public bool Cancelled;
            public Action Callback;
        }

        private readonly IGameTimeService _time;
        private readonly List<TimerItem> _timers = new List<TimerItem>();
        private readonly List<TimerItem> _pending = new List<TimerItem>();
        private int _nextId = 1;
        private bool _isTicking;
        private bool _isDisposed;

        public TimerScheduler(IGameTimeService time)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public TimerHandle Schedule(
            float delaySeconds,
            Action callback,
            TimerTimeMode timeMode = TimerTimeMode.Scaled,
            bool repeating = false)
        {
            ThrowIfDisposed();
            if (delaySeconds < 0f) throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (repeating && delaySeconds <= 0f)
                throw new ArgumentException("循环定时器的间隔必须大于 0。", nameof(delaySeconds));

            var item = new TimerItem
            {
                Id = NextId(),
                Remaining = delaySeconds,
                Interval = delaySeconds,
                TimeMode = timeMode,
                Repeating = repeating,
                Callback = callback
            };

            (_isTicking ? _pending : _timers).Add(item);
            return new TimerHandle(item.Id);
        }

        public bool Cancel(TimerHandle handle)
        {
            ThrowIfDisposed();
            if (!handle.IsValid) return false;
            if (_isTicking)
                return MarkCancelled(_timers, handle.Id) || MarkCancelled(_pending, handle.Id);
            return RemoveById(_timers, handle.Id) || RemoveById(_pending, handle.Id);
        }

        public void Tick()
        {
            ThrowIfDisposed();
            _isTicking = true;
            try
            {
                for (var index = _timers.Count - 1; index >= 0; index--)
                {
                    var timer = _timers[index];
                    if (timer.Cancelled)
                    {
                        _timers.RemoveAt(index);
                        continue;
                    }

                    var deltaTime = timer.TimeMode == TimerTimeMode.Scaled
                        ? _time.DeltaTime
                        : _time.UnscaledDeltaTime;
                    if (deltaTime <= 0f) continue;

                    timer.Remaining -= deltaTime;
                    if (timer.Remaining > 0f) continue;

                    if (timer.Repeating)
                    {
                        timer.Remaining += timer.Interval;
                        if (timer.Remaining <= 0f) timer.Remaining = timer.Interval;
                    }
                    else
                    {
                        _timers.RemoveAt(index);
                    }

                    timer.Callback.Invoke();
                    if (_isDisposed) break;
                }
            }
            finally
            {
                _isTicking = false;
                if (!_isDisposed && _pending.Count > 0)
                {
                    _timers.AddRange(_pending);
                    _pending.Clear();
                }
                if (!_isDisposed) _timers.RemoveAll(timer => timer.Cancelled);
            }
        }

        public void Clear()
        {
            ThrowIfDisposed();
            if (_isTicking)
            {
                for (var index = 0; index < _timers.Count; index++) _timers[index].Cancelled = true;
                _pending.Clear();
                return;
            }
            _timers.Clear();
            _pending.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timers.Clear();
            _pending.Clear();
            _isTicking = false;
        }

        private int NextId()
        {
            if (_nextId == int.MaxValue) _nextId = 1;
            while (ContainsId(_nextId)) _nextId++;
            return _nextId++;
        }

        private bool ContainsId(int id)
        {
            for (var index = 0; index < _timers.Count; index++)
                if (_timers[index].Id == id) return true;
            for (var index = 0; index < _pending.Count; index++)
                if (_pending[index].Id == id) return true;
            return false;
        }

        private static bool RemoveById(List<TimerItem> list, int id)
        {
            for (var index = list.Count - 1; index >= 0; index--)
            {
                if (list[index].Id != id) continue;
                list.RemoveAt(index);
                return true;
            }

            return false;
        }

        private static bool MarkCancelled(List<TimerItem> list, int id)
        {
            for (var index = list.Count - 1; index >= 0; index--)
            {
                if (list[index].Id != id) continue;
                list[index].Cancelled = true;
                return true;
            }

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(TimerScheduler));
        }
    }
}
