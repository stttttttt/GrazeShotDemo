using System;
using System.Threading.Tasks;
using GameFoundation.Core;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Intent;
using ShotGame.Gameplay.Run;
using ShotGame.Gameplay.Scene;
using ShotGame.Presentation.Feedback;
using ShotGame.Presentation.UI;

namespace ShotGame.GameFlow
{
    /// <summary>状态共享的应用级依赖和当前局内 Session，不包含状态切换规则。</summary>
    internal sealed class AppFlowContext
    {
        private readonly Func<AppState, Task> _changeStateAsync;
        private readonly Action<AppState> _queueState;
        private SceneId _loadedGameplayScene;

        public AppFlowContext(
            ISceneService scenes,
            IUIService ui,
            IInputModeService inputMode,
            IGameTimeService time,
            GameplayContentConfig gameplayContent,
            GameplayInputAdapter gameplayInput,
            Func<AppState, Task> changeStateAsync,
            Action<AppState> queueState,
            Action quit)
        {
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            InputMode = inputMode ?? throw new ArgumentNullException(nameof(inputMode));
            Time = time ?? throw new ArgumentNullException(nameof(time));
            GameplayContent = gameplayContent != null
                ? gameplayContent
                : throw new ArgumentNullException(nameof(gameplayContent));
            GameplayInput = gameplayInput ?? throw new ArgumentNullException(nameof(gameplayInput));
            _changeStateAsync = changeStateAsync ?? throw new ArgumentNullException(nameof(changeStateAsync));
            _queueState = queueState ?? throw new ArgumentNullException(nameof(queueState));
            Quit = quit ?? throw new ArgumentNullException(nameof(quit));
        }

        public ISceneService Scenes { get; }
        public IUIService UI { get; }
        public IInputModeService InputMode { get; }
        public IGameTimeService Time { get; }
        public GameplayContentConfig GameplayContent { get; }
        public GameplayInputAdapter GameplayInput { get; }
        public Action Quit { get; }
        public GameplaySession Session { get; private set; }
        public GameplayFeedbackController Feedback { get; private set; }

        public Task ChangeStateAsync(AppState nextState) => _changeStateAsync(nextState);
        public void QueueState(AppState nextState) => _queueState(nextState);

        public void SetLoadedGameplayScene(SceneId sceneId)
        {
            if (!sceneId.IsValid) throw new ArgumentException("Gameplay SceneId 无效。", nameof(sceneId));
            _loadedGameplayScene = sceneId;
        }

        public async Task CreateSessionAsync(GameplaySceneContext sceneContext)
        {
            if (Session != null) throw new InvalidOperationException("当前已经存在 GameplaySession。");

            var session = new GameplaySession(Time);
            Session = session;
            try
            {
                await session.InitializeAsync(sceneContext, GameplayContent, GameplayInput);
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

            var gameplayScene = _loadedGameplayScene;
            _loadedGameplayScene = default;
            DisposeFeedback();
            DisposeSession();
            if (gameplayScene.IsValid && Scenes.IsLoaded(gameplayScene))
                await Scenes.UnloadAsync(gameplayScene);
        }

        public void DisposeSession()
        {
            DisposeFeedback();
            Session?.Dispose();
            Session = null;
        }

        public void CreateFeedback()
        {
            if (Session == null) throw new InvalidOperationException("创建表现控制器前必须先创建 GameplaySession。");
            DisposeFeedback();
            Feedback = new GameplayFeedbackController(Session, GameplayContent,
                GameplayScreen.ActiveInstance);
        }

        private void DisposeFeedback()
        {
            Feedback?.Dispose();
            Feedback = null;
        }
    }
}
