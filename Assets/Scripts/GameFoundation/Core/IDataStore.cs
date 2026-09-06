namespace GameFoundation.Core
{
    /// <summary>面向业务数据的最小持久化端口，不规定 PlayerPrefs、文件或云端实现。</summary>
    public interface IDataStore<T>
    {
        T Load();
        void Save(T data);
    }
}
