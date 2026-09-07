using System;
using UnityEngine;

namespace ShotGame.Gameplay.Config
{
    [Serializable]
    public sealed class WaveSpawnGroup
    {
        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private string _spawnGroup = "Side";
        [Min(1)] [SerializeField] private int _count = 1;
        [Min(0f)] [SerializeField] private float _startDelay;
        [Min(0.01f)] [SerializeField] private float _spawnInterval = 0.8f;
        [SerializeField] private bool _requiredForClear = true;
        [SerializeField] private bool _isKeyTarget;

        public EnemyConfig Enemy => _enemy;
        public string SpawnGroup => _spawnGroup;
        public int Count => _count;
        public float StartDelay => _startDelay;
        public float SpawnInterval => _spawnInterval;
        public bool RequiredForClear => _requiredForClear;
        public bool IsKeyTarget => _isKeyTarget;
    }

    [Serializable]
    public sealed class WaveRandomEntry
    {
        [SerializeField] private EnemyConfig _enemy;
        [SerializeField] private string _spawnGroup = "Side";
        [Min(1)] [SerializeField] private int _weight = 1;
        [Min(0)] [SerializeField] private int _minCount;
        [Min(1)] [SerializeField] private int _maxCount = 3;

        public EnemyConfig Enemy => _enemy;
        public string SpawnGroup => _spawnGroup;
        public int Weight => _weight;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;
    }

    [CreateAssetMenu(fileName = "WaveDefinition", menuName = "Shot Game/Wave Definition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] private string _waveId = "wave_01";
        [SerializeField] private string _displayName = "第 1 波";
        [SerializeField] private WaveObjectiveType _objectiveType;
        [Tooltip("小于 0 时使用 RunDefinition 默认值。")]
        [SerializeField] private float _intervalOverride = -1f;
        [Min(1)] [SerializeField] private int _maxAliveEnemies = 4;
        [SerializeField] private WaveSpawnGroup[] _explicitGroups = Array.Empty<WaveSpawnGroup>();
        [SerializeField] private WaveRandomEntry[] _randomPool = Array.Empty<WaveRandomEntry>();
        [Min(0)] [SerializeField] private int _randomBudget;
        [Min(0f)] [SerializeField] private float _randomStartDelay = 1f;
        [Min(0.01f)] [SerializeField] private float _randomSpawnInterval = 0.8f;

        public string WaveId => _waveId;
        public string DisplayName => _displayName;
        public WaveObjectiveType ObjectiveType => _objectiveType;
        public float IntervalOverride => _intervalOverride;
        public int MaxAliveEnemies => _maxAliveEnemies;
        public WaveSpawnGroup[] ExplicitGroups => _explicitGroups;
        public WaveRandomEntry[] RandomPool => _randomPool;
        public int RandomBudget => _randomBudget;
        public float RandomStartDelay => _randomStartDelay;
        public float RandomSpawnInterval => _randomSpawnInterval;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_waveId)) throw new InvalidOperationException("WaveDefinition 缺少 WaveId。");
            if (string.IsNullOrWhiteSpace(_displayName)) throw new InvalidOperationException($"波次 {_waveId} 缺少显示名称。");
            if (_maxAliveEnemies <= 0 || _randomBudget < 0 || _randomStartDelay < 0f || _randomSpawnInterval <= 0f)
                throw new InvalidOperationException($"波次 {_waveId} 的时间、预算或并发上限无效。");
            var keyTargetCount = 0;
            for (var i = 0; i < _explicitGroups.Length; i++)
            {
                var group = _explicitGroups[i] ?? throw new InvalidOperationException($"波次 {_waveId} 的显式生成组 {i} 为空。");
                ValidateEnemyAndGroup(group.Enemy, group.SpawnGroup);
                if (group.Count <= 0 || group.StartDelay < 0f || group.SpawnInterval <= 0f)
                    throw new InvalidOperationException($"波次 {_waveId} 的显式生成组 {i} 数值无效。");
                if (group.IsKeyTarget)
                {
                    keyTargetCount += group.Count;
                    if (group.Count != 1) throw new InvalidOperationException($"波次 {_waveId} 的关键目标组只能生成一个敌人。");
                }
            }
            for (var i = 0; i < _randomPool.Length; i++)
            {
                var entry = _randomPool[i] ?? throw new InvalidOperationException($"波次 {_waveId} 的随机池项 {i} 为空。");
                ValidateEnemyAndGroup(entry.Enemy, entry.SpawnGroup);
                if (entry.Weight <= 0 || entry.MinCount < 0 || entry.MaxCount < entry.MinCount)
                    throw new InvalidOperationException($"波次 {_waveId} 的随机池项 {i} 数值无效。");
            }
            if (_objectiveType == WaveObjectiveType.KeyTarget && keyTargetCount != 1)
                throw new InvalidOperationException($"关键目标波 {_waveId} 必须且只能配置一个关键目标。");
            if (_objectiveType == WaveObjectiveType.EliminateAll && keyTargetCount != 0)
                throw new InvalidOperationException($"歼灭波 {_waveId} 不能配置关键目标。");
            if (_explicitGroups.Length == 0 && _randomBudget == 0)
                throw new InvalidOperationException($"波次 {_waveId} 没有任何敌人。");
            if (_randomBudget > 0 && _randomPool.Length == 0)
                throw new InvalidOperationException($"波次 {_waveId} 配置了随机预算，但随机池为空。");
        }

        private static void ValidateEnemyAndGroup(EnemyConfig enemy, string group)
        {
            if (enemy == null) throw new InvalidOperationException("波次生成项缺少 EnemyConfig。");
            if (string.IsNullOrWhiteSpace(group)) throw new InvalidOperationException("波次生成项缺少出生点 Group。");
            enemy.Validate();
        }
    }
}
