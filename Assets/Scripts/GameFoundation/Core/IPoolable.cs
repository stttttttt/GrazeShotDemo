namespace GameFoundation.Core
{
    /// <summary>可被对象池复用的实例契约；业务组件只依赖该接口，不依赖具体池实现。</summary>
    public interface IPoolable<in TContext>
    {
        void OnRent(TContext context);
        void OnReturn();
    }
}
