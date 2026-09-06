using System;

namespace GameFoundation.Core
{
    /// <summary>归属明确生命周期范围的对象池端口；不提供全局池注册表。</summary>
    public interface IObjectPool<T, in TContext> : IDisposable where T : class, IPoolable<TContext>
    {
        int ActiveCount { get; }
        int InactiveCount { get; }
        int TotalCount { get; }
        T Rent(TContext context);
        bool Return(T instance);
    }
}
