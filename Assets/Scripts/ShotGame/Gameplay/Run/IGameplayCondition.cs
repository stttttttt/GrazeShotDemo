using ShotGame.Gameplay.World;

namespace ShotGame.Gameplay.Run
{
    public interface IGameplayCondition
    {
        bool IsMet(GameplayWorld world);
    }
}
