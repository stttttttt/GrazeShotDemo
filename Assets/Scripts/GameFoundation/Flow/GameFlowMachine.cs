using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameFoundation.Flow
{
    public interface IGameFlowNode<TState>
    {
        TState State { get; }
        Task EnterAsync();
        Task ExitAsync();
    }

    /// <summary>只有需要逐帧驱动的流程状态才实现此接口。</summary>
    public interface IGameFlowTickable
    {
        void Tick(float unscaledDeltaTime);
    }

    /// <summary>通用流程状态机，只负责节点切换，不知道任何具体游戏状态。</summary>
    public sealed class GameFlowMachine<TState> : IDisposable
    {
        private readonly Dictionary<TState, IGameFlowNode<TState>> _nodes =
            new Dictionary<TState, IGameFlowNode<TState>>();
        private IGameFlowNode<TState> _currentNode;
        private bool _isDisposed;

        public bool HasCurrentState => _currentNode != null;
        public bool IsTransitioning { get; private set; }
        public TState CurrentState => _currentNode != null
            ? _currentNode.State
            : throw new InvalidOperationException("流程状态机尚未进入任何状态。");

        public event Action<TState> StateChanged;

        public void Tick(float unscaledDeltaTime)
        {
            ThrowIfDisposed();
            if (IsTransitioning || _currentNode == null) return;
            if (_currentNode is IGameFlowTickable tickable) tickable.Tick(unscaledDeltaTime);
        }

        public void Register(IGameFlowNode<TState> node)
        {
            ThrowIfDisposed();
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (_nodes.ContainsKey(node.State))
                throw new InvalidOperationException($"流程状态重复注册：{node.State}");
            _nodes.Add(node.State, node);
        }

        public async Task ChangeStateAsync(TState nextState)
        {
            ThrowIfDisposed();
            if (IsTransitioning) throw new InvalidOperationException("流程状态正在切换中。");
            if (_currentNode != null && EqualityComparer<TState>.Default.Equals(_currentNode.State, nextState))
                return;
            if (!_nodes.TryGetValue(nextState, out var nextNode))
                throw new KeyNotFoundException($"流程状态尚未注册：{nextState}");

            IsTransitioning = true;
            var previousNode = _currentNode;
            try
            {
                if (previousNode != null)
                {
                    await previousNode.ExitAsync();
                    _currentNode = null;
                }

                try
                {
                    await nextNode.EnterAsync();
                    _currentNode = nextNode;
                    StateChanged?.Invoke(nextState);
                }
                catch (Exception transitionException)
                {
                    if (previousNode == null) throw;

                    try
                    {
                        await previousNode.EnterAsync();
                        _currentNode = previousNode;
                        StateChanged?.Invoke(previousNode.State);
                    }
                    catch (Exception restoreException)
                    {
                        throw new AggregateException(
                            "进入新流程状态失败，并且无法恢复先前状态。",
                            transitionException,
                            restoreException);
                    }

                    throw;
                }
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public async Task StopAsync()
        {
            ThrowIfDisposed();
            if (IsTransitioning) throw new InvalidOperationException("流程状态正在切换中。");
            if (_currentNode == null) return;

            IsTransitioning = true;
            try
            {
                await _currentNode.ExitAsync();
                _currentNode = null;
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _currentNode = null;
            _nodes.Clear();
            StateChanged = null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(GameFlowMachine<TState>));
        }
    }

    public sealed class DelegateGameFlowNode<TState> : IGameFlowNode<TState>
    {
        private readonly Func<Task> _enter;
        private readonly Func<Task> _exit;

        public DelegateGameFlowNode(
            TState state,
            Func<Task> enter,
            Func<Task> exit = null)
        {
            State = state;
            _enter = enter ?? throw new ArgumentNullException(nameof(enter));
            _exit = exit ?? (() => Task.CompletedTask);
        }

        public TState State { get; }
        public Task EnterAsync() => _enter();
        public Task ExitAsync() => _exit();
    }
}
