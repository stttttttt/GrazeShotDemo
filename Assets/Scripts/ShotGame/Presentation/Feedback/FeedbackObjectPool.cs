using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    public sealed class FeedbackObjectPool<T> : IDisposable where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly int _limit;
        private readonly Queue<T> _available = new Queue<T>();
        private readonly List<T> _all = new List<T>();

        public FeedbackObjectPool(T prefab, Transform root, int limit)
        {
            _prefab = prefab;
            _root = root;
            _limit = Mathf.Max(1, limit);
        }

        public T Rent()
        {
            while (_available.Count > 0)
            {
                var item = _available.Dequeue();
                if (item != null) return item;
            }
            if (_prefab == null || _all.Count >= _limit) return null;
            var created = UnityEngine.Object.Instantiate(_prefab, _root);
            _all.Add(created);
            return created;
        }

        public void Return(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(_root, false);
            _available.Enqueue(item);
        }

        public void Dispose()
        {
            for (var i = 0; i < _all.Count; i++)
                if (_all[i] != null) UnityEngine.Object.Destroy(_all[i].gameObject);
            _all.Clear();
            _available.Clear();
        }
    }
}
