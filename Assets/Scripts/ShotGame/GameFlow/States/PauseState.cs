using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Presentation.UI;

namespace ShotGame.GameFlow.States
{
    internal sealed class PauseState : AppFlowState
    {
        private static readonly UIPageId PausePage = new UIPageId("Pause");

        public PauseState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.Pause;

        public override async Task EnterAsync()
        {
            Context.Time.SetPaused(true);
            Context.InputMode.SetMode(InputMode.UI);

            try
            {
                await Context.UI.OpenAsync(
                    PausePage,
                    new PauseScreenArgs(
                        () => Context.ChangeStateAsync(AppState.Gameplay),
                        () => Context.ChangeStateAsync(AppState.MainMenu)));
            }
            catch
            {
                Context.InputMode.SetMode(InputMode.Gameplay);
                Context.Time.SetPaused(false);
                throw;
            }
        }

        public override Task ExitAsync()
        {
            Context.UI.Close(PausePage);
            return Task.CompletedTask;
        }
    }
}
