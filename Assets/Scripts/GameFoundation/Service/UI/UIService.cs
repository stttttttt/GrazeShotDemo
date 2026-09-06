using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFoundation.Core;
using GameFoundation.Service.Lifecycle;
using UnityEngine;

namespace GameFoundation.Service.UI
{
    /// <summary>只管理显示关系，不拥有 AppFlow 或 RunFlow 的业务状态。</summary>
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    public sealed class UIService : PersistentMonoSingleton<UIService>, IUIService, IAppService
    {
        [Serializable]
        private sealed class PageRegistration
        {
            [SerializeField] private UIPageId _pageId;
            [SerializeField] private ResourceId _resourceId;

            public UIPageId PageId => _pageId;
            public ResourceId ResourceId => _resourceId;
        }

        private sealed class LoadedPage
        {
            public LoadedPage(UIScreen screen, IInstanceHandle<UIScreen> handle)
            {
                Screen = screen;
                Handle = handle;
            }

            public UIScreen Screen { get; }
            public IInstanceHandle<UIScreen> Handle { get; }
        }

        [SerializeField] private List<PageRegistration> _pages = new List<PageRegistration>();

        private readonly Dictionary<UIPageId, ResourceId> _registrations = new Dictionary<UIPageId, ResourceId>();
        private readonly Dictionary<UIPageId, LoadedPage> _loadedPages = new Dictionary<UIPageId, LoadedPage>();
        private readonly Dictionary<UIPageId, Task<LoadedPage>> _loadingPages = new Dictionary<UIPageId, Task<LoadedPage>>();
        private IResourceService _resources;
        private bool _initialized;

        public string Name => "UI 服务";

        public void Configure(IResourceService resources)
        {
            if (_initialized) throw new InvalidOperationException("UI 服务初始化后不能重新配置。");
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        }

        public Task InitializeAsync()
        {
            if (_resources == null) throw new InvalidOperationException("UI 服务尚未配置资源服务。");
            if (_initialized) throw new InvalidOperationException("UI 服务不能重复初始化。");

            _registrations.Clear();
            foreach (var page in _pages)
            {
                if (page == null) continue;
                if (!page.PageId.IsValid) throw new InvalidOperationException("UI 页面注册项缺少 PageId。");
                if (!page.ResourceId.IsValid)
                    throw new InvalidOperationException($"UI 页面 {page.PageId} 缺少 ResourceId。");
                if (_registrations.ContainsKey(page.PageId))
                    throw new InvalidOperationException($"UI 页面重复注册：{page.PageId}");
                _registrations.Add(page.PageId, page.ResourceId);
            }

            _initialized = true;
            return Task.CompletedTask;
        }

        public async Task OpenAsync(UIPageId pageId, object args)
        {
            EnsureInitialized();
            var page = await GetOrLoadAsync(pageId);
            if (page.Screen.Layer == UILayer.Page) CloseLayerExcept(UILayer.Page, pageId);
            page.Screen.Open(args);
        }

        public void Close(UIPageId pageId)
        {
            if (_loadedPages.TryGetValue(pageId, out var page)) page.Screen.Close();
        }

        public bool IsOpen(UIPageId pageId)
        {
            return _loadedPages.TryGetValue(pageId, out var page) && page.Screen.IsOpen;
        }

        public void CloseLayer(UILayer layer)
        {
            CloseLayerExcept(layer, null);
        }

        private void CloseLayerExcept(UILayer layer, UIPageId? except)
        {
            foreach (var pair in _loadedPages)
            {
                if (pair.Value.Screen.Layer == layer && pair.Key != except) pair.Value.Screen.Close();
            }
        }

        public void CloseAll()
        {
            foreach (var page in _loadedPages.Values) page.Screen.Close();
        }

        public void Shutdown()
        {
            CloseAll();
            foreach (var page in _loadedPages.Values) page.Handle.Dispose();
            _loadedPages.Clear();
            _loadingPages.Clear();
            _registrations.Clear();
            _resources = null;
            _initialized = false;
        }

        private async Task<LoadedPage> GetOrLoadAsync(UIPageId pageId)
        {
            if (_loadedPages.TryGetValue(pageId, out var loaded)) return loaded;
            if (!_registrations.TryGetValue(pageId, out var resourceId))
                throw new KeyNotFoundException($"UI 页面未注册：{pageId}");

            if (!_loadingPages.TryGetValue(pageId, out var loadingTask))
            {
                loadingTask = LoadPageAsync(pageId, resourceId);
                _loadingPages.Add(pageId, loadingTask);
            }

            try
            {
                return await loadingTask;
            }
            finally
            {
                if (loadingTask.IsCompleted) _loadingPages.Remove(pageId);
            }
        }

        private async Task<LoadedPage> LoadPageAsync(
            UIPageId pageId,
            ResourceId resourceId)
        {
            IInstanceHandle<UIScreen> handle = null;
            try
            {
                var resources = _resources;
                handle = await resources.InstantiateAsync<UIScreen>(resourceId, transform);
                var screen = handle.Instance;
                // if (screen.PageId != pageId)
                //     throw new InvalidOperationException(
                //         $"UI 资源 {resourceId} 声明的 PageId 是 {screen.PageId}，预期为 {pageId}。");

                screen.Close();
                var loaded = new LoadedPage(screen, handle);
                _loadedPages.Add(pageId, loaded);
                handle = null;
                return loaded;
            }
            finally
            {
                handle?.Dispose();
            }
        }

        private void EnsureInitialized()
        {
            if (!_initialized) throw new InvalidOperationException("UI 服务尚未初始化。");
        }

        protected override void OnDestroy()
        {
            if (IsPrimaryInstance) Shutdown();
            base.OnDestroy();
        }
    }
}
