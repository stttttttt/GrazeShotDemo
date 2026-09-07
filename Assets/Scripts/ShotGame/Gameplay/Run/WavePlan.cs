using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Config;

namespace ShotGame.Gameplay.Run
{
    public sealed class EnemySpawnPlan
    {
        public EnemySpawnPlan(int order, float spawnTime, EnemyConfig enemy, string spawnId,
            bool requiredForClear, bool isKeyTarget)
        {
            Order = order;
            SpawnTime = spawnTime;
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            SpawnId = !string.IsNullOrWhiteSpace(spawnId) ? spawnId : throw new ArgumentException("SpawnId 为空。", nameof(spawnId));
            RequiredForClear = requiredForClear;
            IsKeyTarget = isKeyTarget;
        }

        public int Order { get; }
        public float SpawnTime { get; }
        public EnemyConfig Enemy { get; }
        public string SpawnId { get; }
        public bool RequiredForClear { get; }
        public bool IsKeyTarget { get; }
    }

    public sealed class WavePlanEntry
    {
        public WavePlanEntry(int waveIndex, string waveId, string displayName,
            WaveObjectiveType objectiveType, float intervalAfter, int maxAliveEnemies,
            EnemySpawnPlan[] spawns)
        {
            WaveIndex = waveIndex;
            WaveId = waveId;
            DisplayName = displayName;
            ObjectiveType = objectiveType;
            IntervalAfter = intervalAfter;
            MaxAliveEnemies = maxAliveEnemies;
            Spawns = spawns ?? throw new ArgumentNullException(nameof(spawns));
        }

        public int WaveIndex { get; }
        public string WaveId { get; }
        public string DisplayName { get; }
        public WaveObjectiveType ObjectiveType { get; }
        public float IntervalAfter { get; }
        public int MaxAliveEnemies { get; }
        public IReadOnlyList<EnemySpawnPlan> Spawns { get; }
    }

    public sealed class WavePlan
    {
        public WavePlan(string runId, int seed, float initialCountdown, WavePlanEntry[] waves)
        {
            RunId = runId;
            Seed = seed;
            InitialCountdown = initialCountdown;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
        }

        public string RunId { get; }
        public int Seed { get; }
        public float InitialCountdown { get; }
        public IReadOnlyList<WavePlanEntry> Waves { get; }
    }
}
