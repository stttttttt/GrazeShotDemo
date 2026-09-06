using System;
using System.Collections.Generic;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Pooling
{
    /// <summary>池归创建它的 Scope 所有；不提供静态注册表。</summary>
    public sealed class ComponentPool<T, TContext> : IObjectPool<T, TContext>
        where T : Component, IPoolable<TContext>
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly Stack<T> _available = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly int _maxRetainedCount;
        private bool _isDisposed;

        public ComponentPool(T prefab, Transform root, int preloadCount, int maxRetainedCount)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _root = root;
            _maxRetainedCount = Mathf.Max(0, maxRetainedCount);

            var actualPreloadCount = Mathf.Min(Mathf.Max(0, preloadCount), _maxRetainedCount);
            for (var index = 0; index < actualPreloadCount; index++)
            {
                _available.Push(CreateInstance());
            }
        }

        public int ActiveCount => _active.Count;
        public int InactiveCount => _available.Count;
        public int TotalCount => ActiveCount + InactiveCount;

        public T Rent(TContext context)
        {
            ThrowIfDisposed();
            var instance = _available.Count > 0 ? _available.Pop() : CreateInstance();
            _active.Add(instance);
            instance.gameObject.SetActive(true);
            try
            {
                instance.OnRent(context);
            }
            catch
            {
                _active.Remove(instance);
                UnityEngine.Object.Destroy(instance.gameObject);
                throw;
            }

            return instance;
        }

        public bool Return(T instance)
        {
            ThrowIfDisposed();
            if (instance == null || !_active.Remove(instance)) return false;

            try
            {
                instance.OnReturn();
            }
            catch
            {
                UnityEngine.Object.Destroy(instance.gameObject);
                throw;
            }

            instance.transform.SetParent(_root, false);
            instance.gameObject.SetActive(false);
            if (_available.Count >= _maxRetainedCount)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
            else
            {
                _available.Push(instance);
            }
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            foreach (var instance in _active)
            {
                if (instance == null) continue;
                try
                {
                    instance.OnReturn();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }

            foreach (var instance in _available)
            {
                if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
            }

            _active.Clear();
            _available.Clear();
        }

        private T CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _root);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ComponentPool<T, TContext>));
        }
    }
}
