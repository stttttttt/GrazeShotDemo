using System;
using UnityEngine;

namespace GameFoundation.Core
{
    public interface IInstanceHandle<out T> : IDisposable where T : Component
    {
        T Instance { get; }
    }
}
