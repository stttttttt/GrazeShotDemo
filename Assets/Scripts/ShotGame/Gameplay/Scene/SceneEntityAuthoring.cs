using ShotGame.Gameplay.Entity;
using UnityEngine;

namespace ShotGame.Gameplay.Scene
{
    /// <summary>描述场景内预摆放 Entity；只提供配置，不自行驱动 Gameplay。</summary>
    [DisallowMultipleComponent]
    public sealed class SceneEntityAuthoring : MonoBehaviour
    {
        [SerializeField] private EntityCategory _category = EntityCategory.Wall;
        [SerializeField] private EntityTeam _team = EntityTeam.Neutral;

        public EntityCategory Category => _category;
        public EntityTeam Team => _team;
    }
}
