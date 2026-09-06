using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using UnityEngine.InputSystem;

namespace GameFoundation.Service.Input
{
    public sealed class InputModeService : IInputModeService, IAppService
    {
        private readonly InputActionAsset _source;
        private readonly string _gameplayMapName;
        private readonly string _uiMapName;
        private InputActionAsset _runtimeActions;

        public InputModeService(InputActionAsset source, string gameplayMapName, string uiMapName)
        {
            _source = source;
            _gameplayMapName = gameplayMapName;
            _uiMapName = uiMapName;
        }

        public string Name => "输入服务";
        public InputMode CurrentMode { get; private set; } = InputMode.Disabled;
        public InputActionAsset Actions => _runtimeActions;

        public Task InitializeAsync()
        {
            if (_source == null) throw new InvalidOperationException("GameFoundationConfig 未配置 InputActionAsset。");
            _runtimeActions = UnityEngine.Object.Instantiate(_source);
            SetMode(InputMode.Disabled);
            return Task.CompletedTask;
        }

        public void SetMode(InputMode mode)
        {
            CurrentMode = mode;
            if (_runtimeActions == null) return;

            _runtimeActions.Disable();
            var mapName = mode == InputMode.Gameplay ? _gameplayMapName : _uiMapName;
            if (mode == InputMode.Disabled) return;

            var map = _runtimeActions.FindActionMap(mapName, false);
            if (map == null) throw new InvalidOperationException($"找不到 Input Action Map：{mapName}");
            map.Enable();
        }

        public void Shutdown()
        {
            CurrentMode = InputMode.Disabled;
            if (_runtimeActions == null) return;
            _runtimeActions.Disable();
            UnityEngine.Object.Destroy(_runtimeActions);
            _runtimeActions = null;
        }
    }
}
