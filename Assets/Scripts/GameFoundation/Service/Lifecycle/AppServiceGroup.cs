using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFoundation.Core;

namespace GameFoundation.Service.Lifecycle
{
    /// <summary>显式、有序地管理少量应用级服务，不提供全局按类型查询。</summary>
    public sealed class AppServiceGroup
    {
        private readonly IReadOnlyList<IAppService> _services;
        private int _initializedCount;

        public AppServiceGroup(params IAppService[] services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            for (var index = 0; index < _services.Count; index++)
            {
                if (_services[index] == null)
                    throw new ArgumentException($"服务列表第 {index} 项不能为空。", nameof(services));
            }
        }

        public bool IsInitialized => _initializedCount == _services.Count;

        public async Task InitializeAsync()
        {
            IAppService initializingService = null;
            try
            {
                while (_initializedCount < _services.Count)
                {
                    initializingService = _services[_initializedCount];
                    await initializingService.InitializeAsync();
                    _initializedCount++;
                    initializingService = null;
                }
            }
            catch (Exception exception)
            {
                if (initializingService != null) TryShutdown(initializingService);
                Shutdown();
                var serviceName = initializingService?.Name ?? "未知服务";
                throw new InvalidOperationException($"应用服务“{serviceName}”初始化失败。", exception);
            }
        }

        public void Shutdown()
        {
            for (var index = _initializedCount - 1; index >= 0; index--)
            {
                TryShutdown(_services[index]);
            }

            _initializedCount = 0;
        }

        private static void TryShutdown(IAppService service)
        {
            try
            {
                service.Shutdown();
            }
            catch
            {
                // best-effort：单个服务失败不能阻止其他服务释放。
            }
        }
    }
}
