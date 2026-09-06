using System;
using System.Threading.Tasks;
using UnityEngine;

namespace GameFoundation.Core
{
    /// <summary>业务资源访问边界；具体加载方案由 Service 程序集实现。</summary>
    public interface IResourceService
    {
        Task<IResourceHandle<T>> LoadAsync<T>(ResourceId resourceId)
            where T : UnityEngine.Object;

        Task<IInstanceHandle<T>> InstantiateAsync<T>(
            ResourceId resourceId,
            Transform parent) where T : Component;
    }
}
