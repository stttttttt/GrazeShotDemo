using System;
using System.Threading.Tasks;
using GameFoundation.Service.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class MainMenuScreenArgs
    {
        public MainMenuScreenArgs(
            Func<Task> startGameAsync,
            Func<Task> openSettingsAsync,
            Func<Task> closeGameAsync)
        {
            StartGameAsync = startGameAsync ?? throw new ArgumentNullException(nameof(startGameAsync));
            OpenSettingsAsync = openSettingsAsync ?? throw new ArgumentNullException(nameof(openSettingsAsync));
            CloseGameAsync = closeGameAsync ?? throw new ArgumentNullException(nameof(closeGameAsync));
        }

        public Func<Task> StartGameAsync { get; }
        public Func<Task> OpenSettingsAsync { get; }
        public Func<Task> CloseGameAsync { get; }
    }

    public sealed class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        private MainMenuScreenArgs _args;
        private bool _isBusy;

        protected override void OnOpened(object args)
        {
            _args = args as MainMenuScreenArgs
                ?? throw new ArgumentException("MainMenuScreen 需要 MainMenuScreenArgs。", nameof(args));

            _startButton?.onClick.AddListener(HandleStartClicked);
            _settingsButton?.onClick.AddListener(HandleSettingsClicked);
            _quitButton?.onClick.AddListener(HandleQuitClicked);
            SetButtonsInteractable(true);
        }

        protected override void OnClosing()
        {
            _startButton?.onClick.RemoveListener(HandleStartClicked);
            _settingsButton?.onClick.RemoveListener(HandleSettingsClicked);
            _quitButton?.onClick.RemoveListener(HandleQuitClicked);
            _args = null;
            _isBusy = false;
        }

        private async void HandleStartClicked()
        {
            if (_isBusy || _args == null) return;
            _isBusy = true;
            SetButtonsInteractable(false);

            try
            {
                await _args.StartGameAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _isBusy = false;
                SetButtonsInteractable(true);
            }
        }

        private async void HandleSettingsClicked()
        {
            if (_isBusy || _args == null) return;
            try
            {
                await _args.OpenSettingsAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async void HandleQuitClicked()
        {
            if (_isBusy) return;
            await InvokeMenuActionAsync(_args?.CloseGameAsync, lockMenu: true);
        }

        private async Task InvokeMenuActionAsync(Func<Task> action, bool lockMenu)
        {
            if (_isBusy || action == null) return;
            if (lockMenu)
            {
                _isBusy = true;
                SetButtonsInteractable(false);
            }

            try
            {
                await action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (lockMenu)
                {
                    _isBusy = false;
                    SetButtonsInteractable(true);
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_startButton != null) _startButton.interactable = interactable;
            if (_settingsButton != null) _settingsButton.interactable = interactable;
            if (_quitButton != null) _quitButton.interactable = interactable;
        }
    }
}
