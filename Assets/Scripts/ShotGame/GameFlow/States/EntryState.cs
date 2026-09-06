using System.Threading.Tasks;
using GameFoundation.Core;

namespace ShotGame.GameFlow.States
{
    internal sealed class EntryState : AppFlowState
    {
        public EntryState(AppFlowContext context) : base(context)
        {
        }

        public override AppState State => AppState.Entry;

        public override Task EnterAsync()
        {
            Context.UI.CloseAll();
            Context.InputMode.SetMode(InputMode.Disabled);
            Context.Time.Reset();

            // Enter 完成后由 GameAppFlow 处理排队切换，避免状态机重入。
            Context.QueueState(AppState.MainMenu);
            return Task.CompletedTask;
        }
    }
}
