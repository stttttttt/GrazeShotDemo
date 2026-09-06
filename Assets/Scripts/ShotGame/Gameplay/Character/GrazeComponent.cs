using ShotGame.Gameplay.Entity;

namespace ShotGame.Gameplay.Character
{
    /// <summary>擦弹能力占位，完整窗口、充能与反馈规则在擦弹阶段实现。</summary>
    public sealed class GrazeComponent : EntityComponent
    {
        public bool WasRequestedThisFrame { get; private set; }
        public void ApplyIntent(bool pressed) => WasRequestedThisFrame = pressed;
    }
}
