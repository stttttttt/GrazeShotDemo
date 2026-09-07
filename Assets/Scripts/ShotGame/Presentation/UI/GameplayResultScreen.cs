using System;
using System.Threading.Tasks;
using GameFoundation.Service.UI;
using ShotGame.Gameplay.Run;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Presentation.UI
{
    public sealed class GameplayResultScreenArgs
    {
        public GameplayResultScreenArgs(GameplayResult result, Func<Task> restartAsync,
            Func<Task> returnToMenuAsync)
        {
            Result = result;
            RestartAsync = restartAsync ?? throw new ArgumentNullException(nameof(restartAsync));
            ReturnToMenuAsync = returnToMenuAsync ?? throw new ArgumentNullException(nameof(returnToMenuAsync));
        }

        public GameplayResult Result { get; }
        public Func<Task> RestartAsync { get; }
        public Func<Task> ReturnToMenuAsync { get; }
    }

    /// <summary>单局结束后的简单结算页，只展示结果并转发按钮意图。</summary>
    public sealed class GameplayResultScreen : UIScreen
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _detailsText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _returnToMenuButton;

        private GameplayResultScreenArgs _args;
        private bool _isBusy;

        protected override void OnOpened(object args)
        {
            _args = args as GameplayResultScreenArgs
                ?? throw new ArgumentException("GameplayResultScreen 需要 GameplayResultScreenArgs。", nameof(args));
            if (_titleText == null || _detailsText == null || _restartButton == null ||
                _returnToMenuButton == null)
                throw new InvalidOperationException("GameplayResultScreen 缺少 UI 引用。");

            ShowResult(_args.Result);
            _restartButton.onClick.AddListener(HandleRestartClicked);
            _returnToMenuButton.onClick.AddListener(HandleReturnToMenuClicked);
            SetButtonsInteractable(true);
        }

        protected override void OnClosing()
        {
            _restartButton?.onClick.RemoveListener(HandleRestartClicked);
            _returnToMenuButton?.onClick.RemoveListener(HandleReturnToMenuClicked);
            _args = null;
            _isBusy = false;
        }

        private void ShowResult(GameplayResult result)
        {
            var victory = result.Type == GameplayResultType.Victory;
            _titleText.text = victory ? "挑战成功" : "挑战失败";
            var reachedWave = result.TotalWaveCount > 0
                ? $"到达波次  {result.ReachedWaveIndex}/{result.TotalWaveCount}"
                : $"到达波次  {result.ReachedWaveIndex}（无限模式）";
            var details = reachedWave + "\n" +
                          $"用时  {result.ElapsedTime:0.0} 秒\n" +
                          $"当前武器  {result.PlayerWeaponName}";
            if (!victory)
            {
                var source = result.LastDamageSourceId.IsValid
                    ? result.LastDamageSourceId.ToString()
                    : "未知";
                details += $"\n最后伤害  {result.LastDamageAmount:0.#}（来源 {source}）\n" +
                           $"倒下时擦弹阶段  {result.GrazePhaseAtDefeat}";
            }
            _detailsText.text = details;
        }

        private async void HandleRestartClicked() => await InvokeOnceAsync(_args?.RestartAsync);

        private async void HandleReturnToMenuClicked() => await InvokeOnceAsync(_args?.ReturnToMenuAsync);

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
            if (_restartButton != null) _restartButton.interactable = interactable;
            if (_returnToMenuButton != null) _returnToMenuButton.interactable = interactable;
        }
    }
}
