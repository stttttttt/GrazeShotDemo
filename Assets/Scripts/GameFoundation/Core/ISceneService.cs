using System.Threading.Tasks;

namespace GameFoundation.Core
{
    /// <summary>应用级场景切换边界；不拥有任何局内 Gameplay 状态。</summary>
    public interface ISceneService
    {
        bool IsLoaded(SceneId sceneId);
        Task LoadAdditiveAsync(SceneId sceneId, bool setActive = true);
        Task UnloadAsync(SceneId sceneId);
    }
}
