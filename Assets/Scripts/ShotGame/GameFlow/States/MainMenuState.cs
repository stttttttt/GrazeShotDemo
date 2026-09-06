using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Presentation.UI;

namespace ShotGame.GameFlow.States
{
    internal sealed class MainMenuState : AppFlowState
    {
        private static readonly UIPageId MainMenuPage = new UIPageId("MainMenu");
        private static readonly UIPageId SettingsPage = new UIPageId("Settings");

        public MainMenuState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.MainMenu;

        public override async Task EnterAsync()
        {
            await Context.StopGameplayAsync();
            Context.Time.Reset();
            Context.InputMode.SetMode(InputMode.UI);

            await Context.UI.OpenAsync(
                MainMenuPage,
                new MainMenuScreenArgs(
                    () => Context.ChangeStateAsync(AppState.Gameplay),
                    OpenSettingsAsync,
                    () => Context.ChangeStateAsync(AppState.CloseGame)));
        }

        public override Task ExitAsync()
        {
            Context.UI.Close(SettingsPage);
            Context.UI.Close(MainMenuPage);
            return Task.CompletedTask;
        }

        private Task OpenSettingsAsync()
        {
            return Context.UI.OpenAsync(
                SettingsPage,
                new SettingsScreenArgs(() => Context.UI.Close(SettingsPage)));
        }
    }
}
