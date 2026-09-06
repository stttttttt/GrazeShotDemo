using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Resource
{
    /// <summary>第一版资源后端。Catalog 强引用由 Unity 管理，这里跟踪业务租约以便诊断泄漏。</summary>
    public sealed class CatalogResourceService : IResourceService
    {
        private readonly GameResourceCatalog _catalog;
        private readonly Dictionary<ResourceId, int> _referenceCounts = new Dictionary<ResourceId, int>();

        public CatalogResourceService(GameResourceCatalog catalog)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
        }

        public Task<IResourceHandle<T>> LoadAsync<T>(ResourceId resourceId)
            where T : UnityEngine.Object
        {
            var resource = _catalog.Get<T>(resourceId);
            Retain(resourceId);
            IResourceHandle<T> handle = new ResourceHandle<T>(resource, () => Release(resourceId));
            return Task.FromResult(handle);
        }

        public Task<IInstanceHandle<T>> InstantiateAsync<T>(
            ResourceId resourceId,
            Transform parent) where T : Component
        {
            var prefab = _catalog.Get<GameObject>(resourceId);
            var gameObject = UnityEngine.Object.Instantiate(prefab, parent);
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                UnityEngine.Object.Destroy(gameObject);
                throw new InvalidOperationException($"Prefab {resourceId} 根节点缺少组件 {typeof(T).Name}。");
            }

            Retain(resourceId);
            IInstanceHandle<T> handle = new InstanceHandle<T>(component, () =>
            {
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
                Release(resourceId);
            });
            return Task.FromResult(handle);
        }

        public int GetReferenceCount(ResourceId resourceId)
        {
            return _referenceCounts.TryGetValue(resourceId, out var count) ? count : 0;
        }

        private void Retain(ResourceId id)
        {
            _referenceCounts[id] = GetReferenceCount(id) + 1;
        }

        private void Release(ResourceId id)
        {
            var count = GetReferenceCount(id);
            if (count <= 1) _referenceCounts.Remove(id);
            else _referenceCounts[id] = count - 1;
        }

        private sealed class ResourceHandle<T> : IResourceHandle<T> where T : UnityEngine.Object
        {
            private Action _release;

            public ResourceHandle(T resource, Action release)
            {
                Resource = resource;
                _release = release;
            }

            public T Resource { get; }

            public void Dispose()
            {
                var release = _release;
                _release = null;
                release?.Invoke();
            }
        }

        private sealed class InstanceHandle<T> : IInstanceHandle<T> where T : Component
        {
            private Action _release;

            public InstanceHandle(T instance, Action release)
            {
                Instance = instance;
                _release = release;
            }

            public T Instance { get; }

            public void Dispose()
            {
                var release = _release;
                _release = null;
                release?.Invoke();
            }
        }
    }
}
