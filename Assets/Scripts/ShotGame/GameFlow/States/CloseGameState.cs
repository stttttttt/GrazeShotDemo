using System.Threading.Tasks;
using GameFoundation.Core;

namespace ShotGame.GameFlow.States
{
    internal sealed class CloseGameState : AppFlowState
    {
        public CloseGameState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.CloseGame;

        public override async Task EnterAsync()
        {
            Context.InputMode.SetMode(InputMode.Disabled);
            await Context.StopGameplayAsync();
            Context.UI.CloseAll();
            Context.Quit();
        }
    }
}
