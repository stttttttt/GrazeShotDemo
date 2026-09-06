using System;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Intent;

namespace ShotGame.Gameplay.Character
{
    /// <summary>武器系统的简单接入点；具体射击规则在下一阶段补充。</summary>
    public sealed class WeaponUseComponent : EntityComponent
    {
        public bool IsTriggerHeld { get; private set; }
        public event Action FireRequested;
        public event Action ReloadRequested;

        public void ApplyIntent(in PawnIntent intent)
        {
            IsTriggerHeld = intent.FireHeld;
            if (intent.FirePressed) FireRequested?.Invoke();
            if (intent.ReloadPressed) ReloadRequested?.Invoke();
        }

        public override void Dispose()
        {
            FireRequested = null;
            ReloadRequested = null;
        }
    }
}
