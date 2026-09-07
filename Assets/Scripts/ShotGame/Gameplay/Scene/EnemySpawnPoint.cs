using System;
using UnityEngine;

namespace ShotGame.Gameplay.Scene
{
    /// <summary>只向运行时提供稳定出生点身份和分组，不包含刷怪逻辑。</summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "enemy_spawn";
        [SerializeField] private string _group = "Side";

        public string SpawnId => _spawnId;
        public string Group => _group;

        public EnemySpawnPointData CreateData()
        {
            if (string.IsNullOrWhiteSpace(_spawnId)) throw new InvalidOperationException($"出生点 {name} 缺少 SpawnId。");
            if (string.IsNullOrWhiteSpace(_group)) throw new InvalidOperationException($"出生点 {name} 缺少 Group。");
            return new EnemySpawnPointData(_spawnId, _group, transform);
        }
    }

    public readonly struct EnemySpawnPointData
    {
        public EnemySpawnPointData(string spawnId, string group, Transform transform)
        {
            SpawnId = spawnId;
            Group = group;
            Transform = transform != null ? transform : throw new ArgumentNullException(nameof(transform));
        }

        public string SpawnId { get; }
        public string Group { get; }
        public Transform Transform { get; }
    }
}
