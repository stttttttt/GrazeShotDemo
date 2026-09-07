using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [CreateAssetMenu(fileName = "RunDefinition", menuName = "Shot Game/Run Definition")]
    public sealed class RunDefinition : ScriptableObject
    {
        [SerializeField] private string _runId = "demo_run";
        [Min(0f)] [SerializeField] private float _initialCountdown = 2f;
        [Min(0f)] [SerializeField] private float _defaultWaveInterval = 3f;
        [SerializeField] private int _fixedSeed = 20260903;
        [SerializeField] private WaveDefinition[] _waves = Array.Empty<WaveDefinition>();

        public string RunId => _runId;
        public float InitialCountdown => _initialCountdown;
        public float DefaultWaveInterval => _defaultWaveInterval;
        public int FixedSeed => _fixedSeed;
        public IReadOnlyList<WaveDefinition> Waves => _waves;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_runId)) throw new InvalidOperationException("RunDefinition 缺少 RunId。");
            if (_initialCountdown < 0f || _defaultWaveInterval < 0f)
                throw new InvalidOperationException($"Run {_runId} 的倒计时或波次间隔无效。");
            if (_waves == null || _waves.Length < 1 || _waves.Length > 8)
                throw new InvalidOperationException($"Run {_runId} 需要配置 1 至 8 个波次。");
            var definitions = new HashSet<WaveDefinition>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var enemyIds = new Dictionary<string, EnemyConfig>(StringComparer.Ordinal);
            for (var i = 0; i < _waves.Length; i++)
            {
                var wave = _waves[i] != null ? _waves[i] : throw new InvalidOperationException($"Run {_runId} 的波次 {i + 1} 为空。");
                if (!definitions.Add(wave)) throw new InvalidOperationException($"Run {_runId} 重复引用波次 {wave.name}。");
                wave.Validate();
                if (!ids.Add(wave.WaveId)) throw new InvalidOperationException($"Run {_runId} 存在重复 WaveId：{wave.WaveId}。");
                CollectEnemyIds(wave.ExplicitGroups, enemyIds);
                CollectEnemyIds(wave.RandomPool, enemyIds);
            }
        }

        private static void CollectEnemyIds(WaveSpawnGroup[] groups, Dictionary<string, EnemyConfig> found)
        {
            for (var i = 0; i < groups.Length; i++) AddEnemy(groups[i].Enemy, found);
        }

        private static void CollectEnemyIds(WaveRandomEntry[] entries, Dictionary<string, EnemyConfig> found)
        {
            for (var i = 0; i < entries.Length; i++) AddEnemy(entries[i].Enemy, found);
        }

        private static void AddEnemy(EnemyConfig enemy, Dictionary<string, EnemyConfig> found)
        {
            if (found.TryGetValue(enemy.EnemyId, out var existing) && existing != enemy)
                throw new InvalidOperationException($"不同 EnemyConfig 使用了重复 EnemyId：{enemy.EnemyId}。");
            found[enemy.EnemyId] = enemy;
        }
    }
}
