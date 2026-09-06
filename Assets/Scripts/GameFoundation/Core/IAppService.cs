using System.Threading.Tasks;

namespace GameFoundation.Core
{
    /// <summary>仅用于应用级长期服务；局内对象不注册到这里。</summary>
    public interface IAppService
    {
        string Name { get; }
        Task InitializeAsync();
        void Shutdown();
    }
}
