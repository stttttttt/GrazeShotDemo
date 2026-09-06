using System.Threading.Tasks;
using GameFoundation.Flow;

namespace ShotGame.GameFlow.States
{
    internal abstract class AppFlowState : IGameFlowNode<AppState>
    {
        protected AppFlowState(AppFlowContext context)
        {
            Context = context;
        }

        protected AppFlowContext Context { get; }
        public abstract AppState State { get; }
        public abstract Task EnterAsync();
        public virtual Task ExitAsync() => Task.CompletedTask;
    }
}
