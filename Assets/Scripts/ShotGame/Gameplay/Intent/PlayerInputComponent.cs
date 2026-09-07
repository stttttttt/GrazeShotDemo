using ShotGame.Gameplay.Entity;
using UnityEngine;

namespace ShotGame.Gameplay.Intent
{
    /// <summary>由输入适配层写入，只保存并输出本帧意图。</summary>
    public sealed class PlayerInputComponent : EntityComponent, IPawnIntentSource
    {
        private PawnIntent _intent;

        public PawnIntent GetIntent() => _intent;

        /// <summary>设置玩家移动意图，并限制长度避免斜向移动速度更快。</summary>
        public void SetMoveDirection(Vector2 direction) =>
            _intent.MoveDirection = Vector2.ClampMagnitude(direction, 1f);

        public void SetAimDirection(Vector2 direction) => _intent.AimDirection = direction.normalized;

        public void SetFire(bool isHeld)
        {
            if (isHeld && !_intent.FireHeld) _intent.FirePressed = true;
            if (!isHeld && _intent.FireHeld) _intent.FireReleased = true;
            _intent.FireHeld = isHeld;
        }

        public void SetGraze(bool isHeld)
        {
            if (!isHeld && _intent.GrazeHeld) _intent.GrazeReleased = true;
            _intent.GrazeHeld = isHeld;
        }
        public void PressReload() => _intent.ReloadPressed = true;
        public void PressDash() => _intent.DashPressed = true;
        public void SelectWeaponSlot(int slot) => _intent.SwitchWeaponSlot = slot;
        public void StepWeapon(int step) => _intent.SwitchWeaponStep = step;
        public void PressQuickSwap() => _intent.QuickSwapPressed = true;

        public void ClearFrameIntent()
        {
            _intent.FirePressed = false;
            _intent.FireReleased = false;
            _intent.GrazeReleased = false;
            _intent.ReloadPressed = false;
            _intent.DashPressed = false;
            _intent.SwitchWeaponSlot = 0;
            _intent.SwitchWeaponStep = 0;
            _intent.QuickSwapPressed = false;
        }
    }
}
