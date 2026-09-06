using System;
using System.Collections.Generic;
using GameFoundation.Core;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.World;

namespace ShotGame.Gameplay.Run
{
    public sealed class GameplayRun : IDisposable
    {
        private readonly GameplayWorld _world;
        private readonly GameplayFactHub _facts;
        private readonly IGameTimeService _time;
        private readonly List<IGameplayCondition> _victoryConditions = new List<IGameplayCondition>();
        private readonly List<IGameplayCondition> _defeatConditions = new List<IGameplayCondition>();
        private readonly IDisposable _deathSubscription;
        private readonly IDisposable _victorySubscription;
        private readonly IDisposable _defeatSubscription;
        private readonly float _countdownDuration;
        private float _countdownRemaining;
        private bool _settled;
        private bool _disposed;

        public GameplayRun(GameplayWorld world, GameplayFactHub facts, IGameTimeService time,
            float countdownDuration = 1.5f)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _countdownDuration = Math.Max(0f, countdownDuration);
            _deathSubscription = facts.Subscribe<CharacterDiedFact>(OnCharacterDied);
            _victorySubscription = facts.Subscribe<VictoryRequestedFact>(_ => RequestVictory());
            _defeatSubscription = facts.Subscribe<DefeatRequestedFact>(_ => RequestDefeat());
        }

        public GameplayRunState State { get; private set; } = GameplayRunState.Initializing;
        public GameplayResult Result { get; private set; }
        public float CountdownRemaining => _countdownRemaining;

        public void Start()
        {
            ThrowIfDisposed();
            if (State != GameplayRunState.Initializing) 
                throw new InvalidOperationException("GameplayRun 只能启动一次。");
            _countdownRemaining = _countdownDuration;
            ChangeState(GameplayRunState.Countdown);
            if (_countdownRemaining <= 0f) BeginWave();
        }

        public void Tick(float deltaTime)
        {
            if (_disposed || _settled) return;
            if (State == GameplayRunState.Countdown)
            {
                _countdownRemaining = Math.Max(0f, _countdownRemaining - Math.Max(0f, deltaTime));
                if (_countdownRemaining <= 0f) BeginWave();
                return;
            }
            if (State != GameplayRunState.WaveActive) return;
            if (AnyConditionMet(_defeatConditions)) RequestDefeat();
            else if (AnyConditionMet(_victoryConditions)) RequestVictory();
        }

        public void AddVictoryCondition(IGameplayCondition condition) => AddCondition(_victoryConditions, condition);
        public void AddDefeatCondition(IGameplayCondition condition) => AddCondition(_defeatConditions, condition);

        public void RequestVictory()
        {
            if (!CanSettle()) return;
            Settle(GameplayResultType.Victory, GameplayRunState.Victory);
        }

        public void RequestDefeat()
        {
            if (!CanSettle()) return;
            Settle(GameplayResultType.Defeat, GameplayRunState.Defeat);
        }

        public void Finish()
        {
            if (!_settled || State == GameplayRunState.Finished) return;
            ChangeState(GameplayRunState.Finished);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _deathSubscription.Dispose();
            _victorySubscription.Dispose();
            _defeatSubscription.Dispose();
        }

        private void BeginWave()
        {
            ChangeState(GameplayRunState.WaveActive);
            _world.SetSimulationEnabled(true);
        }

        private void OnCharacterDied(CharacterDiedFact fact)
        {
            if (fact.Category == EntityCategory.Player) RequestDefeat();
        }

        private void Settle(GameplayResultType resultType, GameplayRunState state)
        {
            _settled = true;
            Result = new GameplayResult(resultType, _time.ElapsedTime);
            _world.Stop();
            ChangeState(state);
        }

        private bool CanSettle() => !_disposed && !_settled &&
            (State == GameplayRunState.Countdown || State == GameplayRunState.WaveActive || State == GameplayRunState.WaveInterval);

        private bool AnyConditionMet(List<IGameplayCondition> conditions)
        {
            for (var i = 0; i < conditions.Count; i++)
                if (conditions[i].IsMet(_world)) return true;
            return false;
        }

        private void AddCondition(List<IGameplayCondition> conditions, IGameplayCondition condition)
        {
            ThrowIfDisposed();
            conditions.Add(condition ?? throw new ArgumentNullException(nameof(condition)));
        }

        private void ChangeState(GameplayRunState next)
        {
            var previous = State;
            if (!CanTransition(previous, next))
                throw new InvalidOperationException($"GameplayRun 状态不能从 {previous} 切换到 {next}。");
            State = next;
            _facts.Publish(new RunStateChangedFact(previous, next));
        }

        private static bool CanTransition(GameplayRunState current, GameplayRunState next)
        {
            switch (current)
            {
                case GameplayRunState.Initializing:
                    return next == GameplayRunState.Countdown;
                case GameplayRunState.Countdown:
                    return next == GameplayRunState.WaveActive || next == GameplayRunState.Victory ||
                           next == GameplayRunState.Defeat;
                case GameplayRunState.WaveActive:
                    return next == GameplayRunState.WaveInterval || next == GameplayRunState.Victory ||
                           next == GameplayRunState.Defeat;
                case GameplayRunState.WaveInterval:
                    return next == GameplayRunState.WaveActive || next == GameplayRunState.Victory ||
                           next == GameplayRunState.Defeat;
                case GameplayRunState.Victory:
                case GameplayRunState.Defeat:
                    return next == GameplayRunState.Finished;
                default:
                    return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayRun));
        }
    }
}
