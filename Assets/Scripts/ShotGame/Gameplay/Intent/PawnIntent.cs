using UnityEngine;

namespace ShotGame.Gameplay.Intent
{
    public struct PawnIntent
    {
        /// <summary>玩家输入或 AI 决策产生的期望移动方向，长度通常不超过 1。</summary>
        public Vector2 MoveDirection;

        /// <summary>瞄准方向。</summary>
        public Vector2 AimDirection;

        /// <summary>本帧按下开火。</summary>
        public bool FirePressed;

        /// <summary>持续按住开火。</summary>
        public bool FireHeld;

        /// <summary>本帧松开开火。</summary>
        public bool FireReleased;

        /// <summary>本帧按下擦弹。</summary>
        public bool GrazePressed;

        /// <summary>本帧按下换弹。</summary>
        public bool ReloadPressed;

        /// <summary>本帧请求切换到的武器槽位，0 表示不切换。</summary>
        public int SwitchWeaponSlot;

        /// <summary>本帧请求切换武器的步进方向，0 表示不切换。</summary>
        public int SwitchWeaponStep;

        /// <summary>本帧请求快速切回上一把武器。</summary>
        public bool QuickSwapPressed;
    }
}
