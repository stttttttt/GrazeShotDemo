using System;
using GameFoundation.Core;
using ShotGame.Gameplay.Character;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Facts;
using ShotGame.Gameplay.Time;
using ShotGame.Gameplay.World;

namespace ShotGame.Gameplay.Run
{
    /// <summary>单局状态根：推进倒计时、波次、间隔和唯一胜负结果。</summary>
    public sealed class GameplayRun : IDisposable
    {
        private readonly GameplayWorld _world;
        private readonly GameplayFactHub _facts;
        private readonly IGameTimeService _time;
        private readonly WavePlan _plan;
        private readonly WaveController _waveController;
        private readonly TimeDilationController _timeDilation;
        private readonly EntityId _playerId;
        private readonly IDisposable _deathSubscription;
        private readonly IDisposable _damageSubscription;
        private readonly IDisposable _victorySubscription;
        private readonly IDisposable _defeatSubscription;
        private float _countdownRemaining;
        private float _intervalRemaining;
        private int _lastCountdownSeconds = -1;
        private EntityId _lastDamageSourceId;
        private float _lastDamageAmount;
        private double _startedAt;
        private bool _settled;
        private bool _disposed;

        public GameplayRun(GameplayWorld world, GameplayFactHub facts, IGameTimeService time,
            WavePlan plan, WaveController waveController, TimeDilationController timeDilation,
            EntityId playerId)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _waveController = waveController ?? throw new ArgumentNullException(nameof(waveController));
            _timeDilation = timeDilation ?? throw new ArgumentNullException(nameof(timeDilation));
            _playerId = playerId.IsValid ? playerId : throw new ArgumentException("玩家 EntityId 无效。", nameof(playerId));
            _deathSubscription = facts.Subscribe<CharacterDiedFact>(OnCharacterDied);
            _damageSubscription = facts.Subscribe<CharacterDamagedFact>(OnCharacterDamaged);
            _victorySubscription = facts.Subscribe<VictoryRequestedFact>(_ => RequestVictory());
            _defeatSubscription = facts.Subscribe<DefeatRequestedFact>(_ => RequestDefeat());
        }

        public GameplayRunState State { get; private set; } = GameplayRunState.Initializing;
        public GameplayResult Result { get; private set; }
        public float CountdownRemaining => _countdownRemaining;
        public float WaveIntervalRemaining => _intervalRemaining;
        public int CurrentWaveIndex { get; private set; } = -1;
        public int TotalWaveCount => _plan.Waves.Count;

        public void Start()
        {
            ThrowIfDisposed();
            if (State != GameplayRunState.Initializing) throw new InvalidOperationException("GameplayRun 只能启动一次。");
            _startedAt = _time.ElapsedTime;
            _countdownRemaining = _plan.InitialCountdown;
            ChangeState(GameplayRunState.Countdown);
            PublishCountdown();
            if (_countdownRemaining <= 0f) BeginNextWave();
        }

        public void Tick(float deltaTime)
        {
            if (_disposed || _settled || deltaTime < 0f) return;
            switch (State)
            {
                case GameplayRunState.Countdown:
                    _countdownRemaining = Math.Max(0f, _countdownRemaining - deltaTime);
                    PublishCountdown();
                    if (_countdownRemaining <= 0f) BeginNextWave();
                    break;
                case GameplayRunState.WaveActive:
                    _waveController.Tick(deltaTime);
                    if (_waveController.IsCompleted) CompleteCurrentWave();
                    break;
                case GameplayRunState.WaveInterval:
                    _intervalRemaining = Math.Max(0f, _intervalRemaining - deltaTime);
                    if (_intervalRemaining <= 0f) BeginNextWave();
                    break;
            }
        }

        public void RequestVictory()
        {
            if (CanSettle()) Settle(GameplayResultType.Victory, GameplayRunState.Victory);
        }

        public void RequestDefeat()
        {
            if (CanSettle()) Settle(GameplayResultType.Defeat, GameplayRunState.Defeat);
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
            _damageSubscription.Dispose();
            _victorySubscription.Dispose();
            _defeatSubscription.Dispose();
            _waveController.Dispose();
        }

        private void BeginNextWave()
        {
            CurrentWaveIndex++;
            if (CurrentWaveIndex >= _plan.Waves.Count)
            {
                RequestVictory();
                return;
            }
            if (!_world.IsSimulationEnabled) _world.SetSimulationEnabled(true);
            ChangeState(GameplayRunState.WaveActive);
            _waveController.StartWave(_plan.Waves[CurrentWaveIndex], _plan.Waves.Count);
        }

        private void CompleteCurrentWave()
        {
            if (CurrentWaveIndex >= _plan.Waves.Count - 1)
            {
                RequestVictory();
                return;
            }
            _intervalRemaining = _plan.Waves[CurrentWaveIndex].IntervalAfter;
            ChangeState(GameplayRunState.WaveInterval);
            _facts.Publish(new WaveIntervalStartedFact(CurrentWaveIndex + 2, _intervalRemaining));
            if (_intervalRemaining <= 0f) BeginNextWave();
        }

        private void PublishCountdown()
        {
            var seconds = (int)Math.Ceiling(_countdownRemaining);
            if (seconds == _lastCountdownSeconds) return;
            _lastCountdownSeconds = seconds;
            _facts.Publish(new RunCountdownChangedFact(seconds));
        }

        private void OnCharacterDamaged(CharacterDamagedFact fact)
        {
            if (fact.TargetId != _playerId || fact.Result.AppliedDamage <= 0f) return;
            _lastDamageSourceId = fact.SourceId;
            _lastDamageAmount = fact.Result.AppliedDamage;
        }

        private void OnCharacterDied(CharacterDiedFact fact)
        {
            if (fact.EntityId == _playerId && fact.Category == EntityCategory.Player) RequestDefeat();
        }

        private void Settle(GameplayResultType resultType, GameplayRunState state)
        {
            _settled = true;
            var weaponName = string.Empty;
            var grazePhase = GrazePhase.Idle;
            if (_world.TryGetEntity(_playerId, out var player))
            {
                weaponName = player.GetComponent<EquipmentComponent>()?.CurrentWeapon.Config.DisplayName ?? string.Empty;
                grazePhase = player.GetComponent<GrazeComponent>()?.Phase ?? GrazePhase.Idle;
            }
            Result = new GameplayResult(resultType, Math.Max(0d, _time.ElapsedTime - _startedAt),
                Math.Max(1, CurrentWaveIndex + 1), _plan.Waves.Count,
                _lastDamageSourceId, _lastDamageAmount, weaponName, grazePhase);
            _waveController.Stop();
            _timeDilation.Clear();
            _world.Stop();
            ChangeState(state);
            _facts.Publish(new GameplaySettledFact(Result));
        }

        private bool CanSettle() => !_disposed && !_settled &&
            (State == GameplayRunState.Countdown || State == GameplayRunState.WaveActive ||
             State == GameplayRunState.WaveInterval);

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
                case GameplayRunState.Initializing: return next == GameplayRunState.Countdown;
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
                case GameplayRunState.Defeat: return next == GameplayRunState.Finished;
                default: return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayRun));
        }
    }
}
