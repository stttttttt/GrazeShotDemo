using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Scene;
using ShotGame.Presentation.UI;

namespace ShotGame.GameFlow.States
{
    internal sealed class GameplayState : AppFlowState
    {
        private static readonly UIPageId GameplayHudPage = new UIPageId("Gameplay");
        private static readonly UIPageId GameplayResultPage = new UIPageId("GameplayResult");
        private static readonly SceneId GameplayScene = new SceneId("GamePlay");
        private IDisposable _settlementSubscription;
        private bool _isHandlingResult;

        public GameplayState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.Gameplay;

        public override async Task EnterAsync()
        {
            if (Context.Session != null)
            {
                ResumeGameplay();
                SubscribeToSettlement();
                return;
            }

            await StartNewGameplayAsync();
        }

        public override Task ExitAsync()
        {
            _settlementSubscription?.Dispose();
            _settlementSubscription = null;
            return Task.CompletedTask;
        }

        private async Task StartNewGameplayAsync()
        {
            _isHandlingResult = false;
            Context.InputMode.SetMode(InputMode.Disabled);
            Context.Time.Reset();

            try
            {
                // Gameplay 首次 Enter 包含场景加载和 Session 的完整创建流程。
                await Context.Scenes.LoadAdditiveAsync(GameplayScene);
                Context.SetLoadedGameplayScene(GameplayScene);
                var sceneContext = GameplaySceneResolver.Resolve(GameplayScene);
                await Context.CreateSessionAsync(sceneContext);

                await Context.UI.OpenAsync(GameplayHudPage, Context.Session);
                Context.CreateFeedback();
                SubscribeToSettlement();
                Context.InputMode.SetMode(InputMode.Gameplay);
            }
            catch
            {
                await Context.StopGameplayAsync();
                throw;
            }
        }

        private void ResumeGameplay()
        {
            if (!Context.Session.IsInitialized)
                throw new InvalidOperationException("不能恢复尚未初始化的 GameplaySession。");

            Context.InputMode.SetMode(InputMode.Gameplay);
            Context.Time.SetPaused(false);
        }

        private void SubscribeToSettlement()
        {
            _settlementSubscription?.Dispose();
            _settlementSubscription = Context.Session.Facts.Subscribe<GameplaySettledFact>(OnGameplaySettled);
        }

        private async void OnGameplaySettled(GameplaySettledFact fact)
        {
            if (_isHandlingResult) return;
            _isHandlingResult = true;
            Context.InputMode.SetMode(InputMode.UI);
            Context.Time.SetPaused(false);
            try
            {
                await Context.UI.OpenAsync(
                    GameplayResultPage,
                    new GameplayResultScreenArgs(fact.Result, RestartAsync, ReturnToMenuAsync));
                Context.Session?.Run.Finish();
            }
            catch (Exception exception)
            {
                _isHandlingResult = false;
                UnityEngine.Debug.LogException(exception);
            }
        }

        private async Task RestartAsync()
        {
            Context.InputMode.SetMode(InputMode.Disabled);
            CloseGameplayPages();
            _settlementSubscription?.Dispose();
            _settlementSubscription = null;
            await Context.StopGameplayAsync();
            _isHandlingResult = false;
            await StartNewGameplayAsync();
        }

        private async Task ReturnToMenuAsync()
        {
            Context.InputMode.SetMode(InputMode.Disabled);
            CloseGameplayPages();
            await Context.ChangeStateAsync(AppState.MainMenu);
        }

        private void CloseGameplayPages()
        {
            Context.UI.Close(GameplayResultPage);
            Context.UI.Close(GameplayHudPage);
        }
    }
}
