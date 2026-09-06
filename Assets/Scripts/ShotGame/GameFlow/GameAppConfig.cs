using GameFoundation.Core;
using UnityEngine;

namespace ShotGame.GameFlow
{
    [CreateAssetMenu(fileName = "GameAppConfig", menuName = "Shot Game/Game App Config")]
    public sealed class GameAppConfig : ScriptableObject
    {
        [Header("场景")]
        [SerializeField] private SceneId _gameplayScene = new SceneId("Gameplay");

        public SceneId GameplayScene => _gameplayScene;

        public void Validate()
        {
            if (!_gameplayScene.IsValid) ThrowMissing(nameof(_gameplayScene));
        }

        private static void ThrowMissing(string fieldName)
        {
            throw new System.InvalidOperationException($"GameAppConfig 缺少配置：{fieldName}");
        }
    }
}
