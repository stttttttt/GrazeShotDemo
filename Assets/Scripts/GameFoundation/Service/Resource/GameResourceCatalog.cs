using System;
using System.Collections.Generic;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.Service.Resource
{
    [CreateAssetMenu(fileName = "GameResourceCatalog", menuName = "Game Foundation/Game Resource Catalog")]
    public sealed class GameResourceCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private ResourceId _id;
            [SerializeField] private UnityEngine.Object _asset;

            public ResourceId Id => _id;
            public UnityEngine.Object Asset => _asset;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        private Dictionary<ResourceId, UnityEngine.Object> _lookup;

        public T Get<T>(ResourceId id) where T : UnityEngine.Object
        {
            BuildLookupIfNeeded();
            if (!_lookup.TryGetValue(id, out var asset))
                throw new KeyNotFoundException($"Catalog 中不存在资源：{id}");

            if (asset is T typedAsset) return typedAsset;
            throw new InvalidCastException($"资源 {id} 的类型是 {asset.GetType().Name}，请求类型是 {typeof(T).Name}。");
        }

        private void OnValidate()
        {
            _lookup = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<ResourceId, UnityEngine.Object>();

            foreach (var entry in _entries)
            {
                if (entry == null || !entry.Id.IsValid || entry.Asset == null) continue;
                if (_lookup.ContainsKey(entry.Id))
                    throw new InvalidOperationException($"Catalog 中存在重复资源 ID：{entry.Id}");
                _lookup.Add(entry.Id, entry.Asset);
            }
        }
    }
}
