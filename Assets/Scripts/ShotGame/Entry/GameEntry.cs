using System;
using GameFoundation.Core;
using GameFoundation.Service.Config;
using GameFoundation.Service.Input;
using GameFoundation.Service.Lifecycle;
using GameFoundation.Service.Resource;
using GameFoundation.Service.Scene;
using GameFoundation.Service.Time;
using GameFoundation.Service.UI;
using ShotGame.GameFlow;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Intent;
using UnityEngine;

namespace ShotGame.Entry
{
    /// <summary>游戏唯一总入口、Composition Root 与 Gameplay Tick 驱动器。</summary>
    [DefaultExecutionOrder(-1200)]
    [DisallowMultipleComponent]
    public sealed class GameEntry : PersistentMonoSingleton<GameEntry>
    {
        [Header("应用配置")]
        [SerializeField] private GameFoundationConfig _foundationConfig;
        [SerializeField] private GameResourceCatalog _resourceCatalog;
        [SerializeField] private GameplayContentConfig _gameplayContentConfig;

        [Header("场景内服务")]
        [SerializeField] private UIService _uiService;

        private CatalogResourceService _resources;
        private InputModeService _inputMode;
        private GameTimeService _time;
        private TimerScheduler _timers;
        private AppServiceGroup _appServices;
        private GameAppFlow _appFlow;
        private GameplayInputAdapter _gameplayInput;
        private bool _started;
        private bool _shuttingDown;

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;
            ValidateReferences();
        }

        private async void Start()
        {
            if (!IsPrimaryInstance) return;

            try
            {
                ComposeServices();
                await _appServices.InitializeAsync();
                ComposeGameFlow();
                await _appFlow.StartAsync();
                _started = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShutdownApplication();
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_started || _shuttingDown) return;

            _time.Tick(UnityEngine.Time.unscaledDeltaTime);
            _timers.Tick();
            _appFlow.Tick();
        }

        private void FixedUpdate()
        {
            if (!_started || _shuttingDown) return;
            _appFlow.FixedTick(UnityEngine.Time.fixedDeltaTime);
        }

        protected override void OnDestroy()
        {
            if (IsPrimaryInstance) ShutdownApplication();
            base.OnDestroy();
        }

        private void ComposeServices()
        {
            _resources = new CatalogResourceService(_resourceCatalog);
            _inputMode = new InputModeService(
                _foundationConfig.InputActions,
                _foundationConfig.GameplayActionMap,
                _foundationConfig.UiActionMap);
            _time = new GameTimeService();
            _timers = new TimerScheduler(_time);

            var scenes = SceneService.Instance;
            var ui = _uiService != null ? _uiService : UIService.Instance;
            if (ui == null) throw new InvalidOperationException("Boot 场景中缺少 UIService。");
            ui.Configure(_resources);

            _appServices = new AppServiceGroup(scenes, _inputMode, ui);
        }

        private void ComposeGameFlow()
        {
            _gameplayInput = new GameplayInputAdapter(_inputMode.Actions);
            var scenes = SceneService.Instance;
            var ui = _uiService != null ? _uiService : UIService.Instance;
            _appFlow = new GameAppFlow(
                scenes,
                ui,
                _inputMode,
                _time,
                _gameplayContentConfig,
                _gameplayInput,
                Application.Quit);
        }

        private void ShutdownApplication()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;

            _started = false;
            _appFlow?.Dispose();
            _appFlow = null;

            _gameplayInput?.Dispose();
            _gameplayInput = null;

            _timers?.Dispose();
            _timers = null;

            _time?.Dispose();
            _time = null;

            _appServices?.Shutdown();
            _appServices = null;
            _inputMode = null;
            _resources = null;
        }

        private void ValidateReferences()
        {
            if (_foundationConfig == null)
                throw new InvalidOperationException("GameEntry 缺少 GameFoundationConfig。");
            if (_resourceCatalog == null)
                throw new InvalidOperationException("GameEntry 缺少 GameResourceCatalog。");
            if (_gameplayContentConfig == null)
                throw new InvalidOperationException("GameEntry 缺少 GameplayContentConfig。");
        }
    }
}
