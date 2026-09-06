using System;
using System.Collections.Generic;

namespace ShotGame.Gameplay.Facts
{
    public sealed class GameplayFactHub : IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private bool _disposed;

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayFactHub));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(type, handlers);
            }
            handlers.Add(handler);
            return new Subscription(() => Remove(type, handler));
        }

        public void Publish<T>(T fact)
        {
            if (_disposed) return;
            if (!_handlers.TryGetValue(typeof(T), out var handlers)) return;
            var snapshot = handlers.ToArray();
            for (var i = 0; i < snapshot.Length; i++) ((Action<T>)snapshot[i]).Invoke(fact);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _handlers.Clear();
        }

        private void Remove(Type type, Delegate handler)
        {
            if (!_handlers.TryGetValue(type, out var handlers)) return;
            handlers.Remove(handler);
            if (handlers.Count == 0) _handlers.Remove(type);
        }

        private sealed class Subscription : IDisposable
        {
            private Action _unsubscribe;
            public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
