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
        public WavePlan(string runId, int seed, float initialCountdown, WavePlanEntry[] waves,
            bool isEndless, int endlessEnemyIncreasePerWave)
        {
            RunId = runId;
            Seed = seed;
            InitialCountdown = initialCountdown;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            IsEndless = isEndless;
            EndlessEnemyIncreasePerWave = Math.Max(1, endlessEnemyIncreasePerWave);
        }

        public string RunId { get; }
        public int Seed { get; }
        public float InitialCountdown { get; }
        public IReadOnlyList<WavePlanEntry> Waves { get; }
        public bool IsEndless { get; }
        public int EndlessEnemyIncreasePerWave { get; }

        public WavePlanEntry GetWave(int waveIndex)
        {
            if (waveIndex < 0) throw new ArgumentOutOfRangeException(nameof(waveIndex));
            if (waveIndex < Waves.Count) return Waves[waveIndex];
            if (!IsEndless) return null;

            var template = Waves[Waves.Count - 1];
            var endlessNumber = waveIndex - Waves.Count + 1;
            var extraCount = endlessNumber * EndlessEnemyIncreasePerWave;
            var spawns = new EnemySpawnPlan[template.Spawns.Count + extraCount];
            var lastSpawnTime = 0f;
            for (var i = 0; i < template.Spawns.Count; i++)
            {
                var source = template.Spawns[i];
                spawns[i] = new EnemySpawnPlan(i, source.SpawnTime, source.Enemy,
                    source.SpawnId, true, false);
                lastSpawnTime = Math.Max(lastSpawnTime, source.SpawnTime);
            }
            for (var i = 0; i < extraCount; i++)
            {
                var source = template.Spawns[i % template.Spawns.Count];
                var index = template.Spawns.Count + i;
                spawns[index] = new EnemySpawnPlan(index, lastSpawnTime + (i + 1) * 0.65f,
                    source.Enemy, source.SpawnId, true, false);
            }
            var maxAlive = template.MaxAliveEnemies + Math.Min(4, (endlessNumber + 1) / 2);
            return new WavePlanEntry(waveIndex, $"endless_{waveIndex + 1}",
                $"无限模式 · 第 {waveIndex + 1} 波", WaveObjectiveType.EliminateAll,
                template.IntervalAfter, maxAlive, spawns);
        }
    }
}
