using System;

namespace GameFoundation.Core
{
    public interface IResourceHandle<out T> : IDisposable where T : UnityEngine.Object
    {
        T Resource { get; }
    }
}
