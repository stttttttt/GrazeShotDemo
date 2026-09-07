using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShotGame.Gameplay.Intent
{
    /// <summary>Input System 到 PlayerInputComponent 的普通 C# 适配器。</summary>
    public sealed class GameplayInputAdapter : IDisposable
    {
        private readonly InputActionMap _gameplayMap;
        private readonly InputAction _move;
        private readonly InputAction _aim;
        private readonly InputAction _fire;
        private readonly InputAction _graze;
        private readonly InputAction _reload;
        private readonly InputAction _dash;
        private readonly InputAction _switchWeaponStep;
        private readonly InputAction _weaponSlot1;
        private readonly InputAction _weaponSlot2;
        private readonly InputAction _weaponSlot3;
        private readonly InputAction _quickSwap;
        private readonly InputAction _gameplayPause;
        private readonly InputAction _uiCancel;

        private PlayerInputComponent _playerInput;
        private Transform _playerTransform;
        private Camera _camera;
        private Vector2 _lastAimDirection = Vector2.right;
        private bool _pauseRequested;
        private bool _disposed;

        public GameplayInputAdapter(InputActionAsset actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            _gameplayMap = RequireMap(actions, "Gameplay");
            _move = RequireAction(_gameplayMap, "Move");
            _aim = RequireAction(_gameplayMap, "Aim");
            _fire = RequireAction(_gameplayMap, "Fire");
            _graze = RequireAction(_gameplayMap, "Graze");
            _reload = RequireAction(_gameplayMap, "Reload");
            _dash = RequireAction(_gameplayMap, "Dash");
            _switchWeaponStep = RequireAction(_gameplayMap, "SwitchWeaponStep");
            _weaponSlot1 = RequireAction(_gameplayMap, "WeaponSlot1");
            _weaponSlot2 = RequireAction(_gameplayMap, "WeaponSlot2");
            _weaponSlot3 = RequireAction(_gameplayMap, "WeaponSlot3");
            _quickSwap = RequireAction(_gameplayMap, "QuickSwap");
            _gameplayPause = RequireAction(_gameplayMap, "Pause");
            _uiCancel = RequireAction(RequireMap(actions, "UI"), "Cancel");
        }

        public void Bind(PlayerInputComponent playerInput, Transform playerTransform, Camera gameplayCamera)
        {
            ThrowIfDisposed();
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
            _playerTransform = playerTransform != null ? playerTransform : throw new ArgumentNullException(nameof(playerTransform));
            _camera = gameplayCamera != null ? gameplayCamera : throw new ArgumentNullException(nameof(gameplayCamera));
            _lastAimDirection = _playerTransform.right;
        }

        public void Tick()
        {
            ThrowIfDisposed();
            if (_gameplayPause.WasPressedThisFrame() || _uiCancel.WasPressedThisFrame()) _pauseRequested = true;
            if (_playerInput == null || !_gameplayMap.enabled) return;

            _playerInput.SetMoveDirection(_move.ReadValue<Vector2>());
            UpdateAim();
            _playerInput.SetFire(_fire.IsPressed());
            _playerInput.SetGraze(_graze.IsPressed());
            if (_reload.WasPressedThisFrame()) _playerInput.PressReload();
            if (_dash.WasPressedThisFrame()) _playerInput.PressDash();
            if (_weaponSlot1.WasPressedThisFrame()) _playerInput.SelectWeaponSlot(1);
            if (_weaponSlot2.WasPressedThisFrame()) _playerInput.SelectWeaponSlot(2);
            if (_weaponSlot3.WasPressedThisFrame()) _playerInput.SelectWeaponSlot(3);
            if (_quickSwap.WasPressedThisFrame()) _playerInput.PressQuickSwap();

            var switchValue = _switchWeaponStep.ReadValue<float>();
            if (Mathf.Abs(switchValue) > 0.01f) _playerInput.StepWeapon(switchValue > 0f ? 1 : -1);
        }

        public bool ConsumePauseRequest()
        {
            if (!_pauseRequested) return false;
            _pauseRequested = false;
            return true;
        }

        public void Unbind()
        {
            _playerInput?.SetFire(false);
            _playerInput?.SetGraze(false);
            _playerInput?.SetMoveDirection(Vector2.zero);
            _playerInput = null;
            _playerTransform = null;
            _camera = null;
            _pauseRequested = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Unbind();
            _disposed = true;
        }

        private void UpdateAim()
        {
            // 鼠标为当前 Demo 的主瞄准设备，直接读取指针位置可保证朝向更新及时。
            // 保留 Aim Action 作为没有鼠标时的兼容输入来源。
            var pointer = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : _aim.ReadValue<Vector2>();
            var depth = Mathf.Abs(_camera.transform.position.z - _playerTransform.position.z);
            var worldPoint = _camera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, depth));
            var direction = (Vector2)(worldPoint - _playerTransform.position);
            if (direction.sqrMagnitude > 0.0001f) _lastAimDirection = direction.normalized;
            _playerInput.SetAimDirection(_lastAimDirection);
        }

        private static InputActionMap RequireMap(InputActionAsset actions, string name)
        {
            return actions.FindActionMap(name, false)
                ?? throw new InvalidOperationException($"找不到 Input Action Map：{name}");
        }

        private static InputAction RequireAction(InputActionMap map, string name)
        {
            return map.FindAction(name, false)
                ?? throw new InvalidOperationException($"Input Action Map {map.name} 缺少 Action：{name}");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayInputAdapter));
        }
    }
}
