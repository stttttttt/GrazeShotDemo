using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFoundation.Core;
using GameFoundation.Flow;
using ShotGame.GameFlow.States;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Intent;
using UnityEngine;

namespace ShotGame.GameFlow
{
    /// <summary>只负责状态注册、合法性校验和状态流转；具体工作由状态类完成。</summary>
    public sealed class GameAppFlow : IDisposable
    {
        private static readonly IReadOnlyDictionary<AppState, AppState[]> AllowedTransitions =
            new Dictionary<AppState, AppState[]>
            {
                [AppState.Entry] = new[] { AppState.MainMenu, AppState.CloseGame },
                [AppState.MainMenu] = new[] { AppState.Gameplay, AppState.CloseGame },
                [AppState.Gameplay] = new[] { AppState.Pause, AppState.MainMenu, AppState.CloseGame },
                [AppState.Pause] = new[] { AppState.Gameplay, AppState.MainMenu, AppState.CloseGame },
                [AppState.CloseGame] = Array.Empty<AppState>()
            };

        private readonly GameFlowMachine<AppState> _machine = new GameFlowMachine<AppState>();
        private readonly AppFlowContext _context;

        private AppState? _queuedState;
        private bool _isChangingState;
        private bool _disposed;

        public GameAppFlow(
            ISceneService scenes,
            IUIService ui,
            IInputModeService inputMode,
            IGameTimeService time,
            GameplayContentConfig gameplayContent,
            GameplayInputAdapter gameplayInput,
            Action quit)
        {
            _context = new AppFlowContext(
                scenes,
                ui,
                inputMode,
                time,
                gameplayContent,
                gameplayInput,
                ChangeStateAsync,
                QueueState,
                quit);

            RegisterStates();
        }

        public bool HasCurrentState => _machine.HasCurrentState;
        public AppState CurrentState => _machine.CurrentState;
        public bool IsPaused => _context.Time.IsPaused;

        public Task StartAsync()
        {
            ThrowIfDisposed();
            return ChangeStateAsync(AppState.Entry);
        }

        public Task ChangeStateAsync(AppState nextState)
        {
            ThrowIfDisposed();
            if (_isChangingState)
            {
                QueueState(nextState);
                return Task.CompletedTask;
            }

            return ChangeStateAndDrainQueueAsync(nextState);
        }

        public void Tick()
        {
            ThrowIfDisposed();
            _context.GameplayInput.Tick();
            TryHandlePauseRequest();
            _machine.Tick(_context.Time.UnscaledDeltaTime);
            if (CanTickGameplay())
            {
                _context.Session.Tick();
                _context.Feedback?.Tick(_context.Time.UnscaledDeltaTime);
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ThrowIfDisposed();
            if (CanTickGameplay()) _context.Session.FixedTick(fixedDeltaTime);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _queuedState = null;
            _context.DisposeSession();
            _machine.Dispose();
        }

        private async Task ChangeStateAndDrainQueueAsync(AppState nextState)
        {
            _isChangingState = true;
            try
            {
                var target = nextState;
                while (true)
                {
                    ValidateTransition(target);
                    _queuedState = null;
                    await _machine.ChangeStateAsync(target);

                    if (!_queuedState.HasValue) break;
                    target = _queuedState.Value;
                }
            }
            catch
            {
                _queuedState = null;
                throw;
            }
            finally
            {
                _isChangingState = false;
            }
        }

        private void QueueState(AppState nextState)
        {
            ThrowIfDisposed();
            if (_queuedState.HasValue && _queuedState.Value != nextState)
                throw new InvalidOperationException(
                    $"同一次状态切换中产生了冲突的后续状态：{_queuedState.Value} 与 {nextState}");
            _queuedState = nextState;
        }

        private void ValidateTransition(AppState nextState)
        {
            if (!_machine.HasCurrentState)
            {
                if (nextState != AppState.Entry)
                    throw new InvalidOperationException($"应用必须从 {AppState.Entry} 状态启动。");
                return;
            }

            var currentState = _machine.CurrentState;
            if (currentState == nextState) return;

            var allowed = AllowedTransitions[currentState];
            for (var index = 0; index < allowed.Length; index++)
            {
                if (allowed[index] == nextState) return;
            }

            throw new InvalidOperationException($"不允许从 {currentState} 切换到 {nextState}。");
        }

        private void RegisterStates()
        {
            _machine.Register(new EntryState(_context));
            _machine.Register(new MainMenuState(_context));
            _machine.Register(new GameplayState(_context));
            _machine.Register(new PauseState(_context));
            _machine.Register(new CloseGameState(_context));
        }

        private void TryHandlePauseRequest()
        {
            if (!_context.GameplayInput.ConsumePauseRequest() || _isChangingState || !_machine.HasCurrentState)
                return;

            if (_machine.CurrentState == AppState.Gameplay)
            {
                var runState = _context.Session?.Run.State;
                if (runState == Gameplay.Run.GameplayRunState.Countdown ||
                    runState == Gameplay.Run.GameplayRunState.WaveActive ||
                    runState == Gameplay.Run.GameplayRunState.WaveInterval)
                    ChangeStateFromTick(AppState.Pause);
            }
            else if (_machine.CurrentState == AppState.Pause)
                ChangeStateFromTick(AppState.Gameplay);
        }

        private async void ChangeStateFromTick(AppState state)
        {
            try
            {
                await ChangeStateAsync(state);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private bool CanTickGameplay()
        {
            return _machine.HasCurrentState &&
                   !_machine.IsTransitioning &&
                   _machine.CurrentState == AppState.Gameplay &&
                   _context.Session != null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameAppFlow));
        }
    }
}
