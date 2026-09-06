using System;
using GameFoundation.Service.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class SettingsScreenArgs
    {
        public SettingsScreenArgs(Action close)
        {
            Close = close ?? throw new ArgumentNullException(nameof(close));
        }

        public Action Close { get; }
    }

    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField] private Button _closeButton;
        private SettingsScreenArgs _args;

        protected override void OnOpened(object args)
        {
            _args = args as SettingsScreenArgs
                ?? throw new ArgumentException("SettingsScreen 需要 SettingsScreenArgs。", nameof(args));
            _closeButton?.onClick.AddListener(HandleCloseClicked);
        }

        protected override void OnClosing()
        {
            _closeButton?.onClick.RemoveListener(HandleCloseClicked);
            _args = null;
        }

        private void HandleCloseClicked() => _args?.Close();
    }
}
