using System;
using System.Threading.Tasks;
using GameFoundation.Service.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class PauseScreenArgs
    {
        public PauseScreenArgs(Func<Task> resumeAsync, Func<Task> returnToMenuAsync)
        {
            ResumeAsync = resumeAsync ?? throw new ArgumentNullException(nameof(resumeAsync));
            ReturnToMenuAsync = returnToMenuAsync ?? throw new ArgumentNullException(nameof(returnToMenuAsync));
        }

        public Func<Task> ResumeAsync { get; }
        public Func<Task> ReturnToMenuAsync { get; }
    }

    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _returnToMenuButton;

        private PauseScreenArgs _args;
        private bool _isBusy;

        protected override void OnOpened(object args)
        {
            _args = args as PauseScreenArgs
                ?? throw new ArgumentException("PauseScreen 需要 PauseScreenArgs。", nameof(args));
            _resumeButton?.onClick.AddListener(HandleResumeClicked);
            _returnToMenuButton?.onClick.AddListener(HandleReturnToMenuClicked);
            SetButtonsInteractable(true);
        }

        protected override void OnClosing()
        {
            _resumeButton?.onClick.RemoveListener(HandleResumeClicked);
            _returnToMenuButton?.onClick.RemoveListener(HandleReturnToMenuClicked);
            _args = null;
            _isBusy = false;
        }

        private async void HandleResumeClicked()
        {
            await InvokeOnceAsync(_args?.ResumeAsync);
        }

        private async void HandleReturnToMenuClicked()
        {
            await InvokeOnceAsync(_args?.ReturnToMenuAsync);
        }

        private async Task InvokeOnceAsync(Func<Task> action)
        {
            if (_isBusy || action == null) return;
            _isBusy = true;
            SetButtonsInteractable(false);

            try
            {
                await action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _isBusy = false;
                SetButtonsInteractable(true);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_resumeButton != null) _resumeButton.interactable = interactable;
            if (_returnToMenuButton != null) _returnToMenuButton.interactable = interactable;
        }
    }
}
