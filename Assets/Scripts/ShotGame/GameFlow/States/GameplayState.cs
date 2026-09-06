using System;
using System.Threading.Tasks;
using GameFoundation.Core;

namespace ShotGame.GameFlow.States
{
    internal sealed class GameplayState : AppFlowState
    {
        private static readonly UIPageId GameplayHudPage = new UIPageId("Gameplay");

        public GameplayState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.Gameplay;

        public override async Task EnterAsync()
        {
            if (Context.Session != null)
            {
                ResumeGameplay();
                return;
            }

            Context.InputMode.SetMode(InputMode.Disabled);
            Context.Time.SetPaused(false);
            Context.Time.SetTimeScale(1f);

            try
            {
                // Gameplay 首次 Enter 包含场景加载和 Session 的完整创建流程。
                await Context.Scenes.LoadAdditiveAsync(Context.Config.GameplayScene);
                await Context.CreateSessionAsync();

                await Context.UI.OpenAsync(GameplayHudPage, Context.Session);
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
    }
}
