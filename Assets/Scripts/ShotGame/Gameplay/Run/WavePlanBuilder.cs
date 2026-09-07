using System;
using System.Collections.Generic;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Scene;

namespace ShotGame.Gameplay.Run
{
    public static class WavePlanBuilder
    {
        public static WavePlan Build(RunDefinition definition,
            IReadOnlyList<EnemySpawnPointData> spawnPoints)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (spawnPoints == null) throw new ArgumentNullException(nameof(spawnPoints));
            definition.Validate();
            var groups = BuildSpawnGroups(spawnPoints);
            var random = new Random(definition.FixedSeed);
            var waves = new WavePlanEntry[definition.Waves.Count];
            for (var i = 0; i < waves.Length; i++)
                waves[i] = BuildWave(i, definition.Waves[i], definition.DefaultWaveInterval, groups, random);
            return new WavePlan(definition.RunId, definition.FixedSeed, definition.InitialCountdown, waves,
                definition.EndlessAfterLastWave, definition.EndlessEnemyIncreasePerWave);
        }

        private static WavePlanEntry BuildWave(int waveIndex, WaveDefinition definition,
            float defaultInterval, Dictionary<string, List<EnemySpawnPointData>> groups, Random random)
        {
            var pending = new List<MutableSpawn>();
            var sequence = 0;
            for (var i = 0; i < definition.ExplicitGroups.Length; i++)
            {
                var group = definition.ExplicitGroups[i];
                var points = RequireGroup(groups, group.SpawnGroup, definition.WaveId);
                for (var count = 0; count < group.Count; count++)
                {
                    pending.Add(new MutableSpawn(sequence++, group.StartDelay + group.SpawnInterval * count,
                        group.Enemy, PickSpawnId(points, random), group.RequiredForClear, group.IsKeyTarget));
                }
            }

            AddRandomSpawns(definition, groups, random, pending, ref sequence);
            pending.Sort((left, right) =>
            {
                var timeComparison = left.SpawnTime.CompareTo(right.SpawnTime);
                return timeComparison != 0 ? timeComparison : left.Sequence.CompareTo(right.Sequence);
            });
            var spawns = new EnemySpawnPlan[pending.Count];
            for (var i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                spawns[i] = new EnemySpawnPlan(i, item.SpawnTime, item.Enemy,
                    item.SpawnId, item.RequiredForClear, item.IsKeyTarget);
            }
            var interval = definition.IntervalOverride >= 0f ? definition.IntervalOverride : defaultInterval;
            return new WavePlanEntry(waveIndex, definition.WaveId, definition.DisplayName,
                definition.ObjectiveType, interval, definition.MaxAliveEnemies, spawns);
        }

        private static void AddRandomSpawns(WaveDefinition definition,
            Dictionary<string, List<EnemySpawnPointData>> groups, Random random,
            List<MutableSpawn> output, ref int sequence)
        {
            if (definition.RandomBudget <= 0) return;
            var counts = new int[definition.RandomPool.Length];
            var budget = definition.RandomBudget;
            var randomIndex = 0;
            for (var i = 0; i < definition.RandomPool.Length; i++)
            {
                var entry = definition.RandomPool[i];
                RequireGroup(groups, entry.SpawnGroup, definition.WaveId);
                for (var count = 0; count < entry.MinCount; count++)
                {
                    if (budget < entry.Enemy.BudgetCost)
                        throw new InvalidOperationException($"波次 {definition.WaveId} 的随机预算不足以满足 MinCount。");
                    AddRandomEntry(entry, definition, groups, random, output, ref sequence, randomIndex++);
                    counts[i]++;
                    budget -= entry.Enemy.BudgetCost;
                }
            }

            while (budget > 0)
            {
                var totalWeight = 0;
                for (var i = 0; i < definition.RandomPool.Length; i++)
                {
                    var entry = definition.RandomPool[i];
                    if (counts[i] < entry.MaxCount && entry.Enemy.BudgetCost <= budget)
                        totalWeight += entry.Weight;
                }
                if (totalWeight <= 0) break;
                var roll = random.Next(totalWeight);
                for (var i = 0; i < definition.RandomPool.Length; i++)
                {
                    var entry = definition.RandomPool[i];
                    if (counts[i] >= entry.MaxCount || entry.Enemy.BudgetCost > budget) continue;
                    if (roll >= entry.Weight)
                    {
                        roll -= entry.Weight;
                        continue;
                    }
                    AddRandomEntry(entry, definition, groups, random, output, ref sequence, randomIndex++);
                    counts[i]++;
                    budget -= entry.Enemy.BudgetCost;
                    break;
                }
            }
        }

        private static void AddRandomEntry(WaveRandomEntry entry, WaveDefinition definition,
            Dictionary<string, List<EnemySpawnPointData>> groups, Random random,
            List<MutableSpawn> output, ref int sequence, int randomIndex)
        {
            var points = RequireGroup(groups, entry.SpawnGroup, definition.WaveId);
            var spawnTime = definition.RandomStartDelay + definition.RandomSpawnInterval * randomIndex;
            output.Add(new MutableSpawn(sequence++, spawnTime, entry.Enemy,
                PickSpawnId(points, random), true, false));
        }

        private static Dictionary<string, List<EnemySpawnPointData>> BuildSpawnGroups(
            IReadOnlyList<EnemySpawnPointData> spawnPoints)
        {
            if (spawnPoints.Count == 0) throw new InvalidOperationException("GamePlay 场景没有敌人出生点。");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var groups = new Dictionary<string, List<EnemySpawnPointData>>(StringComparer.Ordinal);
            for (var i = 0; i < spawnPoints.Count; i++)
            {
                var point = spawnPoints[i];
                if (string.IsNullOrWhiteSpace(point.SpawnId) || string.IsNullOrWhiteSpace(point.Group))
                    throw new InvalidOperationException("敌人出生点缺少 SpawnId 或 Group。");
                if (!ids.Add(point.SpawnId)) throw new InvalidOperationException($"重复的敌人 SpawnId：{point.SpawnId}。");
                if (!groups.TryGetValue(point.Group, out var list))
                {
                    list = new List<EnemySpawnPointData>();
                    groups.Add(point.Group, list);
                }
                list.Add(point);
            }
            return groups;
        }

        private static List<EnemySpawnPointData> RequireGroup(
            Dictionary<string, List<EnemySpawnPointData>> groups, string group, string waveId)
        {
            if (!groups.TryGetValue(group, out var points) || points.Count == 0)
                throw new InvalidOperationException($"波次 {waveId} 引用了场景中不存在的出生点组：{group}。");
            return points;
        }

        private static string PickSpawnId(List<EnemySpawnPointData> points, Random random) =>
            points[random.Next(points.Count)].SpawnId;

        private sealed class MutableSpawn
        {
            public MutableSpawn(int sequence, float spawnTime, EnemyConfig enemy, string spawnId,
                bool requiredForClear, bool isKeyTarget)
            {
                Sequence = sequence;
                SpawnTime = spawnTime;
                Enemy = enemy;
                SpawnId = spawnId;
                RequiredForClear = requiredForClear;
                IsKeyTarget = isKeyTarget;
            }

            public int Sequence { get; }
            public float SpawnTime { get; }
            public EnemyConfig Enemy { get; }
            public string SpawnId { get; }
            public bool RequiredForClear { get; }
            public bool IsKeyTarget { get; }
        }
    }
}
