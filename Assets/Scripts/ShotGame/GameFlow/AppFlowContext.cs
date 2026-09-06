using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Gameplay.Run;

namespace ShotGame.GameFlow
{
    /// <summary>状态共享的应用级依赖和当前局内 Session，不包含状态切换规则。</summary>
    internal sealed class AppFlowContext
    {
        private readonly Func<AppState, Task> _changeStateAsync;
        private readonly Action<AppState> _queueState;

        public AppFlowContext(
            GameAppConfig config,
            ISceneService scenes,
            IUIService ui,
            IInputModeService inputMode,
            IGameTimeService time,
            Func<AppState, Task> changeStateAsync,
            Action<AppState> queueState,
            Action quit)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            InputMode = inputMode ?? throw new ArgumentNullException(nameof(inputMode));
            Time = time ?? throw new ArgumentNullException(nameof(time));
            _changeStateAsync = changeStateAsync ?? throw new ArgumentNullException(nameof(changeStateAsync));
            _queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
            Quit = quit ?? throw new ArgumentNullException(nameof(quit));
        }

        public GameAppConfig Config { get; }
        public ISceneService Scenes { get; }
        public IUIService UI { get; }
        public IInputModeService InputMode { get; }
        public IGameTimeService Time { get; }
        public Action Quit { get; }
        public GameplaySession Session { get; private set; }

        public Task ChangeStateAsync(AppState nextState) => _changeStateAsync(nextState);
        public void QueueState(AppState nextState) => _queueState(nextState);

        public async Task CreateSessionAsync()
        {
            if (Session != null) throw new InvalidOperationException("当前已经存在 GameplaySession。");

            var session = new GameplaySession(Time);
            Session = session;
            try
            {
                await session.InitializeAsync();
            }
            catch
            {
                DisposeSession();
                throw;
            }
        }

        public async Task StopGameplayAsync()
        {
            InputMode.SetMode(GameFoundation.Core.InputMode.Disabled);
            Time.SetPaused(false);
            Time.SetTimeScale(1f);

            DisposeSession();
            if (Scenes.IsLoaded(Config.GameplayScene))
                await Scenes.UnloadAsync(Config.GameplayScene);
        }

        public void DisposeSession()
        {
            Session?.Dispose();
            Session = null;
        }
    }
}
